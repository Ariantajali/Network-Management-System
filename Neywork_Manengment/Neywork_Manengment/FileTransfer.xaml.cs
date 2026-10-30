using System;
using System.IO;
using System.Linq;
using System.Windows;
using MahApps.Metro.Controls;
using Neywork_Manengment.Network;

namespace Neywork_Manengment
{
    public partial class FileTransfer : MetroWindow
    {
        private ServerSocketManager serverSocket;

        public FileTransfer(ServerSocketManager existingServerSocket)
        {
            InitializeComponent();
            serverSocket = existingServerSocket;

            // مقداردهی اولیه دیتاگرید
            RefreshClientList();
        }

        private void RefreshClientList()
        {
            // لیست کلاینت‌ها رو از سرور می‌گیره و به DataGrid ست می‌کنه
            ClientsDataGrid.ItemsSource = serverSocket.GetClients().Values.ToList();
            ClientsDataGrid.Items.Refresh();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                FilePathTextBox.Text = dlg.FileName;
            }
        }

        private async void SendFile_Click(object sender, RoutedEventArgs e)
        {
            string filePath = FilePathTextBox.Text;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Select a valid file.");
                return;
            }

            // لیست کلاینت‌های انتخاب‌شده
            var selectedClients = serverSocket.GetClients().Values.Where(c => c.IsSelected).ToList();
            if (!selectedClients.Any())
            {
                MessageBox.Show("Select at least one client.");
                return;
            }

            foreach (var client in selectedClients)
            {
                await serverSocket.SendFileAsync(client.IPAddress, filePath);
            }

            MessageBox.Show("File sent to selected clients.");
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshClientList();
        }
        private async void SendAll_Click(object sender, RoutedEventArgs e)
        {
            string filePath = FilePathTextBox.Text;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Select a valid file.");
                return;
            }

            var allClients = serverSocket.GetClients().Values.ToList();
            if (!allClients.Any())
            {
                MessageBox.Show("No clients found.");
                return;
            }

            foreach (var client in allClients)
            {
                await serverSocket.SendFileAsync(client.IPAddress, filePath);
            }

            MessageBox.Show("File sent to all connected clients.");
        }
    }
}
