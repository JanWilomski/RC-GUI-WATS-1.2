// Services/PositionsService.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class PositionsService
    {
        private RcTcpClientService _clientService;
        private Dictionary<string, Position> _positionsByIsin = new Dictionary<string, Position>();
        private ObservableCollection<Position> _positions = new ObservableCollection<Position>();
        private readonly Dispatcher _dispatcher;

        public ObservableCollection<Position> Positions => _positions;

        public PositionsService(RcTcpClientService clientService)
        {
            _clientService = clientService;
            _clientService.MessageReceived += ProcessMessage;
            _dispatcher = Application.Current.Dispatcher;
        }

        private void ProcessMessage(RcMessage message)
        {
            foreach (var block in message.Blocks)
            {
                if (block.Payload.Length > 0 && (char)block.Payload[0] == 'P')
                {
                    ProcessPositionMessage(block.Payload);
                }
            }
        }

        private void ProcessPositionMessage(byte[] payload)
        {
            if (payload.Length < 25)
                return;

            string isin = Encoding.ASCII.GetString(payload, 1, 12).Trim('\0');
            int net = BitConverter.ToInt32(payload, 13);
            int openLong = BitConverter.ToInt32(payload, 17);
            int openShort = BitConverter.ToInt32(payload, 21);

            if (string.IsNullOrWhiteSpace(isin))
                return;

            // Update positions on UI thread
            _dispatcher.Invoke(() =>
            {
                bool isUpdated = false;
                
                if (_positionsByIsin.TryGetValue(isin, out Position existingPosition))
                {
                    if (existingPosition.Net != net || existingPosition.OpenLong != openLong || existingPosition.OpenShort != openShort)
                    {
                        existingPosition.Net = net;
                        existingPosition.OpenLong = openLong;
                        existingPosition.OpenShort = openShort;
                        isUpdated = true;
                    }
                }
                else
                {
                    var newPosition = new Position
                    {
                        ISIN = isin,
                        Ticker = GetTickerFromIsin(isin),
                        Name = GetNameFromIsin(isin),
                        Net = net,
                        OpenLong = openLong,
                        OpenShort = openShort
                    };

                    _positions.Add(newPosition);
                    _positionsByIsin[isin] = newPosition;
                    isUpdated = true;
                }
            });
        }

        private string GetTickerFromIsin(string isin)
        {
            // Implement based on original code
            switch (isin)
            {
                case "PLBZ00000044": return "SPL";
                case "PLZATRM00012": return "ATT";
                // Additional mappings...
                default: return isin.Substring(Math.Max(0, isin.Length - 3));
            }
        }

        private string GetNameFromIsin(string isin)
        {
            // Implement based on original code
            switch (isin)
            {
                case "PLBZ00000044": return "SANPL";
                case "PLZATRM00012": return "GRUPAAZOTY";
                // Additional mappings...
                default: return "Unknown";
            }
        }

        public void Clear()
        {
            _dispatcher.Invoke(() =>
            {
                _positions.Clear();
                _positionsByIsin.Clear();
            });
        }
    }
}