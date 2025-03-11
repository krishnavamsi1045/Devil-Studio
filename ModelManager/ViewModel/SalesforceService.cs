using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using ModelManager.Model;
using Newtonsoft.Json;

namespace ModelManager.ViewModel
{
    public class SalesforceService : ISalesforceService
    {
        private readonly HttpClient _httpClient;
        private const string DOMAIN_NAME = "https://login.salesforce.com";

        public ObservableCollection<salesforceObject> SalesforceObjects { get; set; }

        public SalesforceService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            // Set the timeout to 30 minutes (1800 seconds)
            _httpClient.Timeout = TimeSpan.FromMinutes(30);

            SalesforceObjects = new ObservableCollection<salesforceObject>();
        }

        //public async Task<SalesforceObject> GetObjectByApiNameAsync(string? instanceUrl, string? accessToken, string apiName)
        //{
        //    // Fetch object details using the provided API name
        //    var objects = await GetObjectsByApiNamesAsync(instanceUrl, accessToken, new List<string> { apiName });
        //    return objects.FirstOrDefault(); // Return the first object if found
        //}

        //public async Task<List<SalesforceObject>> GetObjectsByApiNamesAsync(string instanceUrl, string accessToken, List<string> apiNames)
        //{
        //    var uri = new Uri(instanceUrl);
        //    var baseUrl = $"{uri.Scheme}://{uri.Host}";

        //    var results = new List<SalesforceObject>();

        //    foreach (var apiName in apiNames)
        //    {
        //        SalesforceObject salesforceObject = null;
        //        var requestUrl = $"{baseUrl}/services/data/v62.0/sobjects/{apiName}/describe";
        //        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        //        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        //        try
        //        {
        //            var response = await _httpClient.SendAsync(request);
        //            if (!response.IsSuccessStatusCode)
        //            {
        //                var errorResponse = await response.Content.ReadAsStringAsync();
        //                throw new HttpRequestException($"Failed to retrieve object data for ApiName: {apiName}. ErrorCode: {response.StatusCode}, Message: {errorResponse}");
        //            }

        //            var jsonResponse = await response.Content.ReadAsStringAsync();
        //            var describeData = JsonDocument.Parse(jsonResponse);

        //            // Check if it is a custom object
        //            var isCustom = describeData.RootElement.GetProperty("custom").GetBoolean();
        //            // Continue regardless of custom or standard
        //            // if (!isCustom) continue; // Remove this line to include standard objects

        //            // Extract general object details
        //            var label = describeData.RootElement.GetProperty("label").GetString();
        //            var fieldsCount = describeData.RootElement.GetProperty("fields").GetArrayLength();
        //            var type = isCustom ? "Custom" : "Standard"; // Set type based on object type

        //            // Initialize variables for actual data
        //            string description = "-";
        //            string deploymentStatus = isCustom ? "True" : "-"; // Only applicable for custom objects
        //            DateTime? lastModified = null;

        //            var fieldsList = new List<salesforceField>();
        //            var validationRuleCount = isCustom ? await GetValidationRulesCount(instanceUrl, accessToken, label) : 0; // Only for custom objects
        //            var recordTypeCount = isCustom ? await GetRecordTypeCount(instanceUrl, accessToken, apiName) : 0; // Only for custom objects

        //            // Fetch additional details if it is a custom object
        //            if (isCustom)
        //            {
        //                var customToolingRequestUrl = $"{baseUrl}/services/data/v62.0/tooling/query/?q=SELECT+DeploymentStatus,+Description,+LastModifiedDate+FROM+EntityDefinition+WHERE+QualifiedApiName='{apiName}'";
        //                var customToolingRequest = new HttpRequestMessage(HttpMethod.Get, customToolingRequestUrl);
        //                customToolingRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        //                var customToolingResponse = await _httpClient.SendAsync(customToolingRequest);
        //                if (customToolingResponse.IsSuccessStatusCode)
        //                {
        //                    var customToolingJsonResponse = await customToolingResponse.Content.ReadAsStringAsync();
        //                    var customToolingData = JsonDocument.Parse(customToolingJsonResponse);

        //                    if (customToolingData.RootElement.TryGetProperty("records", out var recordsArray) && recordsArray.GetArrayLength() > 0)
        //                    {
        //                        var record = recordsArray[0];
        //                        description = record.TryGetProperty("Description", out var descProp) ? descProp.GetString() : "-";
        //                        deploymentStatus = record.TryGetProperty("DeploymentStatus", out var deployProp) ? deployProp.GetString() : "-";
        //                        lastModified = record.TryGetProperty("LastModifiedDate", out var lastModifiedProp) ? DateTime.Parse(lastModifiedProp.GetString()) : (DateTime?)null;
        //                    }
        //                }
        //            }

        //            // Fetch field details using Tooling API
        //            var toolingRequestUrl = $"{baseUrl}/services/data/v62.0/tooling/query/?q=SELECT+QualifiedApiName,+Description,+Label+FROM+FieldDefinition+WHERE+EntityDefinition.QualifiedApiName='{apiName}'";
        //            var toolingRequest = new HttpRequestMessage(HttpMethod.Get, toolingRequestUrl);
        //            toolingRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        //            var toolingResponse = await _httpClient.SendAsync(toolingRequest);
        //            if (toolingResponse.IsSuccessStatusCode)
        //            {
        //                var toolingJsonResponse = await toolingResponse.Content.ReadAsStringAsync();
        //                var toolingData = JsonDocument.Parse(toolingJsonResponse);

