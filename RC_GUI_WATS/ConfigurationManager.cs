using System;
using System.Configuration;

namespace RC_GUI_WATS.Services
{
    public static class AppConfig
    {
        // Server connection settings
        public static string ServerIP => GetConfigValue("ServerIP", "127.0.0.1");
        public static int ServerPort => int.Parse(GetConfigValue("ServerPort", "19083"));
        
        // File paths
        public static string InstrumentsFilePath => GetConfigValue("InstrumentsFilePath", "");
        
        // Other settings
        public static string LogLevel => GetConfigValue("LogLevel", "Info");
        public static bool AutoConnect => bool.Parse(GetConfigValue("AutoConnect", "false"));

        // Helper method to get configuration value with fallback
        private static string GetConfigValue(string key, string defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            return !string.IsNullOrEmpty(value) ? value : defaultValue;
        }

        // Method to update a configuration value
        public static void UpdateConfigValue(string key, string value)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings[key].Value = value;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating configuration: {ex.Message}");
                // In a real application, you might want to log this error
            }
        }
    }
}