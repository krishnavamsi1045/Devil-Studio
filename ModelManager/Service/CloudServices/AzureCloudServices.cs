using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using ModelManager.Model;
using ModelManager.Service.GoogleServices;

namespace ModelManager.Service.AzureService
{
    public class AzureCloudServices
    {
         
        public readonly GoogleCloudServices _googleCloudServices;
        public AzureCloudServices()
        {
            _googleCloudServices=new GoogleCloudServices();
        }
        public async Task SaveFilesToOneDriveAsync(string orgName, string credentialsJson, string metadataJson, string email, string provider)
        {
            const string rootFolderName = "Devil Studio";
            const string sourcesFolderName = "Sources";
            const string credentialsFileName = "SalesforceCredentials.json";
            const string metadataFileName = "SalesforceObjects.json";

            try
            {
                // Authenticate user
                var graphClient = AuthenticateUserByEmail(email, provider);

                // Access user drive and create folder structure
                var rootFolder = await EnsureFolderExistsAsync(graphClient, null!, rootFolderName);
                var sourcesFolder = await EnsureFolderExistsAsync(graphClient, rootFolder.Id, sourcesFolderName);
                var orgFolder = await EnsureFolderExistsAsync(graphClient, sourcesFolder.Id, orgName);

                // Save files to OneDrive
                if (!string.IsNullOrEmpty(credentialsJson))
                {
                    await UploadFileToFolderAsync(graphClient, orgFolder.Id, credentialsFileName, credentialsJson);
                }

                if (!string.IsNullOrEmpty(metadataJson))
                {
                    await UploadFileToFolderAsync(graphClient, orgFolder.Id, metadataFileName, metadataJson);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw; // Re-throw to allow handling at higher levels
            }
        }
        public GraphServiceClient AuthenticateUserByEmail(string email, string provider)
        {
            // Load saved accounts
            var accounts = _googleCloudServices.LoadSavedAccounts();

            // Find the account with the specified email and provider
            var account = accounts.FirstOrDefault(acc =>
                acc.Email!.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                acc.Provider!.Equals(provider, StringComparison.OrdinalIgnoreCase));

            if (account == null || string.IsNullOrEmpty(account.AccessToken))
            {
                throw new Exception($"No valid access token found for email: {email} and provider: {provider}");
            }

            // Return a GraphServiceClient using the access token
            return new GraphServiceClient(new DelegateAuthenticationProvider(request =>
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.AccessToken);
                return Task.CompletedTask;
            }));
        }
        public Task<GraphServiceClient> GetOneDriveServiceAsync(Account account)
        {
            try
            {
                if (string.IsNullOrEmpty(account.AccessToken))
                {
                    throw new Exception($"OneDrive AccessToken is null or empty for account {account.Email}");
                }

                // Create the authentication provider using the AccessToken
                var authProvider = new DelegateAuthenticationProvider(requestMessage =>
                {
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.AccessToken);
                    return Task.CompletedTask;
                });

                // Create and return the GraphServiceClient with the authentication provider
                return Task.FromResult(new GraphServiceClient(authProvider));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing OneDrive service for {account.Email}: {ex.Message}");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
        public async Task DeleteDevilStudioFolderAsync(string email, string provider)
        {
            try
            {

                // Authenticate the user using the provided email and provider
                var graphClient = AuthenticateUserByEmail(email, provider);

                // Find the "Devil Studio" folder
                var rootFolder = await EnsureFolderExistsAsync(graphClient, null!, "Devil Studio");

                if (rootFolder != null)
                {
                    // Delete the folder
                    await graphClient.Me.Drive.Items[rootFolder.Id].Request().DeleteAsync();
                    Console.WriteLine("The 'Devil Studio' folder has been successfully deleted.");
                }
                else
                {
                    Console.WriteLine("The 'Devil Studio' folder was not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while trying to delete the 'Devil Studio' folder: {ex.Message}");
            }
        }
        public async Task UploadFileToFolderAsync(GraphServiceClient graphClient, string folderId, string fileName, string fileContent)
        {
            try
            {
                using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
                await graphClient.Me.Drive.Items[folderId]
                    .ItemWithPath(fileName)
                    .Content
                    .Request()
                    .PutAsync<DriveItem>(fileStream);
                Console.WriteLine($"File '{fileName}' uploaded successfully.");
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error uploading file '{fileName}': {ex.Message}");
                throw new Exception($"Failed to upload file '{fileName}'.", ex);
            }
        }
        public async Task<DriveItem> EnsureFolderExistsAsync(GraphServiceClient graphClient, string parentFolderId, string folderName)
        {
            try
            {
                IDriveItemChildrenCollectionPage driveItems;

                if (string.IsNullOrEmpty(parentFolderId))
                {
                    // Check in the root folder
                    driveItems = await graphClient.Me.Drive.Root.Children.Request().GetAsync();
                }
                else
                {
                    // Check within the specified parent folder
                    driveItems = await graphClient.Me.Drive.Items[parentFolderId].Children.Request().GetAsync();
                }

                var folderItem = driveItems.FirstOrDefault(item => item.Name == folderName && item.Folder != null);

                if (folderItem != null)
                    return folderItem;

                // Create the folder if it doesn't exist
                var newFolder = new DriveItem
                {
                    Name = folderName,
                    Folder = new Folder(),
                    AdditionalData = new Dictionary<string, object>
            {
                { "@microsoft.graph.conflictBehavior", "rename" }
            }
                };

                if (string.IsNullOrEmpty(parentFolderId))
                {
                    // Create folder in the root
                    return await graphClient.Me.Drive.Root.Children.Request().AddAsync(newFolder);
                }
                else
                {
                    // Create folder inside parent folder
                    return await graphClient.Me.Drive.Items[parentFolderId].Children.Request().AddAsync(newFolder);
                }
            }
            catch (Exception)
            {
                throw new Exception("Access Token Expired Re-Authinticate Location Manager");
            }
        }
        public class AccessTokenCredential : IAuthenticationProvider
        {
            private readonly string _accessToken;

            public AccessTokenCredential(string accessToken)
            {
                _accessToken = accessToken;
            }

            public async Task AuthenticateRequestAsync(HttpRequestMessage request)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                await Task.CompletedTask;
            }
        }

        //crud and fetch
        public async Task<DriveItem> GetFolderByName(GraphServiceClient graphClient, string folderName, string parentFolderId)
        {
            try
            {
                IDriveItemChildrenCollectionPage driveItems;

                if (string.IsNullOrEmpty(parentFolderId))
                {
                    driveItems = await graphClient.Me.Drive.Root.Children.Request().GetAsync();
                }
                else
                {
                    driveItems = await graphClient.Me.Drive.Items[parentFolderId].Children.Request().GetAsync();
                }

                var folder = driveItems.FirstOrDefault(item => item.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase) && item.Folder != null);
                if (folder == null)
                {
                    Console.WriteLine($"Folder '{folderName}' not found.");
                }

                return folder!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching folder '{folderName}': {ex.Message}");
                throw;
            }
        }
        public async Task<string> DownloadFileContent(GraphServiceClient client, string fileId)
        {
            try
            {
                using (var stream = await client.Me.Drive.Items[fileId].Content.Request().GetAsync())
                using (var reader = new StreamReader(stream))
                {
                    string content = await reader.ReadToEndAsync();

                    if (content.Length > 100)
                    {
                        Console.WriteLine("Success", $"Downloaded file content: {content.Substring(0, 100)}...","OK");  
                    }
                    else
                    {
                        Console.WriteLine("Success", $"Downloaded file content: {content}", "OK"); 
                    }
                    return content;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file {fileId}: {ex.Message}");
                throw;
            }
        }
        public async Task UploadUpdatedFile(GraphServiceClient client, string fileId, Stream contentStream)
        {
            try
            {
                var uploadResult = await client.Me.Drive.Items[fileId]
                    .Content
                    .Request()
                    .PutAsync<DriveItem>(contentStream);

                Console.WriteLine($"Successfully uploaded file: {uploadResult.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file {fileId}: {ex.Message}");
                throw;
            }
        }
        public async Task<IList<DriveItem>> GetFilesInFolder(GraphServiceClient graphClient, string folderId, string fileType)
        {
            try
            {
                var files = await graphClient.Me.Drive.Items[folderId].Children.Request().GetAsync();
                return files.Where(f => f.File != null && f.File.MimeType.Equals(fileType, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching files in folder {folderId}: {ex.Message}");
                throw;
            }
        }
        //update
        public async Task<DriveItem?> GetFileByNameAsync(GraphServiceClient graphClient, string folderId, string fileName)
        {
            try
            {
                var files = await graphClient.Me.Drive.Items[folderId].Children.Request().GetAsync();
                return files.FirstOrDefault(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching file '{fileName}': {ex.Message}");
                return null;
            }
        }
        // object details 
        public async Task<string> getODObjectDetails(string selectedFolderName, string source)
        {
            try
            {
                var startIndex = selectedFolderName.IndexOf('(');
                var endIndex = selectedFolderName.IndexOf(')');

                if (startIndex == -1 || endIndex == -1 || startIndex > endIndex)
                {
                    return "Error: The selected folder name format is incorrect. Expected format: orgname(email@gmail.com)";
                }

                string orgName = selectedFolderName.Substring(0, startIndex).Trim();
                string email = selectedFolderName.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();

                if (string.IsNullOrEmpty(orgName) || string.IsNullOrEmpty(email))
                {
                    return "Error: The organization name or email is missing or invalid.";
                }

                var accounts = _googleCloudServices.LoadSavedAccounts();
                var account = accounts.FirstOrDefault(a =>
                    a.Email!.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                    a.Provider!.Equals("OneDrive", StringComparison.OrdinalIgnoreCase));

                if (account == null)
                {
                    return $"Error: No OneDrive account found for email '{email}'.";
                }

                var graphClient = AuthenticateUserByEmail(email, source);

                var devilStudioFolder = await EnsureFolderExistsAsync(graphClient, null!, "Devil Studio");
                if (devilStudioFolder == null)
                {
                    return "Error: 'Devil Studio' folder not found in OneDrive.";
                }

                var sourcesFolder = await EnsureFolderExistsAsync(graphClient, devilStudioFolder.Id, "Sources");
                if (sourcesFolder == null)
                {
                    return "Error: 'Sources' folder not found inside 'Devil Studio'.";
                }

                var orgFolder = await EnsureFolderExistsAsync(graphClient, sourcesFolder.Id, orgName);
                if (orgFolder == null)
                {
                    return $"Error: Folder for organization '{orgName}' not found.";
                }

                var fileItems = await graphClient.Me.Drive.Items[orgFolder.Id].Children.Request().GetAsync();
                var salesforceFile = fileItems.FirstOrDefault(f => f.File != null && f.Name.Equals("SalesforceObjects.json", StringComparison.OrdinalIgnoreCase));

                if (salesforceFile == null)
                {
                    return "Error: SalesforceObjects.json file not found.";
                }

                var contentStream = await graphClient.Me.Drive.Items[salesforceFile.Id].Content.Request().GetAsync();

                using var reader = new StreamReader(contentStream);
                var fileContent = await reader.ReadToEndAsync();

                return fileContent;  
            }
            catch (Exception ex)
            {
                return $"Error importing from OneDrive: {ex.Message}";
            }
        }

    }
}