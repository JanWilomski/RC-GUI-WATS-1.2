using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using RC_GUI_WATS.Commands;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.Services;

namespace RC_GUI_WATS.ViewModels
{
    public class InstrumentsTabViewModel : BaseViewModel
    {
        private readonly InstrumentsService _instrumentsService;
        
        // Properties
        public ObservableCollection<Instrument> Instruments => _instrumentsService.Instruments;
        
        private string _instrumentsFilePath;
        public string InstrumentsFilePath
        {
            get => _instrumentsFilePath;
            set => SetProperty(ref _instrumentsFilePath, value);
        }
        
        // Commands
        public ICommand SelectInstrumentsFileCommand { get; }
        
        public InstrumentsTabViewModel(
            InstrumentsService instrumentsService,
            string initialInstrumentsFilePath)
        {
            _instrumentsService = instrumentsService;
            
            // Ustaw ścieżkę pliku z konfiguracji
            _instrumentsFilePath = System.IO.Path.GetFileName(initialInstrumentsFilePath);
            
            // Subscribe to status updates
            _instrumentsService.StatusUpdated += OnStatusUpdated;
            
            // Initialize commands
            SelectInstrumentsFileCommand = new RelayCommand(SelectInstrumentsFile);
            
            // Załaduj instrumenty jeśli ścieżka istnieje
            if (!string.IsNullOrEmpty(initialInstrumentsFilePath) && System.IO.File.Exists(initialInstrumentsFilePath))
            {
                _instrumentsService.LoadInstrumentsFromCsv(initialInstrumentsFilePath);
            }
        }
        
        
        private void OnStatusUpdated(string status)
        {
            // Propagate status updates to MainViewModel if needed
        }
        
        private void SelectInstrumentsFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select Instruments CSV File"
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                _instrumentsFilePath = openFileDialog.FileName;
                InstrumentsFilePath = System.IO.Path.GetFileName(_instrumentsFilePath);
                _instrumentsService.LoadInstrumentsFromCsv(_instrumentsFilePath);
            }
        }
    }
    
    // If not already defined elsewhere

}