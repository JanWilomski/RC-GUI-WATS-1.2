using System;
using System.Threading.Tasks;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class RcTcpClientService
    {
        private RcTcpClient _client;
        
        public bool IsConnected => _client?.IsConnected ?? false;

        public event Action<bool> ConnectionStatusChanged;
        public event Action<RcMessage> MessageReceived;
        
        public RcTcpClientService()
        {
            _client = new RcTcpClient();
            _client.ConnectionStatusChanged += OnConnectionStatusChanged;
            _client.MessageReceived += OnMessageReceived;
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            ConnectionStatusChanged?.Invoke(isConnected);
        }

        private void OnMessageReceived(RcMessage message)
        {
            MessageReceived?.Invoke(message);
        }

        public async Task ConnectAsync(string serverIp, int serverPort)
        {
            await _client.ConnectAsync(serverIp, serverPort);
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        public async Task SendMessageAsync(RcMessage message)
        {
            await _client.SendMessageAsync(message);
        }

        public async Task SendSetControlAsync(string controlString)
        {
            await _client.SendSetControlAsync(controlString);
        }

        public async Task SendGetControlsHistoryAsync()
        {
            await _client.SendGetControlsHistoryAsync();
        }

        public async Task SendRewindAsync(uint lastSeenSequence = 0)
        {
            await _client.SendRewindAsync(lastSeenSequence);
        }
    }
}