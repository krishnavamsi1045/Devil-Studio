
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using ModelManager.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace ModelManager.Service.GoogleServices
{
    public class GoogleCloudServices
    {
        public ObservableCollection<SalesforceObject> _allSalesforceObjects = new ObservableCollection<SalesforceObject>(); 
        public ObservableCollection<SalesforceObject> SalesforceObjects { get; private set; } = new();

        public List<Account> LoadSavedAccounts()
        {
            try
            {
                var filePath = Path.Combine(FileSystem.AppDataDirectory, "connected_accounts.json");

                if (System.IO.File.Exists(filePath))
                {
                    var accountsJson = System.IO.File.ReadAllText(filePath);

                    var accounts = JsonConvert.DeserializeObject<List<Account>>(accountsJson);

                    return accounts ?? new List<Account>();
                }
                else
                {
                    return new List<Account>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load accounts: {ex.Message}");
                return new List<Account>();
            }
        }

        // Authenticate with google drive console
        public async Task<DriveService> AuthenticateUserAsync()
        {
            try
            {
                // Define the required scopes for Google Drive
                var scopes = new[] { DriveService.Scope.Drive };

                // Define client secrets
                var clientSecrets = new ClientSecrets
                {
                    ClientId = "802243565178-bj4sf9l8bg9kll136jaelq6s1t04uklo.apps.googleusercontent.com", // Replace with your client ID
                    ClientSecret = "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" // Replace with your client secret
                };

                // Use a custom data store (in-memory) to prevent token caching
                UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new NullDataStore()
                );

                // Create and return the DriveService
                var driveService = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Maui Google Drive App"
                });

                // Retrieve user information (email) from Google Drive's About API
                var aboutRequest = driveService.About.Get();
                aboutRequest.Fields = "user(emailAddress)";
                var aboutResponse = await aboutRequest.ExecuteAsync();

                // Get the user's email from the response
                string userEmail = aboutResponse.User.EmailAddress;

                // Save credentials securely
                await SaveCredentialsAsync(credential, userEmail);

                // Return the DriveService instance
                return driveService;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Authentication failed: {ex.Message}");
                return null!;
            }
        }
        private async Task SaveCredentialsAsync(UserCredential credential, string email)
        {
            try
            {
                var tokenJson = JsonConvert.SerializeObject(credential.Token);

                // Store credentials in SecureStorage using email as key
                await SecureStorage.SetAsync(email, tokenJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving credentials for {email}: {ex.Message}");
            }
        }
        //create folders inside the google drive during the salesforce connection
        public async Task<string> GetOrrrCreateFolderAsync(DriveService service, string folderName, string parentId)
        {
            // Check if folder already exists
            var folderId = await GetFolderIdAsync(service, folderName, parentId);
            if (folderId != null)
            {
                return folderId; // Folder exists, return the ID
            }

            // Create folder if it doesn't exist
            return await CreateFolderAsync(service, folderName, parentId);
        }
        public async Task UploadFileToGoogleDrive(DriveService service, string parentFolderId, string fileName, string data)
        {
            // Check if the file already exists in the folder
            var fileId = await GetFileeIdAsync(service, parentFolderId, fileName);

            // If the file exists, delete it first to replace it with the new one
            if (!string.IsNullOrEmpty(fileId))
            {
                await service.Files.Delete(fileId).ExecuteAsync();
            }

            // Create the file metadata
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileName,
                Parents = new List<string> { parentFolderId },
                MimeType = "application/json"
            };

            // Convert the data to JSON and upload the new file
         ;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                var uploadRequest = service.Files.Create(fileMetadata, stream, "application/json");
                uploadRequest.Fields = "id";
                await uploadRequest.UploadAsync();
            }
        }
        public async Task<string> GetFileeIdAsync(DriveService service, string folderId, string fileName)
        {
            var request = service.Files.List();
            request.Q = $"'{folderId}' in parents and name = '{fileName}' and trashed = false";
            request.Fields = "files(id, name)";
            var result = await request.ExecuteAsync();

            // Return the file ID if it exists, otherwise return null
            return result.Files.FirstOrDefault()?.Id!;
        }
        public async Task<string> CreateFolderAsync(DriveService service, string folderName, string parentId)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = parentId != null ? new List<string> { parentId } : null
            };

            var request = service.Files.Create(fileMetadata);
            request.Fields = "id";
            var file = await request.ExecuteAsync();

            return file.Id;
        }
        public async Task<string> GetFolderIdAsync(DriveService service, string folderName, string parentId)
        {
            string query = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}'";
            if (!string.IsNullOrEmpty(parentId))
            {
                query += $" and '{parentId}' in parents";
            }

            var request = service.Files.List();
            request.Q = query;
            request.Fields = "files(id, name)";
            var result = await request.ExecuteAsync();

            return result.Files.FirstOrDefault()?.Id!;
        }

        //Authenticate with google drive 
        public async Task<DriveService> GetGoogleDriveServiceAsync(Account account)
        {
            try
            {
                if (string.IsNullOrEmpty(account.AccessToken) || string.IsNullOrEmpty(account.RefreshToken))
                {
                    throw new Exception("Google Drive credentials not set.");
                }

                if (IsAccessTokenExpired(account.AccessToken))
                {
                    string newAccessToken = await RefreshAccessTokenAsync(account.RefreshToken);
                    account.AccessToken = newAccessToken;
                }

                var credential = GoogleCredential.FromAccessToken(account.AccessToken);

                return new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Devil Studio Trial",
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Google Drive service for {account.Email}: {ex.Message}");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
        private async Task<string> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", "802243565178-bj4sf9l8bg9kll136jaelq6s1t04uklo.apps.googleusercontent.com"),
                        new KeyValuePair<string, string>("client_secret", "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"),
                        new KeyValuePair<string, string>("refresh_token", refreshToken),
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    });

                    var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var tokenResponse = JsonConvert.DeserializeObject<dynamic>(result);
                        return tokenResponse!.access_token.ToString(); // Return the new Access Token
                    }
                    else
                    {
                        throw new Exception($"Failed to refresh access token. Response: {await response.Content.ReadAsStringAsync()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing access token: {ex.Message}");
                throw;
            }
        }
        private bool IsAccessTokenExpired(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/tokeninfo?access_token=" + accessToken);
                var client = new HttpClient();
                var response = client.SendAsync(request).Result;

                if (!response.IsSuccessStatusCode)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        //Import From Google Drive
        public async Task<Google.Apis.Drive.v3.Data.File> GetFolderByNameAsync(DriveService service, string folderName, string parentId = null!)
        {
            var request = service.Files.List();
            // Use "contains" instead of "=" for more flexibility
            request.Q = $"name = '{folderName}' and mimeType = 'application/vnd.google-apps.folder'";
            if (!string.IsNullOrEmpty(parentId))
            {
                request.Q += $" and '{parentId}' in parents";
            }
            request.Fields = "files(id, name)";
            request.PageSize = 1;

            var result = await request.ExecuteAsync();
            return result.Files.FirstOrDefault()!;
        }
        public async Task<Google.Apis.Drive.v3.Data.File> GetFileByNameAsync(DriveService service, string fileName, string parentId)
        {
            var request = service.Files.List();
            // Searching for the exact file name and filtering by JSON mime type
            request.Q = $"name contains '{fileName}' and mimeType = 'application/json' and '{parentId}' in parents";
            request.Fields = "files(id, name)";
            request.PageSize = 1;

            var result = await request.ExecuteAsync();
            return result.Files.FirstOrDefault()!;
        }
       // loading credential
        public async Task<UserCredential> LoadCredentialsAsync(string email)
        {
            try
            {
                // Get the stored token as a JSON string
                var tokenJson = await SecureStorage.GetAsync(email);
                Console.WriteLine($"Raw token data retrieved: {tokenJson}");

                if (string.IsNullOrEmpty(tokenJson))
                {
                    Console.WriteLine($"No saved credentials found for {email}");
                    return null!;
                }

                // Deserialize the token
                var token = JsonConvert.DeserializeObject<Google.Apis.Auth.OAuth2.Responses.TokenResponse>(tokenJson);
                if (token == null)
                {
                    Console.WriteLine($"Failed to deserialize token for {email}");
                    return null!;
                }

                // Create UserCredential from the token
                var credential = new UserCredential(new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = "802243565178-bj4sf9l8bg9kll136jaelq6s1t04uklo.apps.googleusercontent.com",
                        ClientSecret = "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"
                    }
                }), email, token);

                Console.WriteLine($"Credentials loaded successfully for {email}");
                return credential;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading credentials for {email}: {ex.Message}");
                return null!;
            }
        }

        //Fetching object and fields
        public async Task<string> GetFolderIdByName(DriveService service, string folderName, string parentFolderId)
        {
            try
            {
                var query = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}' and trashed = false";
                if (!string.IsNullOrEmpty(parentFolderId))
                {
                    query += $" and '{parentFolderId}' in parents";
                }

                var request = service.Files.List();
                request.Q = query;
                request.Fields = "files(id, name)";
                var result = await request.ExecuteAsync();

                return result.Files.FirstOrDefault()?.Id!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error locating folder '{folderName}': {ex.Message}");
                throw;
            }
        }
        public async Task<IList<Google.Apis.Drive.v3.Data.File>> GetFilesInFolder(DriveService service, string folderId, string mimeType)
        {
            var request = service.Files.List();
            request.Q = $"'{folderId}' in parents and mimeType = '{mimeType}' and trashed = false";
            request.Fields = "files(id, name)";
            var result = await request.ExecuteAsync();

            return result.Files;
        }

        //update objects
        public async Task<string?> GetFileIdAsync(DriveService service, string? fileName, string? folderId)
        {
            var request = service.Files.List();
            request.Q = $"name = '{fileName}' and '{folderId}' in parents and trashed = false";
            request.Fields = "files(id)";

            var result = await request.ExecuteAsync();
            return result.Files.FirstOrDefault()?.Id; // Return the file ID if found, else null
        }

        //objects details
        public async Task<string> getGDObjectDetails(string selectedFolderName)
        {
            try
            {
                var startIndex = selectedFolderName.IndexOf('(');
                var endIndex = selectedFolderName.IndexOf(')');

                if (startIndex == -1 || endIndex == -1 || startIndex > endIndex)
                {
                    return "Error: The selected folder name format is incorrect. Expected format: orgname(email@gmail.com)";
                }

                var orgName = selectedFolderName.Substring(0, startIndex).Trim();
                var email = selectedFolderName.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();

                if (string.IsNullOrEmpty(orgName) || string.IsNullOrEmpty(email))
                {
                    return "Error: The organization name or email is missing or invalid.";
                }

                var accounts = LoadSavedAccounts();
                var account = accounts.FirstOrDefault(a =>
                    a.Email!.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                    a.Provider!.Equals("Google Drive", StringComparison.OrdinalIgnoreCase));

                if (account == null)
                {
                    return $"Error: No Google Drive account found for email '{email}'.";
                }

                var convertedAccount = new ModelManager.Model.Account
                {
                    Email = account.Email,
                    Provider = account.Provider,
                    AccessToken = account.AccessToken,
                    RefreshToken = account.RefreshToken
                };

                var service = await GetGoogleDriveServiceAsync(convertedAccount);
                var devilStudioFolder = await GetFolderByNameAsync(service, "Devil Studio");

                if (devilStudioFolder == null)
                {
                    return "Error: 'Devil Studio' folder not found in Google Drive.";
                }

                var sourcesFolder = await GetFolderByNameAsync(service, "Sources", devilStudioFolder.Id);
                if (sourcesFolder == null)
                {
                    return "Error: 'Sources' folder not found inside 'Devil Studio'.";
                }

                var orgFolder = await GetFolderByNameAsync(service, orgName, sourcesFolder.Id);
                if (orgFolder == null)
                {
                    return $"Error: Folder for organization '{orgName}' not found.";
                }

                var salesforceFile = await GetFileByNameAsync(service, "SalesforceObjects.json", orgFolder.Id);
                if (salesforceFile == null)
                {
                    return "Error: SalesforceObjects.json file not found.";
                }

                var request = service.Files.Get(salesforceFile.Id);
                var stream = new MemoryStream();
                await request.DownloadAsync(stream);

                stream.Position = 0;
                using var reader = new StreamReader(stream);
                var fileContent = await reader.ReadToEndAsync();
                return fileContent;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        //public async Task ImportJsonDataAsync(string fileContent)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(fileContent))
        //        {
        //            Console.WriteLine("The content is empty.");
        //            return;
        //        }

        //        // Deserialize JSON content into Salesforce objects
        //        var importedObjects = JsonConvert.DeserializeObject<List<SalesforceObject>>(fileContent) ?? new List<SalesforceObject>();

        //        _allSalesforceObjects.Clear();
        //        foreach (var obj in importedObjects)
        //        {
        //            _allSalesforceObjects.Add(obj);
        //        }

        //        SalesforceObjects = new ObservableCollection<SalesforceObject>(_allSalesforceObjects);

        //        Console.WriteLine("File content imported successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error importing content: {ex.Message}");
        //    }
        //}
    }
}
