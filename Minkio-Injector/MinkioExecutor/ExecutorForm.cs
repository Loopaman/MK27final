using System;
using System.Windows.Forms;
using MinkioAPI;

namespace MinkioExecutor
{
    public partial class ExecutorForm : Form
    {
        public ExecutorForm()
        {
            InitializeComponent();
            Text = "Minkio Executor";
            Width = 400;
            Height = 300;

            // Create UI elements
            Label statusLabel = new Label() { Text = "Status:", Left = 10, Top = 10, Width = 100 };
            TextBox statusText = new TextBox() { Left = 110, Top = 10, Width = 260, Height = 150, Multiline = true, ReadOnly = true };
            Button injectButton = new Button() { Text = "Inject", Left = 10, Top = 170, Width = 360 };
            Label infoLabel = new Label() { Text = "Click 'Inject' to inject Minkio into Roblox", Left = 10, Top = 200, Width = 360 };

            // Add controls
            Controls.Add(statusLabel);
            Controls.Add(statusText);
            Controls.Add(injectButton);
            Controls.Add(infoLabel);

            // Refresh status on load
            Shown += (s, e) =>
            {
                RefreshStatus(statusText);
            };

            // Inject button click
            injectButton.Click += (s, e) =>
            {
                injectButton.Enabled = false;
                injectButton.Text = "Injecting...";

                if (MinkioInjector.InjectDLL())
                {
                    MessageBox.Show("Injection successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Injection failed. Check the status above.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                RefreshStatus(statusText);
                injectButton.Enabled = true;
                injectButton.Text = "Inject";
            };
        }

        private void RefreshStatus(TextBox statusText)
        {
            statusText.Text = MinkioInjector.GetStatus();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ExecutorForm());
        }
    }
}
