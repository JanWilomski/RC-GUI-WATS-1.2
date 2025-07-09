using System;
using RC_GUI_WATS.Services;

namespace RC_GUI_WATS.Services
{
    public class SettingsService
    {
        private readonly ConfigurationService _configurationService;

        public event EventHandler SettingsApplied;

        // Properties for CCG Message Colors
        public string ColorCcgError { get; set; }
        public string ColorCcgMassQuote { get; set; }
        public string ColorCcgMassQuoteResponse { get; set; }
        public string ColorCcgOrderAdd { get; set; }
        public string ColorCcgOrderAddResponse { get; set; }
        public string ColorCcgTrade { get; set; }
        public string ColorCcgOrderCancel { get; set; }
        public string ColorCcgOrderCancelResponse { get; set; }
        public string ColorCcgOrderModify { get; set; }
        public string ColorCcgOrderModifyResponse { get; set; }
        public string ColorCcgOrderMassCancel { get; set; }
        public string ColorCcgOrderMassCancelResponse { get; set; }
        public string ColorCcgReject { get; set; }
        public string ColorCcgLogin { get; set; }
        public string ColorCcgLoginResponse { get; set; }
        public string ColorCcgTradeCaptureReportSingle { get; set; }
        public string ColorCcgTradeCaptureReportDual { get; set; }

        // Theme Setting
        public string ThemeName { get; set; }

        // Visual Settings
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public double WindowTop { get; set; }
        public double WindowLeft { get; set; }
        public string WindowState { get; set; }

        // Properties for Order Book Colors
        public string ColorOrderBookStatusNew { get; set; }
        public string ColorOrderBookStatusPartiallyFilled { get; set; }
        public string ColorOrderBookStatusFilled { get; set; }
        public string ColorOrderBookStatusCancelled { get; set; }
        public string ColorOrderBookStatusRejected { get; set; }
        public string ColorOrderBookSideBuy { get; set; }
        public string ColorOrderBookSideSell { get; set; }

        public SettingsService(ConfigurationService configurationService)
        {
            _configurationService = configurationService;
            LoadSettings();
        }

        public void LoadSettings()
        {
            // Load CCG Message Colors
            ColorCcgError = _configurationService.GetConfigValue("Color_Ccg_Error", "Yellow");
            ColorCcgMassQuote = _configurationService.GetConfigValue("Color_Ccg_MassQuote", "#E6E6FA");
            ColorCcgMassQuoteResponse = _configurationService.GetConfigValue("Color_Ccg_MassQuoteResponse", "#F0E6FF");
            ColorCcgOrderAdd = _configurationService.GetConfigValue("Color_Ccg_OrderAdd", "LawnGreen");
            ColorCcgOrderAddResponse = _configurationService.GetConfigValue("Color_Ccg_OrderAddResponse", "#98FB98");
            ColorCcgTrade = _configurationService.GetConfigValue("Color_Ccg_Trade", "#ADD8E6");
            ColorCcgOrderCancel = _configurationService.GetConfigValue("Color_Ccg_OrderCancel", "Red");
            ColorCcgOrderCancelResponse = _configurationService.GetConfigValue("Color_Ccg_OrderCancelResponse", "#F08080");
            ColorCcgOrderModify = _configurationService.GetConfigValue("Color_Ccg_OrderModify", "#FFFFE0");
            ColorCcgOrderModifyResponse = _configurationService.GetConfigValue("Color_Ccg_OrderModifyResponse", "#FFFFE0");
            ColorCcgOrderMassCancel = _configurationService.GetConfigValue("Color_Ccg_OrderMassCancel", "#FFB6C1");
            ColorCcgOrderMassCancelResponse = _configurationService.GetConfigValue("Color_Ccg_OrderMassCancelResponse", "#FFB6C1");
            ColorCcgReject = _configurationService.GetConfigValue("Color_Ccg_Reject", "#FFC0CB");
            ColorCcgLogin = _configurationService.GetConfigValue("Color_Ccg_Login", "#FFA500");
            ColorCcgLoginResponse = _configurationService.GetConfigValue("Color_Ccg_LoginResponse", "#FFA500");
            ColorCcgTradeCaptureReportSingle = _configurationService.GetConfigValue("Color_Ccg_TradeCaptureReportSingle", "#E0FFFF");
            ColorCcgTradeCaptureReportDual = _configurationService.GetConfigValue("Color_Ccg_TradeCaptureReportDual", "#E0FFFF");

            // Load Theme Setting
            ThemeName = _configurationService.GetConfigValue("Theme", "Light");
            Console.WriteLine($"SettingsService: Loaded ThemeName={ThemeName}");

            // Load Visual Settings
            WindowWidth = AppConfig.WindowWidth;
            WindowHeight = AppConfig.WindowHeight;
            WindowTop = AppConfig.WindowTop;
            WindowLeft = AppConfig.WindowLeft;
            WindowState = AppConfig.WindowState.ToString();

            // Load Order Book Colors
            ColorOrderBookStatusNew = _configurationService.GetConfigValue("Color_OrderBook_Status_New", "#E6F3FF");
            ColorOrderBookStatusPartiallyFilled = _configurationService.GetConfigValue("Color_OrderBook_Status_PartiallyFilled", "#FFFACD");
            ColorOrderBookStatusFilled = _configurationService.GetConfigValue("Color_OrderBook_Status_Filled", "#E6FFE6");
            ColorOrderBookStatusCancelled = _configurationService.GetConfigValue("Color_OrderBook_Status_Cancelled", "#F0F0F0");
            ColorOrderBookStatusRejected = _configurationService.GetConfigValue("Color_OrderBook_Status_Rejected", "#FFE6E6");
            ColorOrderBookSideBuy = _configurationService.GetConfigValue("Color_OrderBook_Side_Buy", "Green");
            ColorOrderBookSideSell = _configurationService.GetConfigValue("Color_OrderBook_Side_Sell", "Red");
        }

