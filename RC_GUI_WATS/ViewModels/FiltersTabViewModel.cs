using System;

namespace RC_GUI_WATS.ViewModels
{
    public class FiltersTabViewModel : BaseViewModel
    {
        // Currently the Filters tab is just a placeholder with no functionality
        // Keeping this class minimal to match the original application
        private string _message = "Tutaj pojawią się filtry";
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }
        
        public FiltersTabViewModel()
        {
            // Future filter functionality would be initialized here
        }
    }
}