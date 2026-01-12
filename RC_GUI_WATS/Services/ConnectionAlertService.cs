// Services/ConnectionAlertService.cs - System alertów o rozłączeniu
using System;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using RC_GUI_WATS.Services;

namespace RC_GUI_WATS.Services
{
    public class ConnectionAlertService
    {
        private readonly RcTcpClientService _clientService;
        private readonly HeartbeatMonitorService _heartbeatMonitor;
        private readonly ConfigurationService _configService;
        private bool _lastConnectionState = false;
        private bool _hasShownDisconnectionAlert = false;

        public ConnectionAlertService(
            RcTcpClientService clientService, 
            HeartbeatMonitorService heartbeatMonitor,
            ConfigurationService configService)
        {
            _clientService = clientService;
            _heartbeatMonitor = heartbeatMonitor;
            _configService = configService;
            
            // Subscribe to connection status changes
            _clientService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _heartbeatMonitor.HeartbeatStatusChanged += OnHeartbeatStatusChanged;
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            // Only show alert when transitioning from connected to disconnected
            if (_lastConnectionState && !isConnected)
            {
                ShowConnectionLostAlert();
            }
            else if (!_lastConnectionState && isConnected)
            {
                ShowConnectionRestoredAlert();
            }
            
            _lastConnectionState = isConnected;
        }

        private void OnHeartbeatStatusChanged(HeartbeatStatus status)
        {
            // Show alert when heartbeat status becomes disconnected
            if (status == HeartbeatStatus.Disconnected && !_hasShownDisconnectionAlert)
            {
                ShowHeartbeatLostAlert();
                _hasShownDisconnectionAlert = true;
            }
            else if (status == HeartbeatStatus.Connected)
            {
                _hasShownDisconnectionAlert = false;
            }
        }

        private void ShowConnectionLostAlert()
        {
            Task.Run(() =>
            {
                try
                {
                    // Play system sound
                    SystemSounds.Exclamation.Play();
                    
                    // Show message box on UI thread
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        var result = MessageBox.Show(
                            $"Utracono połączenie z serwerem RC!\n\n" +
                            $"Serwer: {_configService.ServerIP}:{_configService.ServerPort}\n" +
                            $"Czas: {DateTime.Now:HH:mm:ss}\n\n" +
                            "Czy chcesz spróbować ponownie połączyć?",
                            "Błąd połączenia RC",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            // Attempt to reconnect
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await _clientService.ConnectAsync(_configService.ServerIP, _configService.ServerPort);
                                }
                                catch (Exception ex)
                                {
                                    Application.Current?.Dispatcher?.Invoke(() =>
                                    {
                                        MessageBox.Show(
                                            $"Nie udało się ponownie połączyć:\n{ex.Message}",
                                            "Błąd ponownego połączenia",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Error);
                                    });
                                }
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing connection lost alert: {ex.Message}");
                }
            });
        }

        private void ShowConnectionRestoredAlert()
        {
            Task.Run(() =>
            {
                try
                {
                    // Play system sound
                    SystemSounds.Asterisk.Play();
                    
                    // Show brief notification
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Połączenie z serwerem RC zostało przywrócone!\n\n" +
                            $"Serwer: {_configService.ServerIP}:{_configService.ServerPort}\n" +
                            $"Czas: {DateTime.Now:HH:mm:ss}",
                            "Połączenie przywrócone",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing connection restored alert: {ex.Message}");
                }
            });
        }

        private void ShowHeartbeatLostAlert()
        {
            Task.Run(() =>
            {
                try
                {
                    // Play system sound
                    SystemSounds.Hand.Play();
                    
                    // Show warning notification
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Utracono heartbeat z serwerem RC!\n\n" +
                            $"Serwer może być niedostępny lub przeciążony.\n" +
                            $"Czas: {DateTime.Now:HH:mm:ss}\n\n" +
                            "Sprawdź połączenie sieciowe i status serwera.",
                            "Ostrzeżenie Heartbeat",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing heartbeat lost alert: {ex.Message}");
                }
            });
        }
    }
}