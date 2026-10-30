using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using Neywork_Manengment.Models;
using System.Security.Cryptography;


namespace Neywork_Manengment.Services
{
    public class UserService
    {
        private readonly string _dbPath = "users.db"; 
        private readonly string _connectionString;

        public UserService()
        {
            _connectionString = $"Data Source={_dbPath};Version=3;";
            InitializeDatabase();
        }


        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string createTableQuery = @"CREATE TABLE IF NOT EXISTS Users (
                                      Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                      Username TEXT NOT NULL UNIQUE,
                                      Password TEXT NOT NULL,
                                      Role TEXT NOT NULL,
                                      Permissions TEXT NOT NULL
                                  );";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                string checkAdminQuery = "SELECT COUNT(*) FROM Users;";
                using (var command = new SQLiteCommand(checkAdminQuery, connection))
                {
                    long count = (long)command.ExecuteScalar();
                    if (count == 0)
                    {
                        // هش کردن پسورد admin
                        string hashedPassword = HashPassword("aa@@pass200");

                        string insertAdminQuery = "INSERT INTO Users (Username, Password, Role, Permissions) " +
                                                  "VALUES ('admin', @password, 'admin', 'ALL');";
                        using (var insertCmd = new SQLiteCommand(insertAdminQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@password", hashedPassword);
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public bool ValidateLogin(string username, string password)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string hashedPassword = HashPassword(password);

                string query = "SELECT COUNT(*) FROM Users WHERE Username=@username AND Password=@password;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", hashedPassword);

                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }
        public List<string> GetPermissions(string username)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Permissions FROM Users WHERE Username = @username";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);

            string permissions = command.ExecuteScalar()?.ToString() ?? "";
            return permissions == "ALL"
                ? new List<string> { "Remote", "FileTransfer", "Freeze", "Install", "UserManagement" }
                : permissions.Split(',').Select(p => p.Trim()).ToList();
        }
        public bool AddUser(string username, string password, string role, List<string> permissions)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @username;";
            using var checkCmd = new SQLiteCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@username", username);

            long exists = (long)checkCmd.ExecuteScalar();
            if (exists > 0) return false;

            string perms = (role == "admin") ? "ALL" : string.Join(",", permissions);

            // هش کردن پسورد قبل از ذخیره
            string hashedPassword = HashPassword(password);

            string insertQuery = "INSERT INTO Users (Username, Password, Role, Permissions) VALUES (@username, @password, @role, @permissions);";
            using var insertCmd = new SQLiteCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@username", username);
            insertCmd.Parameters.AddWithValue("@password", hashedPassword);
            insertCmd.Parameters.AddWithValue("@role", role);
            insertCmd.Parameters.AddWithValue("@permissions", perms);

            insertCmd.ExecuteNonQuery();
            return true;
        }
        public List<UserModel> GetAllUsers()
        {
            var list = new List<UserModel>();

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Username, Role, Permissions FROM Users";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new UserModel
                {
                    Username = reader.GetString(0),
                    Role = reader.GetString(1),
                    Permissions = reader.GetString(2)
                });
            }

            return list;
        }

        public void DeleteUser(string username)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "DELETE FROM Users WHERE Username = @username";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
            command.ExecuteNonQuery();
        }
        public string GetRole(string username)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Role FROM Users WHERE Username = @username";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);

            var role = command.ExecuteScalar()?.ToString();
            return role ?? "user";
        }
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

    }
}