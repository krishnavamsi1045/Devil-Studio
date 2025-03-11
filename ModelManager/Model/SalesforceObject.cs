using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


namespace ModelManager.Model
{
    public class SalesforceObject : INotifyPropertyChanged
    {
        public static List<SalesforceObject> AllSalesforceObjects { get; private set; } = new List<SalesforceObject>();
        public static bool IsDataImported { get; private set; } = false;
        public static void StoreImportedData(List<SalesforceObject> objects)
        {
            if (objects != null && objects.Count > 0)
            {
                AllSalesforceObjects.Clear();
                AllSalesforceObjects.AddRange(objects);
                IsDataImported = true;
            }
            else
            {
                ClearData();   
            }
        }
        public static void ClearData()
        {
            AllSalesforceObjects.Clear();
            IsDataImported = false;
        }
        public static bool ShouldDisplayData() => IsDataImported && AllSalesforceObjects.Any();

        private bool _isChecked;
        private DateTime? _lastSyncDate;
        private bool _isActive;

        // New property for active state
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                    IsActive = _isChecked;  // Automatically update IsActive when IsChecked changes
                }
            }
        }

        public DateTime? LastSyncDate
        {
            get => _lastSyncDate;
            set
            {
                _lastSyncDate = value;
                OnPropertyChanged(nameof(LastSyncDate));
            }
        }

        public string? Label { get; set; }
        public string? PluralLabel { get; set; }
        public string? ApiName { get; set; }
        public string? Description { get; set; }
        public bool? AllowSharing { get; set; }
        public bool? IsApexTriggerable { get; set; }

        public bool? AllowBulkApiAccess { get; set; }
        public bool? AllowStreamingApiAccess { get; set; }
        public bool Deployed { get; set; }
        public bool? AllowSearch { get; set; }
        public bool? AddNotesAndAttachments { get; set; }
        public bool? LaunchNewCustom { get; set; }
        public string? Type { get; set; }
        public string? LastModified { get; set; }
        public int ChildRelationshipsCount { get; set; }
        public int FieldsCount { get; set; }
        public int ValidationRuleCount { get; set; }
        public int RecordTypeCount { get; set; }
        public int ApexTriggersCount { get; set; }
        public int FlowTriggersCount { get; set; }
        public string? DeploymentStatus { get; set; }
        public string? ExternalSharingModel { get; set; }
        public string? InternalSharingModel { get; set; }
        public string? SharingModel { get; set; }
        public string? NamespacePrefix { get; set; }
        public List<salesforceField>? Fields { get; set; }
        public string[] FieldsForDisplay
        {
            get
            {
                var displayFields = Fields.Take(15).Select(field => field.Name ?? "-").ToList();

                while (displayFields.Count < 15)
                {
                    displayFields.Add("-");
                }

                return displayFields.ToArray();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class salesforceObject
    {
        public string? Label { get; set; }
        public string? ApiName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? SharingModel { get; set; }
        public bool Deployed { get; set; }
        public string? Type { get; set; }
        public bool AllowSearch { get; set; }
        public string? PluralLabel { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public string? LastModified { get; set; }
        public string? CreatedDate { get; set; }
        public string? ExternalSharingModel { get; set; }
        public string? InternalSharingModel { get; set; }
        public string? NamespacePrefix { get; set; }
        public bool? IsApexTriggerable { get; set; }

        public int FieldsCount { get; set; }
        public int ValidationRuleCount { get; set; }
        public int RecordTypeCount { get; set; }
        public int ApexTriggersCount {  get; set; }
        public int FlowTriggersCount { get; set; }
        public List<salesforceField>? Fields { get; set; }
    }
    public class salesforceField
    {
        public string? ObjectName { get; set; }
        public string? Name { get; set; }
        public bool IsFieldCustom { get; set; }
        public string? Label { get; set; }
        public string? Type { get; set; }
        public string? HelpText { get; set; }
        public string? IsCustom {  get; set; }
        public string? description { get; set; }
        public bool IsUnique { get; set; }
        public bool IsRequired { get; set; }
        public bool IsExternalId { get; set; }
        public string? DefaultValue {  get; set; }
        public int? Length {  get; set; }
        public int? Precision {  get; set; }
        public int? Scale { get; set; }
        public bool checkvalue {  get; set; }
        public string? MaskType {  get; set; }
        public string? MaskCharacter { get; set; } 
        public int VisibleLines { get; set; }
        public string? DisplayFormat { get; set; }
        public int? StartingNumber { get; set; }
        public void UpdateFrom(salesforceField source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source), "Source cannot be null.");

            Label = source.Label;
            Type = source.Type;
            description = source.description;
            HelpText = source.HelpText;
            Length = source.Length;
            Scale = source.Scale;
            Precision= source.Precision;
            checkvalue = source.checkvalue;
            IsCustom = "Custom";
            MaskCharacter = source.MaskCharacter;
            MaskType = source.MaskType;
            VisibleLines = source.VisibleLines;
            DisplayFormat = source.DisplayFormat;
            StartingNumber = source.StartingNumber;
        }
    }

    public class Ropce
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }

        public string? AccessToken { get; set; }

        public string? InstanceUrl { get; set; }

        public string? DomainUrl { get; set; }
    }
    public class OAuthCredentials
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
    }
    public class WILCredentials
    {
        public string? InstanceUrl { get; set; }
        public string? AccessToken { get; set; }
    }
   
    public class JWT
    {
        public string? ClientId { get; set; }
        public string? Username { get; set; }
        public string? EndPointUrl { get; set; }
        public string? Certificate { get; set; }
        public string? InstanceUrl { get; set; }
        public string? AccessToken { get; set; }
    }
    public class SalesforceDx
    {
        public string? Alias { get; set; }
        public string? InstanceUrl { get; set; }
        public string? AccessToken { get; set; }
    }
    public class PkceCredentails
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? InstanceUrl { get; set; }
        public string? DomainUrl { get; set; }
    }


    public class ValidationMessage
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }

        public ValidationMessage(bool IsSuccess, string Message)
        {
            this.IsSuccess = IsSuccess;
            this.Message = Message;

        }
    }

}
