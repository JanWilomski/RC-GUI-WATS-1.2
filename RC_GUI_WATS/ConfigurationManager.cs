using System;
using System.Configuration;

namespace RC_GUI_WATS.Services
{
    public static class AppConfig
    {
        // Server connection settings
        public static string ServerIP => GetConfigValue("ServerIP", "127.0.0.1");
        public static int ServerPort => GetConfigValueInt("ServerPort", 19083);
        
        // File paths
        public static string InstrumentsFilePath => GetConfigValue("InstrumentsFilePath", "");
        
        // Other settings
        public static string LogLevel => GetConfigValue("LogLevel", "Info");
        public static bool AutoConnect => bool.Parse(GetConfigValue("AutoConnect", "false"));

        // Visual settings
        public static double WindowWidth => GetConfigValueDouble("WindowWidth", 800);
        public static double WindowHeight => GetConfigValueDouble("WindowHeight", 600);
        public static double WindowTop => GetConfigValueDouble("WindowTop", 100);
        public static double WindowLeft => GetConfigValueDouble("WindowLeft", 100);
        public static System.Windows.WindowState WindowState => GetConfigValueEnum("WindowState", System.Windows.WindowState.Normal);

        // Helper method to get configuration value with fallback
        public static string GetConfigValue(string key, string defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            return !string.IsNullOrEmpty(value) ? value : defaultValue;
        }

        public static int GetConfigValueInt(string key, int defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        public static double GetConfigValueDouble(string key, double defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (double.TryParse(value, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        public static T GetConfigValueEnum<T>(string key, T defaultValue) where T : struct, IConvertible
        {
            string value = ConfigurationManager.AppSettings[key];
            if (Enum.TryParse<T>(value, true, out T result))
            {
                return result;
            }
            return defaultValue;
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