using System.Windows;
using RC_GUI_WATS.Services;
using RC_GUI_WATS.ViewModels;


namespace RC_GUI_WATS
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Setup services
            var configService = new ConfigurationService();
            var clientService = new RcTcpClientService();
            var positionsService = new PositionsService(clientService);
            var capitalService = new CapitalService(clientService);
            var limitsService = new LimitsService(clientService);
            var instrumentsService = new InstrumentsService();
            
            // Setup main ViewModel
            var viewModel = new MainWindowViewModel(
                clientService, 
                positionsService, 
                capitalService, 
                limitsService, 
                instrumentsService, 
                configService);
            
            // Set as DataContext
            DataContext = viewModel;
            
            // Handle window closing
            Closing += (s, e) => 
            {
                if (clientService.IsConnected)
                {
                    clientService.Disconnect();
                }
            };
        }
    }
}