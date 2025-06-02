// CcgMessageDetailsWindow.xaml.cs
using System.Windows;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.ViewModels;

namespace RC_GUI_WATS
{
    public partial class CcgMessageDetailsWindow : Window
    {
        public CcgMessageDetailsWindow(CcgMessage ccgMessage)
        {
            InitializeComponent();
            DataContext = new CcgMessageDetailsViewModel(ccgMessage);
            
            // Set window icon if available
            // Icon = ... 
            
            // Keyboard shortcuts
            this.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    Close();
                }
                else if (e.Key == System.Windows.Input.Key.F5)
                {
                    // Refresh - could re-parse message if needed
                }
            };
        }
    }
}