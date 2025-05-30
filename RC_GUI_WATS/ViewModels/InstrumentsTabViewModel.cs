using System;
using System.Windows.Input;
using Microsoft.Win32;
using RC_GUI_WATS.Commands;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.Services;
using System.Collections.ObjectModel;

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

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Commands
        public ICommand SelectInstrumentsFileCommand { get; }
        public ICommand RefreshCommand { get; }
        
        public InstrumentsTabViewModel(
            InstrumentsService instrumentsService,
            string initialInstrumentsFilePath)
        {
            _instrumentsService = instrumentsService;
            
            // Set initial file path display
            if (!string.IsNullOrEmpty(initialInstrumentsFilePath))
            {
                _instrumentsFilePath = System.IO.Path.GetFileName(initialInstrumentsFilePath);
            }
            else
            {
                _instrumentsFilePath = "No file selected";
            }
            
            // Subscribe to status updates
            _instrumentsService.StatusUpdated += OnStatusUpdated;
            
            // Initialize commands
            SelectInstrumentsFileCommand = new RelayCommand(SelectInstrumentsFile);
            RefreshCommand = new RelayCommand(RefreshInstruments);
            
            // Initialize properties
            _statusText = "Ready";
            
            // Load instruments if file path exists
            if (!string.IsNullOrEmpty(initialInstrumentsFilePath) && System.IO.File.Exists(initialInstrumentsFilePath))
            {
                LoadInstrumentsAsync(initialInstrumentsFilePath);
            }
            else
            {
                StatusText = "No instruments file loaded";
            }
        }
        
        private async void LoadInstrumentsAsync(string filePath)
        {
            IsLoading = true;
            try
            {
                // Load instruments in background thread
                var instruments = await System.Threading.Tasks.Task.Run(() => 
                {
                    return _instrumentsService.LoadInstrumentsFromCsv(filePath);
                });
                
                // Update UI collection on UI thread
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    _instrumentsService.UpdateInstrumentsCollection(instruments);
                    UpdateStatusText();
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    StatusText = $"Error loading file: {ex.Message}";
                });
            }
            finally
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    IsLoading = false;
                });
            }
        }
        
        private void OnStatusUpdated(string status)
        {
            // Always update on UI thread to avoid cross-thread issues
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
            {
                StatusText = status;
            }
            else
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    StatusText = status;
                });
            }
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
                LoadInstrumentsAsync(_instrumentsFilePath);
            }
        }

        private void RefreshInstruments()
        {
            if (!string.IsNullOrEmpty(_instrumentsFilePath) && System.IO.File.Exists(_instrumentsFilePath))
            {
                LoadInstrumentsAsync(_instrumentsFilePath);
            }
            else
            {
                StatusText = "No file selected for refresh";
            }
        }

        private void UpdateStatusText()
        {
            try
            {
                int totalCount = _instrumentsService.Instruments.Count;
                if (totalCount > 0)
                {
                    StatusText = $"Loaded {totalCount} instruments";
                }
                else
                {
                    StatusText = "No instruments loaded";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error updating status: {ex.Message}";
            }
        }
    }
}