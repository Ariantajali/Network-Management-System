using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Neywork_Manengment.Models
{
    public class ClientModel : INotifyPropertyChanged
    {
        private string systemName;
        private string ipAddress;
        private string status;
        private int cpu;
        private int ram;
        private int disk;
        private string statusIcon;
        private string statusColor;
        private DateTime lastUpdateTime;
        private bool isSelected;
        private string installStatus = "Waiting for installation...";

        public string SystemName
        {
            get => systemName;
            set { systemName = value; OnPropertyChanged(); }
        }

        public string IPAddress
        {
            get => ipAddress;
            set { ipAddress = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(); }
        }

        public int CPU
        {
            get => cpu;
            set { cpu = value; OnPropertyChanged(); }
        }

        public int RAM
        {
            get => ram;
            set { ram = value; OnPropertyChanged(); }
        }

        public int Disk
        {
            get => disk;
            set { disk = value; OnPropertyChanged(); }
        }

        public string StatusIcon
        {
            get => statusIcon;
            set { statusIcon = value; OnPropertyChanged(); }
        }

        public string StatusColor
        {
            get => statusColor;
            set { statusColor = value; OnPropertyChanged(); }
        }

        public DateTime LastUpdateTime
        {
            get => lastUpdateTime;
            set { lastUpdateTime = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => isSelected;
            set { isSelected = value; OnPropertyChanged(); }
        }

        public string InstallStatus
        {
            get => installStatus;
            set { installStatus = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class InstallResultModel
    {
        public string IPAddress { get; set; }
        public string Setup { get; set; }
        public string Result { get; set; }
    }
}
