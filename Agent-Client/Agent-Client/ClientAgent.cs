using Agent_Client;
using Agent_Client.Services;
using Agent_Client.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class ClientAgent
{
    private string serverIp;
    private int serverPort = 5055;
    private int listenPort = 6060;
    private const int SCREEN_PORT = 9090;
    private const int MIRROR_PORT = 9091;

    private System.Timers.Timer heartbeatTimer;
    private CancellationTokenSource? _screenCts;
    private CancellationTokenSource? _mirrorCts;
    private MirrorDisplayForm? _mirrorForm;

    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;
    const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    public void Start()
    {
        serverIp = GetServerIpFromFile();
        Console.WriteLine("Server IP loaded: " + serverIp);

        new Thread(() => StartListener()) { IsBackground = true }.Start();
        new Thread(() => StartFileReceiver()) { IsBackground = true }.Start();
        new Thread(() => StartMirrorListener()) { IsBackground = true }.Start();
        new Thread(() => StartControlListener()) { IsBackground = true }.Start(); // ← جدید

        SendDataToServer();
        heartbeatTimer = new System.Timers.Timer(10000);
        heartbeatTimer.Elapsed += (s, e) => SendDataToServer();
        heartbeatTimer.Start();
    }

    private string GetServerIpFromFile()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        if (!File.Exists(path)) return "127.0.0.1";

        try
        {
            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return config != null && config.ContainsKey("ServerIp") ? config["ServerIp"] : "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    private void StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, listenPort);
        listener.Start();

        while (true)
        {
            try
            {
                using var client = listener.AcceptTcpClient();
                using var stream = client.GetStream();

                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (message == "SCREEN_START")
                    StartScreenStreaming();
                else if (message == "SCREEN_STOP")
                    StopScreenStreaming();
                else if (message == "SHUTDOWN")
                    System.Diagnostics.Process.Start("shutdown", "/s /t 0");
                else if (message == "RESTART")
                    System.Diagnostics.Process.Start("shutdown", "/r /t 0");
                else if (message == "LOCK")
                    System.Diagnostics.Process.Start("rundll32.exe", "user32.dll,LockWorkStation");
                else if (message.StartsWith("MESSAGE:"))
                    MessageBox.Show(message.Substring(8), "پیام از سرور");
                else if (message == "REFRESH")
                    SendDataToServer();
                else if (message == "MIRROR_OFF")
                    _mirrorForm?.Invoke(() => _mirrorForm.Close());
                else if (message == "MIRROR_ON")
                {
                    Console.WriteLine("🔔 MIRROR_ON command received. Connecting to server...");
                    new Thread(() => ConnectToMirrorServer()) { IsBackground = true }.Start();
                }
                else if (message.StartsWith("INSTALL_BAT:"))
                {
                    string batName = message.Substring("INSTALL_BAT:".Length);
                    string batPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                        "install", batName);

                    try
                    {
                        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = batPath,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });

                        process.WaitForExit();

                        SendInstallResult("SUCCESS");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("❌ Error executing BAT file: " + ex.Message);
                        SendInstallResult("FAILED");
                    }
                }
                else if (message.StartsWith("FREEZE_BAT:"))
                {
                    string batName = message.Substring("FREEZE_BAT:".Length);
                    string batPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                        "install", batName);

                    try
                    {
                        // ایجاد تسک در Task Scheduler برای اجرای با دسترسی Admin
                        string createTaskArguments = $"/Create /TN \"RunFreeze\" /TR \"{batPath}\" /SC ONCE /ST 00:00 /RL HIGHEST /F";
                        string runTaskArguments = "/Run /TN RunFreeze";

                        // ایجاد Task برای اجرای bat با دسترسی ادمین
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "schtasks",
                            Arguments = createTaskArguments,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        })?.WaitForExit();

                        // اجرای تسک ایجاد شده
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "schtasks",
                            Arguments = runTaskArguments,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        })?.WaitForExit();

                        SendInstallResult("SUCCESS");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("❌ Error executing BAT file with Task Scheduler: " + ex.Message);
                        SendInstallResult("FAILED");
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Listener error: {ex.Message}");
            }
        }
    }

    private void StartScreenStreaming()
    {
        StopScreenStreaming();
        _screenCts = new CancellationTokenSource();
        _ = Task.Run(() => StreamLoopAsync(_screenCts.Token));
    }

    private void StopScreenStreaming()
    {
        _screenCts?.Cancel();
        _screenCts = null;
    }

    private async Task StreamLoopAsync(CancellationToken ct)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(serverIp, SCREEN_PORT, ct);
        using var stream = client.GetStream();

        while (!ct.IsCancellationRequested)
        {
            byte[] jpg = ScreenCaptureService.CaptureScreen();
            byte[] len = BitConverter.GetBytes(jpg.Length);

            await stream.WriteAsync(len, 0, 4, ct);
            await stream.WriteAsync(jpg, 0, jpg.Length, ct);

            await Task.Delay(50, ct);
        }
    }

    private void SendDataToServer()
    {
        try
        {
            var dataObj = new
            {
                SystemName = Environment.MachineName,
                IPAddress = GetLocalIPAddress(),
                Status = "ONLINE",
                CPU = GetCpuUsage(),
                RAM = GetRamUsage(),
                Disk = GetDiskUsage("C:")
            };
            string jsonData = JsonSerializer.Serialize(dataObj);
            using var client = new TcpClient();
            client.Connect(serverIp, serverPort);
            using var stream = client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(jsonData);
            stream.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SendData error: {ex.Message}");
        }
    }

    private void StartFileReceiver()
    {
        int filePort = 7070;
        TcpListener fileListener = new TcpListener(IPAddress.Any, filePort);
        fileListener.Start();

        while (true)
        {
            try
            {
                using var client = fileListener.AcceptTcpClient();
                using var stream = client.GetStream();

                byte[] nameLenBuffer = new byte[4];
                stream.Read(nameLenBuffer, 0, 4);
                int nameLen = BitConverter.ToInt32(nameLenBuffer, 0);

                byte[] nameBuffer = new byte[nameLen];
                stream.Read(nameBuffer, 0, nameLen);
                string fileName = Encoding.UTF8.GetString(nameBuffer);

                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "install");
                Directory.CreateDirectory(folder);
                string savePath = Path.Combine(folder, fileName);

                using var fs = new FileStream(savePath, FileMode.Create);
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    fs.Write(buffer, 0, bytesRead);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ File receiver error: {ex.Message}");
            }
        }
    }

    private int GetCpuUsage()
    {
        var searcher = new ManagementObjectSearcher("select LoadPercentage from Win32_Processor");
        foreach (var obj in searcher.Get())
            return Convert.ToInt32(obj["LoadPercentage"]);
        return 0;
    }

    private int GetRamUsage()
    {
        var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem");
        foreach (var obj in searcher.Get())
        {
            ulong free = Convert.ToUInt64(obj["FreePhysicalMemory"]);
            ulong total = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
            return (int)(100 - ((free * 100) / total));
        }
        return 0;
    }

    private int GetDiskUsage(string driveLetter)
    {
        var searcher = new ManagementObjectSearcher($"SELECT FreeSpace, Size FROM Win32_LogicalDisk WHERE DeviceID='{driveLetter}'");
        foreach (var obj in searcher.Get())
        {
            ulong free = Convert.ToUInt64(obj["FreeSpace"]);
            ulong size = Convert.ToUInt64(obj["Size"]);
            return (int)(100 - ((free * 100) / size));
        }
        return 0;
    }

    private string GetLocalIPAddress()
    {
        foreach (var addr in Dns.GetHostAddresses(Dns.GetHostName()))
            if (addr.AddressFamily == AddressFamily.InterNetwork)
                return addr.ToString();
        return "127.0.0.1";
    }
   
    private void StartMirrorListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, MIRROR_PORT);
        listener.Start();

        while (true)
        {
            try
            {
                var client = listener.AcceptTcpClient();
                _ = Task.Run(() => HandleMirrorStream(client));
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Mirror listener error: " + ex.Message);
            }
        }
    }
    private async Task HandleMirrorStream(TcpClient client)
    {
        //Application.OpenForms[0]?.Invoke(() =>
        //{
        //    if (_mirrorForm == null || _mirrorForm.IsDisposed)
        //    {
        //        _mirrorForm = new MirrorDisplayForm();
        //        _mirrorForm.Show();
        //    }
        //});

        //for (int i = 0; i < 5; i++)
        //{
        //    var bmp = new Bitmap(400, 300);
        //    using (Graphics g = Graphics.FromImage(bmp))
        //    {
        //        g.Clear(Color.Black);
        //        g.DrawString("Test " + i, new Font("Arial", 20), Brushes.White, new PointF(50, 130));
        //    }

        //    using var ms = new MemoryStream();
        //    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
        //    var jpeg = ms.ToArray();

        //    Application.OpenForms[0]?.BeginInvoke(() =>
        //    {
        //        _mirrorForm?.UpdateImage(jpeg);
        //    });

        //    await Task.Delay(1000);
        //}
        Application.OpenForms[0]?.Invoke(() =>
        {
            if (_mirrorForm == null || _mirrorForm.IsDisposed)
            {
                _mirrorForm = new MirrorDisplayForm();
                _mirrorForm.Show();
            }
        });

        using var stream = client.GetStream();

        while (true)
        {
            var lenBuffer = new byte[4];
            int readLen = await stream.ReadAsync(lenBuffer, 0, 4);
            if (readLen == 0) break;

            int len = BitConverter.ToInt32(lenBuffer, 0);
            if (len <= 0 || len > 10_000_000) break;

            var jpegBuffer = new byte[len];
            int totalRead = 0;
            while (totalRead < len)
            {
                int read = await stream.ReadAsync(jpegBuffer, totalRead, len - totalRead);
                if (read == 0) return;
                totalRead += read;
            }

            Application.OpenForms[0]?.BeginInvoke(() =>
            {
                _mirrorForm?.UpdateImage(jpegBuffer);
            });

            await Task.Delay(60);
        }
    }



    private void ConnectToMirrorServer()
    {
        try
        {
            var client = new TcpClient();
            client.Connect(serverIp, MIRROR_PORT); // ← وصل می‌شه به سرور روی 9091
            _ = Task.Run(() => HandleMirrorStream(client));
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Could not connect to mirror server: " + ex.Message);
        }
    }
    private async Task<int> ReadWithTimeout(NetworkStream stream, byte[] buffer, int offset, int count, int timeoutMs)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            return await stream.ReadAsync(buffer, offset, count, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("⏱️ Read timed out");
            return 0;
        }
    }
    private void SendInstallResult(string result)
    {
        try
        {
            var data = new
            {
                IPAddress = GetLocalIPAddress(),
                Setup = "silent_install.bat",
                Result = result
            };

            string json = JsonSerializer.Serialize(data);

            using var client = new TcpClient();
            client.Connect(serverIp, 5056);

            using var stream = client.GetStream();
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            stream.Write(buffer, 0, buffer.Length);

            Console.WriteLine($"📤 Sent install result to server: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending install result: {ex.Message}");
        }
    }
    private void StartControlListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 9092);
        listener.Start();

        while (true)
        {
            var client = listener.AcceptTcpClient();
            _ = Task.Run(() => HandleControlCommands(client));
        }
    }
    private async Task HandleControlCommands(TcpClient client)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) break;

            if (line.StartsWith("MOVE:"))
            {
                var parts = line.Substring(5).Split(',');
                int x = int.Parse(parts[0]);
                int y = int.Parse(parts[1]);
                Cursor.Position = new System.Drawing.Point(x, y);
            }
            else if (line.StartsWith("MOUSE:"))
            {
                var parts = line.Substring(6).Split(',');
                int x = int.Parse(parts[0]);
                int y = int.Parse(parts[1]);
                string btn = parts[2];

                Cursor.Position = new System.Drawing.Point(x, y);

                if (btn == "Left")
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, x, y, 0, 0);
                else if (btn == "Right")
                    mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, x, y, 0, 0);
            }
            else if (line.StartsWith("KEY:"))
            {
                string keyName = line.Substring(4).ToUpperInvariant();

                string mappedKey = keyName switch
                {
                    "BACK" => "{BACKSPACE}",
                    "ENTER" => "{ENTER}",
                    "ESCAPE" => "{ESC}",
                    "TAB" => "{TAB}",
                    "SPACE" => " ",
                    "LEFT" => "{LEFT}",
                    "RIGHT" => "{RIGHT}",
                    "UP" => "{UP}",
                    "DOWN" => "{DOWN}",
                    _ => keyName.Length == 1 ? keyName : $"{{{keyName}}}"
                };

                SendKeys.SendWait(mappedKey);
            }
        }
    }





}
