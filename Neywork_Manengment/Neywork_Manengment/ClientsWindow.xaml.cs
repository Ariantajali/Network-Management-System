using System.Windows;
using MahApps.Metro.Controls;
using Neywork_Manengment.Models;
using Neywork_Manengment.Network;
using System.Linq;

namespace Neywork_Manengment
{
    public partial class ClientsWindow : MetroWindow
    {
        private ServerSocketManager serverSocket;

        public ClientsWindow(ServerSocketManager existingServerSocket)
        {
            InitializeComponent();
            serverSocket = existingServerSocket;

            // ست کردن دیتاسورس اولیه
            ClientsDataGrid.ItemsSource = serverSocket.GetClients().Values.ToList();
            ClientsDataGrid.Items.Refresh();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // ارسال دستور Refresh به همه کلاینت‌ها
            foreach (var client in serverSocket.GetClients().Values)
            {
                serverSocket.SendRefreshCommand(client.IPAddress);
            }

            // ریفرش کردن لیست UI
            ClientsDataGrid.ItemsSource = serverSocket.GetClients().Values.ToList();
            ClientsDataGrid.Items.Refresh();
        }

        private void ClientsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedClient = (ClientModel)ClientsDataGrid.SelectedItem;
            if (selectedClient != null)
            {
                ClientsControl controlWindow = new ClientsControl(serverSocket, selectedClient);
                controlWindow.Show();
            }
        }
        private async void ShutdownAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var client in serverSocket.GetClients().Values)
            {
                await serverSocket.SendShutdownCommand(client.IPAddress);
            }
            MessageBox.Show("Shutdown command sent to all clients.");
        }

        private async void RestartAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var client in serverSocket.GetClients().Values)
            {
                await serverSocket.SendRestartCommand(client.IPAddress);
            }
            MessageBox.Show("Restart command sent to all clients.");
        }

        private async void LockAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var client in serverSocket.GetClients().Values)
            {
                await serverSocket.SendLockCommand(client.IPAddress);
            }
            MessageBox.Show("Lock command sent to all clients.");
        }

        private async void MessageAll_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter message for all clients:", "Broadcast Message", "");
            if (!string.IsNullOrEmpty(input))
            {
                foreach (var client in serverSocket.GetClients().Values)
                {
                    await serverSocket.SendMessageCommand(client.IPAddress, input);
                }
                MessageBox.Show("Message sent to all clients.");
            }
        }

    }
}
