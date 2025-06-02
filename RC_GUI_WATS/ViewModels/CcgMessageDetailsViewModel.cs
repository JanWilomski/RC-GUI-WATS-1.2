// ViewModels/CcgMessageDetailsViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Windows;
using RC_GUI_WATS.Commands;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.Services;

namespace RC_GUI_WATS.ViewModels
{
    public class CcgMessageDetailsViewModel : BaseViewModel
    {
        private readonly CcgMessage _originalMessage;
        private readonly CcgMessageDetails _messageDetails;

        // Properties
        private string _windowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        private string _messageType;
        public string MessageType
        {
            get => _messageType;
            set => SetProperty(ref _messageType, value);
        }

        private string _messageName;
        public string MessageName
        {
            get => _messageName;
            set => SetProperty(ref _messageName, value);
        }

        private string _dateReceived;
        public string DateReceived
        {
            get => _dateReceived;
            set => SetProperty(ref _dateReceived, value);
        }

        private string _transactTime;
        public string TransactTime
        {
            get => _transactTime;
            set => SetProperty(ref _transactTime, value);
        }

        private string _sequenceNumber;
        public string SequenceNumber
        {
            get => _sequenceNumber;
            set => SetProperty(ref _sequenceNumber, value);
        }

        private string _rawDataLength;
        public string RawDataLength
        {
            get => _rawDataLength;
            set => SetProperty(ref _rawDataLength, value);
        }

        private string _parseError;
        public string ParseError
        {
            get => _parseError;
            set => SetProperty(ref _parseError, value);
        }

        private bool _hasParseError;
        public bool HasParseError
        {
            get => _hasParseError;
            set => SetProperty(ref _hasParseError, value);
        }

        public ObservableCollection<CcgMessageField> Fields { get; } = new ObservableCollection<CcgMessageField>();

        private string _rawDataHex;
        public string RawDataHex
        {
            get => _rawDataHex;
            set => SetProperty(ref _rawDataHex, value);
        }

        private string _rawDataText;
        public string RawDataText
        {
            get => _rawDataText;
            set => SetProperty(ref _rawDataText, value);
        }

        private bool _showRawAsHex = true;
        public bool ShowRawAsHex
        {
            get => _showRawAsHex;
            set
            {
                if (SetProperty(ref _showRawAsHex, value))
                {
                    UpdateRawDataDisplay();
                }
            }
        }

        private bool _showRawAsText;
        public bool ShowRawAsText
        {
            get => _showRawAsText;
            set
            {
                if (SetProperty(ref _showRawAsText, value))
                {
                    UpdateRawDataDisplay();
                }
            }
        }

        private string _rawDataDisplay;
        public string RawDataDisplay
        {
            get => _rawDataDisplay;
            set => SetProperty(ref _rawDataDisplay, value);
        }

        // Statistics
        private string _statisticsText;
        public string StatisticsText
        {
            get => _statisticsText;
            set => SetProperty(ref _statisticsText, value);
        }

        // Commands
        public RelayCommand<CcgMessageField> CopyFieldValueCommand { get; }
        public RelayCommand CopyAllFieldsCommand { get; }
        public RelayCommand CopyRawDataCommand { get; }
        public RelayCommand<Window> CloseCommand { get; }

        public CcgMessageDetailsViewModel(CcgMessage ccgMessage)
        {
            _originalMessage = ccgMessage;
            _messageDetails = CcgMessageDetailsParser.ParseMessageDetails(ccgMessage);

            // Initialize commands
            CopyFieldValueCommand = new RelayCommand<CcgMessageField>(CopyFieldValue);
            CopyAllFieldsCommand = new RelayCommand(CopyAllFields);
            CopyRawDataCommand = new RelayCommand(CopyRawData);
            CloseCommand = new RelayCommand<Window>(CloseWindow);

            InitializeProperties();
            LoadFields();
            UpdateStatistics();
        }

