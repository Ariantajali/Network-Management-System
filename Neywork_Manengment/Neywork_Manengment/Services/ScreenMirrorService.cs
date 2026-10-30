using Neywork_Manengment.Network;
using Neywork_Manengment.Utilities;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Neywork_Manengment.Services
{
    public sealed class ScreenMirrorService : IDisposable
    {
        public const int PullPort = 9090;
        public const int MirrorPort = 9091;

        private readonly ConcurrentDictionary<string, TcpClient> _mirrorTargets = new();
        private readonly ServerSocketManager _socketMgr;

        private CancellationTokenSource? _pullCts;
        private CancellationTokenSource? _mirrorCts;

        public event Action<BitmapImage>? FrameReceived;

        public ScreenMirrorService(ServerSocketManager socketMgr)
        {
            _socketMgr = socketMgr;
        }

        //  Pull از کلاینت
        public async Task StartPullAsync(string clientIp)
        {
            StopPull(clientIp);
            _pullCts = new CancellationTokenSource();
            await _socketMgr.SendCommandAsync("SCREEN_START", clientIp);
            _ = Task.Run(() => AcceptFramesLoopAsync(_pullCts.Token));
        }

        private async Task AcceptFramesLoopAsync(CancellationToken ct)
        {
            var listener = new TcpListener(IPAddress.Any, PullPort);
            listener.Start();

            using var tcp = await listener.AcceptTcpClientAsync(ct);
            using var ns = tcp.GetStream();

            while (!ct.IsCancellationRequested)
            {
                var len = BitConverter.ToInt32(await ns.ReadExactAsync(4, ct));
                var jpeg = await ns.ReadExactAsync(len, ct);

                App.Current.Dispatcher.Invoke(() =>
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = new MemoryStream(jpeg);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    img.Freeze();
                    FrameReceived?.Invoke(img);
                });
            }
        }

        public void StopPull(string clientIp)
        {
            _pullCts?.Cancel();
            _socketMgr.SendCommandAsync("SCREEN_STOP", clientIp);
        }

        
        public void StartMirror()
        {
            StopMirror();
            _mirrorCts = new CancellationTokenSource();

            
            _ = Task.Run(() => AcceptClientsLoopAsync(_mirrorCts.Token));

            
            Thread.Sleep(500); 

            
            BroadcastCommand("MIRROR_ON");

           
            _ = Task.Run(() => PushFramesLoopAsync(_mirrorCts.Token));
        }

        public void StopMirror()
        {
            _mirrorCts?.Cancel();
            BroadcastCommand("MIRROR_OFF");
            _mirrorTargets.Clear();
        }

        private async Task AcceptClientsLoopAsync(CancellationToken ct)
        {
            var listener = new TcpListener(IPAddress.Any, MirrorPort);
            listener.Start();

            while (!ct.IsCancellationRequested)
            {
                var cli = await listener.AcceptTcpClientAsync(ct);
                _mirrorTargets[cli.Client.RemoteEndPoint!.ToString()!] = cli;
            }
        }

        private async Task PushFramesLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                byte[] jpeg = ScreenCaptureHelper.CaptureScreenJpeg();
                byte[] len = BitConverter.GetBytes(jpeg.Length);


                foreach (var kvp in _mirrorTargets.ToArray())
                {
                    try
                    {
                        var ns = kvp.Value.GetStream();


                        await ns.WriteAsync(len, 0, 4, ct);
                        await ns.WriteAsync(jpeg, 0, jpeg.Length, ct);
                    }
                    catch
                    {
                        _mirrorTargets.TryRemove(kvp.Key, out _);
                    }
                }

                await Task.Delay(50, ct);
            }
        }


        private void BroadcastCommand(string cmd)
        {
            foreach (var ip in _socketMgr.GetClients().Keys)
            {
                _socketMgr.SendCommandAsync(cmd, ip);
            }
        }

        public void Dispose()
        {
            _pullCts?.Cancel();
            _mirrorCts?.Cancel();
        }
    }
}
