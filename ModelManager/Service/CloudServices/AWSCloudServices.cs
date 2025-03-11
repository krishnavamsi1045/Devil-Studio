using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using static ModelManager.Service.CloudServices.AWSCloudServices;

namespace ModelManager.Service.CloudServices
{
    public class AWSCloudServices
    {
        public class AWSAccount
        {
            public string? UserEmail { get; set; }
            public string? AccessKey { get; set; }
            public string? SecretKey { get; set; }
            public DateTime Date { get; set; }
            public string? Region { get; set; }
            public string? SelectedBucket { get; set; }
        }
        private ObservableCollection<AWSAccount> aWSAccounts = new ObservableCollection<AWSAccount>();
        public ObservableCollection<AWSAccount> LoadAWSCredentials()
        {
            try
            {
                var filePath = Path.Combine(FileSystem.AppDataDirectory, "aws_credentials.json");
                Console.WriteLine($"Loading AWS credentials from: {filePath}"); // Debugging log

                if (System.IO.File.Exists(filePath))
                {
                    var accountsJson = System.IO.File.ReadAllText(filePath);
                    Console.WriteLine($"AWS Credentials JSON: {accountsJson}"); // Debugging log

                    var accounts = JsonConvert.DeserializeObject<List<AWSAccount>>(accountsJson) ?? new List<AWSAccount>();

                    aWSAccounts = new ObservableCollection<AWSAccount>(accounts);
                    return aWSAccounts;
                }
                else
                {
                    Console.WriteLine("AWS credentials file not found."); // Debugging log
                    aWSAccounts = new ObservableCollection<AWSAccount>();
                    return aWSAccounts;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load AWS accounts: {ex.Message}");
                aWSAccounts = new ObservableCollection<AWSAccount>();
                return aWSAccounts;
            }
        }
        public async Task<AmazonS3Client> GetAwsS3ClientAsync(string userEmail)
        {
            try
            {
                aWSAccounts = LoadAWSCredentials();

                if (aWSAccounts == null || !aWSAccounts.Any())
                {
                    throw new Exception("No AWS accounts found in the credentials file.");
                }

                var account = aWSAccounts.FirstOrDefault(a =>
                    string.Equals(a.UserEmail, userEmail, StringComparison.OrdinalIgnoreCase));

                if (account == null)
                {
                    throw new Exception($"No AWS account found for email: {userEmail}");
                }

                if (string.IsNullOrEmpty(account.AccessKey) || string.IsNullOrEmpty(account.SecretKey) || string.IsNullOrEmpty(account.Region))
                {
                    throw new Exception("AWS credentials are incomplete for the specified email.");
                }

                var awsRegion = Amazon.RegionEndpoint.GetBySystemName(account.Region);
                return new AmazonS3Client(account.AccessKey, account.SecretKey, awsRegion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing AWS S3 client: {ex.Message}");
                throw;
            }
        }

        //public async Task<AmazonS3Client> GetAwsS3ClientAsync()
        //{
        //    try
        //    {
        //        var accessKey = await SecureStorage.GetAsync("AWSAccessKey");
        //        var secretKey = await SecureStorage.GetAsync("AWSSecretKey");
        //        var region = await SecureStorage.GetAsync("AWSRegion");

        //        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(region))
        //        {
        //            throw new Exception("AWS credentials not set.");
        //        }

        //        var awsRegion = Amazon.RegionEndpoint.GetBySystemName(region);
        //        var s3Client = new AmazonS3Client(accessKey, secretKey, awsRegion);
        //        return s3Client;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error initializing AWS S3 client: {ex.Message}");
        //        throw;
        //    }
        //}
        //object details
        public async Task<string> getAWSObjectDetails(string selectedOrgName)
        {
            try
            {

                var match = Regex.Match(selectedOrgName, @"^(?<bucket>.+)/(?<orgname>.+)\((?<email>.+)\)$");
                if (!match.Success)
                {
                    return "Error: Invalid organization selection. Ensure the format is 'BucketName/OrgName(email)'.";
                }

                string bucketName = match.Groups["bucket"].Value.Trim();
                string orgName = match.Groups["orgname"].Value.Trim();
                string email = match.Groups["email"].Value.Trim();


                var s3Client = await GetAwsS3ClientAsync(email);
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = $"DevilStudio/sources/{orgName}/" 
                };

                var result = await s3Client.ListObjectsV2Async(request);

                foreach (var obj in result.S3Objects)
                {
                    Console.WriteLine($"- {obj.Key}");
                }

                if (!result.S3Objects.Any())
                {
                    return $"Error: No files found in '{request.Prefix}' within bucket '{bucketName}'.";
                }

                var salesforceFile = result.S3Objects.FirstOrDefault(f => f.Key.EndsWith("SalesforceObjects.json", StringComparison.OrdinalIgnoreCase));

                if (salesforceFile == null)
                {
                    return "Error: SalesforceObjects.json file not found.";
                }
                var getObjectResponse = await s3Client.GetObjectAsync(bucketName, salesforceFile.Key);

                using var reader = new StreamReader(getObjectResponse.ResponseStream);
                var fileContent = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(fileContent) || fileContent.Trim() == "[]")
                {
                    return "Error: SalesforceObjects.json file is empty.";
                }
                return fileContent;
            }
            catch (AmazonS3Exception s3Ex)
            {
                return $"AWS S3 Exception: {s3Ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error importing from AWS: {ex.Message}";
            }
        }
    }
}