        private void InitializeProperties()
        {
            WindowTitle = $"CCG Message Details - {_messageDetails.MessageName}";
            MessageType = _messageDetails.MessageType;
            MessageName = _messageDetails.MessageName;
            DateReceived = _originalMessage.DateReceived.ToString("yyyy-MM-dd HH:mm:ss.fff");
            TransactTime = _originalMessage.TransactTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            SequenceNumber = _originalMessage.SequenceNumber.ToString();
            RawDataLength = $"{_messageDetails.RawData?.Length ?? 0} bytes";

            // Parse error handling
            ParseError = _messageDetails.ParseError;
            HasParseError = !string.IsNullOrEmpty(_messageDetails.ParseError);

            // Raw data
            RawDataHex = _messageDetails.RawDataHex;
            RawDataText = ConvertToReadableText(_messageDetails.RawData);
            
            UpdateRawDataDisplay();
        }

        private void LoadFields()
        {
            Fields.Clear();
            foreach (var field in _messageDetails.Fields)
            {
                Fields.Add(field);
            }
        }

        private void UpdateRawDataDisplay()
        {
            if (ShowRawAsHex)
            {
                RawDataDisplay = RawDataHex;
            }
            else
            {
                RawDataDisplay = RawDataText;
            }
        }

        private string ConvertToReadableText(byte[] data)
        {
            if (data == null || data.Length == 0)
                return "";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                if (b >= 32 && b <= 126) // Printable ASCII
                {
                    sb.Append((char)b);
                }
                else
                {
                    sb.Append($"[{b:X2}]");
                }

                // Add space every 16 characters for readability
                if ((i + 1) % 16 == 0)
                {
                    sb.AppendLine();
                }
                else if ((i + 1) % 4 == 0)
                {
                    sb.Append(' ');
                }
            }
            return sb.ToString();
        }

        private void UpdateStatistics()
        {
            var fieldsCount = Fields.Count;
            var dataSize = _messageDetails.RawData?.Length ?? 0;
            var parsedSize = 0;
            
            foreach (var field in Fields)
            {
                parsedSize += field.Length;
            }

            var unparsedSize = dataSize - parsedSize;
            var parsePercentage = dataSize > 0 ? (double)parsedSize / dataSize * 100 : 0;

            StatisticsText = $"Fields: {fieldsCount}, Data Size: {dataSize} bytes, " +
                           $"Parsed: {parsedSize} bytes ({parsePercentage:F1}%), " +
                           $"Unparsed: {unparsedSize} bytes";
        }

        private void CopyFieldValue(CcgMessageField field)
        {
            if (field != null)
            {
                try
                {
                    Clipboard.SetText($"{field.Name}: {field.Value}");
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to copy field value: {ex.Message}");
                }
            }
        }

        private void CopyAllFields()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"CCG Message Details: {MessageName}");
                sb.AppendLine($"Date Received: {DateReceived}");
                sb.AppendLine($"Transact Time: {TransactTime}");
                sb.AppendLine($"Sequence Number: {SequenceNumber}");
                sb.AppendLine($"Message Type: {MessageType}");
                sb.AppendLine();
                sb.AppendLine("Fields:");
                sb.AppendLine("-------");

                foreach (var field in Fields)
                {
                    sb.AppendLine($"{field.Name} ({field.Type}) @ offset {field.Offset}: {field.Value}");
                    if (!string.IsNullOrEmpty(field.Description))
                    {
                        sb.AppendLine($"  Description: {field.Description}");
                    }
                    sb.AppendLine();
                }

                if (HasParseError)
                {
                    sb.AppendLine($"Parse Error: {ParseError}");
                    sb.AppendLine();
                }

                sb.AppendLine("Raw Data (Hex):");
                sb.AppendLine("---------------");
                sb.AppendLine(RawDataHex);

                Clipboard.SetText(sb.ToString());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy all fields: {ex.Message}");
            }
        }

        private void CopyRawData()
        {
            try
            {
                Clipboard.SetText(RawDataDisplay);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy raw data: {ex.Message}");
            }
        }

        private void CloseWindow(Window window)
        {
            window?.Close();
        }
    }
}