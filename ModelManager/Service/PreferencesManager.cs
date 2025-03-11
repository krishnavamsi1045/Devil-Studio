using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ModelManager.Service
{
    public class PreferencesManager
    {
        private const string PreferencesFileName = "Preferences.json";
        private readonly string PreferencesFilePath;

        public PreferencesManager()
        {
            string devilStudioFolder = FindOrCreateDevilStudioFolder();
            PreferencesFilePath = Path.Combine(devilStudioFolder, PreferencesFileName);

            if (!File.Exists(PreferencesFilePath))
            {
                var initialContent = new Dictionary<string, Dictionary<string, List<string>>>(); 
                File.WriteAllText(PreferencesFilePath, JsonConvert.SerializeObject(initialContent, Formatting.Indented));
            }
        }

        private string FindOrCreateDevilStudioFolder()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string devilStudioFolder = Path.Combine(desktopPath, "Devil Studio");

            if (!Directory.Exists(devilStudioFolder))
            {
                Directory.CreateDirectory(devilStudioFolder);
            }

            return devilStudioFolder;
        }

        public Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> LoadPreferences()
        {
            if (File.Exists(PreferencesFilePath))
            {
                string json = File.ReadAllText(PreferencesFilePath);
                return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>>(json)
                       ?? new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>();
            }
            return new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>();
        }

        private void SavePreferences(Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> preferences)
        {
            string json = JsonConvert.SerializeObject(preferences, Formatting.Indented);
            File.WriteAllText(PreferencesFilePath, json);
        }

        public async Task<List<string>> LoadApiNames(string providerName, string organizationName, string listName)
        {
            try
            {
                // Offload the synchronous work to a background thread.
                return await Task.Run(() =>
                {
                    var preferences = LoadPreferences();

                    if (preferences.ContainsKey(providerName) &&
                        preferences[providerName].ContainsKey(organizationName) &&
                        preferences[providerName][organizationName].ContainsKey(listName))
                    {
                        return preferences[providerName][organizationName][listName];
                    }
                    return new List<string>();
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while loading the '{listName}' list for provider '{providerName}', organization '{organizationName}': {ex.Message}");
            }
        }

        public void SaveApiNames(string providerName, string organizationName, string listName, List<string> apiNames)
        {
            try
            {
                var preferences = LoadPreferences();

                if (!preferences.ContainsKey(providerName))
                {
                    preferences[providerName] = new Dictionary<string, Dictionary<string, List<string>>>();
                }

                if (!preferences[providerName].ContainsKey(organizationName))
                {
                    preferences[providerName][organizationName] = new Dictionary<string, List<string>>();
                }


                preferences[providerName][organizationName][listName] = apiNames;
                SavePreferences(preferences);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while saving the '{listName}' list for organization '{organizationName}': {ex.Message}");
            }
        }
        public void ClearApiNames(string providerName, string organizationName, string listName)
        {
            try
            {
                var preferences = LoadPreferences();
                if (preferences.ContainsKey(providerName) &&
                   preferences[providerName].ContainsKey(organizationName) &&
                   preferences[providerName][organizationName].ContainsKey(listName))
                {
                    preferences[providerName][organizationName].Remove(listName);
                    SavePreferences(preferences);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while clearing the '{listName}' list for organization '{organizationName}': {ex.Message}");
            }
        }
        public void ResetFieldCount(string providerName, string organizationName)
        {
            try
            {
                var preferences = LoadPreferences();

                if (preferences.ContainsKey(providerName) &&
                    preferences[providerName].ContainsKey("MainPage") &&
                    preferences[providerName]["MainPage"].ContainsKey(organizationName))
                {
                    var mainPageList = preferences[providerName]["MainPage"][organizationName];

                    for (int i = 0; i < mainPageList.Count; i++)
                    {
                        if (mainPageList[i].StartsWith("TotalFields:") ||
                            mainPageList[i].StartsWith("StandardFields:") ||
                            mainPageList[i].StartsWith("CustomFields:"))
                        {
                            mainPageList[i] = mainPageList[i].Split(':')[0] + ": 0";
                        }
                    }

                    SavePreferences(preferences);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while resetting the field count for organization '{organizationName}': {ex.Message}");
            }
        }

        public void DeleteDataBasedOnOrgName(string providerName, string orgNameWithEmail)
        {
            try
            {
                var preferences = LoadPreferences();

                if (preferences.ContainsKey(providerName))
                {
                    var providerData = preferences[providerName];

                    string fullOrgKey = $"{providerName}: {orgNameWithEmail}";
                    if (providerData.ContainsKey(fullOrgKey))
                    {
                        providerData.Remove(fullOrgKey);
                    }

                    if (providerData.ContainsKey("MainPage") && providerData["MainPage"].ContainsKey(orgNameWithEmail))
                    {
                        providerData["MainPage"].Remove(orgNameWithEmail);
                    }

                    if (providerData.ContainsKey("MainPage") && providerData["MainPage"].Count == 0)
                    {
                        providerData.Remove("MainPage");
                    }

                    if (providerData.Count == 0)
                    {
                        preferences.Remove(providerName);
                    }

                    SavePreferences(preferences);

                    Console.WriteLine($"Successfully deleted all data for '{orgNameWithEmail}' under '{providerName}'.");
                }
                else
                {
                    Console.WriteLine($"No data found for provider '{providerName}'.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while deleting '{orgNameWithEmail}' under '{providerName}': {ex.Message}");
            }
        }
        public void DeleteDataBasedOnEmailId(string providerName, string email)
        {
            try
            {
                var preferences = LoadPreferences();
                if (preferences.ContainsKey(providerName))
                {
                    var providerData = preferences[providerName];
                    var keysToRemove = providerData.Keys.Where(key => key.Contains($"({email})")).ToList();
                    foreach (var key in keysToRemove)
                    {
                        providerData.Remove(key);
                    }
                    if (providerData.ContainsKey("MainPage"))
                    {
                        var mainPageEntries = providerData["MainPage"];
                        var mainKeysToRemove = mainPageEntries.Keys.Where(k => k.Contains($"({email})")).ToList();

                        foreach (var key in mainKeysToRemove)
                        {
                            mainPageEntries.Remove(key);
                        }

                        if (mainPageEntries.Count == 0)
                        {
                            providerData.Remove("MainPage");
                        }
                    }
                    if (providerData.Count == 0)
                    {
                        preferences.Remove(providerName);
                    }
                    SavePreferences(preferences);
                    Console.WriteLine($"Successfully deleted all organization data for email '{email}' under provider '{providerName}'.");
                }
                else
                {
                    Console.WriteLine($"No data found for provider '{providerName}'.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while deleting data for email '{email}' under '{providerName}': {ex.Message}");
            }
        }

        public void IncrementTotalObjects(string providerName, string organizationName)
        {
            try
            {
                var preferences = LoadPreferences();

                if (!preferences.ContainsKey(providerName))
                {
                    preferences[providerName] = new Dictionary<string, Dictionary<string, List<string>>>();
                }

                if (!preferences[providerName].ContainsKey("MainPage"))
                {
                    preferences[providerName]["MainPage"] = new Dictionary<string, List<string>>();
                }

                if (!preferences[providerName]["MainPage"].ContainsKey(organizationName))
                {
                    preferences[providerName]["MainPage"][organizationName] = new List<string>
            {
                "TotalObjects: 0",
                "CustomObjects: 0"
            };
                }

                var mainPageList = preferences[providerName]["MainPage"][organizationName];

                bool totalObjectsUpdated = false;
                bool customObjectsUpdated = false;

                for (int i = 0; i < mainPageList.Count; i++)
                {
                    if (mainPageList[i].StartsWith("TotalObjects:"))
                    {
                        string[] parts = mainPageList[i].Split(":");
                        if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int count))
                        {
                            mainPageList[i] = $"TotalObjects: {count + 1}";
                            totalObjectsUpdated = true;
                        }
                    }
                    else if (mainPageList[i].StartsWith("CustomObjects:"))
                    {
                        string[] parts = mainPageList[i].Split(":");
                        if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int count))
                        {
                            mainPageList[i] = $"CustomObjects: {count + 1}";
                            customObjectsUpdated = true;
                        }
                    }
                }

                // If "TotalObjects" or "CustomObjects" were not found, add them
                if (!totalObjectsUpdated)
                {
                    mainPageList.Add("TotalObjects: 1");
                }
                if (!customObjectsUpdated)
                {
                    mainPageList.Add("CustomObjects: 1");
                }

                SavePreferences(preferences);
                Console.WriteLine($"Successfully incremented 'TotalObjects' and 'CustomObjects' for '{organizationName}' under '{providerName}'.");
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while incrementing 'TotalObjects' for organization '{organizationName}': {ex.Message}");
            }
        }


        public void DecrementTotalObjects(string providerName, string organizationName, bool isCustomObject)
        {
            try
            {
                var preferences = LoadPreferences();

                if (preferences.ContainsKey(providerName) &&
                    preferences[providerName].ContainsKey("MainPage") &&
                    preferences[providerName]["MainPage"].ContainsKey(organizationName))
                {
                    var mainPageList = preferences[providerName]["MainPage"][organizationName];

                    for (int i = 0; i < mainPageList.Count; i++)
                    {
                        if (mainPageList[i].StartsWith("TotalObjects:"))
                        {
                            string[] parts = mainPageList[i].Split(":");
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int count))
                            {
                                int newCount = Math.Max(0, count - 1);
                                mainPageList[i] = $"TotalObjects: {newCount}";
                            }
                        }
                        else if (isCustomObject && mainPageList[i].StartsWith("CustomObjects:"))
                        {
                            string[] parts = mainPageList[i].Split(":");
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int count))
                            {
                                int newCount = Math.Max(0, count - 1);
                                mainPageList[i] = $"CustomObjects: {newCount}";
                            }
                        }
                        else if (!isCustomObject && mainPageList[i].StartsWith("StandardObjects:"))
                        {
                            string[] parts = mainPageList[i].Split(":");
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int count))
                            {
                                int newCount = Math.Max(0, count - 1);
                                mainPageList[i] = $"StandardObjects: {newCount}";
                            }
                        }
                    }

                    SavePreferences(preferences);
                    Console.WriteLine($"Successfully decremented 'TotalObjects' and '{(isCustomObject ? "CustomObjects" : "StandardObjects")}' count for '{organizationName}' under '{providerName}'.");
                }
                else
                {
                    Console.WriteLine($"No 'TotalObjects' entry found for organization '{organizationName}' under provider '{providerName}'.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while decrementing 'TotalObjects' for organization '{organizationName}': {ex.Message}");
            }
        }

        public void DecrementTotalFields(string providerName, string organizationName, int countToRemove, int standardFieldsToRemove, int customFieldsToRemove)
        {
            try
            {
                var preferences = LoadPreferences();

                if (preferences.ContainsKey(providerName) &&
                    preferences[providerName].ContainsKey("MainPage") &&
                    preferences[providerName]["MainPage"].ContainsKey(organizationName))
                {
                    var mainPageList = preferences[providerName]["MainPage"][organizationName];

                    for (int i = 0; i < mainPageList.Count; i++)
                    {
                        if (mainPageList[i].StartsWith("TotalFields:"))
                        {
                            string[] parts = mainPageList[i].Split(":");
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int currentCount))
                            {
                                int newCount = Math.Max(0, currentCount - countToRemove);
                                mainPageList[i] = $"TotalFields: {newCount}";
                            }
                        }
                        else if (mainPageList[i].StartsWith("StandardFields:") && standardFieldsToRemove > 0)
                        {
                            string[] parts = mainPageList[i].Split(":");
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int currentCount))
                            {
                                int newCount = Math.Max(0, currentCount - standardFieldsToRemove);
                                mainPageList[i] = $"StandardFields: {newCount}";
                            }
                        }
                        else if (mainPageList[i].StartsWith("CustomFields:") && customFieldsToRemove > 0)
                        {
                            string[] parts = mainPageList[i].Split(":");
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int currentCount))
                            {
                                int newCount = Math.Max(0, currentCount - customFieldsToRemove);
                                mainPageList[i] = $"CustomFields: {newCount}";
                            }
                        }
                    }

                    SavePreferences(preferences);
                    Console.WriteLine($"Successfully decremented 'TotalFields', 'StandardFields', and 'CustomFields' counts for '{organizationName}' under '{providerName}'.");
                }
                else
                {
                    Console.WriteLine($"No 'TotalFields' entry found for organization '{organizationName}' under provider '{providerName}'.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while decrementing field counts for organization '{organizationName}': {ex.Message}");
            }
        }

    }
}
