using DevilStudio.Models;
using static DevilStudio.Views.SignUp;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using ModelManager.Model;

namespace DevilStudio.Views
{
    public partial class ConfirmUserDetails : ContentPage
    {

        private List<Countrycode> countryCodes;
        private string selectedCountryCode = "+91"; // Default to India
        private string selectedCountry;
        private readonly SalesforceCredentials _credentials;


        private string _userName; // ? Change from readonly field to a private variable
                                  //private readonly string jsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UserDetails.json");

        //private const string SalesforceApiBaseUrl = "https://devilstudio-beta.my.salesforce.com/services/data/v62.0";
        //private const string SalesforceAccessToken = "00DdN00000Tiw63!AQEAQAIcssRydspG11GWRRd2f.ezhuS.3p1D3xNzkiLWZU9_nZB0mk3fjr8.t6pYzrnnGYGBt7iYx5yv1mQz00N4PngUrPcm"; // Replace with actual token
        //private const string SalesforceRefreshToken = "5Aep861f8ltQWUFLBBial9tGcKAD0HoKpvQexGkN.TqlnwCz3esBIiRM8oThscupSs4E2hl.Ph9eG5eb0TKYQdz"; // Replace with actual token
        //private const string SalesforceClientId = "3MVG9GBhY6wQjl2s741pBIpkcBmLHuhZ9jXFGbqy_ITS8Sv6pJY1sPVgvJF.6o7xPKDtudjolf4DgdUIFTJ.B"; // For token refresh
        //private const string SalesforceClientSecret = "FB51C41F29E3145D85F2CBE4405A8DC6A64977B1B1011870A8009508ACCB565A"; // For token refresh

        public ConfirmUserDetails()
        {
            InitializeComponent();
            _credentials = SalesforceCredentials.Load(); // Load credentials from the static method
            _userName = string.Empty; // ? Assign a default value to avoid null issues
            InitAsync(); // ? Call async method without awaiting inside constructor
        }

        private async Task InitAsync() // ? Change void ? Task
        {
            _userName = await SecureStorage.GetAsync("UserName") ?? string.Empty;
            await LoadUserData();
            LoadCountryCodes();
            LoadCountryNames();
        }
        private void LoadCountryCodes()
        {
            // Load country codes from the embedded JSON file
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("DevilStudio.countrycodes.json"))
            using (StreamReader reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                countryCodes = JsonConvert.DeserializeObject<List<Countrycode>>(json);
            }

            // Populate the Picker
            CountryCodePicker.ItemsSource = countryCodes.Select(c => $"{c.Country} ({c.DialCode})").ToList();

