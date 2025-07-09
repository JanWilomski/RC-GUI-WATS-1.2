using System.Windows;
using RC_GUI_WATS.ViewModels;
using RC_GUI_WATS.Services;

namespace RC_GUI_WATS
{
    public partial class MainWindow : Window
    {
        private readonly SettingsService _settingsService;

        public MainWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            Loaded += MainWindow_Loaded;
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply visual settings
            Width = _settingsService.WindowWidth;
            Height = _settingsService.WindowHeight;
            Top = _settingsService.WindowTop;
            Left = _settingsService.WindowLeft;
            WindowState = (WindowState)System.Enum.Parse(typeof(WindowState), _settingsService.WindowState);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save visual settings
            _settingsService.WindowWidth = Width;
            _settingsService.WindowHeight = Height;
            _settingsService.WindowTop = Top;
            _settingsService.WindowLeft = Left;
            _settingsService.WindowState = WindowState.ToString();
            _settingsService.ApplySettings();

            if (DataContext is MainWindowViewModel viewModel && viewModel.IsConnected)
            {
                viewModel.DisconnectCommand.Execute(null);
            }
        }
    }
}