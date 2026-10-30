using System.Data;
using System.Windows;
using MahApps.Metro.Controls;
using Neywork_Manengment.Network;
using Neywork_Manengment.Services;
using Neywork_Manengment.Views;

namespace Neywork_Manengment
{
    public partial class MainDashboard : MetroWindow
    {
        private ServerSocketManager serverSocket;
        private ScreenMirrorService screenMirrorService;
        private readonly string _currentRole;
        private readonly List<string> _permissions;

        public MainDashboard(string role, List<string> permissions)
        {
            InitializeComponent();
            _currentRole = role;
            _permissions = permissions;

            serverSocket = new ServerSocketManager();
            screenMirrorService = new ScreenMirrorService(serverSocket);
            serverSocket.StartServer();
            ApplyPermissionFiltering();
        }

        private void Clients_Click(object sender, RoutedEventArgs e)
        {
            ClientsWindow clientsWindow = new ClientsWindow(serverSocket); // ServerSocketManager رو پاس میدیم
            clientsWindow.Show();
        }

        private void File_Transfer_Click(object sender, RoutedEventArgs e)
        {
            FileTransfer filetransfer = new FileTransfer(serverSocket);
            filetransfer.Show();
        }

        private void Install_Program_Click(object sender, RoutedEventArgs e)
        {
            InstallProgram installprogram = new InstallProgram(serverSocket);
            installprogram.Show();
        }

        private void Remote_Click(object sender, RoutedEventArgs e)
        {
            var mirrorWindow = new ClientMirrorPanel(serverSocket, screenMirrorService);
            mirrorWindow.Show();
        }

        private void DeepFreezeControl_Click(object sender, RoutedEventArgs e)
        {
            DeepFreezeControl deepFreezeControl = new DeepFreezeControl(serverSocket);
            deepFreezeControl.Show();
        }

        private void UserManagement_Click(object sender, RoutedEventArgs e)
        {
            var win = new UserManagementWindow();
            win.ShowDialog();
        }
        private void ApplyPermissionFiltering()
        {
            if (_currentRole == "admin") return;

            File_Transfer.IsEnabled = _permissions.Contains("FileTransfer");
            RemoteTile.IsEnabled = _permissions.Contains("Remote");
            DeepFreezeControl.IsEnabled = _permissions.Contains("Freeze");
            Install_Program.IsEnabled = _permissions.Contains("Install");

            // only watch admin user manengment panel
            if (!_permissions.Contains("UserManagement"))
                UserManagement.Visibility = Visibility.Collapsed;
        }
    }
}
