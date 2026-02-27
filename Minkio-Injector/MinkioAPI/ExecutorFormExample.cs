using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MinkioAPI;

namespace MinkioExecutor
{
    /// <summary>
    /// ExecutorForm - Enhanced version with injection and script execution
    /// </summary>
    public partial class ExecutorForm : Form
    {
        private bool _isInjected = false;
        private bool _isAttaching = false;

        public ExecutorForm()
        {
            InitializeComponent();
            UpdateStatus("Ready");
        }

        /// <summary>
        /// Update the status label
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(message)));
                return;
            }

            // Assuming you have a statusLabel control
            // statusLabel.Text = "Status: " + message;
            Console.WriteLine("[Executor] " + message);
        }

        /// <summary>
        /// Button click: Attach (inject DLL)
        /// </summary>
        private void AttachButton_Click(object sender, EventArgs e)
        {
            if (_isAttaching)
            {
                UpdateStatus("Already attaching...");
                return;
            }

            if (_isInjected)
            {
                UpdateStatus("Already injected. Restart Roblox to re-inject.");
                return;
            }

            // Run injection on background thread
            Task.Run(() => PerformInjection());
        }

        /// <summary>
        /// Background task: Perform injection
        /// </summary>
        private void PerformInjection()
        {
            try
            {
                _isAttaching = true;
                UpdateStatus("Checking Roblox process...");

                // Check if Roblox is running
                if (!MinkioInjector.IsRobloxRunning())
                {
                    UpdateStatus("ERROR: RobloxPlayerBeta.exe is not running");
                    _isAttaching = false;
                    return;
                }

                UpdateStatus("Found Roblox process (PID: " + MinkioInjector.GetRobloxPID() + ")");

                // Check if already injected
                if (MinkioInjector.IsDLLLoaded("Minkio.dll"))
                {
                    UpdateStatus("Minkio.dll already loaded");
                    _isInjected = true;
                    _isAttaching = false;
                    return;
                }

                // Get DLL path
                string dllPath = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath),
                    "Minkio.dll");

                if (!File.Exists(dllPath))
                {
                    UpdateStatus("ERROR: Minkio.dll not found at " + dllPath);
                    _isAttaching = false;
                    return;
                }

                UpdateStatus("Starting injection...");

                // Perform injection
                bool injectionSuccess = MinkioInjector.InjectDLL(dllPath);

                if (injectionSuccess)
                {
                    // Wait a bit for DLL initialization
                    UpdateStatus("Injection successful! Waiting for initialization...");
                    Thread.Sleep(1000);

                    // Verify DLL is loaded
                    if (MinkioInjector.IsDLLLoaded("Minkio.dll"))
                    {
                        UpdateStatus("SUCCESS: Minkio.dll is now loaded");
                        _isInjected = true;

                        // Test the pipe connection
                        UpdateStatus("Testing script pipe...");
                        if (ScriptPipeClient.IsPipeAvailable())
                        {
                            UpdateStatus("Script pipe is available! Ready to execute scripts.");
                        }
                        else
                        {
                            UpdateStatus("WARNING: Script pipe not available yet (may still be initializing)");
                        }
                    }
                    else
                    {
                        UpdateStatus("WARNING: Injection command sent but DLL verification failed");
                        _isInjected = true;  // Assume it worked
                    }
                }
                else
                {
                    UpdateStatus("ERROR: Injection failed");
                }

                _isAttaching = false;
            }
            catch (Exception ex)
            {
                UpdateStatus("ERROR: " + ex.Message);
                _isAttaching = false;
            }
        }

        /// <summary>
        /// Button click: Execute Script
        /// </summary>
        private void ExecuteButton_Click(object sender, EventArgs e)
        {
            if (!_isInjected)
            {
                UpdateStatus("ERROR: DLL not injected. Click 'Attach' first.");
                MessageBox.Show("Please click 'Attach' to inject the DLL first.", "Not Attached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Assuming you have a scriptTextBox control
            // string script = scriptTextBox.Text;
            string script = "print('Hello from Minkio!')";  // Placeholder

            if (string.IsNullOrWhiteSpace(script))
            {
                UpdateStatus("ERROR: No script entered");
                return;
            }

            ExecuteScriptAsync(script);
        }

        /// <summary>
        /// Execute a script asynchronously
        /// </summary>
        private void ExecuteScriptAsync(string script)
        {
            Task.Run(() =>
            {
                try
                {
                    UpdateStatus("Sending script to Roblox...");

                    bool success = ScriptPipeClient.SendScript(script);

                    if (success)
                    {
                        UpdateStatus("Script executed successfully");
                    }
                    else
                    {
                        UpdateStatus("ERROR: Script execution failed");
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("ERROR: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// Form Load event
        /// </summary>
        private void ExecutorForm_Load(object sender, EventArgs e)
        {
            // Register for injection status updates
            MinkioInjector.OnStatusChange += (s, message) =>
            {
                UpdateStatus(message);
            };

            MinkioInjector.OnError += (s, ex) =>
            {
                UpdateStatus("Exception: " + ex.Message);
            };

            // Check initial state
            if (MinkioInjector.IsRobloxRunning())
            {
                UpdateStatus("Roblox is running");

                if (MinkioInjector.IsDLLLoaded("Minkio.dll"))
                {
                    UpdateStatus("Minkio.dll already loaded");
                    _isInjected = true;
                }
            }
            else
            {
                UpdateStatus("Roblox is not running");
            }
        }

        /// <summary>
        /// Example: Basic form setup code
        /// Add these controls in the designer or programmatically:
        /// 
        /// - Button: AttachButton (text: "Attach")
        /// - Button: ExecuteButton (text: "Execute")
        /// - TextBox: scriptTextBox (multiline, large)
        /// - Label: statusLabel (for status messages)
        /// </summary>
        private void InitializeComponent()
        {
            // This would be auto-generated by Windows Forms designer
            // For now, we'll create a minimal example

            this.Text = "Minkio Executor";
            this.Size = new System.Drawing.Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create Attach button
            Button attachButton = new Button();
            attachButton.Text = "Attach (Inject)";
            attachButton.Location = new System.Drawing.Point(10, 10);
            attachButton.Size = new System.Drawing.Size(120, 30);
            attachButton.Click += AttachButton_Click;
            this.Controls.Add(attachButton);

            // Create status label
            Label statusLabel = new Label();
            statusLabel.Text = "Status: Ready";
            statusLabel.Location = new System.Drawing.Point(150, 15);
            statusLabel.Size = new System.Drawing.Size(400, 20);
            statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(statusLabel);

            // Create script textbox
            TextBox scriptTextBox = new TextBox();
            scriptTextBox.Multiline = true;
            scriptTextBox.Location = new System.Drawing.Point(10, 50);
            scriptTextBox.Size = new System.Drawing.Size(570, 350);
            scriptTextBox.Text = "-- Enter Lua script here\nprint('Hello from Minkio!')";
            scriptTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(scriptTextBox);

            // Create Execute button
            Button executeButton = new Button();
            executeButton.Text = "Execute";
            executeButton.Location = new System.Drawing.Point(10, 410);
            executeButton.Size = new System.Drawing.Size(100, 30);
            executeButton.Click += ExecuteButton_Click;
            this.Controls.Add(executeButton);

            // Create Clear button
            Button clearButton = new Button();
            clearButton.Text = "Clear";
            clearButton.Location = new System.Drawing.Point(120, 410);
            clearButton.Size = new System.Drawing.Size(100, 30);
            clearButton.Click += (s, e) => scriptTextBox.Clear();
            this.Controls.Add(clearButton);
        }
    }
}
