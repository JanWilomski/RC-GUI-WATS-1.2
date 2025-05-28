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
            switch (ScopeTypeComboBox.SelectedIndex)
            {
                case 0: // Wszystkie instrumenty
                    ScopeValueTextBox.Text = "(ALL)";
                    ScopeValueTextBox.IsEnabled = false;
                    HintTextBlock.Text = "Limit będzie zastosowany do wszystkich instrumentów";
                    break;
                    
                case 1: // Typ instrumentu
                    ScopeValueTextBox.Text = "(EQUITY)";
                    ScopeValueTextBox.IsEnabled = true;
                    HintTextBlock.Text = "Przykłady typów: (EQUITY), (BOND), (FUTURE), (OPTION), (INDEX), (CURRENCY), (COMMODITY)";
                    break;
                    
                case 2: // Grupa instrumentów
                    ScopeValueTextBox.Text = "[11*]";
                    ScopeValueTextBox.IsEnabled = true;
                    HintTextBlock.Text = "Przykłady grup: [11*], [ABC*], [12?], [*EUR] - używaj * i ? jako wildcard";
                    break;
                    
                case 3: // Pojedynczy instrument
                    ScopeValueTextBox.Text = "PLPKO0000016";
                    ScopeValueTextBox.IsEnabled = true;
                    HintTextBlock.Text = "Wprowadź kod ISIN instrumentu, np. PLPKO0000016";
                    break;
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
            
            // Walidacja dla typu instrumentu
            if (ScopeTypeComboBox.SelectedIndex == 1) // Typ instrumentu
            {
                string scopeValue = ScopeValueTextBox.Text.Trim();
                if (!scopeValue.StartsWith("(") || !scopeValue.EndsWith(")"))
                {
                    MessageBox.Show("Typ instrumentu musi być w nawiasach okrągłych, np. (EQUITY)", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (scopeValue.Length <= 2) // Tylko nawiasy bez zawartości
                {
                    MessageBox.Show("Podaj nazwę typu instrumentu w nawiasach, np. (EQUITY)", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            
            // Walidacja dla grupy instrumentów
            if (ScopeTypeComboBox.SelectedIndex == 2) // Grupa instrumentów
            {
                string scopeValue = ScopeValueTextBox.Text.Trim();
                if (!scopeValue.StartsWith("[") || !scopeValue.EndsWith("]"))
                {
                    MessageBox.Show("Grupa instrumentów musi być w nawiasach kwadratowych, np. [11*]", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (scopeValue.Length <= 2) // Tylko nawiasy bez zawartości
                {
                    MessageBox.Show("Podaj wzorzec grupy w nawiasach, np. [11*]", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            
            // Tworzenie limitu
            string scope = ScopeValueTextBox.Text.Trim();
            string limitType = (LimitTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string limitValue = LimitValueTextBox.Text.Trim();
            
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