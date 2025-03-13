using DevilStudio.Models;
using Google.Apis.Drive.v3.Data;
using Microsoft.EntityFrameworkCore;
using ModelManager.Model;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text;
namespace DevilStudio.Views;

public partial class ConsentPage : ContentPage
{
    //private readonly MyDbContext _dbContext;
    private readonly UserDetail _currentUser;
    private readonly SalesforceCredentials _credentials;
    //private readonly string jsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UserDetails.json");

    //private const string SalesforceApiBaseUrl = "https://devilstudio-beta.my.salesforce.com/services/data/v62.0";
    //private const string SalesforceAccessToken = "00DdN00000Tiw63!AQEAQAIcssRydspG11GWRRd2f.ezhuS.3p1D3xNzkiLWZU9_nZB0mk3fjr8.t6pYzrnnGYGBt7iYx5yv1mQz00N4PngUrPcm"; // Replace with actual token
    //private const string SalesforceRefreshToken = "5Aep861f8ltQWUFLBBial9tGcKAD0HoKpvQexGkN.TqlnwCz3esBIiRM8oThscupSs4E2hl.Ph9eG5eb0TKYQdz"; // Replace with actual token
    //private const string SalesforceClientId = "3MVG9GBhY6wQjl2s741pBIpkcBmLHuhZ9jXFGbqy_ITS8Sv6pJY1sPVgvJF.6o7xPKDtudjolf4DgdUIFTJ.B"; // For token refresh
    //private const string SalesforceClientSecret = "FB51C41F29E3145D85F2CBE4405A8DC6A64977B1B1011870A8009508ACCB565A"; // For token refresh

    public ConsentPage(UserDetail currentUser)
    {
        InitializeComponent();
        //_dbContext = new MyDbContext();
        _credentials = SalesforceCredentials.Load(); // Load credentials from the static method
        _currentUser = currentUser;
        BindingContext = new ConsentViewModel();
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

    // Enable or disable the Accept button based on checkbox state
    private void OnAgreementCheckedChanged(object sender, CheckedChangedEventArgs e)
    {

        if (sender == AgreementCheckBox)
        {
            // Make the "I Accept" button visible when the mandatory checkbox is checked
            AcceptButton.IsEnabled = e.Value;
        }
        else if (sender == CheckBox) // First checkbox
        {
            _currentUser.CookiesChk = e.Value ? true : false;
        }
        else if (sender == CheckBox1) // Third checkbox
        {
            _currentUser.Personalchk = e.Value ? true : false;
        }
        else if (sender == CheckBox2) // Fourth checkbox
        {
            _currentUser.Advertisementchk = e.Value ? true : false;
        }
        else if (sender == CheckBox3) // Fifth checkbox
        {
            _currentUser.Marketingchk = e.Value ? true : false;
        }
    }

    private async Task<string> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            using var httpClient = new HttpClient();

            var requestData = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", _credentials.SalesforceClientId },
            { "client_secret", _credentials.SalesforceClientSecret }
        };

            var response = await httpClient.PostAsync("https://login.salesforce.com/services/oauth2/token", new FormUrlEncodedContent(requestData));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<dynamic>(content);
                var newAccessToken = tokenResponse.access_token;

