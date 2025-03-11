using System;
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
            string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SalesforceCredentials.json");
            Console.WriteLine($"Looking for Salesforce credentials file at: {jsonFilePath}");

            if (System.IO.File.Exists(jsonFilePath))
            {
                var json = System.IO.File.ReadAllText(jsonFilePath);
                return JsonConvert.DeserializeObject<SalesforceCredentials>(json);
            }
            else
            {
                throw new FileNotFoundException($"Salesforce credentials file not found at: {jsonFilePath}");
            }
        }
    }
}
