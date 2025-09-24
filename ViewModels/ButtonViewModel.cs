using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OscilloscopeApp.ViewModels
{
    public partial class ButtonViewModel : ObservableObject
    {
        [ObservableProperty] private bool isButtonConnected = false;

        public string ConnectButtonText => IsButtonConnected ? "Disconnect" : "Connect";
        public string ConnectButtonColor => IsButtonConnected ? "DarkRed" : "Green";
        
        // Event to notify connection state changes to MainViewModel
        public event EventHandler<bool>? ButtonConnectedChanged;

        public ButtonViewModel()
        {
            // Initial state is already set by field initializers
            Console.WriteLine("Init button ViewModel.");

        }

        // This method is called automatically when IsButtonConnected changes
        partial void OnIsButtonConnectedChanged(bool value)
        {
            Console.WriteLine("Button connection state changed.");
            ButtonConnectedChanged?.Invoke(this, value);
            // Update the button's appearance for binding
            OnPropertyChanged(nameof(ConnectButtonText));
            OnPropertyChanged(nameof(ConnectButtonColor));
        }

        [RelayCommand]
        private void ConnectButton()
        {
            Console.WriteLine("Button connection toggled." + (IsButtonConnected ? "Disconnecting..." : "Connecting..."));
            IsButtonConnected = !IsButtonConnected;
        }
    }
}
