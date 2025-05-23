using System;
using System.Windows.Media;
using System.Windows.Threading;
using RC_GUI_WATS.Services;

namespace RC_GUI_WATS.ViewModels
{
    public class HeartbeatIndicatorViewModel : BaseViewModel
    {
        private readonly HeartbeatMonitorService _heartbeatMonitor;
        private readonly DispatcherTimer _updateTimer;
        private DateTime _lastHeartbeatTime;

        private Brush _statusBrush;
        public Brush StatusBrush
        {
            get => _statusBrush;
            set => SetProperty(ref _statusBrush, value);
        }

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _lastHeartbeatText;
        public string LastHeartbeatText
        {
            get => _lastHeartbeatText;
            set => SetProperty(ref _lastHeartbeatText, value);
        }

        public HeartbeatIndicatorViewModel(HeartbeatMonitorService heartbeatMonitor)
        {
            _heartbeatMonitor = heartbeatMonitor;
            _heartbeatMonitor.HeartbeatStatusChanged += OnHeartbeatStatusChanged;
            
            // Timer to update the "last heartbeat" time display
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateLastHeartbeatDisplay;
            _updateTimer.Start();
            
            // Initialize with current status
            OnHeartbeatStatusChanged(_heartbeatMonitor.CurrentStatus);
        }

        private void OnHeartbeatStatusChanged(HeartbeatStatus status)
        {
            switch (status)
            {
                case HeartbeatStatus.Connected:
                    StatusBrush = Brushes.Green;
                    StatusText = "RC Online";
                    _lastHeartbeatTime = DateTime.Now;
                    break;
                    
                case HeartbeatStatus.Warning:
                    StatusBrush = Brushes.Orange;
                    StatusText = "RC Warning";
                    break;
                    
                case HeartbeatStatus.Disconnected:
                    StatusBrush = Brushes.Red;
                    StatusText = "RC Offline";
                    break;
            }
            
            UpdateLastHeartbeatDisplay(null, null);
        }

        private void UpdateLastHeartbeatDisplay(object sender, EventArgs e)
        {
            if (_heartbeatMonitor.CurrentStatus == HeartbeatStatus.Connected)
            {
                var elapsed = DateTime.Now - _lastHeartbeatTime;
                if (elapsed.TotalSeconds < 60)
                {
                    LastHeartbeatText = $"({elapsed.TotalSeconds:F0}s)";
                }
                else
                {
                    LastHeartbeatText = $"({elapsed.TotalMinutes:F0}m)";
                }
            }
            else if (_heartbeatMonitor.CurrentStatus == HeartbeatStatus.Warning)
            {
                var elapsed = DateTime.Now - _lastHeartbeatTime;
                LastHeartbeatText = $"({elapsed.TotalSeconds:F0}s ago)";
            }
            else
            {
                LastHeartbeatText = "";
            }
        }

        public void Dispose()
        {
            _updateTimer?.Stop();
            _heartbeatMonitor.HeartbeatStatusChanged -= OnHeartbeatStatusChanged;
        }
    }
}