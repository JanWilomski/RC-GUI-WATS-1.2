// Views/OrderBookCcgMessagesWindow.xaml.cs
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.Services;

namespace RC_GUI_WATS.Views
{
    public partial class OrderBookCcgMessagesWindow : Window
    {
        private readonly OrderBookEntry _orderEntry;
        private readonly List<CcgMessage> _relatedMessages;
        public ConfigurationService ConfigurationService { get; } 

        public OrderBookCcgMessagesWindow(OrderBookEntry orderEntry, List<CcgMessage> relatedMessages, ConfigurationService configurationService)
        {
            InitializeComponent();
            
            _orderEntry = orderEntry;
            _relatedMessages = relatedMessages;
            ConfigurationService = configurationService;
            
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            // Set window title
            Title = $"CCG Messages for Order {_orderEntry.OrderId}";
            
            // Set DataContext for binding
            DataContext = _orderEntry;
            
            // Populate the DataGrid
            CcgMessagesDataGrid.ItemsSource = _relatedMessages;
            
            // Update statistics
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            if (_relatedMessages?.Count > 0)
            {
                var messageTypes = _relatedMessages.GroupBy(m => m.Name)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                var statisticsItems = new List<string>();
                
                if (messageTypes.ContainsKey("OrderAdd"))
                    statisticsItems.Add($"OrderAdd: {messageTypes["OrderAdd"]}");
                
                if (messageTypes.ContainsKey("OrderAddResponse"))
                    statisticsItems.Add($"OrderAddResponse: {messageTypes["OrderAddResponse"]}");
                
                if (messageTypes.ContainsKey("OrderModify"))
                    statisticsItems.Add($"OrderModify: {messageTypes["OrderModify"]}");
                
                if (messageTypes.ContainsKey("OrderModifyResponse"))
                    statisticsItems.Add($"OrderModifyResponse: {messageTypes["OrderModifyResponse"]}");
                
                if (messageTypes.ContainsKey("OrderCancel"))
                    statisticsItems.Add($"OrderCancel: {messageTypes["OrderCancel"]}");
                
                if (messageTypes.ContainsKey("OrderCancelResponse"))
                    statisticsItems.Add($"OrderCancelResponse: {messageTypes["OrderCancelResponse"]}");
                
                if (messageTypes.ContainsKey("Trade"))
                    statisticsItems.Add($"Trade: {messageTypes["Trade"]}");
                
                // Add other message types
                var otherTypes = messageTypes.Where(kvp => !new[] { "OrderAdd", "OrderAddResponse", "OrderModify", 
                    "OrderModifyResponse", "OrderCancel", "OrderCancelResponse", "Trade" }.Contains(kvp.Key));
                
                foreach (var type in otherTypes)
                {
                    statisticsItems.Add($"{type.Key}: {type.Value}");
                }
                
                StatisticsText.Text = $"Total: {_relatedMessages.Count} | " + string.Join(" | ", statisticsItems);
            }
            else
            {
                StatisticsText.Text = "No related CCG messages found";
            }
        }

        private void CcgMessagesDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void CcgMessagesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Handle double-click on CCG message to show details
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is CcgMessage selectedMessage)
            {
                // Check if CcgMessageDetailsWindow exists, otherwise show a simple message box
                try
                {
                    var detailsWindow = new CcgMessageDetailsWindow(selectedMessage);
                    detailsWindow.Owner = this;
                    detailsWindow.ShowDialog();
                }
                catch
                {
                    // Fallback if CcgMessageDetailsWindow doesn't exist
                    MessageBox.Show($"Message Details:\n\n" +
                                  $"Type: {selectedMessage.Name}\n" +
                                  $"Date: {selectedMessage.DateReceivedDisplay}\n" +
                                  $"ClientOrderId: {selectedMessage.ClientOrderId}\n" +
                                  $"InstrumentId: {selectedMessage.InstrumentId}\n" +
                                  $"Price: {selectedMessage.PriceDisplay}\n" +
                                  $"Quantity: {selectedMessage.QuantityDisplay}\n" +
                                  $"Side: {selectedMessage.Side}\n\n" +
                                  $"Raw Data: {selectedMessage.RawDataHex}",
                                  "CCG Message Details",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}