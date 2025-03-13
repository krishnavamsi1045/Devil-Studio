using DevilStudio.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static DevilStudio.Views.SignUp;

namespace DevilStudio
{
    public partial class AppShell : Shell, INotifyPropertyChanged
    {
        private string _welcomeText;

        public string WelcomeText
        {
            get => _welcomeText;
            set
            {
                _welcomeText = value;
                OnPropertyChanged();
            }
        }

        private string _providerLogo;
        public string ProviderLogo
        {
            get => _providerLogo;
            set
            {
                _providerLogo = value;
                OnPropertyChanged(nameof(ProviderLogo));
            }
        }

        private ToolbarItem _welcomeToolbarItem;
        private ToolbarItem _providerLogoToolbarItem;

        public AppShell()
        {
            InitializeComponent();
            BindingContext = this;
            // Save references to toolbar items to use later
            _welcomeToolbarItem = WelcomeToolbarItem;
            _providerLogoToolbarItem = ProviderLogoToolbarItem;
            LoadUserData();

        }

        public async void LoadUserData()
        {
            var userName = await SecureStorage.GetAsync("UserName");
            var provider = await SecureStorage.GetAsync("Provider");

            if (!string.IsNullOrEmpty(userName))
            {
                WelcomeText = $"Welcome {userName}";
                ProviderLogo = GetProviderLogoUrl(provider);
            }
            else
            {
                WelcomeText = "Welcome Unknown";
                ProviderLogo = string.Empty;
            }
        }

        private string GetProviderLogoUrl(string providerType)
        {
            // Define URLs for provider logos
            return providerType switch
            {
                "Google" => "https://uxwing.com/wp-content/themes/uxwing/download/brands-and-social-media/google-color-icon.png",
                "GitHub" => "https://img.icons8.com/?size=512&id=63777&format=png",
                "Facebook" => "https://uxwing.com/wp-content/themes/uxwing/download/brands-and-social-media/facebook-round-color-icon.png",
                "Instagram" => "https://uxwing.com/wp-content/themes/uxwing/download/brands-and-social-media/ig-instagram-icon.png",
                "LinkedIn" => "https://uxwing.com/wp-content/themes/uxwing/download/brands-and-social-media/linkedin-app-icon.png",
                "YouTube" => "https://uxwing.com/wp-content/themes/uxwing/download/brands-and-social-media/youtube-color-icon.png",
                _ => string.Empty,
            };
        }

        private async void Logout_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Clear session information
                //UserSession.UserName = null;
                //UserSession.Email = null;
                //UserSession.ProviderType = null;

                // Clear SecureStorage
                SecureStorage.Remove("UserName");
                SecureStorage.Remove("Email");
                SecureStorage.Remove("Provider");
                SecureStorage.Remove("AccessToken");
                SecureStorage.Remove("RefreshToken");

                // Display a confirmation message to the user
                await DisplayAlert("Logged Out", "You have been successfully logged out.", "OK");

                // Clear the navigation stack to prevent the back button from returning to a logged-in state
                // In AppShell.xaml.cs (Logout_Clicked)
                await Shell.Current.GoToAsync("//SignUp");
                //Application.Current.MainPage = new AppShell(); // Optional: Only if needed

            }
            catch (Exception ex)
            {
                // Handle any exceptions
                await DisplayAlert("Error", $"An error occurred during logout: {ex.Message}", "OK");
            }
        }

        // Instead of trying to set IsVisible, remove and add the toolbar items
        public void ToggleHeaderVisibility(bool isVisible)
        {
            if (isVisible)
            {
                // Clear existing toolbar items
                this.ToolbarItems.Clear();

                // Add items in the desired order
                this.ToolbarItems.Add(HomeToolbarItem);
                this.ToolbarItems.Add(_welcomeToolbarItem);
                this.ToolbarItems.Add(_providerLogoToolbarItem);
                this.ToolbarItems.Add(UpdateProfileToolbarItem);
                this.ToolbarItems.Add(LogoutToolbarItem);
            }
            else
            {
                // Clear existing toolbar items to hide them
                this.ToolbarItems.Clear();
                // Show only Logout button
                this.ToolbarItems.Add(HomeToolbarItem);
                this.ToolbarItems.Add(UpdateProfileToolbarItem);
                this.ToolbarItems.Add(LogoutToolbarItem);
            }
        }
        public void ConsentHeaderVisibility(bool isVisible)
        {
            if (isVisible)
            {
                // Clear existing toolbar items
                this.ToolbarItems.Clear();

                // Add items in the desired order
                this.ToolbarItems.Add(HomeToolbarItem);
                this.ToolbarItems.Add(_welcomeToolbarItem);
                this.ToolbarItems.Add(_providerLogoToolbarItem);
                this.ToolbarItems.Add(UpdateProfileToolbarItem);
                this.ToolbarItems.Add(LogoutToolbarItem);
            }
            else
            {
                // Clear existing toolbar items to hide them
                this.ToolbarItems.Clear();
                this.ToolbarItems.Add(LogoutToolbarItem);
            }
        }
        public void HomeHeaderVisibility(bool isVisible)
        {
            if (isVisible)
            {
                // Clear existing toolbar items
                this.ToolbarItems.Clear();

                // Add items in the desired order
                this.ToolbarItems.Add(HomeToolbarItem);
                this.ToolbarItems.Add(_welcomeToolbarItem);
                this.ToolbarItems.Add(_providerLogoToolbarItem);
                this.ToolbarItems.Add(UpdateProfileToolbarItem);
                this.ToolbarItems.Add(LogoutToolbarItem);
            }
            else
            {
                // Clear existing toolbar items to hide them
                this.ToolbarItems.Clear();
                // Show only Logout button
                this.ToolbarItems.Add(_welcomeToolbarItem);
                this.ToolbarItems.Add(_providerLogoToolbarItem);
                this.ToolbarItems.Add(UpdateProfileToolbarItem);
                this.ToolbarItems.Add(LogoutToolbarItem);
            }
        }
        public void PrivacyHeaderVisibility(bool isVisible)
        {
            if (isVisible)
            {
                // Clear existing toolbar items
                this.ToolbarItems.Clear();

                // Add items in the desired order
                this.ToolbarItems.Add(HomeToolbarItem);
                this.ToolbarItems.Add(_welcomeToolbarItem);
                this.ToolbarItems.Add(_providerLogoToolbarItem);
                this.ToolbarItems.Add(UpdateProfileToolbarItem);
                this.ToolbarItems.Add(LogoutToolbarItem);
            }
            else
            {
                // Clear existing toolbar items to hide them
                this.ToolbarItems.Clear();
                // Show only Logout button

            }
        }


        // Implement INotifyPropertyChanged
        public new event PropertyChangedEventHandler PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void Profile_Clicked(object sender, EventArgs e)
        {
            //await Application.Current.MainPage.Navigation.PushAsync(new ConnectionAccount());
        }

        private void Home_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new MainPage());
        }

        private async void UpdateProfile(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConfirmUserDetails());
        }


    }
}
