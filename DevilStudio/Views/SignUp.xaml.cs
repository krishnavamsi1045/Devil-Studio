using Google;
using Microsoft.Identity.Client;
using static System.Net.WebRequestMethods;
using DevilStudio.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Azure.Core;
using Google.Apis.Drive.v3.Data;
using DocumentFormat.OpenXml.EMMA;
using Newtonsoft.Json;
using System.Text;
using ModelManager.ViewModel;
using ModelManager.Model;

namespace DevilStudio.Views;

public partial class SignUp : ContentPage
{

    private readonly OAuthService _oauthService;
    private readonly OAuthHelper _oauthHelper;
    private readonly SalesforceCredentials _credentials;
    //private readonly MyDbContext _context;
    //private readonly string jsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UserDetails.json");
    //private const string SalesforceApiBaseUrl = "https://devilstudio-beta.my.salesforce.com/services/data/v62.0";
    //private const string SalesforceAccessToken = "00DdN00000Tiw63!AQEAQAIcssRydspG11GWRRd2f.ezhuS.3p1D3xNzkiLWZU9_nZB0mk3fjr8.t6pYzrnnGYGBt7iYx5yv1mQz00N4PngUrPcm"; // Replace with actual token
    //private const string SalesforceRefreshToken = "5Aep861f8ltQWUFLBBial9tGcKAD0HoKpvQexGkN.TqlnwCz3esBIiRM8oThscupSs4E2hl.Ph9eG5eb0TKYQdz"; // Replace with actual token
    //private const string SalesforceClientId = "3MVG9GBhY6wQjl2s741pBIpkcBmLHuhZ9jXFGbqy_ITS8Sv6pJY1sPVgvJF.6o7xPKDtudjolf4DgdUIFTJ.B"; // For token refresh
    //private const string SalesforceClientSecret = "FB51C41F29E3145D85F2CBE4405A8DC6A64977B1B1011870A8009508ACCB565A"; // For token refresh

    public SignUp()
    {
        InitializeComponent();
        _credentials = SalesforceCredentials.Load(); // Load credentials from the static method
        _oauthService = new OAuthService();
        _oauthHelper = new OAuthHelper();
        //_context = new MyDbContext();
    }

    private async Task UpdateUserStatusAsync(UserDetail user)
    {
        try
        {
            string Uname = user.FirstName + " " + user.LastName;
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _credentials.SalesforceAccessToken);

            // Query Salesforce for the user by UserName
            var query = $"SELECT Id, IsActive__c FROM Contact WHERE Name = '{Uname}'";
            var queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");

            if (queryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var newAccessToken = await RefreshAccessTokenAsync(_credentials.SalesforceRefreshToken);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);
                queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");
            }

            if (queryResponse.IsSuccessStatusCode)
            {
                var queryContent = await queryResponse.Content.ReadAsStringAsync();
                var queryData = JsonConvert.DeserializeObject<dynamic>(queryContent);
                var records = queryData.records;

                if (records != null && records.Count > 0)
                {
                    var users = records[0];
                    var userId = records[0].Id.ToString();

                    var updatePayload = new
                    {
                        users.IsActive__c
                    };

                    var jsonPayload = JsonConvert.SerializeObject(updatePayload);
                    var updateResponse = await httpClient.PatchAsync(
                        $"{_credentials.SalesforceApiBaseUrl}/sobjects/Contact/{userId}",
                        new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

                    if (!updateResponse.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to update user status in Salesforce: {updateResponse.ReasonPhrase}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while updating user status: {ex.Message}", "OK");
        }
    }

    private async Task<bool> IsActiveAsync(string userName)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _credentials.SalesforceAccessToken);

            var query = $"SELECT IsActive__c FROM Contact WHERE Name = '{userName}'";
            var queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");

            if (queryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var newAccessToken = await RefreshAccessTokenAsync(_credentials.SalesforceRefreshToken);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);
                queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");
            }

