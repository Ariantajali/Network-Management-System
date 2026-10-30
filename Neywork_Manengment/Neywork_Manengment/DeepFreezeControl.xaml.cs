using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Neywork_Manengment.Models;
using Neywork_Manengment.Network;
using System.IO;
using Path = System.IO.Path;

namespace Neywork_Manengment
{
    /// <summary>
    /// Interaction logic for DeepFreezeControl.xaml
    /// </summary>
    public partial class DeepFreezeControl : MetroWindow
    {
        private ServerSocketManager serverSocket;
        public DeepFreezeControl(ServerSocketManager existingServerSocket)
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
        private void RefreshClients_Click(object sender, RoutedEventArgs e)
        {
            RefreshClientsList();
        }
        private async void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is not ClientModel selectedClient)
            {
                StatusText.Text = "Please select a client.";
                return;
            }

            await ExecuteFreezeCommandOnClients(new List<ClientModel> { selectedClient });
        }
        private async void SendCommandToSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedClients = serverSocket.GetClients().Values.Where(c => c.IsSelected).ToList();
            if (!selectedClients.Any())
            {
                StatusText.Text = "Please select one or more clients.";
                return;
            }

            await ExecuteFreezeCommandOnClients(selectedClients);
        }
        private async void SendCommandToAll_Click(object sender, RoutedEventArgs e)
        {
            var allClients = serverSocket.GetClients().Values.ToList();
            if (!allClients.Any())
            {
                StatusText.Text = "No clients found.";
                return;
            }

            await ExecuteFreezeCommandOnClients(allClients);
        }


        private async Task ExecuteFreezeCommandOnClients(List<ClientModel> clients)
        {
            string action = (ActionComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            string password = PasswordInput.Password.Trim();
            string dfcPath = DfcPathInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(dfcPath))
            {
                StatusText.Text = "Please fill all fields (Action, Password, DFC Path).";
                return;
            }

            string dfcCommand = action switch
            {
                "FREEZE" => "/BOOTFROZEN",
                "THAW" => "/BOOTTHAWED",
                "LOCK" => "/LOCK",
                "UNLOCK" => "/UNLOCK",
                _ => ""
            };

            if (string.IsNullOrEmpty(dfcCommand))
            {
                StatusText.Text = "Invalid action selected.";
                return;
            }

            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string templatePath = Path.Combine(basePath, "apps", "freeze_template.bat");
                string targetPath = Path.Combine(basePath, "apps", "freeze.bat");

                string template = File.ReadAllText(templatePath);
                string modified = template.Replace("{PASSWORD}", password)
                                          .Replace("{DFC_COMMAND}", dfcCommand)
                                          .Replace("{DFC_PATH}", dfcPath);

                File.WriteAllText(targetPath, modified);
                string batFileName = "freeze.bat";

                foreach (var client in clients)
                {
                    await serverSocket.SendFileAsync(client.IPAddress, targetPath);
                    await serverSocket.SendCommandAsync($"FREEZE_BAT:{batFileName}", client.IPAddress);
                }

                StatusText.Text = $"Command '{action}' sent to {clients.Count} clients.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }


    }
}
