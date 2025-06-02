// Views/MessagesTabControl.xaml.cs
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
        
        private void CcgDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void CcgDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is CcgMessage selectedMessage)
            {
                var viewModel = DataContext as MessagesTabViewModel;
                viewModel?.ShowCcgMessageDetailsCommand?.Execute(selectedMessage);
            }
        }
    }
}