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
            // Create a simple service container
            var configService = new ConfigurationService();
            var fileLoggingService = new FileLoggingService(); // Create logging service first
            var clientService = new RcTcpClientService();
            var positionsService = new PositionsService(clientService);
            var capitalService = new CapitalService(clientService);
            var limitsService = new LimitsService(clientService);
            var instrumentsService = new InstrumentsService();

            // Pass InstrumentsService to CcgMessagesService for instrument mapping
            var ccgMessagesService = new CcgMessagesService(clientService, instrumentsService);
            var orderBookService = new OrderBookService(ccgMessagesService, instrumentsService);

            // Create heartbeat monitor service
            var heartbeatMonitorService = new HeartbeatMonitorService(clientService);

            // Log application startup
            fileLoggingService.LogSettings("Application startup", "Initializing RC GUI WATS");

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
                ccgMessagesService,
                orderBookService,
                fileLoggingService);

            mainWindow.DataContext = mainViewModel;

            // Log window creation
            fileLoggingService.LogSettings("Main window created", "Application ready to start");

            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Log application shutdown - we could create a static reference to fileLoggingService if needed
            // For now, we'll just rely on the file logging service in the main window
            base.OnExit(e);
        }
    }
}