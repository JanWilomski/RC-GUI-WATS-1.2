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

        // Helper method to get configuration value with fallback
        private string GetConfigValue(string key, string defaultValue)
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