        //                if (toolingData.RootElement.TryGetProperty("records", out var recordsArray))
        //                {
        //                    foreach (var field in describeData.RootElement.GetProperty("fields").EnumerateArray())
        //                    {
        //                        var fieldName = field.GetProperty("name").GetString();
        //                        var fieldLabel = field.GetProperty("label").GetString();
        //                        var fieldType = field.GetProperty("type").GetString();
        //                        var helpText = field.GetProperty("inlineHelpText").GetString();

        //                        string fieldDescription = "-";
        //                        foreach (var record in recordsArray.EnumerateArray())
        //                        {
        //                            if (record.GetProperty("QualifiedApiName").GetString() == fieldName)
        //                            {
        //                                fieldDescription = record.GetProperty("Description").GetString();
        //                                break;
        //                            }
        //                        }

        //                        fieldsList.Add(new salesforceField
        //                        {
        //                            Name = fieldName,
        //                            Label = fieldLabel,
        //                            Type = fieldType,
        //                            HelpText = helpText,
        //                            description = fieldDescription
        //                        });
        //                    }
        //                }
        //            }

        //            // Construct the Salesforce object with the collected data
        //            salesforceObject = new SalesforceObject
        //            {
        //                Label = label,
        //                ApiName = apiName,
        //                Type = type,
        //                Description = description,
        //                Deployed = isCustom && (deploymentStatus.Equals("Deployed", StringComparison.OrdinalIgnoreCase) || deploymentStatus.Equals("True", StringComparison.OrdinalIgnoreCase)),
        //                LastModified = lastModified?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "-",
        //                FieldsCount = fieldsCount,
        //                ValidationRuleCount = validationRuleCount,
        //                RecordTypeCount = recordTypeCount,
        //                Fields = fieldsList
        //            };
        //        }
        //        catch (Exception ex)
        //        {
        //            throw new SystemException($"Exception occurred while trying to retrieve object data for ApiName: {apiName}. Error: {ex.Message}");
        //        }

        //        // Add the fetched Salesforce object to results
        //        if (salesforceObject != null)
        //        {
        //            results.Add(salesforceObject);
        //        }
        //    }

        //    return results; // Return the list of Salesforce objects (both custom and standard)
        //}

        //public async Task<List<SalesforceObject>> SyncAllSalesforceObjectsAsync(string instanceUrl, string accessToken)
        //{
        //    var results = new List<SalesforceObject>();
        //    var baseUrl = $"{new Uri(instanceUrl).Scheme}://{new Uri(instanceUrl).Host}";

        //    try
        //    {
        //        // Step 1: Fetch all objects metadata
        //        var requestUrl = $"{baseUrl}/services/data/v62.0/sobjects";
        //        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        //        var response = await _httpClient.SendAsync(request);
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            var errorResponse = await response.Content.ReadAsStringAsync();
        //            throw new HttpRequestException($"Failed to retrieve all objects. ErrorCode: {response.StatusCode}, Message: {errorResponse}");
        //        }

        //        var jsonResponse = await response.Content.ReadAsStringAsync();
        //        var describeData = JsonDocument.Parse(jsonResponse);

        //        if (describeData.RootElement.TryGetProperty("sobjects", out var sObjectsArray))
        //        {
        //            var apiNames = sObjectsArray.EnumerateArray().Select(obj => obj.GetProperty("name").GetString()).ToList();

        //            // Step 2: Fetch details for all objects using existing methods
        //            results = await GetObjectsByApiNamesAsync(instanceUrl, accessToken, apiNames);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"An error occurred while retrieving all objects: {ex.Message}");
        //        throw;
        //    }

        //    return results;
        //}

        public async Task<ValidationMessage> GetAccessTokenAsync(string username, string password, string consumerKey, string consumerSecret, string domainUrl)
        {
            var parameters = new Dictionary<string, string>
    {
        { "grant_type", "password" },
        { "client_id", consumerKey },
        { "client_secret", consumerSecret },
        { "username", username },
        { "password", password }
    };

            var content = new FormUrlEncodedContent(parameters);

            try
            {
                var response = await _httpClient.PostAsync($"{domainUrl}/services/oauth2/token", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var tokenData = JsonDocument.Parse(jsonResponse);
                    var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();
                    var instanceUrl = tokenData.RootElement.GetProperty("instance_url").GetString();

                    // return $"{accessToken}" +

                    return new ValidationMessage(true, $"{accessToken}|{instanceUrl}");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    var jsonBody = JsonDocument.Parse(errorResponse);

                    string Error = "";
                    string ErrorDescription = "";
                    if (jsonBody.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (jsonBody.RootElement.TryGetProperty("error", out var error))
                        {
                            Error = error.ToString();
                            Debug.WriteLine(Error);
                        }
                        if (jsonBody.RootElement.TryGetProperty("error_description", out var error_description))
                        {
                            ErrorDescription = error_description.ToString();
                            Debug.WriteLine(error_description);
                        }
                    }

                    return new ValidationMessage(false, $"{Error} : {ErrorDescription}");
                }
            }
            catch (HttpRequestException ex)
            {
                return new ValidationMessage(false, $"Network error: {ex.Message}. Please check your internet connection.");
            }
            catch (TaskCanceledException ex)
            {
                return new ValidationMessage(false, $"Connection timed out: {ex.Message} The Salesforce server is taking too long to respond.");
            }
            catch (Exception ex)
            {
                return new ValidationMessage(false, $"Unexpected error: {ex.Message}");
            }
        }
        public async Task<ValidationMessage> TestingProvidedCredentials(string? instanceUrl, string? accessToken)
        {
            var requestUrl = $"{instanceUrl}/services/data/v62.0/sobjects";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return new ValidationMessage(true, "Successfully connected to Salesforce.");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return new ValidationMessage(false, $"Salesforce API Error: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                return new ValidationMessage(false, $"Network error: {ex.Message}. Please check your internet connection.");
            }
            catch (TaskCanceledException ex)
            {
                return new ValidationMessage(false, $"Connection timed out: {ex.Message} . The Salesforce server is taking too long to respond.");
            }
            catch (Exception ex)
            {
                return new ValidationMessage(false, $"Unexpected error: {ex.Message}");
            }
        }

