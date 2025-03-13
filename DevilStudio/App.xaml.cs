
using DevilStudio.Views;
using ModelManager.Model;
using System.Reflection;
using Microsoft.Maui.Controls;

namespace DevilStudio
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
           // Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzQzMTIwMUAzMjM2MmUzMDJlMzBLOFlLNnVKZFV2WUYrVkMyVlJnQ05sUCtWY3NjSWtaYkFUcWZLKzExWmF3PQ==");

            // Get the version from the assembly
            string appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            //MainPage = new NavigationPage(new AppShell());
            // Set the splash screen as the initial page
            MainPage = new SplashPage();

            // Check for an existing session after a delay
            Task.Delay(5000).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    CheckSessionAsync();
                });
            });
        }

        private async void CheckSessionAsync()
        {
            try
            {
                var accessToken = await SecureStorage.GetAsync("AccessToken");
                var provider = await SecureStorage.GetAsync("Provider");
                var refreshToken = await SecureStorage.GetAsync("RefreshToken");

                if (!string.IsNullOrEmpty(accessToken) && await IsTokenValid(refreshToken, accessToken, provider))
                {
                    SessionManager.ObjLastopen = true;
                    MainPage = new AppShell(); // Set MainPage to AppShell
                    await Shell.Current.GoToAsync("//MainPage"); // Navigate to MainPage
                }
                else
                {
                    MainPage = new AppShell();
                    await Shell.Current.GoToAsync("//SignUp");
                }
            }
            catch
            {
                MainPage = new AppShell();
                await Shell.Current.GoToAsync("//SignUp");
            }
        }

        private static async Task<bool> IsTokenValid(string refreshToken, string accessToken, string provider)
        {
            try
            {
                var oauthService = new OAuthService();

                if (provider == "GitHub")
                {
                    await SecureStorage.SetAsync("AccessToken", accessToken);
                    var usersInfo = await oauthService.GetUserInfoAsync(provider, accessToken);
                    return usersInfo != null;
                }
                else if (provider == "LinkedIn")
                {
                    await SecureStorage.SetAsync("AccessToken", accessToken);
                    return true;
                }
                else
                {
                    var newTokens = await oauthService.RefreshAccessTokenAsync(refreshToken, provider);
                    await SecureStorage.SetAsync("AccessToken", newTokens);
                    var userInfo = await oauthService.GetUserInfoAsync(provider, newTokens);
                    return userInfo != null;
                }
            }
            catch
            {
                return await RefreshToken();
            }
        }

        private static async Task<bool> RefreshToken()
        {
            var refreshToken = await SecureStorage.GetAsync("RefreshToken");
            var provider = await SecureStorage.GetAsync("Provider");

            if (string.IsNullOrEmpty(refreshToken) || provider == "GitHub")
                return false;

            try
            {
                var oauthService = new OAuthService();
                var newTokens = await oauthService.RefreshAccessTokenAsync(refreshToken, provider);
                await SecureStorage.SetAsync("AccessToken", newTokens);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}