        public void ApplySettings()
        {
            // Save CCG Message Colors
            _configurationService.UpdateConfigValue("Color_Ccg_Error", ColorCcgError);
            _configurationService.UpdateConfigValue("Color_Ccg_MassQuote", ColorCcgMassQuote);
            _configurationService.UpdateConfigValue("Color_Ccg_MassQuoteResponse", ColorCcgMassQuoteResponse);
            _configurationService.UpdateConfigValue("Color_Ccg_OrderAdd", ColorCcgOrderAdd);
            _configurationService.UpdateConfigValue("Color_Ccg_OrderAddResponse", ColorCcgOrderAddResponse);
            _configurationService.UpdateConfigValue("Color_Ccg_Trade", ColorCcgTrade);
            _configurationService.UpdateConfigValue("Color_Ccg_OrderCancel", ColorCcgOrderCancel);
            _configurationService.UpdateConfigValue("Color_Ccg_OrderCancelResponse", ColorCcgOrderCancelResponse);
            _configurationService.UpdateConfigValue("Color_Ccg_OrderModify", ColorCcgOrderModify);
            _configurationService.UpdateConfigValue("Color_Ccg_OrderModifyResponse", ColorCcgOrderModifyResponse);
            _configurationService.UpdateConfigValue("Color_Ccg_OrderMassCancel", ColorCcgOrderMassCancel);
            _configurationService.UpdateConfigValue("Color_Ccg_OrderMassCancelResponse", ColorCcgOrderMassCancelResponse);
            _configurationService.UpdateConfigValue("Color_Ccg_Reject", ColorCcgReject);
            _configurationService.UpdateConfigValue("Color_Ccg_Login", ColorCcgLogin);
            _configurationService.UpdateConfigValue("Color_Ccg_LoginResponse", ColorCcgLoginResponse);
            _configurationService.UpdateConfigValue("Color_Ccg_TradeCaptureReportSingle", ColorCcgTradeCaptureReportSingle);
            _configurationService.UpdateConfigValue("Color_Ccg_TradeCaptureReportDual", ColorCcgTradeCaptureReportDual);

            // Save Theme Setting
            _configurationService.UpdateConfigValue("Theme", ThemeName);

            // Save Visual Settings
            _configurationService.UpdateConfigValue("WindowWidth", WindowWidth.ToString());
            _configurationService.UpdateConfigValue("WindowHeight", WindowHeight.ToString());
            _configurationService.UpdateConfigValue("WindowTop", WindowTop.ToString());
            _configurationService.UpdateConfigValue("WindowLeft", WindowLeft.ToString());
            _configurationService.UpdateConfigValue("WindowState", WindowState.ToString());

            // Save Order Book Colors
            _configurationService.UpdateConfigValue("Color_OrderBook_Status_New", ColorOrderBookStatusNew);
            _configurationService.UpdateConfigValue("Color_OrderBook_Status_PartiallyFilled", ColorOrderBookStatusPartiallyFilled);
            _configurationService.UpdateConfigValue("Color_OrderBook_Status_Filled", ColorOrderBookStatusFilled);
            _configurationService.UpdateConfigValue("Color_OrderBook_Status_Cancelled", ColorOrderBookStatusCancelled);
            _configurationService.UpdateConfigValue("Color_OrderBook_Status_Rejected", ColorOrderBookStatusRejected);
            _configurationService.UpdateConfigValue("Color_OrderBook_Side_Buy", ColorOrderBookSideBuy);
            _configurationService.UpdateConfigValue("Color_OrderBook_Side_Sell", ColorOrderBookSideSell);

            OnSettingsApplied();
        }

        protected virtual void OnSettingsApplied()
        {
            SettingsApplied?.Invoke(this, EventArgs.Empty);
        }
    }
}
