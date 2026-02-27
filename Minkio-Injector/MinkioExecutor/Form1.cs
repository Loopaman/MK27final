using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MinkioAPI;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;
        private bool _isInjected = false;
        private int _robloxPID = 0;

        public Form1()
        {
            InitializeComponent();
            InitializeNotifyIcon();
            SetupControls();
        }

        /// <summary>
        /// Initialize the NotifyIcon for system tray notifications
        /// </summary>
        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Text = "Minkio Injector",
                Visible = true,
                Icon = SystemIcons.Application
            };
        }

        /// <summary>
        /// Setup form controls - initialize button text and states
        /// </summary>
        private void SetupControls()
        {
            // Set button labels
            button2.Text = "Attach";
            button1.Text = "Execute";
            
            // Disable Execute until injection succeeds
            button1.Enabled = false;
            button1.BackColor = System.Drawing.Color.Gray;

            // Set form title
            this.Text = "Minkio Executor - Disconnected";

            // Setup richTextBox for script input
            richTextBox1.Text = "-- Enter Lua script here\n-- Example: print('Hello from Minkio!')";
        }

        /// <summary>
        /// Attach button - Inject Minkio.dll into RobloxPlayerBeta.exe
        /// </summary>
        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            this.Text = "Minkio Executor - Attempting injection...";

            try
            {
                // Get Roblox process ID
                _robloxPID = await Task.Run(() => MinkioInjector.GetRobloxPID());

                if (_robloxPID == 0)
                {
                    ShowNotification("Minkio Injector", "Roblox not found. Please start a game first.", ToolTipIcon.Error);
                    this.Text = "Minkio Executor - Disconnected";
                    button2.Enabled = true;
                    return;
                }

                // Inject DLL
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Minkio.dll");
                if (!File.Exists(dllPath))
                {
                    ShowNotification("Minkio Injector", $"DLL not found: {dllPath}", ToolTipIcon.Error);
                    this.Text = "Minkio Executor - Disconnected";
                    button2.Enabled = true;
                    return;
                }

                bool injected = await Task.Run(() => MinkioInjector.InjectDLL(_robloxPID, dllPath));

                if (injected)
                {
                    _isInjected = true;
                    button1.Enabled = true;
                    button1.BackColor = System.Drawing.Color.LimeGreen;
                    this.Text = "Minkio Executor - Connected (PID: " + _robloxPID + ")";
                    ShowNotification("Minkio Injector", "Successfully injected! Ready to execute scripts.", ToolTipIcon.Info);
                }
                else
                {
                    ShowNotification("Minkio Injector", "Injection failed. Try running as Administrator.", ToolTipIcon.Error);
                    this.Text = "Minkio Executor - Disconnected";
                }
            }
            catch (Exception ex)
            {
                ShowNotification("Minkio Injector", "Error: " + ex.Message, ToolTipIcon.Error);
                this.Text = "Minkio Executor - Disconnected";
            }
            finally
            {
                button2.Enabled = true;
            }
        }

        /// <summary>
        /// Execute button - Send script to injected DLL via named pipe
        /// </summary>
        private async void button1_Click(object sender, EventArgs e)
        {
            if (!_isInjected || richTextBox1.Text.Length == 0)
            {
                ShowNotification("Minkio Injector", "Please inject first and enter a script.", ToolTipIcon.Warning);
                return;
            }

            button1.Enabled = false;
            this.Text = "Minkio Executor - Executing...";

            try
            {
                string script = richTextBox1.Text;
                bool success = await Task.Run(() => SendScriptViaPipe(script));

                if (success)
                {
                    ShowNotification("Minkio Injector", "Script executed successfully!", ToolTipIcon.Info);
                    this.Text = "Minkio Executor - Connected (PID: " + _robloxPID + ")";
                }
                else
                {
                    ShowNotification("Minkio Injector", "Script execution failed. DLL may have crashed.", ToolTipIcon.Error);
                    _isInjected = false;
                    button1.Enabled = false;
                    button1.BackColor = System.Drawing.Color.Gray;
                    this.Text = "Minkio Executor - Disconnected";
                }
            }
            catch (Exception ex)
            {
                ShowNotification("Minkio Injector", "Error: " + ex.Message, ToolTipIcon.Error);
                this.Text = "Minkio Executor - Connected (PID: " + _robloxPID + ")";
                button1.Enabled = true;
            }
        }

        /// <summary>
        /// Send script to injected DLL via named pipe (IPC)
        /// Pipe name: \\.\pipe\Minkio_Script_Server
        /// </summary>
        private bool SendScriptViaPipe(string script)
        {
            const string PIPE_NAME = "Minkio_Script_Server";
            const int TIMEOUT = 5000;  // 5 seconds

            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.InOut))
                {
                    // Try to connect with timeout
                    pipeClient.Connect(TIMEOUT);

                    if (!pipeClient.IsConnected)
                        return false;

                    // Write script to pipe
                    byte[] scriptBytes = Encoding.UTF8.GetBytes(script);
                    pipeClient.Write(scriptBytes, 0, scriptBytes.Length);
                    pipeClient.Flush();

                    // Read ACK response from DLL
                    byte[] buffer = new byte[1024];
                    int bytesRead = pipeClient.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        return response.Contains("OK");
                    }

                    return false;
                }
            }
            catch (TimeoutException)
            {
                // Pipe server not responding - DLL likely not running
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Pipe communication error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Show notification in system tray
        /// </summary>
        private void ShowNotification(string title, string message, ToolTipIcon icon)
        {
            if (notifyIcon != null)
            {
                notifyIcon.ShowBalloonTip(3000, title, message, icon);
            }
        }

        /// <summary>
        /// Form closing - cleanup resources
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
        }
    }
}
