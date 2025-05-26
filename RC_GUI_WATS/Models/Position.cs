// Models/Position.cs
using RC_GUI_WATS.ViewModels;

namespace RC_GUI_WATS.Models
{
    public class Position : BaseViewModel
    {
        private string _isin;
        public string ISIN
        {
            get => _isin;
            set => SetProperty(ref _isin, value);
        }

        private string _ticker = "";
        public string Ticker
        {
            get => _ticker;
            set => SetProperty(ref _ticker, value);
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _net;
        public int Net
        {
            get => _net;
            set => SetProperty(ref _net, value);
        }

        private int _openLong;
        public int OpenLong
        {
            get => _openLong;
            set => SetProperty(ref _openLong, value);
        }

        private int _openShort;
        public int OpenShort
        {
            get => _openShort;
            set => SetProperty(ref _openShort, value);
        }
    }
}