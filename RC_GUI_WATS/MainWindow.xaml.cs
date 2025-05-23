using System.Windows;
using RC_GUI_WATS.ViewModels;

namespace RC_GUI_WATS
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel && viewModel.IsConnected)
            {
                viewModel.DisconnectCommand.Execute(null);
            }
        }
    }
}