                // Optionally update the hardcoded SalesforceAccessToken (if mutable)
                return newAccessToken;
            }
            else
            {
                throw new Exception($"Failed to refresh access token: {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while refreshing the access token: {ex.Message}", "OK");
            return null;
        }
    }

    private async Task CheckSubscriptionStatus()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _credentials.SalesforceAccessToken);

            var query = $"SELECT Id, PlanType__c, EndDate__c FROM Contact WHERE Name = '{_currentUser}'";
            var queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");

            if (queryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var newAccessToken = await RefreshAccessTokenAsync(_credentials.SalesforceRefreshToken);
                if (!string.IsNullOrEmpty(newAccessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);
                    queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");
                }
            }

            if (queryResponse.IsSuccessStatusCode)
            {
                var queryContent = await queryResponse.Content.ReadAsStringAsync();
                var queryData = JsonConvert.DeserializeObject<dynamic>(queryContent);
                var records = queryData.records;

                if (records != null && records.Count > 0)
                {
                    var planType = records[0].PlanType__c?.ToString();
                    var endDate = DateTime.Parse(records[0].EndDate__c.ToString());

                    if (planType == null || endDate < DateTime.Now)
                    {
                        AcceptButton.IsVisible = true;
                    }
                    else
                    {
                        AcceptButton.IsVisible = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while checking subscription status: {ex.Message}", "OK");
        }
    }

    private async void OnAcceptClicked(object sender, EventArgs e)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _credentials.SalesforceAccessToken);

            // Step 1: Query Salesforce for the current user's account
            var query = $"SELECT Id FROM Contact WHERE Name = '{_currentUser.UserName}'";
            var queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");

            if (queryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var newAccessToken = await RefreshAccessTokenAsync(_credentials.SalesforceRefreshToken);
                if (!string.IsNullOrEmpty(newAccessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);
                    queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");
                }
            }

            if (queryResponse.IsSuccessStatusCode)
            {
                var queryContent = await queryResponse.Content.ReadAsStringAsync();
                var queryData = JsonConvert.DeserializeObject<dynamic>(queryContent);
                var records = queryData.records;

                if (records != null && records.Count > 0)
                {
                    // Step 2: Update subscription details for the user
                    var existingUserId = records[0].Id.ToString();

                    var updatePayload = new
                    {
                        IsActive__c = true,
                        CookiesChk__c = CheckBox.IsChecked,
                        Personalchk__c = CheckBox1.IsChecked,
                        Advertisementchk__c = CheckBox2.IsChecked,
                        Marketingchk__c = CheckBox3.IsChecked
                    };

                    var updateJsonPayload = JsonConvert.SerializeObject(updatePayload);
                    var updateResponse = await httpClient.PatchAsync(
                        $"{_credentials.SalesforceApiBaseUrl}/sobjects/Contact/{existingUserId}",
                        new StringContent(updateJsonPayload, Encoding.UTF8, "application/json"));

                    if (updateResponse.IsSuccessStatusCode)
                    {
                        // App.Current.UserAppTheme = AppTheme.Light; // Temporary switch to light theme
                        ShowMessage("You have been subscribed for 180 days free.", true);
                        App.Current.UserAppTheme = AppTheme.Unspecified; // Restore system theme
                        AcceptButton.IsVisible = false;

                        if (App.Current.MainPage is AppShell appShell)
                        {
                            appShell.LoadUserData(); // Refresh the app's header after the update
                        }

                        await Navigation.PushAsync(new MainPage());
                    }
                    else
                    {
                        throw new Exception($"Failed to update subscription status: {updateResponse.ReasonPhrase}");
                    }
                }
                else
                {
                    ShowMessage("User not found in Salesforce.", false);
                }
            }
            else
            {
                throw new Exception($"Failed to query Salesforce: {queryResponse.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"An error occurred: {ex.Message}", false);
        }
    }


    //private async Task CheckSubscriptionStatus()
    //{
    //    try
    //    {
    //        // Load users from JSON file
    //        if (!System.IO.File.Exists(jsonFilePath))
    //            return;

    //        var json = await System.IO.File.ReadAllTextAsync(jsonFilePath);
    //        var users = JsonConvert.DeserializeObject<List<UserDetail>>(json) ?? new List<UserDetail>();

    //        // Find the current user
    //        var user = users.FirstOrDefault(u => u.Id == _currentUser.Id);
    //        if (user?.PlanType == null)
    //        {
    //            AcceptButton.IsVisible = true;
    //        }
    //        else if (user.EndDate >= DateTime.Now)
    //        {
    //            AcceptButton.IsVisible = false;
    //        }
    //        else
    //        {
    //            AcceptButton.IsVisible = true;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        await DisplayAlert("Error", $"An error occurred while checking subscription status: {ex.Message}", "OK");
    //    }
    //}

    //private async void OnAcceptClicked(object sender, EventArgs e)
    //{
    //    try
    //    {
    //        // Load existing users from JSON file
    //        List<UserDetail> users;
    //        if (System.IO.File.Exists(jsonFilePath))
    //        {
    //            var json = await System.IO.File.ReadAllTextAsync(jsonFilePath);
    //            users = JsonConvert.DeserializeObject<List<UserDetail>>(json) ?? new List<UserDetail>();
    //        }
    //        else
    //        {
    //            users = new List<UserDetail>();
    //        }

    //        // Find the current user
    //        var user = users.FirstOrDefault(u => u.Id == _currentUser.Id);
    //        if (user != null)
    //        {
    //            DateTime startDate = DateTime.Now;
    //            DateTime endDate = startDate.AddDays(180); // Set subscription duration to 180 days

    //            // Update user details
    //            user.ModifiedDate = DateTime.Now;
    //            user.PlanType = "Free(180 Days)";
    //            user.StartDate = startDate;
    //            user.EndDate = endDate;
    //            user.IsActive = true;

    //            // Update checkbox states
    //            user.CookiesChk = CheckBox.IsChecked;
    //            user.Personalchk = CheckBox1.IsChecked;
    //            user.Advertisementchk = CheckBox2.IsChecked;
    //            user.Marketingchk = CheckBox3.IsChecked;

    //            // Save updated list back to the JSON file
    //            var updatedJson = JsonConvert.SerializeObject(users, Formatting.Indented);
    //            await System.IO.File.WriteAllTextAsync(jsonFilePath, updatedJson);

    //            await DisplayAlert("Success", "You have been subscribed for 180 days free.", "OK");
    //            // Hide the Accept button and ensure TakeSubscriptions is hidden
    //            AcceptButton.IsVisible = false;

    //            // Navigate to the main page
    //            if (App.Current.MainPage is AppShell appShell)
    //            {
    //                appShell.LoadUserData(); // Refresh header after login
    //            }
    //            await Navigation.PushAsync(new MainPage());
    //        }
    //        else
    //        {
    //            await DisplayAlert("Error", "User not found.", "OK");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
    //    }
    //}

    //private async Task CheckSubscriptionStatus()
    //{
    //    // Get the user's subscription from the database
    //    var subscription = await _dbContext.UserDetails
    //        .AsNoTracking()
    //        .Where(s => s.Id == _currentUser.Id)
    //        .OrderByDescending(s => s.EndDate)
    //        .FirstOrDefaultAsync();
    //    if (subscription?.PlanType == null)
    //    {
    //        AcceptButton.IsVisible = true;
    //        //TakeSubscriptionsButton.IsVisible = true;
    //    }

    //    else if (subscription.EndDate >= DateTime.Now)
    //    {
    //        // If the 180-day subscription is still active, hide TakeSubscriptions and show Accept
    //        AcceptButton.IsVisible = false;
    //        // TakeSubscriptionsButton.IsVisible = true;
    //        //await Navigation.PushAsync(new MainPage());
    //    }
    //    else
    //    {
    //        // If the 180-day subscription has expired, show TakeSubscriptions and hide Accept
    //        AcceptButton.IsVisible = false;
    //        //TakeSubscriptionsButton.IsVisible = true;
    //    }
    //}

    //private async void OnAcceptClicked(object sender, EventArgs e)
    //{
    //    _currentUser.ModifiedDate = DateTime.Now;

    //    DateTime startDate = DateTime.Now;
    //    DateTime endDate = startDate.AddDays(180);  // Set the subscription duration to 180 days

    //    var freePlan = "Free(180 Days)";

    //    _currentUser.PlanType = freePlan;
    //    _currentUser.StartDate = startDate;
    //    _currentUser.EndDate = endDate;
    //    _currentUser.IsActive = true;

    //    // Save Checkbox States (0 for unchecked, 1 for checked)
    //    _currentUser.CookiesChk = CheckBox.IsChecked ? true : false;
    //    _currentUser.Personalchk = CheckBox1.IsChecked ? true : false;
    //    _currentUser.Advertisementchk = CheckBox2.IsChecked ? true : false;
    //    _currentUser.Marketingchk = CheckBox3.IsChecked ? true : false;
    //    // Update the user consent in the database
    //    _dbContext.UserDetails.Update(_currentUser);
    //    await _dbContext.SaveChangesAsync();

    //    await DisplayAlert("Success", $"You have been subscribed for 180 days free.", "OK");

    //    // Hide the Accept button and ensure TakeSubscriptions is hidden
    //    AcceptButton.IsVisible = false;
    //    //TakeSubscriptionsButton.IsVisible = false;
    //    if (App.Current.MainPage is AppShell appShell)
    //    {
    //        appShell.LoadUserData(); // Refresh header after login
    //    }
    //    await Navigation.PushAsync(new MainPage());
    //}

    private async void OnDecline(object sender, EventArgs e)
    {
        // navigate to signup page.
        Application.Current.MainPage = new AppShell();
    }

    //private async void TakeSubscriptions(object sender, EventArgs e)
    //{
    //    //await Navigation.PushAsync(new Subscription(_currentUser));

    //    await DisplayAlert("Alert!", $"Comming Soon...", "OK");
    //    if (App.Current.MainPage is AppShell appShell)
    //    {
    //        appShell.LoadUserData(); // Refresh header after login
    //    }
    //    await Navigation.PushAsync(new MainPage());
    //}

    private void ShowMessage(string message, bool isSuccess)
    {
        MessageLabel.Text = message;
        MessageLabel.TextColor = isSuccess ? Microsoft.Maui.Graphics.Colors.Green : Microsoft.Maui.Graphics.Colors.Red;
        MessageLabel.IsVisible = true;

        // Optionally hide the message after a delay
        Microsoft.Maui.Controls.Device.StartTimer(TimeSpan.FromSeconds(5), () =>
        {
            MessageLabel.IsVisible = false;
            return false; // Stops the timer
        });
    }
}

