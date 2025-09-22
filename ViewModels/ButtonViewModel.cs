using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Media;

namespace OscilloscopeApp.ViewModels
{
    public partial class ButtonViewModel : ObservableObject
    {
        [ObservableProperty] private bool isButtonConnected = false;
        public string ConnectButtonText => IsButtonConnected ? "Disconnect" : "Connect";
        public string ConnectButtonColor => IsButtonConnected ? "Red" : "Green";

        public event EventHandler<bool>? ButtonConnectedChanged;

        public ButtonViewModel()
        {
            // Initial state is already set by field initializers
            Console.WriteLine("Init button ViewModel.");

        }

        partial void OnIsButtonConnectedChanged(bool value)
        {
            Console.WriteLine("Button connection state changed.");
            ButtonConnectedChanged?.Invoke(this, value);
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
