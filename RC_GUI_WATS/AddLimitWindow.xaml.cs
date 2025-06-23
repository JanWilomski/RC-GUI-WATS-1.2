using System.Windows;
using RC_GUI_WATS.Models;
using System.Windows.Controls;
using System.Collections.Generic;

namespace RC_GUI_WATS
{
    public partial class AddLimitWindow : Window
    {
        public ControlLimit ControlLimit { get; private set; }
        
        // Definicje typów limitów dla różnych zakresów
        private readonly List<LimitTypeInfo> _allInstrumentsLimits = new List<LimitTypeInfo>
        {
            new LimitTypeInfo("halt", "Y/N", "Zatrzymuje handel na wszystkich instrumentach"),
            new LimitTypeInfo("maxMessageCount", "liczba", "Maksymalna liczba wiadomości"),
            new LimitTypeInfo("maxOrderRate", "liczba/s", "Maksymalny rate zleceń, np. 100/s"),
            new LimitTypeInfo("maxTransaction", "procent", "Procent nominalnej wartości transakcji"),
            new LimitTypeInfo("maxAbsShares", "liczba", "Maksymalna bezwzględna pozycja"),
            new LimitTypeInfo("maxShortShares", "liczba", "Maksymalna pozycja krótka"),
            new LimitTypeInfo("maxCapital", "kwota", "Maksymalny kapitał"),
            new LimitTypeInfo("collars", "wartość", "Ograniczenia cenowe"),
            new LimitTypeInfo("maxShortCapital", "kwota", "Maksymalny kapitał pozycji krótkiej")
        };
        
        private readonly List<LimitTypeInfo> _otherScopesLimits = new List<LimitTypeInfo>
        {
            new LimitTypeInfo("halt", "Y/N", "Zatrzymuje handel na wybranych instrumentach"),
            new LimitTypeInfo("maxTransaction", "procent", "Procent nominalnej wartości transakcji"),
            new LimitTypeInfo("maxShortCapital", "kwota", "Maksymalny kapitał pozycji krótkiej"),
            new LimitTypeInfo("capitalImpact", "procent", "Wpływ na kapitał (tylko futures)"),
            new LimitTypeInfo("maxAbsShares", "liczba", "Maksymalna bezwzględna pozycja"),
            new LimitTypeInfo("maxShortShares", "liczba", "Maksymalna pozycja krótka")
        };
        
        public AddLimitWindow()
        {
            InitializeComponent();
            
            // Domyślnie wybierz pierwszy element (Wszystkie instrumenty)
            ScopeTypeComboBox.SelectedIndex = 0;
            
            // Inicjalizuj typy limitów dla domyślnego zakresu
            UpdateLimitTypes();
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
            
            // Aktualizuj dostępne typy limitów
            UpdateLimitTypes();
        }
        
        private void UpdateLimitTypes()
        {
            // Zapisz aktualnie wybraną wartość
            string currentSelection = (LimitTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            
            // Wyczyść obecne opcje
            LimitTypeComboBox.Items.Clear();
            
            // Wybierz odpowiednią listę limitów
            List<LimitTypeInfo> limitsToShow;
            if (ScopeTypeComboBox.SelectedIndex == 0) // Wszystkie instrumenty (ALL)
            {
                limitsToShow = _allInstrumentsLimits;
            }
            else // Inne zakresy
            {
                limitsToShow = _otherScopesLimits;
            }
            
            // Dodaj opcje do ComboBox
            foreach (var limitInfo in limitsToShow)
            {
                var item = new ComboBoxItem
                {
                    Content = limitInfo.Name,
                    Tag = limitInfo
                };
                LimitTypeComboBox.Items.Add(item);
            }
            
            // Spróbuj przywrócić poprzednią selekcję lub wybierz pierwszy element
            bool selectionRestored = false;
            if (!string.IsNullOrEmpty(currentSelection))
            {
                foreach (ComboBoxItem item in LimitTypeComboBox.Items)
                {
                    if (item.Content.ToString() == currentSelection)
                    {
                        LimitTypeComboBox.SelectedItem = item;
                        selectionRestored = true;
                        break;
                    }
                }
            }
            
            if (!selectionRestored && LimitTypeComboBox.Items.Count > 0)
            {
                LimitTypeComboBox.SelectedIndex = 0;
            }
            
            // Aktualizuj podpowiedź dla wybranego typu limitu
            UpdateLimitTypeHint();
        }
        
        private void LimitTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLimitTypeHint();
        }
        
