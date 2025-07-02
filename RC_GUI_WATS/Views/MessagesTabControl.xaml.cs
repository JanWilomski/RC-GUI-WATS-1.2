// Views/MessagesTabControl.xaml.cs - Poprawiona wersja

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.ViewModels;

namespace RC_GUI_WATS.Views
{
    public partial class MessagesTabControl : UserControl
    {
        public MessagesTabControl()
        {
            InitializeComponent();
        }
        
        private void PositionsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
        
        private void CcgDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
        
        private void OrderBookDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
        
        private void CcgDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the clicked item
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is CcgMessage selectedMessage)
            {
                // Open details window
                var detailsWindow = new CcgMessageDetailsWindow(selectedMessage);
                detailsWindow.Owner = System.Windows.Application.Current.MainWindow;
                detailsWindow.ShowDialog();
            }
        }
        
        
        private void OrderBookDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Sprawdź czy kliknięcie było na wierszu z danymi
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is OrderBookEntry selectedOrder)
            {
                try
                {
                    // Pobierz ViewModel z DataContext
                    if (DataContext is MessagesTabViewModel viewModel)
                    {
                        // Pobierz powiązane CCG messages z OrderBookService
                        var relatedMessages = viewModel.GetRelatedCcgMessages(selectedOrder);
                
                        // Otwórz okno z CCG messages
                        var ccgMessagesWindow = new OrderBookCcgMessagesWindow(selectedOrder, relatedMessages);
                        ccgMessagesWindow.Owner = System.Windows.Application.Current.MainWindow;
                        ccgMessagesWindow.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("Unable to access order book service.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening CCG messages window: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine($"Error in OrderBookDataGrid_MouseDoubleClick: {ex.Message}");
                }
            }
        }
        
    }
}