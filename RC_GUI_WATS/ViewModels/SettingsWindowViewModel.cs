using RC_GUI_WATS.Commands;
using RC_GUI_WATS.Services;

namespace RC_GUI_WATS.ViewModels
{
    public class SettingsWindowViewModel : BaseViewModel
    {
        private readonly ThemeService _themeService;
        private readonly ConfigurationService _configurationService;

        private bool _isLightTheme;
        public bool IsLightTheme
        {
            get => _isLightTheme;
            set
            {
                if (SetProperty(ref _isLightTheme, value) && value)
                {
                    ApplyTheme("Light");
                }
            }
        }

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {             if (SetProperty(ref _isDarkTheme, value) && value)
                {
                    ApplyTheme("Dark");
                }
            }
        }

        // CCG Message Colors
        private string _colorCcgError;
        public string ColorCcgError
        {
            get => _colorCcgError;
            set
            {
                if (SetProperty(ref _colorCcgError, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_Error", value);
                }
            }
        }

        private string _colorCcgMassQuote;
        public string ColorCcgMassQuote
        {
            get => _colorCcgMassQuote;
            set
            {
                if (SetProperty(ref _colorCcgMassQuote, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_MassQuote", value);
                }
            }
        }

        private string _colorCcgMassQuoteResponse;
        public string ColorCcgMassQuoteResponse
        {
            get => _colorCcgMassQuoteResponse;
            set
            {
                if (SetProperty(ref _colorCcgMassQuoteResponse, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_MassQuoteResponse", value);
                }
            }
        }

        private string _colorCcgOrderAdd;
        public string ColorCcgOrderAdd
        {
            get => _colorCcgOrderAdd;
            set
            {
                if (SetProperty(ref _colorCcgOrderAdd, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_OrderAdd", value);
                }
            }
        }

        private string _colorCcgOrderAddResponse;
        public string ColorCcgOrderAddResponse
        {
            get => _colorCcgOrderAddResponse;
            set
            {
                if (SetProperty(ref _colorCcgOrderAddResponse, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_OrderAddResponse", value);
                }
            }
        }

        private string _colorCcgTrade;
        public string ColorCcgTrade
        {
            get => _colorCcgTrade;
            set
            {
                if (SetProperty(ref _colorCcgTrade, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_Trade", value);
                }
            }
        }

        private string _colorCcgOrderCancel;
        public string ColorCcgOrderCancel
        {
            get => _colorCcgOrderCancel;
            set
            {
                if (SetProperty(ref _colorCcgOrderCancel, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_OrderCancel", value);
                }
            }
        }

        private string _colorCcgOrderCancelResponse;
        public string ColorCcgOrderCancelResponse
        {
            get => _colorCcgOrderCancelResponse;
            set
            {
                if (SetProperty(ref _colorCcgOrderCancelResponse, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_OrderCancelResponse", value);
                }
            }
        }

        private string _colorCcgOrderModify;
        public string ColorCcgOrderModify
        {
            get => _colorCcgOrderModify;
            set
            {
                if (SetProperty(ref _colorCcgOrderModify, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_OrderModify", value);
                }
            }
        }

        private string _colorCcgOrderModifyResponse;
        public string ColorCcgOrderModifyResponse
        {
            get => _colorCcgOrderModifyResponse;
            set
            {
                if (SetProperty(ref _colorCcgOrderModifyResponse, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_OrderModifyResponse", value);
                }
            }
        }

        private string _colorCcgOrderMassCancel;
        public string ColorCcgOrderMassCancel
        {
            get => _colorCcgOrderMassCancel;
            set
            {
                if (SetProperty(ref _colorCcgOrderMassCancel, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_OrderMassCancel", value);
                }
            }
        }

        private string _colorCcgOrderMassCancelResponse;
        public string ColorCcgOrderMassCancelResponse
        {
            get => _colorCcgOrderMassCancelResponse;
            set
            {
                if (SetProperty(ref _colorCcgOrderMassCancelResponse, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_OrderMassCancelResponse", value);
                }
            }
        }

        private string _colorCcgReject;
        public string ColorCcgReject
        {
            get => _colorCcgReject;
            set
            {
                if (SetProperty(ref _colorCcgReject, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_Reject", value);
                }
            }
        }

        private string _colorCcgLogin;
        public string ColorCcgLogin
        {
            get => _colorCcgLogin;
            set
            {
                if (SetProperty(ref _colorCcgLogin, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_Login", value);
                }
            }
        }

        private string _colorCcgLoginResponse;
        public string ColorCcgLoginResponse
        {
            get => _colorCcgLoginResponse;
            set
            {
                if (SetProperty(ref _colorCcgLoginResponse, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_LoginResponse", value);
                }
            }
        }

        private string _colorCcgTradeCaptureReportSingle;
        public string ColorCcgTradeCaptureReportSingle
        {
            get => _colorCcgTradeCaptureReportSingle;
            set
            {
                if (SetProperty(ref _colorCcgTradeCaptureReportSingle, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_TradeCaptureReportSingle", value);
                }
            }
        }

        private string _colorCcgTradeCaptureReportDual;
        public string ColorCcgTradeCaptureReportDual
        {
            get => _colorCcgTradeCaptureReportDual;
            set
            {
                if (SetProperty(ref _colorCcgTradeCaptureReportDual, value))
                {
                    _configurationService.UpdateConfigValue("Color_Ccg_TradeCaptureReportDual", value);
                }
            }
        }

        // Order Book Colors
        private string _colorOrderBookStatusNew;
        public string ColorOrderBookStatusNew
        {
            get => _colorOrderBookStatusNew;
            set
            {
                if (SetProperty(ref _colorOrderBookStatusNew, value))
                {
                    _configurationService.UpdateConfigValue("Color_OrderBook_Status_New", value);
                }
            }
        }

        private string _colorOrderBookStatusPartiallyFilled;
        public string ColorOrderBookStatusPartiallyFilled
        {
            get => _colorOrderBookStatusPartiallyFilled;
            set
            {
                if (SetProperty(ref _colorOrderBookStatusPartiallyFilled, value))
                {
                    _configurationService.UpdateConfigValue("Color_OrderBook_Status_PartiallyFilled", value);
                }
            }
        }

        private string _colorOrderBookStatusFilled;
        public string ColorOrderBookStatusFilled
        {
            get => _colorOrderBookStatusFilled;
            set
            {
                if (SetProperty(ref _colorOrderBookStatusFilled, value))
                {
                    _configurationService.UpdateConfigValue("Color_OrderBook_Status_Filled", value);
                }
            }
        }

        private string _colorOrderBookStatusCancelled;
        public string ColorOrderBookStatusCancelled
        {
            get => _colorOrderBookStatusCancelled;
            set
            {
                if (SetProperty(ref _colorOrderBookStatusCancelled, value))
                {
                    _configurationService.UpdateConfigValue("Color_OrderBook_Status_Cancelled", value);
                }
            }
        }

        private string _colorOrderBookStatusRejected;
        public string ColorOrderBookStatusRejected
        {
            get => _colorOrderBookStatusRejected;
            set
            {
                if (SetProperty(ref _colorOrderBookStatusRejected, value))
                {
                    _configurationService.UpdateConfigValue("Color_OrderBook_Status_Rejected", value);
                }
            }
        }

        private string _colorOrderBookSideBuy;
        public string ColorOrderBookSideBuy
        {
            get => _colorOrderBookSideBuy;
            set
            {
                if (SetProperty(ref _colorOrderBookSideBuy, value))
                {
                    _configurationService.UpdateConfigValue("Color_OrderBook_Side_Buy", value);
                }
            }
        }

        private string _colorOrderBookSideSell;
        public string ColorOrderBookSideSell
        {
            get => _colorOrderBookSideSell;
            set
            {
                if (SetProperty(ref _colorOrderBookSideSell, value))
                {
                    _configurationService.UpdateConfigValue("Color_OrderBook_Side_Sell", value);
                }
            }
        }

        public SettingsWindowViewModel(ThemeService themeService, ConfigurationService configurationService)
        {
            _themeService = themeService;
            _configurationService = configurationService;

            var currentTheme = _configurationService.GetConfigValue("Theme", "Light");
            if (currentTheme == "Dark")
            {
                _isDarkTheme = true;
            }
            else
            {
                _isLightTheme = true;
            }

            // Initialize CCG Message Colors
            _colorCcgError = _configurationService.Color_Ccg_Error;
            _colorCcgMassQuote = _configurationService.Color_Ccg_MassQuote;
            _colorCcgMassQuoteResponse = _configurationService.Color_Ccg_MassQuoteResponse;
            _colorCcgOrderAdd = _configurationService.Color_Ccg_OrderAdd;
            _colorCcgOrderAddResponse = _configurationService.Color_Ccg_OrderAddResponse;
            _colorCcgTrade = _configurationService.Color_Ccg_Trade;
            _colorCcgOrderCancel = _configurationService.Color_Ccg_OrderCancel;
            _colorCcgOrderCancelResponse = _configurationService.Color_Ccg_OrderCancelResponse;
            _colorCcgOrderModify = _configurationService.Color_Ccg_OrderModify;
            _colorCcgOrderModifyResponse = _configurationService.Color_Ccg_OrderModifyResponse;
            _colorCcgOrderMassCancel = _configurationService.Color_Ccg_OrderMassCancel;
            _colorCcgOrderMassCancelResponse = _configurationService.Color_Ccg_OrderMassCancelResponse;
            _colorCcgReject = _configurationService.Color_Ccg_Reject;
            _colorCcgLogin = _configurationService.Color_Ccg_Login;
            _colorCcgLoginResponse = _configurationService.Color_Ccg_LoginResponse;
            _colorCcgTradeCaptureReportSingle = _configurationService.Color_Ccg_TradeCaptureReportSingle;
            _colorCcgTradeCaptureReportDual = _configurationService.Color_Ccg_TradeCaptureReportDual;

            // Initialize Order Book Colors
            _colorOrderBookStatusNew = _configurationService.Color_OrderBook_Status_New;
            _colorOrderBookStatusPartiallyFilled = _configurationService.Color_OrderBook_Status_PartiallyFilled;
            _colorOrderBookStatusFilled = _configurationService.Color_OrderBook_Status_Filled;
            _colorOrderBookStatusCancelled = _configurationService.Color_OrderBook_Status_Cancelled;
            _colorOrderBookStatusRejected = _configurationService.Color_OrderBook_Status_Rejected;
            _colorOrderBookSideBuy = _configurationService.Color_OrderBook_Side_Buy;
            _colorOrderBookSideSell = _configurationService.Color_OrderBook_Side_Sell;
        }

        private void ApplyTheme(string themeName)
        {
            _themeService.ApplyTheme(themeName);
            _configurationService.UpdateConfigValue("Theme", themeName);
        }
    }
}
