using System.Windows;
using RC_GUI_WATS.Models;
using System.Windows.Controls;

namespace RC_GUI_WATS
{
    public partial class AddLimitWindow : Window
    {
        public ControlLimit ControlLimit { get; private set; }
        
        public AddLimitWindow()
        {
            InitializeComponent();
            
            // Domyślnie wybierz pierwszy element (Wszystkie instrumenty)
            ScopeTypeComboBox.SelectedIndex = 0;
            LimitTypeComboBox.SelectedIndex = 0;
        }
        
        private void ScopeTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Dostosuj pole zakresu w zależności od wybranego typu
            if (ScopeTypeComboBox.SelectedIndex == 0) // Wszystkie instrumenty
            {
                ScopeValueTextBox.Text = "(ALL)";
                ScopeValueTextBox.IsEnabled = false;
            }
            else if (ScopeTypeComboBox.SelectedIndex == 1) // Grupa instrumentów
            {
                ScopeValueTextBox.Text = "[11*]";
                ScopeValueTextBox.IsEnabled = true;
            }
            else if (ScopeTypeComboBox.SelectedIndex == 2) // Pojedynczy instrument
            {
                ScopeValueTextBox.Text = "PLPKO0000016";
                ScopeValueTextBox.IsEnabled = true;
            }
        }
        
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Walidacja
            if (string.IsNullOrWhiteSpace(ScopeValueTextBox.Text))
            {
                MessageBox.Show("Podaj wartość zakresu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (LimitTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Wybierz typ limitu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(LimitValueTextBox.Text))
            {
                MessageBox.Show("Podaj wartość limitu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Tworzenie limitu
            string scope = ScopeValueTextBox.Text;
            string limitType = (LimitTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string limitValue = LimitValueTextBox.Text;
            
            ControlLimit = new ControlLimit(scope, limitType, limitValue);
            
            DialogResult = true;
            Close();
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}