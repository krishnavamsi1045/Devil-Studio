using DevilStudio.Models;

namespace DevilStudio.Views;

public partial class LinkedInUserDetails : ContentPage
{
    private string? _email;
    private TaskCompletionSource<UserDetail> _completionSource;

    public LinkedInUserDetails(string? email, TaskCompletionSource<UserDetail> completionSource)
    {
        InitializeComponent();
        _email = email;
        _completionSource = completionSource;

        EmailEntry.Text = _email; // Display the extracted email (ReadOnly)
    }

    private void OnSubmitButtonClicked(object sender, EventArgs e)
    {
        string? userName = UsernameEntry.Text?.Trim();
        string? firstName = FirstnameEntry.Text?.Trim();
        string? lastName = LastnameEntry.Text?.Trim();

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
        {
            DisplayAlert("Error", "All fields are required!", "OK");
            return;
        }

        // Store user details in UserDetail model
        var userDetails = new UserDetail
        {
            UserName = userName,
            FirstName = firstName,
            LastName = lastName,
            Email = _email
        };

        // Return the user details to SignUpWithProvider
        _completionSource.SetResult(userDetails);

        // Navigate back
        //Navigation.PopAsync();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var appShell = (AppShell)Application.Current.MainPage;
        appShell.ConsentHeaderVisibility(false); // Hides the header on ConsentPage

        // Check if the user's subscription is still valid
        //await CheckSubscriptionStatus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        var appShell = (AppShell)Application.Current.MainPage;
        appShell.ConsentHeaderVisibility(true); // Show header on other pages
    }
}