        private void UpdateLimitTypeHint()
        {
            if (LimitTypeComboBox.SelectedItem is ComboBoxItem selectedItem && 
                selectedItem.Tag is LimitTypeInfo limitInfo)
            {
                LimitValueHintTextBlock.Text = $"Format: {limitInfo.ValueFormat} - {limitInfo.Description}";
                
                // Ustaw przykładową wartość w polu
                switch (limitInfo.Name)
                {
                    case "halt":
                        LimitValueTextBox.Text = "Y";
                        break;
                    case "maxMessageCount":
                        LimitValueTextBox.Text = "10000";
                        break;
                    case "maxOrderRate":
                        LimitValueTextBox.Text = "100/s";
                        break;
                    case "maxTransaction":
                        LimitValueTextBox.Text = "10";
                        break;
                    case "maxAbsShares":
                        LimitValueTextBox.Text = "1000";
                        break;
                    case "maxShortShares":
                        LimitValueTextBox.Text = "500";
                        break;
                    case "maxCapital":
                        LimitValueTextBox.Text = "1000000";
                        break;
                    case "collars":
                        LimitValueTextBox.Text = "0.05";
                        break;
                    case "maxShortCapital":
                        LimitValueTextBox.Text = "500000";
                        break;
                    case "capitalImpact":
                        LimitValueTextBox.Text = "15";
                        break;
                    default:
                        LimitValueTextBox.Text = "";
                        break;
                }
            }
            else
            {
                LimitValueHintTextBlock.Text = "Wybierz typ limitu";
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
            
            // Dodatkowa walidacja dla specjalnych typów limitów
            var selectedLimitType = (LimitTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (!ValidateLimitValue(selectedLimitType, LimitValueTextBox.Text.Trim()))
            {
                return; // Błąd już wyświetlony w ValidateLimitValue
            }
            
            // Tworzenie limitu
            string scope = ScopeValueTextBox.Text.Trim();
            string limitType = selectedLimitType;
            string limitValue = LimitValueTextBox.Text.Trim();
            
            ControlLimit = new ControlLimit(scope, limitType, limitValue);
            
            DialogResult = true;
            Close();
        }
        
        private bool ValidateLimitValue(string limitType, string limitValue)
        {
            switch (limitType)
            {
                case "halt":
                    if (limitValue.ToUpper() != "Y" && limitValue.ToUpper() != "N")
                    {
                        MessageBox.Show("Wartość dla 'halt' musi być Y lub N", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;
                    
                case "maxOrderRate":
                    if (!limitValue.Contains("/"))
                    {
                        MessageBox.Show("Wartość dla 'maxOrderRate' musi być w formacie liczba/jednostka_czasu, np. 100/s", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;
                    
                case "maxMessageCount":
                case "maxAbsShares":
                case "maxShortShares":
                    if (!int.TryParse(limitValue, out int intValue) || intValue < 0)
                    {
                        MessageBox.Show($"Wartość dla '{limitType}' musi być liczbą całkowitą większą lub równą 0", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;
                    
                case "maxTransaction":
                case "capitalImpact":
                    if (!decimal.TryParse(limitValue, out decimal percentValue) || percentValue < 0 || percentValue > 100)
                    {
                        MessageBox.Show($"Wartość dla '{limitType}' musi być liczbą z zakresu 0-100 (procent)", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;
                    
                case "maxCapital":
                case "maxShortCapital":
                    if (!decimal.TryParse(limitValue, out decimal capitalValue) || capitalValue < 0)
                    {
                        MessageBox.Show($"Wartość dla '{limitType}' musi być liczbą większą lub równą 0", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;
                    
                case "collars":
                    if (!decimal.TryParse(limitValue, out decimal collarValue) || collarValue < 0)
                    {
                        MessageBox.Show("Wartość dla 'collars' musi być liczbą większą lub równą 0", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;
            }
            
            return true;
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
    
    // Klasa pomocnicza do przechowywania informacji o typach limitów
    public class LimitTypeInfo
    {
        public string Name { get; set; }
        public string ValueFormat { get; set; }
        public string Description { get; set; }
        
        public LimitTypeInfo(string name, string valueFormat, string description)
        {
            Name = name;
            ValueFormat = valueFormat;
            Description = description;
        }
    }
}