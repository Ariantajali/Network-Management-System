using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using Neywork_Manengment.Network;

namespace Neywork_Manengment
{
    public partial class InstallProgram : MetroWindow
    {
        private ServerSocketManager serverSocket;
        private string setupFilePath = "";

        public InstallProgram(ServerSocketManager existingServerSocket)
        {
            InitializeComponent();
            serverSocket = existingServerSocket;
            RefreshClientsList();
        }

        private void RefreshClientsList()
        {
            ClientsDataGrid.ItemsSource = serverSocket.GetClients().Values.ToList();
            ClientsDataGrid.Items.Refresh();
        }

        private void BrowseSetupFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Setup Files (*.exe;*.msi)|*.exe;*.msi";
            if (dlg.ShowDialog() == true)
            {
                setupFilePath = dlg.FileName;
                txtSetupFile.Text = setupFilePath;
            }
        }

        private async void btnSendFiles_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(setupFilePath))
            {
                MessageBox.Show("Please select a setup file.");
                return;
            }

            var selectedClients = serverSocket.GetClients().Values.Where(c => c.IsSelected).ToList();
            if (!selectedClients.Any())
            {
                MessageBox.Show("Please select at least one client.");
                return;
            }

            string setupFileName = Path.GetFileName(setupFilePath);
            string batFileName = Path.GetFileNameWithoutExtension(setupFilePath) + "_install.bat";

            // گرفتن سوییچ انتخابی از ComboBox
            string selectedSwitch = (cmbSilentSwitch.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(selectedSwitch) && cmbSilentSwitch.Text != null)
                selectedSwitch = cmbSilentSwitch.Text.Trim();

            if (string.IsNullOrWhiteSpace(selectedSwitch))
            {
                MessageBox.Show("Please select or enter a silent install switch.");
                return;
            }

            // ساخت فایل .bat موقت
            string batFilePath = Path.Combine(Path.GetTempPath(), batFileName);
            File.WriteAllText(batFilePath, $@"
@echo off
cd %~dp0
start /wait """" ""{setupFileName}"" {selectedSwitch}
exit
");

            foreach (var client in selectedClients)
            {
                await serverSocket.SendFileAsync(client.IPAddress, setupFilePath);
                await serverSocket.SendFileAsync(client.IPAddress, batFilePath);

                string installCommand = $"INSTALL_BAT:{batFileName}";
                await serverSocket.SendCommandAsync(installCommand, client.IPAddress);
            }

            MessageBox.Show("Files have been sent and installation has started for selected clients.");
        }

        private void RefreshClients_Click(object sender, RoutedEventArgs e)
        {
            RefreshClientsList();
        }
        private async void btnSendFilesToAll_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(setupFilePath))
            {
                MessageBox.Show("Please select a setup file.");
                return;
            }

            string setupFileName = Path.GetFileName(setupFilePath);
            string batFileName = Path.GetFileNameWithoutExtension(setupFilePath) + "_install.bat";

            string selectedSwitch = (cmbSilentSwitch.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(selectedSwitch) && cmbSilentSwitch.Text != null)
                selectedSwitch = cmbSilentSwitch.Text.Trim();

            if (string.IsNullOrWhiteSpace(selectedSwitch))
            {
                MessageBox.Show("Please select or enter a silent install switch.");
                return;
            }

            string batFilePath = Path.Combine(Path.GetTempPath(), batFileName);
            File.WriteAllText(batFilePath, $@"
@echo off
cd %~dp0
start /wait """" ""{setupFileName}"" {selectedSwitch}
exit
");

            var allClients = serverSocket.GetClients().Values.ToList();
            if (!allClients.Any())
            {
                MessageBox.Show("No clients found.");
                return;
            }

            foreach (var client in allClients)
            {
                await serverSocket.SendFileAsync(client.IPAddress, setupFilePath);
                await serverSocket.SendFileAsync(client.IPAddress, batFilePath);
                await serverSocket.SendCommandAsync($"INSTALL_BAT:{batFileName}", client.IPAddress);
            }

            MessageBox.Show("Files have been sent and installation has started for all clients.");
        }
    }
}
