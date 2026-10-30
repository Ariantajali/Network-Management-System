using System;
using System.IO;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace Agent_Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Visible = false;
            notifyIcon1.Visible = true;
            EnsureServerIpConfigured();
            AddToStartup(); // ⭐️ اضافه کردن به Startup فقط یک بار

            // Start agent
            ClientAgent agent = new ClientAgent();
            agent.Start();
        }

        private void AddToStartup()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolder, "AgentClient.lnk");

            if (!File.Exists(shortcutPath))
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.Description = "Client Agent";
                shortcut.TargetPath = Application.ExecutablePath;
                shortcut.Save();
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Config Ip Server", null, (s, e) =>
            {
                new ConfigForm().ShowDialog();
            });
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
            notifyIcon1.ContextMenuStrip = trayMenu;
        }
        private void EnsureServerIpConfigured()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

            if (!File.Exists(configPath))
            {
                var result = new ConfigForm().ShowDialog();
                if (result != DialogResult.OK)
                {
                    MessageBox.Show("The program cannot run without a server address.");
                    Application.Exit();
                }
            }
        }

    }
}