            if (queryResponse.IsSuccessStatusCode)
            {
                var queryContent = await queryResponse.Content.ReadAsStringAsync();
                var queryData = JsonConvert.DeserializeObject<dynamic>(queryContent);
                var records = queryData.records;

                if (records != null && records.Count > 0)
                {
                    return records[0].IsActive__c == true;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while checking user status: {ex.Message}", "OK");
            return false;
        }
    }

    private async Task<bool> IsMobileNumberNullAsync(string userName)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _credentials.SalesforceAccessToken);

            var query = $"SELECT MobilePhone FROM Contact WHERE Name = '{userName}'";
            var queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");

            if (queryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var newAccessToken = await RefreshAccessTokenAsync(_credentials.SalesforceRefreshToken);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);
                queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");
            }

            if (queryResponse.IsSuccessStatusCode)
            {
                var queryContent = await queryResponse.Content.ReadAsStringAsync();
                var queryData = JsonConvert.DeserializeObject<dynamic>(queryContent);
                var records = queryData.records;

                if (records != null && records.Count > 0)
                {
                    return string.IsNullOrEmpty(records[0].MobilePhone.ToString());
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while checking the mobile number: {ex.Message}", "OK");
            return true;
        }
    }

    //private async Task UpdateUserStatusAsync(UserDetail user)
    //{
    //    try
    //    {
    //        // Load user data from the JSON file
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

    //        // Find the existing user by UserName
    //        var existingUser = users.FirstOrDefault(u => u.UserName == user.UserName);

    //        if (existingUser != null)
    //        {
    //            // Update the user's IsActive and ModifiedDate fields
    //            existingUser.IsActive = user.IsActive;
    //            existingUser.ModifiedDate = DateTime.Now;

    //            // Save the updated list back to the JSON file
    //            var updatedJson = JsonConvert.SerializeObject(users, Formatting.Indented);
    //            await System.IO.File.WriteAllTextAsync(jsonFilePath, updatedJson);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        await DisplayAlert("Error", $"An error occurred while updating user status: {ex.Message}", "OK");
    //    }
    //}

    //private async Task UpdateUserStatusAsync(UserDetail user)
    //{
    //    using var context = new MyDbContext();
    //    var existingUser = await context.UserDetails.FirstOrDefaultAsync(u => u.UserName == user.UserName);

    //    if (existingUser != null)
    //    {
    //        existingUser.IsActive = user.IsActive; // Update IsActive based on EndDate
    //        existingUser.ModifiedDate = DateTime.Now;
    //        await context.SaveChangesAsync();
    //    }
    //}
    //private async Task<bool> IsActiveAsync(string userName)
    //{
    //    using var context = new MyDbContext();
    //    var isActive = await context.UserDetails.FirstOrDefaultAsync(u => u.UserName == userName);
    //    return isActive?.IsActive == true;
    //}
    //private async Task<bool> IsActiveAsync(string userName)
    //{
    //    if (!System.IO.File.Exists(jsonFilePath))
    //        return false;

    //    var json = await System.IO.File.ReadAllTextAsync(jsonFilePath);
    //    var users = JsonConvert.DeserializeObject<List<UserDetail>>(json) ?? new List<UserDetail>();
    //    var user = users.FirstOrDefault(u => u.UserName == userName);
    //    return user?.IsActive == true;
    //}

    //private async Task<bool> IsMobileNumberNullAsync(string userName)
    //{
    //    if (!System.IO.File.Exists(jsonFilePath))
    //        return true;

    //    var json = await System.IO.File.ReadAllTextAsync(jsonFilePath);
    //    var users = JsonConvert.DeserializeObject<List<UserDetail>>(json) ?? new List<UserDetail>();
    //    var user = users.FirstOrDefault(u => u.UserName == userName);
    //    return string.IsNullOrEmpty(user?.MobileNumber);
    //}


    //private async Task<bool> IsMobileNumberNullAsync(string userName)
    //{
    //    using (var context = new MyDbContext())
    //    {
    //        var user = await context.UserDetails.FirstOrDefaultAsync(u => u.UserName == userName);
    //        return user?.MobileNumber == null;
    //    }
    //}


    //private async Task<UserDetail> SaveUserDataAsync(string userName, string email, string provider, string firstName, string lastName, string refreshToken, string accessToken)
    //{
    //    string? access_token = await _oauthService.RefreshAccessTokenAsync(refreshToken, provider);
    //    using (var db = new MyDbContext())
    //    {
    //        var existingUser = await db.UserDetails.FirstOrDefaultAsync(u => u.Email == email);

    //        if (existingUser != null)
    //        {
    //            // Update existing user
    //            existingUser.LastProviderType = existingUser.ProviderType; // Set last provider type to current provider type
    //            existingUser.ProviderType = GetProviderType(provider); // Update current provider type
    //            existingUser.UserName = userName; // Update username
    //            existingUser.FirstName = firstName;
    //            existingUser.LastName = lastName;
    //            existingUser.ModifiedDate = DateTime.Now;
    //            existingUser.RefreshToken = refreshToken;
    //            existingUser.AccessToken = access_token;
    //        }
    //        else
    //        {
    //            var newUser = new UserDetail
    //            {
    //                UserName = userName, // Set username
    //                Email = email,
    //                ProviderType = GetProviderType(provider), // Set current provider type
    //                LastProviderType = 0, // No previous provider type for new users
    //                FirstName = firstName,
    //                LastName = lastName,
    //                CreatedDate = DateTime.Now,
    //                ModifiedDate = DateTime.Now,
    //                RefreshToken = refreshToken,
    //                AccessToken = accessToken

    //            };

    //            db.UserDetails.Add(newUser);

    //            db.ConnectedAccounts.Add(new ConnectedAccount
    //            {
    //                UserDetail = newUser, // Set the foreign key relationship
    //                ProviderType = GetProviderType(provider),
    //                UserName = userName, // Set username
    //                Email = email,
    //                AccessToken = accessToken,
    //                CreatedDate = DateTime.Now
    //            });

    //            existingUser = newUser;
    //        }

    //        await db.SaveChangesAsync();
    //        UserSession.UserName = userName;
    //        UserSession.ProviderType = provider;
    //        UserSession.Email = email;

    //        return existingUser; // Return the UserDetail object
    //    }
    //}
    //private async Task<UserDetail> SaveUserDataAsync(string userName, string email, string provider, string firstName, string lastName, string refreshToken, string accessToken)
    //{
    //    // Create a list to hold user details
    //    List<UserDetail> users;

    //    // Load existing data from the JSON file
    //    if (System.IO.File.Exists(jsonFilePath))
    //    {
    //        var json = await System.IO.File.ReadAllTextAsync(jsonFilePath);
    //        users = JsonConvert.DeserializeObject<List<UserDetail>>(json) ?? new List<UserDetail>();
    //    }
    //    else
    //    {
    //        users = new List<UserDetail>();
    //    }

    //    // Check if the user already exists
    //    var existingUser = users.FirstOrDefault(u => u.Email == email);

    //    if (existingUser != null)
    //    {
    //        // Update existing user
    //        existingUser.LastProviderType = existingUser.ProviderType;
    //        existingUser.ProviderType = GetProviderType(provider);
    //        existingUser.UserName = userName;
    //        existingUser.FirstName = firstName;
    //        existingUser.LastName = lastName;
    //        existingUser.ModifiedDate = DateTime.Now;
    //        existingUser.RefreshToken = refreshToken;
    //        existingUser.AccessToken = accessToken;
    //    }
    //    else
    //    {
    //        // Add a new user
    //        var newUser = new UserDetail
    //        {
    //            UserName = userName,
    //            Email = email,
    //            ProviderType = GetProviderType(provider),
    //            LastProviderType = 0,
    //            FirstName = firstName,
    //            LastName = lastName,
    //            CreatedDate = DateTime.Now,
    //            ModifiedDate = DateTime.Now,
    //            RefreshToken = refreshToken,
    //            AccessToken = accessToken
    //        };

    //        users.Add(newUser);
    //        existingUser = newUser;
    //    }

    //    // Save the updated list back to the JSON file
    //    var updatedJson = JsonConvert.SerializeObject(users, Formatting.Indented);
    //    await System.IO.File.WriteAllTextAsync(jsonFilePath, updatedJson);
    //    UserSession.UserName = userName;
    //    UserSession.ProviderType = provider;
    //    UserSession.Email = email;
    //    return existingUser;
    //}
    private async Task<UserDetail> SaveUserDataAsync(string userName, string firstName, string lastName, string email, string provider, string refreshToken, string accessToken)
    {
        try
        {
            //UserSession.UserName = userName;
            //UserSession.ProviderType = provider;
            //UserSession.Email = email;
            DateTime startDate = DateTime.Now;
            DateTime endDate = startDate.AddDays(180); // 180-day subscription
            string Uname = firstName + " " + lastName;
            await SecureStorage.SetAsync("UserName", Uname);
            await SecureStorage.SetAsync("Email", email);
            await SecureStorage.SetAsync("Provider", provider);
            await SecureStorage.SetAsync("AccessToken", accessToken);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await SecureStorage.SetAsync("RefreshToken", refreshToken);
            }

            var providerType = GetProviderType(provider);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _credentials.SalesforceAccessToken);

            // Step 1: Check if the user already exists in Salesforce
            var query = $"SELECT Id, IsActive__c, PlanType__c, EndDate__c, StartDate__c FROM Contact WHERE Name = '{Uname}'";
            var queryResponse = await httpClient.GetAsync($"{_credentials.SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");

            if (queryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Access token expired, refresh it
                var newAccessToken = await RefreshAccessTokenAsync(_credentials.SalesforceRefreshToken);

                if (!string.IsNullOrEmpty(newAccessToken))
                {
                    // Retry the query with the refreshed token
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
                    // User already exists, update their information
                    var existingUser = records[0];
                    var existingUserId = records[0].Id.ToString();

                    var updatePayload = new
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        //ProviderType__c = providerType,
                        //LastProviderType__c = (int)existingUser.ProviderType__c,
                        //RefreshToken__c = refreshToken,
                        //AccessToken__c = accessToken,
                        existingUser.IsActive__c,
                        existingUser.PlanType__c,
                        //existingUser.InCompleteMobileNumber__c,
                        existingUser.StartDate__c,
                        existingUser.EndDate__c
                    };

                    var updateJsonPayload = JsonConvert.SerializeObject(updatePayload);
                    var updateResponse = await httpClient.PatchAsync(
                        $"{_credentials.SalesforceApiBaseUrl}/sobjects/Contact/{existingUserId}",
                        new StringContent(updateJsonPayload, Encoding.UTF8, "application/json"));

                    if (updateResponse.IsSuccessStatusCode)
                    {
                        // Return the updated user details
                        return new UserDetail
                        {
                            UserName = userName,
                            FirstName = firstName,
                            LastName = lastName,
                            Email = email,
                            //ProviderType = providerType,
                            //LastProviderType = updatePayload.LastProviderType__c,
                            //RefreshToken = refreshToken,
                            //AccessToken = accessToken,
                            ModifiedDate = DateTime.Now,
                            IsActive = updatePayload.IsActive__c,
                            PlanType = updatePayload.PlanType__c,
                            //InCompleteMobileNumber = updatePayload.InCompleteMobileNumber__c,
                            StartDate = updatePayload.StartDate__c,
                            EndDate = updatePayload.EndDate__c,
                        };
                    }
                    else
                    {
                        throw new Exception($"Failed to update user in Salesforce: {updateResponse.ReasonPhrase}");
                    }
                }
            }

            // Step 2: If the user does not exist, create a new user
            var createPayload = new
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                //ProviderType__c = providerType,
                //LastProviderType__c = 0,
                //RefreshToken__c = refreshToken,
                //AccessToken__c = accessToken,
                PlanType__c = "Free",
                StartDate__c = startDate.ToString("yyyy-MM-dd"),
                EndDate__c = endDate.ToString("yyyy-MM-dd")
            };

            var createJsonPayload = JsonConvert.SerializeObject(createPayload);
            var createResponse = await httpClient.PostAsync(
                $"{_credentials.SalesforceApiBaseUrl}/sobjects/Contact",
                new StringContent(createJsonPayload, Encoding.UTF8, "application/json"));

            if (createResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Access token expired, refresh it
                var newAccessToken = await RefreshAccessTokenAsync(_credentials.SalesforceRefreshToken);

                if (!string.IsNullOrEmpty(newAccessToken))
                {
                    // Retry the creation with the refreshed token
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);
                    createResponse = await httpClient.PostAsync(
                        $"{_credentials.SalesforceApiBaseUrl}/sobjects/Contact",
                        new StringContent(createJsonPayload, Encoding.UTF8, "application/json"));
                }
            }

            if (createResponse.IsSuccessStatusCode)
            {
                var createContent = await createResponse.Content.ReadAsStringAsync();
                var createData = JsonConvert.DeserializeObject<dynamic>(createContent);

                // Return the newly created user details
                return new UserDetail
                {
                    UserName = userName,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    //ProviderType = GetProviderType(provider),
                    //LastProviderType = 0,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    IsActive = false,
                    PlanType = null,
                    InCompleteMobileNumber = false,
                    StartDate = startDate,
                    EndDate = endDate,
                };
            }

            throw new Exception($"Failed to create user in Salesforce: {createResponse.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while saving user data: {ex.Message}", "OK");
            return null;
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

    private async void SignUpWithProvider(string provider)
    {
        string authorizationUrl = _oauthService.GetOAuthUrl(provider);

        try
        {
            await Launcher.Default.OpenAsync(new Uri(authorizationUrl));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to open browser: {ex.Message}", "OK");
            return;
        }

        string code = await _oauthHelper.StartLocalHttpServerAsync();
        if (!string.IsNullOrEmpty(code))
        {
            var tokenResponse = await _oauthService.GetAccessTokenAsync(provider, code);
            if (tokenResponse != null)
            {
                string? accessToken = tokenResponse.AccessToken;
                string? refreshToken = tokenResponse.RefreshToken;

                // Declare these variables at the start to make them accessible globally
                JObject? userInfo = null;
                string? userName = null;
                string? firstName = null;
                string? lastName = null;
                string? email = null;
                UserDetail? userDetail = null;

                if (provider == "LinkedIn")
                {
                    string? emailAddress = tokenResponse.Email; // Extract email from id_token

                    // Wait for user to input their details on LinkedInUserDetails page
                    var completionSource = new TaskCompletionSource<UserDetail>();
                    await Navigation.PushAsync(new LinkedInUserDetails(emailAddress, completionSource));

                    userDetail = await completionSource.Task; // Wait for user input
                }
                else
                {
                    // Fetch user info for non-LinkedIn providers
                    userInfo = await _oauthService.GetUserInfoAsync(provider, accessToken);

                    // Extract values based on the provider
                    userName = provider == "GitHub" ? userInfo["login"]?.ToString() : userInfo["name"]?.ToString();
                    email = userInfo["email"]?.ToString() ?? "noemail@domain.com";

                    if (provider == "GitHub")
                    {
                        string? fullName = userInfo["name"]?.ToString();
                        if (!string.IsNullOrEmpty(fullName))
                        {
                            var nameParts = fullName.Trim().Split(' ');
                            firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                            lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : string.Empty;
                        }
                    }
                    else
                    {
                        firstName = userInfo["given_name"]?.ToString();
                        lastName = userInfo["family_name"]?.ToString();
                    }
                }

                // After returning from LinkedInUserDetails, process userDetail
                if (provider == "LinkedIn" && userDetail != null)
                {
                    userName = userDetail.UserName;
                    firstName = userDetail.FirstName;
                    lastName = userDetail.LastName;
                    email = userDetail.Email;
                }

                // Now use userName, firstName, lastName, and email for all providers
                if (!string.IsNullOrEmpty(userName))
                {
                    bool isMobileNumberNull = await IsMobileNumberNullAsync(firstName + " " + lastName);

                    var user = await SaveUserDataAsync(userName, firstName, lastName, email, provider, refreshToken, accessToken);

                    if (isMobileNumberNull)
                    {
                        if (App.Current.MainPage is AppShell appShell)
                        {
                            appShell.LoadUserData(); // Refresh header after login
                        }
                        await Navigation.PushAsync(new ConfirmUserDetails());
                    }
                    else
                    {
                        // Check EndDate and set IsActive accordingly
                        if (user.EndDate.HasValue && user.EndDate.Value < DateTime.Now)
                        {
                            user.IsActive = false;
                        }

                        await UpdateUserStatusAsync(user); // Update IsActive in the database

                        bool isActive = await IsActiveAsync(firstName + " " + lastName);
                        if (isActive && user.PlanType != null)
                        {
                            if (App.Current.MainPage is AppShell appShell)
                            {
                                appShell.LoadUserData(); // Refresh header after login
                            }
                            await Navigation.PushAsync(new MainPage());
                        }
                        else
                        {
                            if (App.Current.MainPage is AppShell appShell)
                            {
                                appShell.LoadUserData(); // Refresh header after login
                            }
                            await Navigation.PushAsync(new ConsentPage(user)); // Pass UserDetail object here
                        }
                    }
                }
                else
                {
                    await DisplayAlert("Error", "Failed to retrieve username.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Failure", "Failed to get access token.", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "Failed to retrieve authorization code.", "OK");
        }
    }

    private int GetProviderType(string provider)
    {
        switch (provider)
        {
            case "Google":
                return 1;
            case "Facebook":
                return 2;
            case "Instagram":
                return 3;
            case "GitHub":
                return 4;
            case "LinkedIn":
                return 5;
            case "YouTube":
                return 6;
            default:
                throw new NotImplementedException();
        }
    }
    private string GetProviderType1(int provider)
    {
        switch (provider)
        {
            case 1:
                return "Google";
            case 2:
                return "Facebook";
            case 3:
                return "Instagram";
            case 4:
                return "GitHub";
            case 5:
                return "LinkedIn";
            case 6:
                return "YouTube";
            default:
                throw new NotImplementedException();
        }
    }

    private void SignUpWithGoogle(object sender, EventArgs e)
    {
        SignUpWithProvider("Google");
    }

    private void SignUpWithGitHub(object sender, EventArgs e)
    {
        SignUpWithProvider("GitHub");
    }
    // Add similar methods for other providers if needed

    private void SignUpWithFacebook(object sender, EventArgs e)
    {
        //SignUpWithProvider("Facebook");
        DisplayAlert("Alert!", "Coming Soon...", "OK");
    }

    private void SignUpWithInstagram(object sender, EventArgs e)
    {
        //SignUpWithProvider("Instagram");
        DisplayAlert("Alert!", "Coming Soon...", "OK");
    }

    private void SignUpWithLinkedIn(object sender, EventArgs e)
    {
        SignUpWithProvider("LinkedIn");
        //DisplayAlert("Alert!", "Coming Soon...", "OK");
    }
    private void SignUpWithYouTube(object sender, EventArgs e)
    {
        SignUpWithProvider("YouTube");
    }
    public static class UserSession
    {
        public static string? UserName { get; set; }
        public static string? ProviderType { get; set; }
        public static string? Email { get; set; }
    }

    // Added tapgesture privacy policy for footer 
    private async void OnPrivacyPolicyTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new BeforeSignupprivacypolicy());
    }

    //submit button
    //private async void userNameSubmit_Clicked(object sender, EventArgs e)
    //{
    //    var emailid = userName.Text;

    //    if (string.IsNullOrEmpty(emailid))
    //    {
    //        await DisplayAlert("Invalid", "Please enter Email Address", "OK");
    //        userName.Text = string.Empty;
    //        return;
    //    }

    //    try
    //    {
    //        using var httpClient = new HttpClient();
    //        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SalesforceAccessToken);

    //        var query = $"SELECT Id, Name, Email, ProviderType__c FROM Contact WHERE Email = '{emailid}'";
    //        var queryResponse = await httpClient.GetAsync($"{SalesforceApiBaseUrl}/query?q={Uri.EscapeDataString(query)}");

    //        if (queryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    //        {
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
    //                var user = records[0];
    //                //UserSession.UserName = user.Name;
    //                //UserSession.ProviderType = GetProviderType1((int)user.ProviderType__c);
    //                //UserSession.Email = user.Email;
    //                await SecureStorage.SetAsync("UserName", user.Name.ToString());
    //                await SecureStorage.SetAsync("Provider", GetProviderType1((int)user.ProviderType__c));
    //                await SecureStorage.SetAsync("Email", user.Email.ToString());
    //                await SecureStorage.SetAsync("AccessToken", user.AccessToken__c.ToString());

    //                if (!string.IsNullOrEmpty(user.RefreshToken__c.ToString()))
    //                {
    //                    await SecureStorage.SetAsync("RefreshToken", user.RefreshToken__c.ToString());
    //                }
    //                if (App.Current.MainPage is AppShell appShell)
    //                {
    //                    appShell.LoadUserData();
    //                }

    //                await Navigation.PushAsync(new MainPage());
    //            }
    //            else
    //            {
    //                await DisplayAlert("Error", "Email Address is not valid", "OK");
    //                userName.Text = string.Empty;
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
    //    }
    //}

    //private async void userNameSubmit_Clicked(object sender, EventArgs e)
    //{
    //    var emailid = userName.Text;

    //    if (string.IsNullOrEmpty(emailid))
    //    {
    //        await DisplayAlert("Invalid", "Please enter Username or Email Address", "OK");
    //        userName.Text = string.Empty;
    //        return;
    //    }

    //    try
    //    {
    //        // Load user data from the JSON file
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

    //        // Find the user by username or email
    //        var user = users.FirstOrDefault(x => x.UserName == emailid || x.Email == emailid);
    //        if (user == null)
    //        {
    //            userName.Text = string.Empty;
    //            await DisplayAlert("Error", "Email Address or Username is not valid", "OK");
    //            return;
    //        }
    //        else
    //        {
    //            // Update the UserSession with the found user's details
    //            UserSession.UserName = user.UserName;
    //            UserSession.ProviderType = GetProviderType1(user.ProviderType);
    //            UserSession.Email = user.Email;

    //            // Refresh header after login
    //            if (App.Current.MainPage is AppShell appShell)
    //            {
    //                appShell.LoadUserData();
    //            }

    //            // Navigate to the MainPage
    //            await Navigation.PushAsync(new MainPage());
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
    //    }
    //}

    //private async void userNameSubmit_Clicked(object sender, EventArgs e)
    //{
    //    var emailid = userName.Text;

    //    if (string.IsNullOrEmpty(emailid))
    //    {
    //        await DisplayAlert("Invalid", "Please enter Username or Email Address", "OK");
    //        userName.Text = string.Empty;
    //        return;
    //    }

    //    // Assign UserName here after validation
    //    //var username = userName.Text;

    //    var user = _context.UserDetails.FirstOrDefault(x => x.UserName == emailid || x.Email == emailid);
    //    if (user == null)
    //    {
    //        userName.Text = string.Empty;
    //        await DisplayAlert("Error", "Email Address or Username is not valid", "OK");
    //        return;
    //    }
    //    else
    //    {
    //        UserSession.UserName = user.UserName;
    //        UserSession.ProviderType = GetProviderType1(user.ProviderType);
    //        UserSession.Email = user.Email;
    //        if (App.Current.MainPage is AppShell appShell)
    //        {
    //            appShell.LoadUserData(); // Refresh header after login
    //        }
    //        await Navigation.PushAsync(new MainPage());
    //    }
    //}

}
