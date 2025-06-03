// Views/MessagesTabControl.xaml.cs - Fixed version
using System.Windows.Controls;

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
    }
}