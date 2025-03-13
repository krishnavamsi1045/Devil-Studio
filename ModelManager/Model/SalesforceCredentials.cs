using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace ModelManager.Model
{
    public class SalesforceCredentials
    {
        public string? SalesforceApiBaseUrl { get; set; }
        public string? SalesforceAccessToken { get; set; }
        public string? SalesforceRefreshToken { get; set; }
        public string? SalesforceClientId { get; set; }
        public string? SalesforceClientSecret { get; set; }

        // Static method to load Salesforce credentials from JSON
        public static SalesforceCredentials Load()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            string fileName = "salesforcecredentials.json";
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, fileName)))
            {

                directory = directory.Parent;
                if (directory != null)
                {
                    Debug.WriteLine(directory.FullName);
                }
            }
            var salesforceCredentialsPath = Path.Combine(directory.FullName, fileName);
           
            Debug.WriteLine($"Looking for Salesforce credentials file at: {salesforceCredentialsPath}");

            if (System.IO.File.Exists(salesforceCredentialsPath))
            {
                var json = System.IO.File.ReadAllText(salesforceCredentialsPath);
                return JsonConvert.DeserializeObject<SalesforceCredentials>(json);
            }
            else
            {
                throw new FileNotFoundException($"Salesforce credentials file not found at: {salesforceCredentialsPath}");
            }
        }
    }
}
