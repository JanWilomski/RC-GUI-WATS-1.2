using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class InstrumentsService
    {
        private ObservableCollection<Instrument> _instruments = new ObservableCollection<Instrument>();
        public ObservableCollection<Instrument> Instruments => _instruments;

        public event Action<string> StatusUpdated;

        public void LoadInstrumentsFromCsv(string filePath)
        {
            try
            {
                StatusUpdated?.Invoke("Loading instruments file...");
                _instruments.Clear();
                
                // Read all lines from the file
                string[] lines = File.ReadAllLines(filePath);
                
                // Get headers (first line)
                if (lines.Length > 0)
                {
                    string[] headers = lines[0].Split(',');
                    
                    // Process data rows
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (!string.IsNullOrEmpty(line))
                        {
                            try
                            {
                                string[] values = line.Split(',');
                                
                                if (values.Length >= 57) // Ensure we have all columns
                                {
                                    var instrument = new Instrument
                                    {
                                        InstrumentID = ParseInt(values[0]),
                                        ISIN = values[1],
                                        ProductCode = values[2],
                                        // Additional properties...
                                    };
                                    
                                    _instruments.Add(instrument);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing line {i}: {ex.Message}");
                            }
                        }
                    }
                }
                
                StatusUpdated?.Invoke($"Loaded {_instruments.Count} instruments from {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke("Error loading instruments file");
                throw;
            }
        }

        // Helper methods for parsing values
        private int ParseInt(string value)
        {
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        private double ParseDouble(string value)
        {
            // Handle both culture-specific number formats
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;
            return 0;
        }

        private DateTime ParseDate(string value)
        {
            if (DateTime.TryParse(value, out DateTime result))
                return result;
            return DateTime.MinValue;
        }

        private bool ParseBool(string value)
        {
            if (bool.TryParse(value, out bool result))
                return result;
            
            // Handle "1"/"0" as bool
            if (value == "1")
                return true;
            if (value == "0") 
                return false;
                
            return false;
        }
    }
}