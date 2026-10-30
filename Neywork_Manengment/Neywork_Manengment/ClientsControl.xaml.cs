using System.Windows;
using MahApps.Metro.Controls;
using Neywork_Manengment.Models;
using Neywork_Manengment.Network;
using Microsoft.VisualBasic;

namespace Neywork_Manengment
{
    public partial class ClientsControl : MetroWindow
    {
        private ServerSocketManager serverSocket;
        private ClientModel client;

        public ClientsControl(ServerSocketManager server, ClientModel selectedClient)
        {
            InitializeComponent();
            serverSocket = server;
            client = selectedClient;

            lblSystemName.Text = $"System Name: {client.SystemName}";
            lblIp.Text = $"IP Address: {client.IPAddress}";
            lblStatus.Text = $"Status: {client.Status}";
            lblCpu.Text = $"CPU: {client.CPU}%";
            lblRam.Text = $"RAM: {client.RAM}%";
            lblDisk.Text = $"Disk: {client.Disk}%";
        }

        private async void Shutdown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await serverSocket.SendShutdownCommand(client.IPAddress);
                MessageBox.Show($"Shutdown command sent to {client.IPAddress}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send shutdown command: {ex.Message}");
            }
        }

        private async void Restart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await serverSocket.SendRestartCommand(client.IPAddress);
                MessageBox.Show($"Restart command sent to {client.IPAddress}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send restart command: {ex.Message}");
            }
        }

        private async void Lock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await serverSocket.SendLockCommand(client.IPAddress);
                MessageBox.Show($"Lock command sent to {client.IPAddress}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send lock command: {ex.Message}");
            }
        }

        private async void Message_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter message to send:", "Send Message", "");
            if (!string.IsNullOrEmpty(input))
            {
                try
                {
                    await serverSocket.SendMessageCommand(client.IPAddress, input);
                    MessageBox.Show($"Message sent to {client.IPAddress}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to send message: {ex.Message}");
                }
            }
        }

    }
}
