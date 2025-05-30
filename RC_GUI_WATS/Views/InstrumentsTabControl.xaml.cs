using System.Windows.Controls;

namespace RC_GUI_WATS.Views
{
    public partial class InstrumentsTabControl : UserControl
    {
        public InstrumentsTabControl()
        {
            InitializeComponent();
        }

        private void InstrumentsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}