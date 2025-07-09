﻿// Services/HeartbeatMonitorService.cs - Thread-safe version
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public enum HeartbeatStatus
    {
        Connected,      // Green - receiving heartbeats
        Warning,        // Yellow - missed one heartbeat
        Disconnected    // Red - missed two or more heartbeats
    }

    public class HeartbeatMonitorService
    {
        private readonly RcTcpClientService _clientService;
        private readonly ThemeService _themeService;
        private readonly SettingsService _settingsService;
        private Timer _heartbeatTimer;
        private DateTime _lastHeartbeatReceived;
        private int _missedHeartbeats;
        private HeartbeatStatus _currentStatus;

        // Expected heartbeat interval from documentation (1 second)
        private const int HEARTBEAT_INTERVAL_MS = 1000;
        // Check interval - slightly more frequent than expected heartbeat
        private const int CHECK_INTERVAL_MS = 1200;
        
        public event Action<HeartbeatStatus> HeartbeatStatusChanged;
        
        public HeartbeatStatus CurrentStatus 
        { 
            get => _currentStatus; 
            private set
            {
                if (_currentStatus != value)
                {
                    _currentStatus = value;
                    
                    // Safely invoke event on UI thread
                    try
                    {
                        if (Application.Current?.Dispatcher?.CheckAccess() == true)
                        {
                            // Already on UI thread
                            HeartbeatStatusChanged?.Invoke(_currentStatus);
                        }
                        else
                        {
                            // Dispatch to UI thread
                            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                            {
                                HeartbeatStatusChanged?.Invoke(_currentStatus);
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error invoking HeartbeatStatusChanged: {ex.Message}");
                    }
                }
            }
        }

        public HeartbeatMonitorService(RcTcpClientService clientService, ThemeService themeService, SettingsService settingsService)
        {
            _clientService = clientService;
            _themeService = themeService;
            _settingsService = settingsService;

            _clientService.MessageReceived += OnMessageReceived;
            _clientService.ConnectionStatusChanged += OnConnectionStatusChanged;
            this.HeartbeatStatusChanged += OnHeartbeatStatusChanged; // Subscribe to its own event

            _currentStatus = HeartbeatStatus.Disconnected;
            _missedHeartbeats = 0;
        }

        private void OnHeartbeatStatusChanged(HeartbeatStatus status)
        {
            if (status == HeartbeatStatus.Disconnected)
            {
                _themeService.ApplyTheme("Red");
            }
            else
            {
                _themeService.ApplyTheme(_settingsService.ThemeName);
            }
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            if (isConnected)
            {
                StartMonitoring();
            }
            else
            {
                StopMonitoring();
                CurrentStatus = HeartbeatStatus.Disconnected;
                _missedHeartbeats = 0;
            }
        }

        private void OnMessageReceived(RcMessage message)
        {
            // Check if this is a heartbeat message
            // According to documentation: "A message consisting of just a header is a heartbeat"
            if (message.Header.BlockCount == 0)
            {
                OnHeartbeatReceived();
            }
        }

        private void OnHeartbeatReceived()
        {
            _lastHeartbeatReceived = DateTime.Now;
            _missedHeartbeats = 0;
            CurrentStatus = HeartbeatStatus.Connected;
        }

        private void StartMonitoring()
        {
            StopMonitoring(); // Stop any existing timer
            
            _lastHeartbeatReceived = DateTime.Now;
            _missedHeartbeats = 0;
            CurrentStatus = HeartbeatStatus.Connected;
            
            _heartbeatTimer = new Timer(CheckHeartbeat, null, CHECK_INTERVAL_MS, CHECK_INTERVAL_MS);
        }

        private void StopMonitoring()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        private void CheckHeartbeat(object state)
        {
            if (!_clientService.IsConnected)
            {
                return;
            }

            var timeSinceLastHeartbeat = DateTime.Now - _lastHeartbeatReceived;
            
            // If we haven't received a heartbeat in more than expected interval
            if (timeSinceLastHeartbeat.TotalMilliseconds > HEARTBEAT_INTERVAL_MS + 500) // 500ms tolerance
            {
                _missedHeartbeats++;
                
                if (_missedHeartbeats == 1)
                {
                    CurrentStatus = HeartbeatStatus.Warning;
                }
                else if (_missedHeartbeats >= 2)
                {
                    CurrentStatus = HeartbeatStatus.Disconnected;
                }
            }
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }
}