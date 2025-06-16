// ViewModels/HeartbeatIndicatorViewModel.cs
using System;
using System.Windows.Media;
using RC_GUI_WATS.Services;

namespace RC_GUI_WATS.ViewModels
{
    public class HeartbeatIndicatorViewModel : BaseViewModel
    {
        private readonly HeartbeatMonitorService _heartbeatMonitor;

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private Brush _statusBrush;
        public Brush StatusBrush
        {
            get => _statusBrush;
            set => SetProperty(ref _statusBrush, value);
        }

        private string _lastHeartbeatText;
        public string LastHeartbeatText
        {
            get => _lastHeartbeatText;
            set => SetProperty(ref _lastHeartbeatText, value);
        }

        private DateTime _lastHeartbeatTime;

        public HeartbeatIndicatorViewModel(HeartbeatMonitorService heartbeatMonitor)
        {
            _heartbeatMonitor = heartbeatMonitor;
            _heartbeatMonitor.HeartbeatStatusChanged += OnHeartbeatStatusChanged;
            
            // Initialize with current status
            UpdateDisplay(_heartbeatMonitor.CurrentStatus);
        }

        private void OnHeartbeatStatusChanged(HeartbeatStatus status)
        {
            // Use safer approach for UI thread dispatching
            try
            {
                if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == true)
                {
                    // We're already on UI thread
                    UpdateDisplay(status);
                }
                else
                {
                    // We need to dispatch to UI thread
                    System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new System.Action(() =>
                    {
                        UpdateDisplay(status);
                    }));
                }
            }
            catch (Exception ex)
            {
                // Fallback - log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error updating heartbeat status: {ex.Message}");
            }
        }

        private void UpdateDisplay(HeartbeatStatus status)
        {
            switch (status)
            {
                case HeartbeatStatus.Connected:
                    StatusText = "Connected";
                    StatusBrush = Brushes.Green;
                    _lastHeartbeatTime = DateTime.Now;
                    LastHeartbeatText = "";
                    break;
                    
                case HeartbeatStatus.Warning:
                    StatusText = "Warning";
                    StatusBrush = Brushes.Orange;
                    UpdateLastHeartbeatText();
                    break;
                    
                case HeartbeatStatus.Disconnected:
                    StatusText = "Disconnected";
                    StatusBrush = Brushes.Red;
                    UpdateLastHeartbeatText();
                    break;
                    
                default:
                    StatusText = "Unknown";
                    StatusBrush = Brushes.Gray;
                    LastHeartbeatText = "";
                    break;
            }
        }

        private void UpdateLastHeartbeatText()
        {
            if (_lastHeartbeatTime != default)
            {
                var elapsed = DateTime.Now - _lastHeartbeatTime;
                LastHeartbeatText = $"({elapsed.TotalSeconds:F0}s ago)";
            }
            else
            {
                LastHeartbeatText = "";
            }
        }
    }
}