using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class RcTcpClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;
        private string _serverIp;
        private int _serverPort;
        private bool _isConnected;

        public delegate void MessageReceivedEventHandler(RcMessage message);
        public event MessageReceivedEventHandler MessageReceived;

        public delegate void ConnectionStatusChangedEventHandler(bool isConnected);
        public event ConnectionStatusChangedEventHandler ConnectionStatusChanged;

        public bool IsConnected
        {
            get { return _isConnected; }
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    ConnectionStatusChanged?.Invoke(_isConnected);
                }
            }
        }

        public async Task ConnectAsync(string serverIp, int serverPort)
        {
            if (IsConnected)
                return;

            _serverIp = serverIp;
            _serverPort = serverPort;
            
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_serverIp, _serverPort);
                _stream = _client.GetStream();
                
                _cancellationTokenSource = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token));
                
                IsConnected = true;
            }
            catch (Exception ex)
            {
                Disconnect();
                throw new Exception($"Nie można połączyć się z serwerem: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }

            if (_client != null)
            {
                _client.Close();
                _client = null;
            }

            IsConnected = false;
        }

        public async Task SendMessageAsync(RcMessage message)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Nie połączono z serwerem");

            byte[] data = message.ToBytes();
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            byte[] headerBuffer = new byte[RcHeader.HeaderSize];
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Odbieranie nagłówka
                    int headerBytesRead = 0;
                    while (headerBytesRead < RcHeader.HeaderSize)
                    {
                        int bytesRead = await _stream.ReadAsync(
                            headerBuffer, 
                            headerBytesRead, 
                            RcHeader.HeaderSize - headerBytesRead,
                            cancellationToken);
                        
                        if (bytesRead == 0)
                            throw new Exception("Serwer zakończył połączenie");
                        
                        headerBytesRead += bytesRead;
                    }
                    
                    // Parsowanie nagłówka
                    RcHeader header = RcHeader.FromBytes(headerBuffer);
                    
                    // Obliczanie całkowitego rozmiaru wiadomości
                    int messageSize = RcHeader.HeaderSize;
                    
                    // Jeśli to tylko heartbeat (bez bloków), nie trzeba czytać więcej danych
                    if (header.BlockCount == 0)
                    {
                        var heartbeatMessage = new RcMessage { Header = header };
                        MessageReceived?.Invoke(heartbeatMessage);
                        continue;
                    }
                    
                    // Odbieranie bloków
                    byte[] fullMessageBuffer = new byte[1024 * 1024]; // 1MB bufor na całą wiadomość
                    Array.Copy(headerBuffer, fullMessageBuffer, RcHeader.HeaderSize);
                    
                    int totalBytesRead = RcHeader.HeaderSize;
                    int remainingBytes = 0;
                    
                    // Czytamy każdy blok (najpierw długość bloku, potem jego zawartość)
                    for (int i = 0; i < header.BlockCount; i++)
                    {
                        // Odczytanie nagłówka bloku (2 bajty)
                        remainingBytes = RcMessageBlock.BlockHeaderSize;
                        while (remainingBytes > 0)
                        {
                            int bytesRead = await _stream.ReadAsync(
                                fullMessageBuffer, 
                                totalBytesRead, 
                                remainingBytes,
                                cancellationToken);
                            
                            if (bytesRead == 0)
                                throw new Exception("Serwer zakończył połączenie");
                            
                            totalBytesRead += bytesRead;
                            remainingBytes -= bytesRead;
                        }
                        
                        // Odczytanie długości bloku
                        ushort blockLength = BitConverter.ToUInt16(fullMessageBuffer, totalBytesRead - RcMessageBlock.BlockHeaderSize);
                        
                        // Odczytanie danych bloku
                        remainingBytes = blockLength;
                        while (remainingBytes > 0)
                        {
                            int bytesRead = await _stream.ReadAsync(
                                fullMessageBuffer, 
                                totalBytesRead, 
                                remainingBytes,
                                cancellationToken);
                            
                            if (bytesRead == 0)
                                throw new Exception("Serwer zakończył połączenie");
                            
                            totalBytesRead += bytesRead;
                            remainingBytes -= bytesRead;
                        }
                    }
                    
                    // Konstruowanie pełnej wiadomości
                    byte[] messageData = new byte[totalBytesRead];
                    Array.Copy(fullMessageBuffer, messageData, totalBytesRead);
                    
                    var message = RcMessage.FromBytes(messageData);
                    MessageReceived?.Invoke(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Normalne zamknięcie podczas anulowania
            }
            catch (Exception ex)
            {
                // Obsługa innych wyjątków
                Console.WriteLine($"Błąd podczas odbierania wiadomości: {ex.Message}");
            }
            finally
            {
                // Zapewnienie rozłączenia w przypadku błędu
                if (!cancellationToken.IsCancellationRequested)
                {
                    Disconnect();
                }
            }
        }

        // Metoda pomocnicza do tworzenia kontroli Set Control (S)
        public async Task SendSetControlAsync(string controlString)
        {
            var message = new RcMessage
            {
                Header = new RcHeader
                {
                    Session = "RCGUI",
                    SequenceNumber = 0
                }
            };

            var block = new RcMessageBlock();
            byte[] typeBytes = new byte[] { (byte)'S' };
            byte[] lengthBytes = BitConverter.GetBytes((ushort)controlString.Length);
            byte[] controlBytes = System.Text.Encoding.ASCII.GetBytes(controlString);
            
            block.Length = (ushort)(1 + 2 + controlString.Length);
            block.Payload = new byte[block.Length];
            
            typeBytes.CopyTo(block.Payload, 0);
            lengthBytes.CopyTo(block.Payload, 1);
            controlBytes.CopyTo(block.Payload, 3);
            
            message.Blocks.Add(block);
            
            await SendMessageAsync(message);
        }

        // Metoda pomocnicza do żądania historii kontroli (G)
        public async Task SendGetControlsHistoryAsync()
        {
            var message = new RcMessage
            {
                Header = new RcHeader
                {
                    Session = "RCGUI",
                    SequenceNumber = 0
                }
            };

            var block = new RcMessageBlock
            {
                Length = 1,
                Payload = new byte[] { (byte)'G' }
            };
            
            message.Blocks.Add(block);
            
            await SendMessageAsync(message);
        }

        // Metoda pomocnicza do żądania rewindowania wiadomości (R)
        public async Task SendRewindAsync(uint lastSeenSequence = 0)
        {
            var message = new RcMessage
            {
                Header = new RcHeader
                {
                    Session = "RCGUI",
                    SequenceNumber = 0
                }
            };

            var block = new RcMessageBlock
            {
                Length = 5,
                Payload = new byte[5]
            };
            
            block.Payload[0] = (byte)'R';
            BitConverter.GetBytes(lastSeenSequence).CopyTo(block.Payload, 1);
            
            message.Blocks.Add(block);
            
            await SendMessageAsync(message);
        }
    }
}