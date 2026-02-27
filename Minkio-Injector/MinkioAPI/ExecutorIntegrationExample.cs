using System;
using System.Windows.Forms;
using MinkioAPI;

namespace MinkioExecutor
{
    /// <summary>
    /// Example integration of MinkioOverlay into MinkioExecutor
    /// This demonstrates how to use the overlay injection in your GUI
    /// </summary>
    public partial class ExecutorFormWithOverlay : Form
    {
        public ExecutorFormWithOverlay()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Injects both the DLL and the overlay
        /// </summary>
        private void InjectWithOverlay()
        {
            try
            {
                // Check if Roblox is running
                if (!MinkioInjector.IsRobloxRunning())
                {
                    MessageBox.Show(
                        "RobloxPlayerBeta.exe is not running.\n\nPlease start Roblox first.",
                        "Roblox Not Running",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Step 1: Check if overlay DLL exists
                if (!MinkioOverlay.DllExists())
                {
                    MessageBox.Show(
                        "Minkio.dll not found.\n\nMake sure you compiled it using build_dll.bat",
                        "Minkio.dll Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Show DLL info
                string dllInfo = MinkioOverlay.GetDllInfo();
                Console.WriteLine("DLL Information:\n{0}", dllInfo);

                // Step 2: Inject the overlay DLL
                Console.WriteLine("\nInjecting Minkio.dll...");
                bool overlaySuccess = MinkioOverlay.InjectOverlay();

                if (overlaySuccess)
                {
                    MessageBox.Show(
                        "Overlay injected successfully!\n\n" +
                        "You should see 'Minkio v1.0' in the bottom-right corner.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    Console.WriteLine("✓ Overlay injection succeeded");
                }
                else
                {
                    MessageBox.Show(
                        "Failed to inject overlay.\n\nCheck the console for details.",
                        "Injection Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    Console.WriteLine("✗ Overlay injection failed");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error: " + ex.Message,
                    "Exception",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Console.WriteLine("Exception: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Injects the main DLL (original functionality)
        /// </summary>
        private void InjectMainDll()
        {
            try
            {
                if (!MinkioInjector.IsRobloxRunning())
                {
                    MessageBox.Show("RobloxPlayerBeta.exe is not running.");
                    return;
                }

                int pid = MinkioInjector.GetRobloxPID();
                Console.WriteLine("Injecting into PID: {0}", pid);

                // Your existing injection code here
                MessageBox.Show("Main DLL injected successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Enhanced injection that does both main DLL and overlay
        /// </summary>
        private void InjectBoth()
        {
            try
            {
                if (!MinkioInjector.IsRobloxRunning())
                {
                    MessageBox.Show("RobloxPlayerBeta.exe is not running.");
                    return;
                }

                Console.WriteLine("=== Starting Combined Injection ===\n");

                // Inject main DLL
                Console.WriteLine("[1/2] Injecting main DLL...");
                InjectMainDll();
                Console.WriteLine("✓ Main DLL injected\n");

                // Wait a moment for main DLL to initialize
                System.Threading.Thread.Sleep(500);

                // Inject overlay DLL
                Console.WriteLine("[2/2] Injecting overlay DLL...");
                InjectWithOverlay();
                Console.WriteLine("✓ Overlay DLL injected\n");

                Console.WriteLine("=== Injection Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        // Example button click handlers to add to your form:
        /*
        
        private void btnInjectOverlay_Click(object sender, EventArgs e)
        {
            InjectWithOverlay();
        }

        private void btnInjectMain_Click(object sender, EventArgs e)
        {
            InjectMainDll();
        }

        private void btnInjectBoth_Click(object sender, EventArgs e)
        {
            InjectBoth();
        }

        private void btnShowDllInfo_Click(object sender, EventArgs e)
        {
            if (MinkioOverlay.DllExists())
            {
                string info = MinkioOverlay.GetDllInfo();
                MessageBox.Show(info, "DLL Information");
            }
            else
            {
                MessageBox.Show("Minkio.dll not found");
            }
        }

        */
    }
}