        public async Task<int> GetValidationRulesCount(string? instanceUrl, string? accessToken, string? objectName)
        {
            var requestUrl = $"{instanceUrl}/services/data/v62.0/tooling/query/?q=SELECT+Id+FROM+ValidationRule+WHERE+EntityDefinition.DeveloperName='{objectName}'";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonData = JsonDocument.Parse(jsonResponse);

                    if (jsonData.RootElement.TryGetProperty("totalSize", out var totalSizeProperty))
                    {
                        return totalSizeProperty.GetInt32();
                    }
                    else
                    {
                        Console.WriteLine("TotalSize not found in validation rules response.");
                        return 0; // Return 0 if the property is missing
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to retrieve validation rules. ErrorCode: {response.StatusCode}, Message: {errorResponse}");
                    return 0; // Return 0 if the request fails
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while trying to retrieve validation rules: {ex.Message}");
                return 0; // Return 0 in case of an exception
            }
        }

        public async Task<int> GetRecordTypeCount(string? instanceUrl, string? accessToken, string? objectName)
        {
            var requestUrl = $"{instanceUrl}/services/data/v62.0/tooling/query/?q=SELECT+Id+FROM+RecordType+WHERE+SObjectType='{objectName}'";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonData = JsonDocument.Parse(jsonResponse);

                    if (jsonData.RootElement.TryGetProperty("totalSize", out var totalSizeProperty))
                    {
                        return totalSizeProperty.GetInt32();
                    }
                    else
                    {
                        Console.WriteLine("TotalSize not found in record type response.");
                        return 0; // Return 0 if the property is missing
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to retrieve record types. ErrorCode: {response.StatusCode}, Message: {errorResponse}");
                    return 0; // Return 0 if the request fails
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while trying to retrieve record types: {ex.Message}");
                return 0; // Return 0 in case of an exception
            }
        }

        public async Task<int> GetApexTriggersCount(string? instanceUrl, string? accessToken, string? objectName)
        {
            var requestUrl = $"{instanceUrl}/services/data/v62.0/tooling/query/?q=SELECT+Id+FROM+ApexTrigger+WHERE+TableEnumOrId='{objectName}'";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonData = JsonDocument.Parse(jsonResponse);

                    if (jsonData.RootElement.TryGetProperty("totalSize", out var totalSizeProperty))
                    {
                        return totalSizeProperty.GetInt32();
                    }
                    else
                    {
                        Console.WriteLine("TotalSize not found in Apex triggers response.");
                        return 0; // Return 0 if the property is missing
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to retrieve Apex triggers. ErrorCode: {response.StatusCode}, Message: {errorResponse}");
                    return 0; // Return 0 if the request fails
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while trying to retrieve Apex triggers: {ex.Message}");
                return 0; // Return 0 in case of an exception
            }
        }

        public async Task<int> GetFlowTriggersCount(string? instanceUrl, string? accessToken, string? objectName)
        {
            var requestUrl = $"{instanceUrl}/services/data/v62.0/tooling/query/?q=SELECT+Id+FROM+Flow+WHERE+ObjectApiName='{objectName}'";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonData = JsonDocument.Parse(jsonResponse);

                    if (jsonData.RootElement.TryGetProperty("totalSize", out var totalSizeProperty))
                    {
                        return totalSizeProperty.GetInt32();
                    }
                    else
                    {
                        Console.WriteLine("TotalSize not found in Flow triggers response.");
                        return 0; // Return 0 if the property is missing
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to retrieve Flow triggers. ErrorCode: {response.StatusCode}, Message: {errorResponse}");
                    return 0; // Return 0 if the request fails
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while trying to retrieve Flow triggers: {ex.Message}");
                return 0; // Return 0 in case of an exception
            }
        }

