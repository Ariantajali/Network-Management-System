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
using Neywork_Manengment.Services;

namespace Neywork_Manengment
{
    /// <summary>
    /// Interaction logic for UserManagementWindow.xaml
    /// </summary>
    public partial class UserManagementWindow : MetroWindow
    {
        private readonly UserService _userService;
        public UserManagementWindow()
        {
            InitializeComponent();
            _userService = new UserService();
            LoadUsers();
        }
        private void LoadUsers()
        {
            var users = _userService.GetAllUsers();
            UsersGrid.ItemsSource = users;
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and password cannot be empty.");
                return;
            }
            string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "user";

            var permissions = new List<string>();
            if (RemotePermission.IsChecked == true) permissions.Add("Remote");
            if (FileTransferPermission.IsChecked == true) permissions.Add("FileTransfer");
            if (InstallPermission.IsChecked == true) permissions.Add("Install");
            if (FreezePermission.IsChecked == true) permissions.Add("Freeze");
            if (UserManagementPermission.IsChecked == true) permissions.Add("UserManagement");

            bool result = _userService.AddUser(username, password, role, permissions);

            if (result)
            {
                MessageBox.Show("User added successfully.");
                LoadUsers();
                UsernameBox.Clear();
                PasswordBox.Clear();
                RoleComboBox.SelectedIndex = 1;
                RemotePermission.IsChecked = false;
                FileTransferPermission.IsChecked = false;
                InstallPermission.IsChecked = false;
                FreezePermission.IsChecked = false;
                UserManagementPermission.IsChecked = false;
            }
            else
            {
                MessageBox.Show("User already exists.");
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string username)
            {
                if (username == "admin")
                {
                    MessageBox.Show("Cannot delete default admin.");
                    return;
                }

                if (MessageBox.Show($"Are you sure you want to delete '{username}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _userService.DeleteUser(username);
                    LoadUsers();
                }
            }
        }
    
}
}
