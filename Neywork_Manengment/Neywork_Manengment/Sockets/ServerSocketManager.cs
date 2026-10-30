using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Neywork_Manengment.Models;

namespace Neywork_Manengment.Network
{
    public class ServerSocketManager
    {
        private TcpListener listener;
        private bool isRunning;
        private int port = 5055;
        private System.Timers.Timer cleanupTimer;

        private readonly object _clientsLock = new object();
        private ConcurrentDictionary<string, ClientModel> ConnectedClients = new ConcurrentDictionary<string, ClientModel>();

        public void StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                isRunning = true;

                Console.WriteLine($" Server listening on port {port}...");

                Thread acceptThread = new Thread(AcceptClients);
                acceptThread.IsBackground = true;
                acceptThread.Start();

                StartInstallResultListener();
                StartCleanupTimer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Server failed to start: {ex.Message}");
            }
        }

        private void AcceptClients()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Console.WriteLine($" Received data from client: {message}");

                    var clientData = JsonSerializer.Deserialize<ClientModel>(message);

                    if (clientData != null)
                    {
                        clientData.StatusIcon = (clientData.Status == "ONLINE") ? "CheckCircle" : "CloseCircle";
                        clientData.StatusColor = (clientData.Status == "ONLINE") ? "Green" : "Red";
                        clientData.LastUpdateTime = DateTime.Now;

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            lock (_clientsLock)
                            {
                                ConnectedClients.AddOrUpdate(clientData.IPAddress, clientData, (key, existing) =>
                                {
                                    existing.SystemName = clientData.SystemName;
                                    existing.Status = clientData.Status;
                                    existing.CPU = clientData.CPU;
                                    existing.RAM = clientData.RAM;
                                    existing.Disk = clientData.Disk;
                                    existing.StatusIcon = clientData.StatusIcon;
                                    existing.StatusColor = clientData.StatusColor;
                                    existing.LastUpdateTime = DateTime.Now;
                                    return existing;
                                });
                            }
                        });
                    }

                    stream.Close();
                    client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Accept client error: {ex.Message}");
                }
            }
        }

        private void StartCleanupTimer()
        {
            cleanupTimer = new System.Timers.Timer(5000);
            cleanupTimer.Elapsed += (s, e) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    var staleClients = ConnectedClients.Values
                        .Where(c => (DateTime.Now - c.LastUpdateTime).TotalSeconds > 15)
                        .ToList();

                    foreach (var client in staleClients)
                    {
                        ConnectedClients.TryRemove(client.IPAddress, out _);
                        Console.WriteLine($" Removed stale client: {client.IPAddress}");
                    }
                });
            };
            cleanupTimer.Start();
        }

        public void StartInstallResultListener()
        {
            Thread thread = new Thread(() =>
            {
                TcpListener resultListener = new TcpListener(IPAddress.Any, 5056);
                resultListener.Start();

                Console.WriteLine("📡 Listening for install results on port 5056...");

                while (true)
                {
                    try
                    {
                        TcpClient client = resultListener.AcceptTcpClient();
                        using NetworkStream stream = client.GetStream();

                        byte[] buffer = new byte[1024];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string resultJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        var result = JsonSerializer.Deserialize<InstallResultModel>(resultJson);
                        if (result != null)
                        {
                            Console.WriteLine($"📦 Install result received from {result.IPAddress} → {result.Result}");

                            App.Current.Dispatcher.Invoke(() =>
                            {
                                if (ConnectedClients.TryGetValue(result.IPAddress, out var clientModel))
                                {
                                    clientModel.InstallStatus = result.Result;
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error receiving install result: {ex.Message}");
                    }
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }

        public async Task SendCommandAsync(string command, string clientIp)
        {
            int clientPort = 6060;
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(clientIp, clientPort);

                byte[] data = Encoding.UTF8.GetBytes(command);
                using NetworkStream stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length);

                Console.WriteLine($"✅ Command '{command}' sent to {clientIp}:{clientPort}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending '{command}' to {clientIp}:{clientPort} - {ex.Message}");
            }
        }

        public async Task SendFileAsync(string clientIp, string filePath)
        {
            int filePort = 7070;
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(clientIp, filePort);

                using NetworkStream stream = client.GetStream();

                string fileName = Path.GetFileName(filePath);
                byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                byte[] nameLenBytes = BitConverter.GetBytes(nameBytes.Length);

                await stream.WriteAsync(nameLenBytes, 0, nameLenBytes.Length);
                await stream.WriteAsync(nameBytes, 0, nameBytes.Length);

                using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead);
                }

                Console.WriteLine($"✅ Sent file '{fileName}' to {clientIp}:{filePort}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending file to {clientIp}:7070 - {ex.Message}");
            }
        }

        public Task SendShutdownCommand(string clientIp) => SendCommandAsync("SHUTDOWN", clientIp);
        public Task SendRestartCommand(string clientIp) => SendCommandAsync("RESTART", clientIp);
        public Task SendLockCommand(string clientIp) => SendCommandAsync("LOCK", clientIp);
        public Task SendRefreshCommand(string clientIp) => SendCommandAsync("REFRESH", clientIp);
        public Task SendMessageCommand(string clientIp, string text) => SendCommandAsync($"MESSAGE:{text}", clientIp);

        public ConcurrentDictionary<string, ClientModel> GetClients() => ConnectedClients;
    }
}