        public async Task<string> GetObjectSharingModel(string? instanceUrl, string? accessToken, string? objectName)
        {
            if (string.IsNullOrWhiteSpace(instanceUrl) || string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(objectName))
            {
                return "Error: Instance URL, access token, and object name must be provided.";
            }

            try
            {
                if (objectName.EndsWith("__c", StringComparison.OrdinalIgnoreCase))
                {
                    objectName = objectName.Substring(0, objectName.Length - 3);
                }

                string query = $"SELECT SharingModel FROM CustomObject WHERE DeveloperName = '{objectName}'";
                string toolingApiUrl = $"{instanceUrl}/services/data/v62.0/tooling/query/?q={Uri.EscapeDataString(query)}";


                var request = new HttpRequestMessage(HttpMethod.Get, toolingApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    using JsonDocument document = JsonDocument.Parse(jsonResponse);
                    var root = document.RootElement;

                    if (root.TryGetProperty("records", out var records))
                    {
                        if (records.GetArrayLength() > 0)
                        {
                            var firstRecord = records[0];
                            if (firstRecord.TryGetProperty("SharingModel", out var sharingModelProp))
                            {
                                return sharingModelProp.GetString() ?? "Sharing model is null.";
                            }
                        }
                        return "No records found for the specified object.";
                    }
                    return "No records found (records property not found)";
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return $"Error retrieving sharing model: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return $"Error occurred while trying to retrieve sharing model.:{ex.Message}";
            }
        }

        string GetFriendlyFieldType(string? fieldType)
        {
            switch (fieldType)
            {
                case "string":
                    return "Text";
                case "boolean":
                    return "Checkbox";
                case "double":
                    return "Number";
                case "int":
                    return "Number";
                case "currency":
                    return "Currency";
                case "date":
                    return "Date";
                case "datetime":
                    return "DateTime";
                case "email":
                    return "Email";
                case "picklist":
                    return "Picklist";
                case "textarea":
                    return "Text Area";
                case "phone":
                    return "Phone";
                case "url":
                    return "URL";
                case "reference":
                    return "Lookup Relationship";
                case "id":
                    return "ID";
                case "percent":
                    return "Percent";
                case "multipicklist":
                    return "Multi-Select Picklist";
                case "combobox":
                    return "Combo Box";
                case "anyType":
                    return "Any Type";
                case "location":
                    return "Location";
                case "richtext":
                    return "Rich Text Area";
                case "html":
                    return "HTML";
                case "formula":
                    return "Formula";
                case "autoNumber":
                    return "Auto Number";
                case "rollupSummary":
                    return "Roll-Up Summary";
                case "masterDetail":
                    return "Master-Detail Relationship";
                case "lookup":
                    return "Lookup Relationship";
                case "geolocation":
                    return "Geolocation";
                case "encryptedstring":
                    return "Encrypted Text";
                case "sobject":
                    return "SObject";
                case "time":
                    return "Time";

                default:
                    return "Unknown";
            }
        }

        public async Task<List<salesforceObject>> RetrieveObjectsAsync(string? instanceUrl, string? accessToken)
        {
            var allObjects = new List<salesforceObject>();
            const int batchSize = 2000;
            int offset = 0;
            bool moreRecords = true;

            try
            {
                var preferencesManager = new ModelManager.Service.PreferencesManager();
                var selectedOrg = SessionManager.FetchObjectsOrg;
                var providername = SessionManager.LocationType;

                // Load preferences once and cache
                var apiNames = await preferencesManager.LoadApiNames(providername!, selectedOrg!, "SelectedOptions") ?? new List<string>();
                var selectedOptions = new HashSet<string>(apiNames, StringComparer.OrdinalIgnoreCase); // Case-insensitive comparison

                // Pre-fetch all metadata in parallel
                var validationRulesTask = FetchValidationRulesAsync(instanceUrl, accessToken);
                var recordTypesTask = FetchRecordTypesAsync(instanceUrl, accessToken);
                var apexTriggersTask = FetchApexTriggersAsync(instanceUrl, accessToken);

                await Task.WhenAll(validationRulesTask, recordTypesTask, apexTriggersTask);

                var validationRules = await validationRulesTask;
                var recordTypes = await recordTypesTask;
                var apexTriggers = await apexTriggersTask;

                var validationRuleCounts = validationRules.GroupBy(vr => vr.EntityName!)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
                var recordTypeCounts = recordTypes.GroupBy(rt => rt.SObjectType!, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Count());
                var apexTriggerCounts = apexTriggers.GroupBy(at => at.TableEnumOrId!)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                var baseQuery = $"{instanceUrl}/services/data/v62.0/tooling/query?q=" +
                                $"SELECT+QualifiedApiName,Label,DeploymentStatus,Description,NamespacePrefix,IsApexTriggerable,LastModifiedDate,IsCustomizable,IsSearchable,InternalSharingModel,ExternalSharingModel,PluralLabel+" +
                                $"FROM+EntityDefinition+LIMIT+{batchSize}+OFFSET+";

                while (moreRecords)
                {
                    var entityDefinitionQuery = $"{baseQuery}{offset}";
                    var entityDefinitionData = await ExecuteToolingQueryAsync(entityDefinitionQuery, accessToken);

                    if (entityDefinitionData.Count == 0)
                    {
                        moreRecords = false;
                        break;
                    }

                    var objectsBatch = entityDefinitionData
                        .AsParallel() 
                        .Select(record => ProcessRecord(record, selectedOptions, validationRuleCounts!, recordTypeCounts!, apexTriggerCounts!))
                        .Where(obj => obj != null)
                        .ToList();

                    allObjects.AddRange(objectsBatch!);
                    offset += batchSize;
                }
            }
            catch (Exception ex)
            {
                throw new SystemException($"Error occurred while retrieving Salesforce objects: {ex.Message}");
            }

            return allObjects;
        }

        private salesforceObject? ProcessRecord(JsonElement record, HashSet<string> selectedOptions,
            Dictionary<string, int> validationRuleCounts, Dictionary<string, int> recordTypeCounts,
            Dictionary<string, int> apexTriggerCounts)
        {
            string name = record.TryGetProperty("QualifiedApiName", out var qualifiedApiName) ? qualifiedApiName.GetString()! : string.Empty;
            string? namespacePrefix = record.TryGetProperty("NamespacePrefix", out var namespacePrefixProp) ? namespacePrefixProp.GetString() : null;
            bool isApexTriggerable = record.TryGetProperty("IsApexTriggerable", out var apexTriggerableProp) && apexTriggerableProp.GetBoolean();

            if (!DetermineObjectInclusion(selectedOptions, name, namespacePrefix, isApexTriggerable) || IsExcludedObject(name))
                return null;

            return new salesforceObject
            {
                ApiName = name,
                Label = record.TryGetProperty("Label", out var label) ? label.GetString()! : string.Empty,
                Type = GetObjectType(name),
                PluralLabel = record.TryGetProperty("PluralLabel", out var pluralLabelProp) ? pluralLabelProp.GetString()! : string.Empty,
                AllowSearch = record.TryGetProperty("IsSearchable", out var searchableProp) && searchableProp.GetBoolean(),
                Deployed = record.TryGetProperty("DeploymentStatus", out var deploymentStatusProp) && deploymentStatusProp.GetString() == "Deployed",
                Description = record.TryGetProperty("Description", out var desc) ? desc.GetString()! : string.Empty,
                LastModified = record.TryGetProperty("LastModifiedDate", out var lastModifiedProp) &&
                               DateTime.TryParse(lastModifiedProp.GetString(), out var parsedDate)
                               ? parsedDate.ToString("yyyy-MM-dd HH:mm:ss") : null,
                ExternalSharingModel = record.TryGetProperty("ExternalSharingModel", out var externalSharingProp) ? externalSharingProp.GetString()! : string.Empty,
                InternalSharingModel = record.TryGetProperty("InternalSharingModel", out var internalSharingProp) ? internalSharingProp.GetString()! : string.Empty,
                NamespacePrefix = namespacePrefix,
                IsApexTriggerable = isApexTriggerable,
                ValidationRuleCount = validationRuleCounts.TryGetValue(name, out var vrCount) ? vrCount : 0,
                RecordTypeCount = recordTypeCounts.TryGetValue(name, out var rtCount) ? rtCount : 0,
                ApexTriggersCount = apexTriggerCounts.TryGetValue(name, out var atCount) ? atCount : 0,
                Fields = new List<salesforceField>()
            };
        }

        private static string GetObjectType(string name)
        {
            if (name.EndsWith("__c", StringComparison.OrdinalIgnoreCase)) return "Custom";
            if (name.EndsWith("__e", StringComparison.OrdinalIgnoreCase)) return "Custom Event";
            if (name.EndsWith("__mdt", StringComparison.OrdinalIgnoreCase)) return "Custom Metadata";
            return "Standard";
        }
        private bool IsExcludedObject(string name)
        {
            return name.EndsWith("feed", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("Share", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("History", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("ChangeEvent", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("OwnerSharingRule", StringComparison.OrdinalIgnoreCase);
        }

        public bool DetermineObjectInclusion(HashSet<string> selectedOptions, string name, string? namespacePrefix, bool isApexTriggerable)
        {
            bool hasNamespace = selectedOptions.Contains("Namespace");
            bool hasCustomMetadata = selectedOptions.Contains("CustomMetadata");
            bool hasCustomEvent = selectedOptions.Contains("CustomEvent");
            bool hasNonTriggerables = selectedOptions.Contains("NonTriggerable");
            bool hasManagePackages = selectedOptions.Contains("ManagePackages");

            bool isCustomMetadata = name.EndsWith("__mdt", StringComparison.OrdinalIgnoreCase);
            bool isCustomEvent = name.EndsWith("__e", StringComparison.OrdinalIgnoreCase);
            bool isManagePackage = Regex.Matches(name, "__").Count == 2;

            if (selectedOptions.Count == 0)
            {
                return isApexTriggerable && namespacePrefix == null && !isCustomMetadata && !isCustomEvent && !isManagePackage;
            }

            return DetermineInclusionBasedOnOptions(
                isApexTriggerable, namespacePrefix, isCustomMetadata, isCustomEvent, isManagePackage,
                hasNamespace, hasCustomMetadata, hasCustomEvent, hasNonTriggerables, hasManagePackages, selectedOptions.Count
            );
        }

        private bool DetermineInclusionBasedOnOptions(
            bool isApexTriggerable, string? namespacePrefix, bool isCustomMetadata, bool isCustomEvent, bool isManagePackage,
            bool hasNamespace, bool hasCustomMetadata, bool hasCustomEvent, bool hasNonTriggerables, bool hasManagePackages, int optionCount)
        {
            if (optionCount == 1)
            {
                return SingleOptionInclusion(isApexTriggerable, namespacePrefix, isCustomMetadata, isCustomEvent, isManagePackage, hasNamespace, hasCustomMetadata, hasCustomEvent, hasNonTriggerables, hasManagePackages);
            }
            else if (optionCount == 2)
            {
                return TwoOptionsInclusion(isApexTriggerable, namespacePrefix, isCustomMetadata, isCustomEvent, isManagePackage, hasNamespace, hasCustomMetadata, hasCustomEvent, hasNonTriggerables, hasManagePackages);
            }
            else if (optionCount == 3)
            {
                return ThreeOptionsInclusion(isApexTriggerable, namespacePrefix, isCustomMetadata, isCustomEvent, isManagePackage, hasNamespace, hasCustomMetadata, hasCustomEvent, hasNonTriggerables, hasManagePackages);
            }
            else if (optionCount == 4)
            {
                return FourOptionsInclusion(isApexTriggerable, namespacePrefix, isCustomMetadata, isCustomEvent, isManagePackage, hasNamespace, hasCustomMetadata, hasCustomEvent, hasNonTriggerables, hasManagePackages);
            }
            else if (optionCount == 5)
            {
                return FiveOptionsInclusion(namespacePrefix, isCustomMetadata, isCustomEvent, isManagePackage);
            }

            return false;
        }

        private bool SingleOptionInclusion(
            bool isApexTriggerable, string? namespacePrefix, bool isCustomMetadata, bool isCustomEvent, bool isManagePackage,
            bool hasNamespace, bool hasCustomMetadata, bool hasCustomEvent, bool hasNonTriggerables, bool hasManagePackages)
        {
            if (hasNamespace)
            {
                return isApexTriggerable && !isCustomEvent && !isCustomMetadata && !isManagePackage;
            }
            if (hasNonTriggerables)
            {
                return namespacePrefix == null && !isCustomEvent && !isCustomMetadata && !isManagePackage;
            }
            if (hasCustomMetadata)
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomEvent && !isManagePackage) || (isCustomMetadata && namespacePrefix == null && isApexTriggerable);
            }
            if (hasCustomEvent)
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomMetadata && !isManagePackage) || (isCustomEvent && namespacePrefix == null && isApexTriggerable);
            }
            if (hasManagePackages)
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomEvent && !isCustomMetadata) || (isManagePackage && isApexTriggerable);
            }

