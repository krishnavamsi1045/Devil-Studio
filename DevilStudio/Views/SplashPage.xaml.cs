using Microsoft.Maui.Controls;
using System.Reflection;


namespace DevilStudio.Views
{
    public partial class SplashPage : ContentPage
    {
        public SplashPage()
        {
            InitializeComponent();
            // Fetch and display the application version
            string appVersion = AppInfo.VersionString;
            BuildVersionLabel.Text = $"Build Version {appVersion}";
        }

        //private void LoadBuildVersion()
        //{
        //    // Retrieve the version number from the assembly
        //    var version = Assembly.GetExecutingAssembly()
        //                          .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        //    // Set the version text in the label
        //    BuildVersionLabel.Text = $"Version: {version}";
        //}

        //protected override async void OnAppearing()
        //{
        //    base.OnAppearing();

        //    // Simulate a loading delay for the splash screen
        //    await Task.Delay(10000);

        //    // Navigate to the main page (replace with your actual navigation logic)
        //    Application.Current.MainPage = new AppShell();
        //}
    }
}
