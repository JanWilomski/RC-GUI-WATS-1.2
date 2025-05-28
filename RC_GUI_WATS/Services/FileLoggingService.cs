// Services/FileLoggingService.cs
using System;
using System.IO;
using System.Threading.Tasks;

namespace RC_GUI_WATS.Services
{
    public class FileLoggingService
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();
        private const int MAX_LOG_FILE_SIZE = 10 * 1024 * 1024; // 10 MB

        public FileLoggingService()
        {
            // Create logs directory if it doesn't exist
            string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logsDirectory);
            
            // Create log file with current date
            string logFileName = $"RC_GUI_WATS_{DateTime.Now:yyyyMMdd}.log";
            _logFilePath = Path.Combine(logsDirectory, logFileName);
        }

        public void LogMessage(string message, LogLevel level = LogLevel.Info)
        {
            Task.Run(() => WriteLogAsync(message, level));
        }

        public void LogRawMessage(Models.RcMessage message)
        {
            var logText = FormatRawMessage(message);
            LogMessage(logText, LogLevel.Debug);
        }

        public void LogError(string message, Exception exception = null)
        {
            var errorMessage = exception != null 
                ? $"{message} - Exception: {exception.Message}\nStackTrace: {exception.StackTrace}"
                : message;
            LogMessage(errorMessage, LogLevel.Error);
        }

        private async Task WriteLogAsync(string message, LogLevel level)
        {
            try
            {
                lock (_lockObject)
                {
                    // Check if file is too large and needs rotation
                    if (File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length > MAX_LOG_FILE_SIZE)
                    {
                        RotateLogFile();
                    }

                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logFilePath, logEntry);
                }
            }
            catch (Exception ex)
            {
                // Log to console if file logging fails
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
                Console.WriteLine($"Original message: {message}");
            }
        }

        private void RotateLogFile()
        {
            try
            {
                string backupFileName = Path.ChangeExtension(_logFilePath, $".{DateTime.Now:HHmmss}.log");
                File.Move(_logFilePath, backupFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to rotate log file: {ex.Message}");
            }
        }

        private string FormatRawMessage(Models.RcMessage message)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"--- RC Message {DateTime.Now:HH:mm:ss.fff} ---");
            sb.AppendLine($"Session: {message.Header.Session}, Sequence: {message.Header.SequenceNumber}, Blocks: {message.Header.BlockCount}");
            
            int blockIndex = 0;
            foreach (var block in message.Blocks)
            {
                sb.AppendLine($"Block {blockIndex++} | Length: {block.Length}");
                
                if (block.Payload.Length > 0)
                {
                    char type = (char)block.Payload[0];
                    sb.AppendLine($"Type: {type}");
                    
                    // Show raw data in hexadecimal (limited to first 50 bytes for file size)
                    var dataToShow = block.Payload.Length > 50 
                        ? new byte[50] 
                        : block.Payload;
                    
                    if (block.Payload.Length > 50)
                    {
                        Array.Copy(block.Payload, dataToShow, 50);
                        sb.AppendLine($"Data (first 50 bytes): {BitConverter.ToString(dataToShow)}...");
                    }
                    else
                    {
                        sb.AppendLine($"Data: {BitConverter.ToString(dataToShow)}");
                    }
                    
                    // Try to show as ASCII text for readable messages
                    try
                    {
                        if (type == 'I' || type == 'W' || type == 'E' || type == 'D')
                        {
                            // Log messages - extract text content
                            if (block.Payload.Length >= 3)
                            {
                                ushort msgLength = BitConverter.ToUInt16(block.Payload, 1);
                                if (block.Payload.Length >= 3 + msgLength && msgLength > 0)
                                {
                                    string text = System.Text.Encoding.ASCII.GetString(block.Payload, 3, msgLength);
                                    sb.AppendLine($"Message: {text}");
                                }
                            }
                        }
                        else if (type == 'S')
                        {
                            // Control messages - extract control string
                            if (block.Payload.Length >= 3)
                            {
                                ushort msgLength = BitConverter.ToUInt16(block.Payload, 1);
                                if (block.Payload.Length >= 3 + msgLength && msgLength > 0)
                                {
                                    string control = System.Text.Encoding.ASCII.GetString(block.Payload, 3, msgLength);
                                    sb.AppendLine($"Control: {control}");
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore text extraction errors
                    }
                }
                
                sb.AppendLine();
            }
            
            return sb.ToString();
        }

        public string GetLogFilePath() => _logFilePath;

        public void LogControlLimit(string action, string controlString)
        {
            LogMessage($"CONTROL_LIMIT: {action} - {controlString}", LogLevel.Info);
        }

        public void LogConnection(string action, string details = "")
        {
            LogMessage($"CONNECTION: {action}" + (string.IsNullOrEmpty(details) ? "" : $" - {details}"), LogLevel.Info);
        }

        public void LogSettings(string action, string details = "")
        {
            LogMessage($"SETTINGS: {action}" + (string.IsNullOrEmpty(details) ? "" : $" - {details}"), LogLevel.Info);
        }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}