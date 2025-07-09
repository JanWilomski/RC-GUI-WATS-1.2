using System.Windows;
using RC_GUI_WATS.ViewModels;

namespace RC_GUI_WATS.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}