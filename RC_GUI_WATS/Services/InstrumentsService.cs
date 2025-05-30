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

        public System.Collections.Generic.List<Instrument> LoadInstrumentsFromCsv(string filePath)
        {
            var loadedInstruments = new System.Collections.Generic.List<Instrument>();
            
            try
            {
                StatusUpdated?.Invoke("Loading instruments file...");
                
                // Read all lines from the file with sharing enabled (read-only access)
                string[] lines;
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream))
                {
                    var linesList = new System.Collections.Generic.List<string>();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        linesList.Add(line);
                    }
                    lines = linesList.ToArray();
                }
                
                if (lines.Length < 2) // Need at least header + 1 data row
                {
                    StatusUpdated?.Invoke("File is empty or contains only headers");
                    return loadedInstruments;
                }

                // Get headers (first line) - just for logging
                string[] headers = ParseCsvLine(lines[0]);
                StatusUpdated?.Invoke($"Found {headers.Length} columns in CSV file");
                
                // Process data rows
                int successCount = 0;
                int errorCount = 0;
                
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        try
                        {
                            string[] values = ParseCsvLine(line);
                            
                            if (values.Length >= 10) // Ensure we have minimum required columns
                            {
                                var instrument = ParseInstrumentFromCsvValues(values);
                                if (instrument != null)
                                {
                                    loadedInstruments.Add(instrument);
                                    successCount++;
                                }
                                else
                                {
                                    errorCount++;
                                }
                            }
                            else
                            {
                                errorCount++;
                                if (errorCount < 5) // Log first few errors
                                {
                                    Console.WriteLine($"Line {i}: Not enough columns ({values.Length})");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            if (errorCount < 5) // Log first few errors
                            {
                                Console.WriteLine($"Error parsing line {i}: {ex.Message}");
                            }
                        }
                    }
                }
                
                string statusMessage = $"Loaded {successCount} instruments from {Path.GetFileName(filePath)}";
                if (errorCount > 0)
                {
                    statusMessage += $" ({errorCount} errors)";
                }
                StatusUpdated?.Invoke(statusMessage);
                
                return loadedInstruments;
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke($"Error loading instruments file: {ex.Message}");
                throw;
            }
        }

        public void UpdateInstrumentsCollection(System.Collections.Generic.List<Instrument> instruments)
        {
            _instruments.Clear();
            foreach (var instrument in instruments)
            {
                _instruments.Add(instrument);
            }
        }

        private string[] ParseCsvLine(string line)
        {
            // Simple CSV parser that handles quoted fields
            var values = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            string currentValue = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.Trim());
                    currentValue = "";
                }
                else
                {
                    currentValue += c;
                }
            }
            
            // Add the last value
            values.Add(currentValue.Trim());
            
            return values.ToArray();
        }

        private Instrument ParseInstrumentFromCsvValues(string[] values)
        {
            try
            {
                var instrument = new Instrument();
                
                // Parse all columns based on the CSV structure provided
                if (values.Length > 0) instrument.InstrumentID = ParseInt(values[0]);
                if (values.Length > 1) instrument.ISIN = CleanString(values[1]);
                if (values.Length > 2) instrument.ProductCode = CleanString(values[2]);
                if (values.Length > 3) instrument.ReferencePrice = ParseDouble(values[3]);
                if (values.Length > 4) instrument.Multiplier = ParseDouble(values[4]);
                if (values.Length > 5) instrument.MIC = CleanString(values[5]);
                if (values.Length > 6) instrument.InstrumentTypeID = ParseInt(values[6]);
                if (values.Length > 7) instrument.InstrumentType = CleanString(values[7]);
                if (values.Length > 8) instrument.InstrumentSubtypeID = ParseInt(values[8]);
                if (values.Length > 9) instrument.InstrumentSubtype = CleanString(values[9]);
                if (values.Length > 10) instrument.MarketStructureID = ParseInt(values[10]);
                if (values.Length > 11) instrument.MarketStructureName = CleanString(values[11]);
                if (values.Length > 12) instrument.Currency = CleanString(values[12]);
                if (values.Length > 13) instrument.CollarGroupId = ParseInt(values[13]);
                if (values.Length > 14) instrument.OrderStaticCollarModeId = ParseInt(values[14]);
                if (values.Length > 15) instrument.OrderStaticCollarMode = CleanString(values[15]);
                if (values.Length > 16) instrument.OrderStaticCollarExpressionTypeId = ParseInt(values[16]);
                if (values.Length > 17) instrument.OrderStaticCollarExpressionType = CleanString(values[17]);
                if (values.Length > 18) instrument.OrderStaticCollarLowerBound = ParseDouble(values[18]);
                if (values.Length > 19) instrument.OrderStaticCollarValue = ParseDouble(values[19]);
                if (values.Length > 20) instrument.OrderStaticCollarLowerBid = ParseDouble(values[20]);
                if (values.Length > 21) instrument.OrderStaticCollarUpperBid = ParseDouble(values[21]);
                if (values.Length > 22) instrument.OrderStaticCollarUpperAsk = ParseDouble(values[22]);
                if (values.Length > 23) instrument.OrderStaticCollarLowerAsk = ParseDouble(values[23]);
                if (values.Length > 24) instrument.FirstTradingDate = ParseDate(values[24]);
                if (values.Length > 25) instrument.LastTradingDate = ParseDate(values[25]);
                if (values.Length > 26) instrument.ProductID = ParseInt(values[26]);
                if (values.Length > 27) instrument.LotSize = ParseInt(values[27]);
                if (values.Length > 28) instrument.PriceExpressionType = ParseInt(values[28]);
                if (values.Length > 29) instrument.CalendarID = ParseInt(values[29]);
                if (values.Length > 30) instrument.TickTableID = ParseInt(values[30]);
                if (values.Length > 31) instrument.TradingScheduleID = ParseInt(values[31]);
                if (values.Length > 32) instrument.NominalValueType = ParseInt(values[32]);
                if (values.Length > 33) instrument.StrikePrice = ParseDouble(values[33]);
                if (values.Length > 34) instrument.SettlementCalendarID = ParseInt(values[34]);
                if (values.Length > 35) instrument.BondCouponType = ParseInt(values[35]);
                if (values.Length > 36) instrument.Liquidity = ParseBool(values[36]);
                if (values.Length > 37) instrument.MarketModelTypeID = ParseInt(values[37]);
                if (values.Length > 38) instrument.MarketModelType = CleanString(values[38]);
                if (values.Length > 39) instrument.IssuerRegCountry = CleanString(values[39]);
                if (values.Length > 40) instrument.UnderlyingInstrumentID = ParseInt(values[40]);
                if (values.Length > 41) instrument.CFICode = CleanString(values[41]);
                if (values.Length > 42) instrument.IssueSize = ParseInt(values[42]);
                if (values.Length > 43) instrument.NominalCurrency = CleanString(values[43]);
                if (values.Length > 44) instrument.USIndicator = ParseInt(values[44]);
                if (values.Length > 45) instrument.ExpiryDate = ParseInt(values[45]);
                if (values.Length > 46) instrument.VersionNumber = ParseInt(values[46]);
                if (values.Length > 47) instrument.SettlementType = ParseInt(values[47]);
                if (values.Length > 48) instrument.OptionType = ParseInt(values[48]);
                if (values.Length > 49) instrument.ExerciseType = ParseInt(values[49]);
                if (values.Length > 50) instrument.ProductName = CleanString(values[50]);
                if (values.Length > 51) instrument.Status = ParseInt(values[51]);
                if (values.Length > 52) instrument.InitialPhaseID = ParseInt(values[52]);
                if (values.Length > 53) instrument.ThresholdMax = ParseDouble(values[53]);
                if (values.Length > 54) instrument.ThresholdMin = ParseDouble(values[54]);
                if (values.Length > 55) instrument.IsLeverage = ParseBool(values[55]);
                if (values.Length > 56) instrument.NominalValue = ParseDouble(values[56]);
                
                // Validation - require at least ISIN
                if (string.IsNullOrWhiteSpace(instrument.ISIN))
                {
                    return null;
                }
                
                return instrument;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing instrument: {ex.Message}");
                return null;
            }
        }

        // Helper methods for parsing values
        private string CleanString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            
            // Remove quotes and trim
            value = value.Trim('"', ' ', '\t');
            return value;
        }

        private int ParseInt(string value)
        {
            value = CleanString(value);
            if (string.IsNullOrEmpty(value))
                return 0;
            
            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
                return result;
            return 0;
        }

        private double ParseDouble(string value)
        {
            value = CleanString(value);
            if (string.IsNullOrEmpty(value))
                return 0;
            
            // Handle both culture-specific number formats
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return result;
            return 0;
        }

        private DateTime ParseDate(string value)
        {
            value = CleanString(value);
            if (string.IsNullOrEmpty(value))
                return DateTime.MinValue;
            
            // Try various date formats
            string[] formats = { 
                "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "MM/dd/yyyy",
                "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss"
            };
            
            foreach (string format in formats)
            {
                if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                    return result;
            }
            
            if (DateTime.TryParse(value, out DateTime fallbackResult))
                return fallbackResult;
                
            return DateTime.MinValue;
        }

        private bool ParseBool(string value)
        {
            value = CleanString(value);
            if (string.IsNullOrEmpty(value))
                return false;
            
            // Handle various boolean representations
            value = value.ToLower();
            if (value == "true" || value == "1" || value == "yes" || value == "t" || value == "y")
                return true;
            if (value == "false" || value == "0" || value == "no" || value == "f" || value == "n")
                return false;
                
            return false;
        }
    }
}