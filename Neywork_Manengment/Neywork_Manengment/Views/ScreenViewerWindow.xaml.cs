using MahApps.Metro.Controls;
using Neywork_Manengment.Services;
using System;
using System.IO;
using System.Net.Sockets;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Neywork_Manengment.Views
{
    public partial class ScreenViewerWindow : MetroWindow
    {
        private readonly ScreenMirrorService _service;
        private readonly string _clientIp;

        private TcpClient _controlClient;
        private NetworkStream _controlStream;

        public ScreenViewerWindow(ScreenMirrorService service, string clientIp)
        {
            InitializeComponent();
            _service = service;
            _clientIp = clientIp;

            _service.FrameReceived += OnFrame;
            Loaded += async (_, __) =>
            {
                await _service.StartPullAsync(_clientIp);
                ConnectControlChannel();
            };
            Closed += (_, __) =>
            {
                _service.StopPull(_clientIp);
                _controlStream?.Close();
                _controlClient?.Close();
            };

            RemoteImage.MouseDown += RemoteImage_MouseDown;
            RemoteImage.MouseMove += RemoteImage_MouseMove;
            this.KeyDown += ScreenViewerWindow_KeyDown;
        }

        private void OnFrame(BitmapImage img)
        {
            RemoteImage.Source = img;
        }

        private void ConnectControlChannel()
        {
            _controlClient = new TcpClient();
            _controlClient.Connect(_clientIp, 9092);
            _controlStream = _controlClient.GetStream();
        }

        private void SendControlCommand(string command)
        {
            if (_controlStream != null)
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(command + "\n");
                _controlStream.Write(data, 0, data.Length);
            }
        }

        private void RemoteImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(RemoteImage);
            SendControlCommand($"MOUSE:{(int)pos.X},{(int)pos.Y},{e.ChangedButton}");
        }

        private void RemoteImage_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(RemoteImage);
            SendControlCommand($"MOVE:{(int)pos.X},{(int)pos.Y}");
        }

        private void ScreenViewerWindow_KeyDown(object sender, KeyEventArgs e)
        {
            SendControlCommand($"KEY:{e.Key.ToString()}");
        }
    }
}
