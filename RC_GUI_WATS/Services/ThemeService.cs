using System;
using System.Linq;
using System.Windows;

namespace RC_GUI_WATS.Services
{
    public class ThemeService
    {
        public void ApplyTheme(string themeName)
        {
            var themeUri = new Uri($"Themes/{themeName}Theme.xaml", UriKind.Relative);

            // Find and remove the old theme dictionary if it exists
            var oldTheme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme"));
            if (oldTheme != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(oldTheme);
            }

            // Add the new theme dictionary
            var newTheme = new ResourceDictionary { Source = themeUri };
            Application.Current.Resources.MergedDictionaries.Add(newTheme);
        }
    }
}
