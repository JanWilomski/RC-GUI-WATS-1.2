// Views/MessagesTabControl.xaml.cs - Poprawiona wersja
using System.Windows.Controls;
using System.Windows.Input;
using RC_GUI_WATS.Models;

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
    }
}