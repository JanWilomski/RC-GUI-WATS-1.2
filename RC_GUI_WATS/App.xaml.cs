// App.xaml.cs
using System.Windows;
using RC_GUI_WATS.Services;
using RC_GUI_WATS.ViewModels;

namespace RC_GUI_WATS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Setup dependency injection
            ConfigureServices();
        }

        private void ConfigureServices()
        {
            // Create services in the proper order to avoid threading issues
            var configService = new ConfigurationService();
            var clientService = new RcTcpClientService();
            
            // These services handle UI thread marshalling internally
            var positionsService = new PositionsService(clientService);
            var capitalService = new CapitalService(clientService);
            var limitsService = new LimitsService(clientService);
            var instrumentsService = new InstrumentsService();
            var ccgMessagesService = new CcgMessagesService(clientService); // Add CCG Messages service
            
            // Create heartbeat monitor service
            var heartbeatMonitorService = new HeartbeatMonitorService(clientService);

            // Set up the main window with view model
            var mainWindow = new MainWindow();
            var mainViewModel = new MainWindowViewModel(
                clientService,
                positionsService,
                capitalService,
                limitsService,
                instrumentsService,
                configService,  
                heartbeatMonitorService,
                ccgMessagesService); // Add CCG Messages service to constructor

            mainWindow.DataContext = mainViewModel;
            mainWindow.Show();
        }
    }
}