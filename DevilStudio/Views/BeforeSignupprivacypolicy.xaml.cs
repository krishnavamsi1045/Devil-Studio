namespace DevilStudio.Views;

public partial class BeforeSignupprivacypolicy : ContentPage
{
    public BeforeSignupprivacypolicy()
    {
        InitializeComponent();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            IsVisible = false
        });
        // Access the AppShell instance and hide the header content
        var appShell = (AppShell)Application.Current.MainPage;
        appShell.PrivacyHeaderVisibility(false); // Hides the welcome label and provider logo
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Show the header again when leaving the ConnectionAccount page
        var appShell = (AppShell)Application.Current.MainPage;
        appShell.PrivacyHeaderVisibility(true); // Shows the header on other pages
    }
    private async void Back(object sender, EventArgs e)
    {
        Application.Current.MainPage = new AppShell();
    }
}