            // Set default country code (India)
            var defaultIndex = countryCodes.FindIndex(c => c.DialCode == "+91");
            CountryCodePicker.SelectedIndex = defaultIndex;
        }
        private void LoadCountryNames()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("DevilStudio.countrycodes.json"))
            using (StreamReader reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                countryCodes = JsonConvert.DeserializeObject<List<Countrycode>>(json);
            }

            // Populate Country Name Picker
            CountryPicker.ItemsSource = countryCodes.Select(c => c.Country).ToList();
            var defaultIndex = countryCodes.FindIndex(c => c.Country == "India");
            CountryPicker.SelectedIndex = defaultIndex;
        }
        private void OnCountryCodeChanged(object sender, EventArgs e)
        {
            var selectedItem = CountryCodePicker.SelectedItem as string;
            selectedCountryCode = countryCodes.FirstOrDefault(c => $"{c.Country} ({c.DialCode})" == selectedItem)?.DialCode;
        }

        private async Task LoadUserData()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _credentials.SalesforceAccessToken);

                var query = $"SELECT Id, Name, FirstName, LastName, Email FROM Contact WHERE Name = '{_userName}'";
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
                        var user = records[0];
                        UsernameEntry.Text = user.Name;
                        EmailEntry.Text = user.Email;
                        FirstnameEntry.Text = user.FirstName;
                        LastnameEntry.Text = user.LastName;
                    }
                    else
                    {
                        await DisplayAlert("Error", "User not found in Salesforce.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while loading user data: {ex.Message}", "OK");
            }
        }

        //private async void LoadUserData()
        //{
        //    try
        //    {
        //        using (var db = new MyDbContext())
        //        {
        //            var user = await db.UserDetails.FirstOrDefaultAsync(u => u.UserName == _userName);
        //            if (user != null)
        //            {
        //                UsernameEntry.Text = user.UserName;
        //                EmailEntry.Text = user.Email;
        //                FirstnameEntry.Text = user.FirstName;
        //                LastnameEntry.Text = user.LastName;
        //            }
        //            else
        //            {
        //                await DisplayAlert("Error", "User not found.", "OK");
        //                Console.WriteLine($"User not found: {_userName}"); // Debug statement
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        //        Console.WriteLine($"An error occurred while loading user data: {ex.Message}"); // Debug statement
        //    }
        //}
        //private async void LoadUserData()
        //{
        //    try
        //    {
        //        if (!System.IO.File.Exists(jsonFilePath))
        //        {
        //            await DisplayAlert("Error", "User data file not found.", "OK");
        //            return;
        //        }

        //        var json = await System.IO.File.ReadAllTextAsync(jsonFilePath);
        //        var users = JsonConvert.DeserializeObject<List<UserDetail>>(json) ?? new List<UserDetail>();

        //        // Find the user by username
        //        var user = users.FirstOrDefault(u => u.UserName == _userName);

        //        if (user != null)
        //        {
        //            UsernameEntry.Text = user.UserName;
        //            EmailEntry.Text = user.Email;
        //            FirstnameEntry.Text = user.FirstName;
        //            LastnameEntry.Text = user.LastName;
        //        }
        //        else
        //        {
        //            await DisplayAlert("Error", "User not found.", "OK");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"An error occurred while loading user data: {ex.Message}", "OK");
        //    }
        //}

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

        //private async Task SaveUserDataAsync(string userName, string email, string provider, string firstName, string lastName)
        //{
        //    try
        //    {
        //        using (var db = new MyDbContext())
        //        {
        //            var existingUser = await db.UserDetails.FirstOrDefaultAsync(u => u.Email == email);
        //            if (existingUser != null)
        //            {
        //                // Update existing user
        //                existingUser.LastProviderType = existingUser.ProviderType;
        //                existingUser.UserName = userName;
        //                existingUser.FirstName = firstName;
        //                existingUser.LastName = lastName;
        //                existingUser.ModifiedDate = DateTime.Now;
        //            }
        //            else
        //            {
        //                // Add new user
        //                var newUser = new UserDetail
        //                {
        //                    UserName = userName,
        //                    Email = email,
        //                    LastProviderType = 0,
        //                    FirstName = firstName,
        //                    LastName = lastName,
        //                    CreatedDate = DateTime.Now,
        //                    ModifiedDate = DateTime.Now
        //                };

        //                db.UserDetails.Add(newUser);
        //            }

        //            int result = await db.SaveChangesAsync();
        //            if (result > 0)
        //            {
        //                // Store the logged-in username in the static class
        //                UserSession.UserName = userName;
        //                UserSession.ProviderType = provider;

        //                // Navigate to ConfirmUserDetails page
        //                await Navigation.PushAsync(new ConfirmUserDetails());
        //            }
        //            else
        //            {
        //                await DisplayAlert("Error", "Failed to save user data.", "OK");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        //    }
        //}

        //private async Task SaveUserDataAsync(string userName, string email, string provider, string firstName, string lastName)
        //{
        //    try
        //    {
        //        // Ensure the JSON file exists or initialize an empty list
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

        //        // Check if the user already exists
        //        var existingUser = users.FirstOrDefault(u => u.Email == email);

        //        if (existingUser != null)
        //        {
        //            // Update existing user
        //            existingUser.LastProviderType = existingUser.ProviderType;
        //            existingUser.UserName = userName;
        //            existingUser.FirstName = firstName;
        //            existingUser.LastName = lastName;
        //            existingUser.ModifiedDate = DateTime.Now;
        //        }
        //        else
        //        {
        //            // Add new user
        //            var newUser = new UserDetail
        //            {
        //                UserName = userName,
        //                Email = email,
        //                LastProviderType = 0,
        //                FirstName = firstName,
        //                LastName = lastName,
        //                CreatedDate = DateTime.Now,
        //                ModifiedDate = DateTime.Now
        //            };

        //            users.Add(newUser);
        //        }

        //        // Save updated list back to the JSON file
        //        var updatedJson = JsonConvert.SerializeObject(users, Formatting.Indented);
        //        await System.IO.File.WriteAllTextAsync(jsonFilePath, updatedJson);

        //        // Store the logged-in username in the static class
        //        UserSession.UserName = userName;
        //        UserSession.ProviderType = provider;

        //        // Navigate to ConfirmUserDetails page
        //        await Navigation.PushAsync(new ConfirmUserDetails());
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"An error occurred while saving user data: {ex.Message}", "OK");
        //    }
        //}


        private void OnSubmitButtonClicked(object sender, EventArgs e)
        {
            // Clear any previous error messages
            MobileNumberErrorLabel.IsVisible = false;

            // Get the mobile number entered by the user
            string mobileNumber = MobileNumberEntry.Text;

            // Validate the mobile number
            if (string.IsNullOrEmpty(mobileNumber))
            {
                // Show error if the mobile number is empty
                MobileNumberErrorLabel.Text = "Mobile number is required.";
                MobileNumberErrorLabel.IsVisible = true;
            }
            else if (!IsValidMobileNumber(mobileNumber))
            {
                // Show error if the mobile number is not valid
                MobileNumberErrorLabel.Text = "Please enter a valid mobile number for the selected country.";
                MobileNumberErrorLabel.IsVisible = true;
            }
            else
            {
                // Proceed with saving the data or navigating to the next page
                SaveMobileNumber(mobileNumber, selectedCountry);
            }
        }

        private bool IsValidMobileNumber(string mobileNumber)
        {
            // Get the country-specific rules from the JSON or hardcoded list
            var country = countryCodes.FirstOrDefault(c => c.DialCode == selectedCountryCode);

            if (country == null)
            {
                // If no country is found, default to a general validation
                return new Regex(@"^\d+$").IsMatch(mobileNumber); // Default: Only digits allowed
            }

            // Define country-specific mobile number rules
            var countryRules = new Dictionary<string, string>
            {
                  { "+91", @"^\d{10}$" }, // Example: India requires a 10-digit number
                  { "+1", @"^\d{10}$" },  // USA: 10-digit number
                  { "+44", @"^\d{10,11}$" }, // UK: 10 to 11 digits
                  { "+971", @"^\d{9}$" } // UAE: Mobile numbers are exactly 9 digits after the country code
        // Add more rules as needed for other countries
            };

            // Validate the mobile number against the country-specific rule
            if (countryRules.ContainsKey(country.DialCode))
            {
                return new Regex(countryRules[country.DialCode]).IsMatch(mobileNumber);
            }

            // Default validation if no specific rule exists for the country
            return new Regex(@"^\d+$").IsMatch(mobileNumber); // General: Only digits allowed
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
        private async void SaveMobileNumber(string mobileNumber, string country)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _credentials.SalesforceAccessToken);

                var query = $"SELECT Id, MobilePhone, MailingCountry FROM Contact WHERE Name = '{_userName}'";
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
                        var existingUserId = records[0].Id.ToString();
                        string fullMobileNumber = $"{selectedCountryCode} {mobileNumber}";

                        var updatePayload = new
                        {
                            MobilePhone = fullMobileNumber,
                            MailingCountry = country
                        };

                        var updateJsonPayload = JsonConvert.SerializeObject(updatePayload);
                        var updateResponse = await httpClient.PatchAsync(
                            $"{_credentials.SalesforceApiBaseUrl}/sobjects/Contact/{existingUserId}",
                            new StringContent(updateJsonPayload, Encoding.UTF8, "application/json"));

                        if (updateResponse.IsSuccessStatusCode)
                        {
                            //await DisplayAlert("Success", "Mobile number saved successfully!", "OK");
                            var userDetail = new UserDetail { UserName = _userName, MobileNumber = fullMobileNumber, Country = country };
                            await Navigation.PushAsync(new ConsentPage(userDetail));
                        }
                        else
                        {
                            throw new Exception($"Failed to update mobile number: {updateResponse.ReasonPhrase}");
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", "User not found in Salesforce.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while saving the mobile number: {ex.Message}", "OK");
            }
        }

        private void OnCountryChanged(object sender, EventArgs e)
        {
            selectedCountry = CountryPicker.SelectedItem as string;
        }


        //private async void OnSkip(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        using var httpClient = new HttpClient();
        //        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SalesforceAccessToken);

        //        // Query Salesforce to find the user by username
        //        var query = $"SELECT Id FROM Contact WHERE Name = '{_userName}'";
        //        var queryResponse = await httpClient.GetAsync($"{SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");

        //        if (queryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        //        {
        //            // Refresh the access token if expired
        //            var newAccessToken = await RefreshAccessTokenAsync(SalesforceRefreshToken);
        //            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);
        //            queryResponse = await httpClient.GetAsync($"{SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");
        //        }

        //        if (queryResponse.IsSuccessStatusCode)
        //        {
        //            var queryContent = await queryResponse.Content.ReadAsStringAsync();
        //            var queryData = JsonConvert.DeserializeObject<dynamic>(queryContent);
        //            var records = queryData.records;

        //            if (records != null && records.Count > 0)
        //            {
        //                var userId = records[0].Id.ToString();

        //                // Mark the user's mobile number as incomplete in Salesforce
        //                var updatePayload = new { Phone = (string)null, InCompleteMobileNumber__c = true };
        //                var updateJsonPayload = JsonConvert.SerializeObject(updatePayload);

        //                var updateResponse = await httpClient.PatchAsync(
        //                    $"{SalesforceApiBaseUrl}/sobjects/Contact/{userId}",
        //                    new StringContent(updateJsonPayload, Encoding.UTF8, "application/json"));

        //                if (updateResponse.IsSuccessStatusCode)
        //                {
        //                    var userDetail = new UserDetail { UserName = _userName };
        //                    await Navigation.PushAsync(new ConsentPage(userDetail));
        //                }
        //                else
        //                {
        //                    throw new Exception($"Failed to update user status in Salesforce: {updateResponse.ReasonPhrase}");
        //                }
        //            }
        //            else
        //            {
        //                await DisplayAlert("Error", "User not found in Salesforce.", "OK");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        //    }
        //}

        //private async void SaveMobileNumber(string mobileNumber)
        //{
        //    try
        //    {
        //        using (var db = new MyDbContext())
        //        {
        //            var user = await db.UserDetails.FirstOrDefaultAsync(u => u.UserName == _userName);
        //            if (user != null)
        //            {
        //                string fullMobileNumber = $"{selectedCountryCode} {mobileNumber}";
        //                user.MobileNumber = fullMobileNumber;
        //                await db.SaveChangesAsync();

        //                await DisplayAlert("Success", "Mobile number saved successfully!", "OK");
        //                await Navigation.PushAsync(new ConsentPage(user));
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"An error occurred while saving the mobile number: {ex.Message}", "OK");
        //    }
        //}
        //private async void SaveMobileNumber(string mobileNumber)
        //{
        //    try
        //    {
        //        if (!System.IO.File.Exists(jsonFilePath))
        //        {
        //            await DisplayAlert("Error", "User data file not found.", "OK");
        //            return;
        //        }

        //        var json = await System.IO.File.ReadAllTextAsync(jsonFilePath);
        //        var users = JsonConvert.DeserializeObject<List<UserDetail>>(json) ?? new List<UserDetail>();

        //        // Find the user by username
        //        var user = users.FirstOrDefault(u => u.UserName == _userName);

        //        if (user != null)
        //        {
        //            string fullMobileNumber = $"{selectedCountryCode} {mobileNumber}";
        //            user.MobileNumber = fullMobileNumber;

        //            // Save updated data back to the JSON file
        //            var updatedJson = JsonConvert.SerializeObject(users, Formatting.Indented);
        //            await System.IO.File.WriteAllTextAsync(jsonFilePath, updatedJson);

        //            await DisplayAlert("Success", "Mobile number saved successfully!", "OK");
        //            await Navigation.PushAsync(new ConsentPage(user));
        //        }
        //        else
        //        {
        //            await DisplayAlert("Error", "User not found.", "OK");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"An error occurred while saving the mobile number: {ex.Message}", "OK");
        //    }
        //}

        //private async void OnSkip(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (!System.IO.File.Exists(jsonFilePath))
        //        {
        //            await DisplayAlert("Error", "User data file not found.", "OK");
        //            return;
        //        }

        //        var json = await System.IO.File.ReadAllTextAsync(jsonFilePath);
        //        var users = JsonConvert.DeserializeObject<List<UserDetail>>(json) ?? new List<UserDetail>();

        //        // Find the user by username
        //        var user = users.FirstOrDefault(u => u.UserName == _userName);

        //        if (user != null)
        //        {
        //            user.InCompleteMobileNumber = true;
        //            user.MobileNumber = null; // Explicitly set to null

        //            // Save updated data back to the JSON file
        //            var updatedJson = JsonConvert.SerializeObject(users, Formatting.Indented);
        //            await System.IO.File.WriteAllTextAsync(jsonFilePath, updatedJson);

        //            await Navigation.PushAsync(new ConsentPage(user));
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


        //private async void OnSkip(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        using (var db = new MyDbContext())
        //        {
        //            // Fetch the current user
        //            var user = await db.UserDetails.FirstOrDefaultAsync(u => u.UserName == _userName);

        //            if (user != null)
        //            {
        //                // Mark as setup complete without mobile number
        //                user.InCompleteMobileNumber = true;
        //                user.MobileNumber = null; // Explicitly set to null
        //                await db.SaveChangesAsync();

        //                // Navigate to ConsentPage
        //                await Navigation.PushAsync(new ConsentPage(user));
        //            }
        //            else
        //            {
        //                await DisplayAlert("Error", "User not found.", "OK");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        //    }
        //}
    }
}
