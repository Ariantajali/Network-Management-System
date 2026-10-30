using MahApps.Metro.Controls;
using Neywork_Manengment.Models;
using Neywork_Manengment.Network;
using Neywork_Manengment.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iconPacks = MahApps.Metro.IconPacks;

namespace Neywork_Manengment.Views
{
    public partial class ClientMirrorPanel : MetroWindow
    {
        private readonly ServerSocketManager _serverSocket;
        private readonly ScreenMirrorService _mirrorService;
        private bool _mirrorServerActive = false;

        public ClientMirrorPanel(ServerSocketManager socket, ScreenMirrorService mirrorService)
        {
            InitializeComponent();
            _serverSocket = socket;
            _mirrorService = mirrorService;
            RefreshClientList();
        }

        private void RefreshClientList()
        {
            var clients = _serverSocket.GetClients().Values.ToList();
            ClientsDataGrid.ItemsSource = clients;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshClientList();
        }

        private void ShowDesktop_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is ClientModel client)
            {
                var viewer = new ScreenViewerWindow(_mirrorService, client.IPAddress);
                viewer.Owner = this;
                viewer.Show();
            }
            else
            {
                MessageBox.Show("Select a client from the list.");
            }
        }

        private void StartMirror_Click(object sender, RoutedEventArgs e)
        {
            if (_mirrorServerActive)
            {
                _mirrorService.StopMirror();
                _mirrorServerActive = false;
                UpdateMirrorButton(false);
            }
            else
            {
                _mirrorService.StartMirror();
                _mirrorServerActive = true;
                UpdateMirrorButton(true);
            }
        }

        private void UpdateMirrorButton(bool isOn)
        {
            BtnStartMirror.Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new iconPacks.PackIconMaterial
                    {
                        Kind = isOn ? iconPacks.PackIconMaterialKind.StopCircleOutline : iconPacks.PackIconMaterialKind.MonitorShare,
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(0, 0, 5, 0)
                    },
                    new TextBlock
                    {
                        Text = isOn ? "Stop Share screen" : "Start Share screen server"
                    }
                }
            };
        }
    }
}