            return false;
        }

        private bool TwoOptionsInclusion(
            bool isApexTriggerable, string? namespacePrefix, bool isCustomMetadata, bool isCustomEvent, bool isManagePackage,
            bool hasNamespace, bool hasCustomMetadata, bool hasCustomEvent, bool hasNonTriggerables, bool hasManagePackages)
        {
            if (hasNamespace && hasCustomMetadata)
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomEvent && !isManagePackage) || isCustomMetadata;
            }
            if (hasNamespace && hasCustomEvent)
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomMetadata && !isManagePackage) || isCustomEvent;
            }
            if (hasNonTriggerables && hasNamespace)
            {
                return !isCustomMetadata && !isCustomEvent && !isManagePackage;
            }
            if (hasNonTriggerables && hasCustomMetadata)
            {
                return (namespacePrefix == null && !isCustomEvent && !isManagePackage) || (isCustomMetadata && namespacePrefix == null);
            }
            if (hasNonTriggerables && hasCustomEvent)
            {
                return (namespacePrefix == null && !isCustomMetadata && !isManagePackage) || (isCustomEvent && namespacePrefix == null);
            }
            if (hasManagePackages && hasNamespace)
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomEvent && !isCustomMetadata) || (isManagePackage && isApexTriggerable);
            }
            if (hasManagePackages && hasCustomEvent)
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomMetadata) || (isCustomEvent && isApexTriggerable && namespacePrefix == null) || (isManagePackage && isApexTriggerable );
            }
            if (hasManagePackages && hasCustomMetadata)
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomEvent) || (isCustomMetadata && isApexTriggerable && namespacePrefix == null) || (isManagePackage && isApexTriggerable );
            }
            if (hasManagePackages && hasNonTriggerables)
            {
                return (namespacePrefix == null && !isCustomMetadata && !isCustomEvent) || (isManagePackage );
            }
            if (hasCustomMetadata && hasCustomEvent )
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomEvent && !isCustomMetadata && !isManagePackage) || (isCustomEvent && namespacePrefix == null) || (isCustomMetadata && namespacePrefix == null);
            }
            return false;
        }

        private bool ThreeOptionsInclusion(
            bool isApexTriggerable, string? namespacePrefix, bool isCustomMetadata, bool isCustomEvent, bool isManagePackage,
            bool hasNamespace, bool hasCustomMetadata, bool hasCustomEvent, bool hasNonTriggerables, bool hasManagePackages)
        {
            if (hasNamespace && hasCustomMetadata && hasCustomEvent)
            {
                return (isApexTriggerable && namespacePrefix == null && !isManagePackage) || isCustomEvent || isCustomMetadata;
            }
            if (hasNamespace && hasNonTriggerables && hasCustomMetadata)
            {
                return (!isCustomEvent && !isManagePackage) || isCustomMetadata;
            }
            if (hasNamespace && hasNonTriggerables && hasCustomEvent)
            {
                return (!isCustomMetadata && !isManagePackage) || isCustomEvent;
            }
            if (hasManagePackages && hasNamespace && hasCustomEvent)
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomMetadata) || (isManagePackage && isApexTriggerable) || (isCustomEvent && isApexTriggerable);
            }
            if (hasManagePackages && hasNamespace && hasCustomMetadata)
            {
                return (isApexTriggerable && namespacePrefix == null && !isCustomEvent) || (isManagePackage && isApexTriggerable) || (isCustomMetadata && isApexTriggerable);
            }
            if (hasManagePackages && hasNamespace && hasNonTriggerables)
            {
                return (namespacePrefix == null && !isCustomEvent && !isCustomMetadata) || isManagePackage;
            }
            if (hasManagePackages && hasCustomEvent && hasCustomMetadata)
            {
                return (isApexTriggerable && namespacePrefix == null) || (isCustomMetadata && isApexTriggerable && namespacePrefix == null) || (isCustomEvent && isApexTriggerable && namespacePrefix == null) || (isManagePackage && isApexTriggerable);
            }
            if (hasNonTriggerables && hasCustomEvent && hasCustomMetadata)
            {
                return (namespacePrefix == null && !isManagePackage) || (isCustomEvent && namespacePrefix == null) || (isCustomMetadata && namespacePrefix == null);
            }
            if (hasManagePackages && hasCustomEvent && hasNonTriggerables)
            {
              return (namespacePrefix == null && !isCustomMetadata) || (isCustomEvent && namespacePrefix == null) || (isManagePackage);
            }
            if (hasManagePackages && hasCustomMetadata && hasNonTriggerables)
            {
                return (namespacePrefix == null && !isCustomEvent) || (isCustomMetadata && namespacePrefix == null) || (isManagePackage);
            }
            return false;
        }

        private bool FourOptionsInclusion(
            bool isApexTriggerable, string? namespacePrefix, bool isCustomMetadata, bool isCustomEvent, bool isManagePackage,
            bool hasNamespace, bool hasCustomMetadata, bool hasCustomEvent, bool hasNonTriggerables, bool hasManagePackages)
        {
            if (hasNamespace && hasNonTriggerables && hasCustomMetadata && hasCustomEvent)
            {
                return !isManagePackage || (isCustomMetadata || isCustomEvent);
            }
            if (hasManagePackages && hasNamespace && hasCustomEvent && hasCustomMetadata)
            {
                return (namespacePrefix == null && isApexTriggerable) || (isCustomEvent && isApexTriggerable) || (isCustomMetadata && isApexTriggerable) || (isManagePackage && isApexTriggerable);
            }
            if (hasManagePackages && hasNamespace && hasNonTriggerables && hasCustomEvent)
            {
                return (namespacePrefix == null && !isCustomMetadata) || (isCustomEvent) || (isManagePackage);
            }
            if (hasManagePackages && hasNamespace && hasNonTriggerables && hasCustomMetadata)
            {
                return (namespacePrefix == null && !isCustomEvent) || (isCustomMetadata) || (isManagePackage);
            }
            if (hasManagePackages && hasCustomEvent && hasCustomMetadata && hasNonTriggerables)
            {
                return (namespacePrefix == null) || (isCustomMetadata && namespacePrefix == null) || (isCustomEvent && namespacePrefix == null) || (isManagePackage);
            }

            return false;
        }

        private bool FiveOptionsInclusion(string? namespacePrefix, bool isCustomMetadata, bool isCustomEvent, bool isManagePackage)
        {
            return (namespacePrefix == null) || isCustomMetadata || isManagePackage || isCustomEvent;
        }


        private async Task<List<JsonElement>> ExecuteToolingQueryAsync(string query, string? accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, query);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to execute query. Error: {response.StatusCode}, Message: {errorResponse}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonData = JsonDocument.Parse(jsonResponse);

            return jsonData.RootElement.GetProperty("records").EnumerateArray().ToList();
        }
        private async Task<List<ValidationRule>> FetchValidationRulesAsync(string? instanceUrl, string? accessToken)
        {
            var requestUrl = $"{instanceUrl}/services/data/v62.0/tooling/query/?q=SELECT+EntityDefinition.QualifiedApiName+FROM+ValidationRule";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to fetch validation rules. Error: {response.StatusCode}, Message: {errorResponse}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonData = JsonDocument.Parse(jsonResponse);

            return jsonData.RootElement.GetProperty("records")
                .EnumerateArray()
                .Select(record =>
                {
                    return record.TryGetProperty("EntityDefinition", out var entityDef) &&
                           entityDef.TryGetProperty("QualifiedApiName", out var qualifiedApiName)
                        ? new ValidationRule
                        {
                            EntityName = qualifiedApiName.GetString()
                        }
                        : null;
                })
                .Where(rule => rule != null)
                .ToList()!;
        }

        private async Task<List<RecordType>> FetchRecordTypesAsync(string? instanceUrl, string? accessToken)
        {
            var requestUrl = $"{instanceUrl}/services/data/v62.0/tooling/query/?q=SELECT+SObjectType,Id+FROM+RecordType";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to fetch record types. Error: {response.StatusCode}, Message: {errorResponse}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine("RecordType JSON Response: " + jsonResponse);

            var jsonData = JsonDocument.Parse(jsonResponse);

            return jsonData.RootElement.GetProperty("records")
                .EnumerateArray()
                .Select(record =>
                {
                    if (record.TryGetProperty("SobjectType", out var sObjectType))
                    {
                        return new RecordType
                        {
                            SObjectType = sObjectType.GetString()
                        };
                    }
                    else
                    {
                        Console.WriteLine("Missing SObjectType property in record: " + record);
                        return null;
                    }
                })
                .Where(rt => rt != null) // Filter out null results
                .ToList()!;
        }

        private async Task<List<ApexTrigger>> FetchApexTriggersAsync(string? instanceUrl, string? accessToken)
        {
            var requestUrl = $"{instanceUrl}/services/data/v62.0/tooling/query/?q=SELECT+TableEnumOrId+FROM+ApexTrigger";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonData = JsonDocument.Parse(jsonResponse);

            return jsonData.RootElement.GetProperty("records")
                .EnumerateArray()
                .Select(record => new ApexTrigger
                {
                    TableEnumOrId = record.GetProperty("TableEnumOrId").GetString()
                })
                .ToList();
        }

        public class ValidationRule
        {
            public string? EntityName { get; set; }
        }


        public class RecordType
        {
            public string? SObjectType { get; set; }
        }

        public class ApexTrigger
        {
            public string? TableEnumOrId { get; set; }
        }

        public async Task<List<salesforceField>> FetchFieldsFromSalesforceAsync(string instanceUrl, string accessToken)
        {
            var fieldsList = new List<salesforceField>();
            int batchSize = 101;
            int offset = 0;
            bool moreRecords = true;
            var tasks = new List<Task<List<salesforceField>>>();

            Dictionary<string, string?> customFieldDescriptions = new();

            try
            {
                var descriptionQuery = $"{instanceUrl}/services/data/v62.0/tooling/query/?" +
                                       $"q=SELECT+DeveloperName,EntityDefinition.DeveloperName,InlineHelpText+FROM+CustomField";
                var descriptionResponse = await ExecuteToolingQueryAsync(descriptionQuery, accessToken);

                customFieldDescriptions = descriptionResponse
                    .Where(customField => customField.TryGetProperty("DeveloperName", out var fieldName) && fieldName.ValueKind != JsonValueKind.Null)
                    .Where(customField => customField.TryGetProperty("EntityDefinition", out var entityDefinition) && entityDefinition.ValueKind != JsonValueKind.Null)
                    .ToDictionary(
                        customField => $"{customField.GetProperty("EntityDefinition").GetProperty("DeveloperName").GetString()}.{customField.GetProperty("DeveloperName").GetString()}",
                        customField => customField.TryGetProperty("InlineHelpText", out var descProp) ? descProp.GetString() : null
                    );
                Debug.WriteLine($"CustomFieldDescriptions count: {customFieldDescriptions.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching field descriptions: {ex.Message}");
            }

            while (moreRecords)
            {
                var entityQuery = $"{instanceUrl}/services/data/v62.0/tooling/query/?" +
                                  $"q=SELECT+QualifiedApiName+FROM+EntityDefinition+LIMIT+{batchSize}+OFFSET+{offset}";

                try
                {
                    var entityResponse = await ExecuteToolingQueryAsync(entityQuery, accessToken);

                    if (entityResponse.Count == 0)
                    {
                        moreRecords = false;
                        break;
                    }

                    var filteredEntities = entityResponse
                        .Select(entity => entity.GetProperty("QualifiedApiName").GetString())
                        .Where(qualifiedName => !(
                            qualifiedName!.EndsWith("OwnerSharingRule", StringComparison.OrdinalIgnoreCase) ||
                            qualifiedName.EndsWith("ChangeEvent", StringComparison.OrdinalIgnoreCase) ||
                            qualifiedName.EndsWith("History", StringComparison.OrdinalIgnoreCase) ||
                            qualifiedName.EndsWith("Share", StringComparison.OrdinalIgnoreCase) ||
                            qualifiedName.EndsWith("feed", StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (filteredEntities.Count == 0)

                    {
                        offset += batchSize;
                        continue;
                    }

                    for (int i = 0; i < filteredEntities.Count; i += batchSize)
                    {
                        var batch = filteredEntities.Skip(i).Take(batchSize).ToList();
                        var inClauseQuery = string.Join(",", batch.Select(entityName => $"'{entityName}'"));

                        var fieldQuery = $"{instanceUrl}/services/data/v62.0/tooling/query/?" +
                                         $"q=SELECT+ID,DataType,DeveloperName,NamespacePrefix,Label,Length,Description, Scale, Precision,EntityDefinition.QualifiedApiName,DurableId+FROM+FieldDefinition+" +
                                         $"WHERE+EntityDefinition.QualifiedApiName+IN+({inClauseQuery})";

                        tasks.Add(FetchFieldsForBatchAsync(fieldQuery, accessToken, customFieldDescriptions));
                    }


                    offset += batchSize;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error fetching entity definitions: {ex.Message}");
                    moreRecords = false;
                }
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                if (result != null)
                {
                    fieldsList.AddRange(result);
                }
            }

            Debug.WriteLine($"Total Fields Retrieved: {fieldsList.Count}");
            return fieldsList;
        }

        private async Task<List<salesforceField>> FetchFieldsForBatchAsync(
            string fieldQuery,
            string accessToken,
            Dictionary<string, string?> customFieldDescriptions)
        {
            var fieldsForBatch = new List<salesforceField>();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, fieldQuery);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var fieldResponse = await _httpClient.SendAsync(request);

                if (!fieldResponse.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Error fetching fields for batch: {fieldResponse.StatusCode}");
                    return fieldsForBatch;
                }

                var fieldJson = await fieldResponse.Content.ReadAsStringAsync();
                var fieldDoc = JsonDocument.Parse(fieldJson);

                if (fieldDoc.RootElement.TryGetProperty("records", out var fields))
                {
                    foreach (var field in fields.EnumerateArray())
                    {
                        var SanitizedentityName = "";
                        var IsCustom = "Standard";
                        var fieldName = field.GetProperty("DeveloperName").GetString();
                        var entityName = field.GetProperty("EntityDefinition").GetProperty("QualifiedApiName").GetString();
                        if (entityName!.EndsWith("__c", StringComparison.OrdinalIgnoreCase))
                            SanitizedentityName = entityName.Substring(0, entityName.Length - 3);


                        var helpText = customFieldDescriptions.GetValueOrDefault($"{SanitizedentityName}.{fieldName}");
                        if (customFieldDescriptions.ContainsKey($"{SanitizedentityName}.{fieldName}") || fieldName!.EndsWith("__c"))
                            IsCustom = "Custom";


                        var salesforceField = new salesforceField
                        {
                            ObjectName = entityName,
                            Name = fieldName,
                            Label = field.GetProperty("Label").GetString(),
                            Type = field.GetProperty("DataType").GetString(),
                            IsCustom = IsCustom,
                            description = field.GetProperty("Description").ToString(),
                            HelpText = helpText,

                        };

                        fieldsForBatch.Add(salesforceField);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error describing fields for batch: {ex.Message}");
            }

            return fieldsForBatch;
        }


    }
}


