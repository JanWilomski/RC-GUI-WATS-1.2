using System;
using System.Configuration;

namespace RC_GUI_WATS.Services
{
    public class ConfigurationService
    {
        // Dodajemy konstruktor, który natychmiast ładuje wartości
        private string _serverIP;
        private int _serverPort;
        private string _instrumentsFilePath;
        private string _logLevel;
        private bool _autoConnect;

        public ConfigurationService()
        {
            // Ładujemy wartości z app.config podczas inicjalizacji
            _serverIP = GetConfigValue("ServerIP", "127.0.0.1");
            _serverPort = int.Parse(GetConfigValue("ServerPort", "19083"));
            _instrumentsFilePath = GetConfigValue("InstrumentsFilePath", "");
            _logLevel = GetConfigValue("LogLevel", "Info");
            _autoConnect = bool.Parse(GetConfigValue("AutoConnect", "false"));

            // Logowanie dla debugowania
            Console.WriteLine($"Loaded configuration: ServerIP={_serverIP}, ServerPort={_serverPort}, AutoConnect={_autoConnect}");
        }

        // Właściwości zwracają już załadowane wartości
        public string ServerIP => _serverIP;
        public int ServerPort => _serverPort;
        public string InstrumentsFilePath => _instrumentsFilePath;
        public string LogLevel => _logLevel;
        public bool AutoConnect => _autoConnect;

        // CCG Message Colors
        public string Color_Ccg_Error => GetConfigValue("Color_Ccg_Error", "Yellow");
        public string Color_Ccg_MassQuote => GetConfigValue("Color_Ccg_MassQuote", "#E6E6FA");
        public string Color_Ccg_MassQuoteResponse => GetConfigValue("Color_Ccg_MassQuoteResponse", "#F0E6FF");
        public string Color_Ccg_OrderAdd => GetConfigValue("Color_Ccg_OrderAdd", "LawnGreen");
        public string Color_Ccg_OrderAddResponse => GetConfigValue("Color_Ccg_OrderAddResponse", "#98FB98");
        public string Color_Ccg_Trade => GetConfigValue("Color_Ccg_Trade", "#ADD8E6");
        public string Color_Ccg_OrderCancel => GetConfigValue("Color_Ccg_OrderCancel", "Red");
        public string Color_Ccg_OrderCancelResponse => GetConfigValue("Color_Ccg_OrderCancelResponse", "#F08080");
        public string Color_Ccg_OrderModify => GetConfigValue("Color_Ccg_OrderModify", "#FFFFE0");
        public string Color_Ccg_OrderModifyResponse => GetConfigValue("Color_Ccg_OrderModifyResponse", "#FFFFE0");
        public string Color_Ccg_OrderMassCancel => GetConfigValue("Color_Ccg_OrderMassCancel", "#FFB6C1");
        public string Color_Ccg_OrderMassCancelResponse => GetConfigValue("Color_Ccg_OrderMassCancelResponse", "#FFB6C1");
        public string Color_Ccg_Reject => GetConfigValue("Color_Ccg_Reject", "#FFC0CB");
        public string Color_Ccg_Login => GetConfigValue("Color_Ccg_Login", "#FFA500");
        public string Color_Ccg_LoginResponse => GetConfigValue("Color_Ccg_LoginResponse", "#FFA500");
        public string Color_Ccg_TradeCaptureReportSingle => GetConfigValue("Color_Ccg_TradeCaptureReportSingle", "#E0FFFF");
        public string Color_Ccg_TradeCaptureReportDual => GetConfigValue("Color_Ccg_TradeCaptureReportDual", "#E0FFFF");

        // Order Book Colors
        public string Color_OrderBook_Status_New => GetConfigValue("Color_OrderBook_Status_New", "#E6F3FF");
        public string Color_OrderBook_Status_PartiallyFilled => GetConfigValue("Color_OrderBook_Status_PartiallyFilled", "#FFFACD");
        public string Color_OrderBook_Status_Filled => GetConfigValue("Color_OrderBook_Status_Filled", "#E6FFE6");
        public string Color_OrderBook_Status_Cancelled => GetConfigValue("Color_OrderBook_Status_Cancelled", "#F0F0F0");
        public string Color_OrderBook_Status_Rejected => GetConfigValue("Color_OrderBook_Status_Rejected", "#FFE6E6");
        public string Color_OrderBook_Side_Buy => GetConfigValue("Color_OrderBook_Side_Buy", "Green");
        public string Color_OrderBook_Side_Sell => GetConfigValue("Color_OrderBook_Side_Sell", "Red");

        // Helper method to get configuration value with fallback
        public string GetConfigValue(string key, string defaultValue)
        {
            try
            {
                string value = ConfigurationManager.AppSettings[key];
                return !string.IsNullOrEmpty(value) ? value : defaultValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading config value {key}: {ex.Message}");
                return defaultValue;
            }
        }

        // Method to update a configuration value
        public void UpdateConfigValue(string key, string value)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                // Sprawdź, czy klucz już istnieje
                if (config.AppSettings.Settings[key] != null)
                {
                    config.AppSettings.Settings[key].Value = value;
                }
                else
                {
                    // Dodaj nowy klucz, jeśli nie istnieje
                    config.AppSettings.Settings.Add(key, value);
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                
                // Odśwież wartości w pamięci
                switch (key)
                {
                    case "ServerIP":
                        _serverIP = value;
                        break;
                    case "ServerPort":
                        _serverPort = int.Parse(value);
                        break;
                    case "InstrumentsFilePath":
                        _instrumentsFilePath = value;
                        break;
                    case "LogLevel":
                        _logLevel = value;
                        break;
                    case "AutoConnect":
                        _autoConnect = bool.Parse(value);
                        break;
                    // New color cases
                    case "Color_Ccg_Error":
                    case "Color_Ccg_MassQuote":
                    case "Color_Ccg_MassQuoteResponse":
                    case "Color_Ccg_OrderAdd":
                    case "Color_Ccg_OrderAddResponse":
                    case "Color_Ccg_Trade":
                    case "Color_Ccg_OrderCancel":
                    case "Color_Ccg_OrderCancelResponse":
                    case "Color_Ccg_OrderModify":
                    case "Color_Ccg_OrderModifyResponse":
                    case "Color_Ccg_OrderMassCancel":
                    case "Color_Ccg_OrderMassCancelResponse":
                    case "Color_Ccg_Reject":
                    case "Color_Ccg_Login":
                    case "Color_Ccg_LoginResponse":
                    case "Color_Ccg_TradeCaptureReportSingle":
                    case "Color_Ccg_TradeCaptureReportDual":
                    case "Color_OrderBook_Status_New":
                    case "Color_OrderBook_Status_PartiallyFilled":
                    case "Color_OrderBook_Status_Filled":
                    case "Color_OrderBook_Status_Cancelled":
                    case "Color_OrderBook_Status_Rejected":
                    case "Color_OrderBook_Side_Buy":
                    case "Color_OrderBook_Side_Sell":
                        // No private field to update for these, as they are directly read from config
                        // via GetConfigValue properties.
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating configuration: {ex.Message}");
            }
        }

        // Dodaj metodę do przeładowania konfiguracji
        public void ReloadConfiguration()
        {
            UpdateConfigValue("ServerIP", _serverIP);
            UpdateConfigValue("ServerPort", _serverPort.ToString());
            UpdateConfigValue("InstrumentsFilePath", _instrumentsFilePath);
            UpdateConfigValue("LogLevel", _logLevel);
            UpdateConfigValue("AutoConnect", _autoConnect.ToString());
        }
    }
}