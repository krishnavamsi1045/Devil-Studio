using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.Model
{
    public static class SessionManager
    {
        public static string? QEInstanceURL { get; set; }
        public static string? QEAccessToken { get; set; }
        public static string? EInstanceURL { get; set; }
        public static string? EAccessToken { get; set; }
        public static bool QEStatus { get; set; }
        public static string? JsonFilePath { get; set; }
        public static string? NameofManager { get; set; }
        public static string? SelectedFolderName { get; set; }
        public static string? SalesforceUsername { get; set; }
        public static string? SalesforcePassword { get; set; }
        public static string? SalesforceSecurityToken { get; set; }
        public static string? SalesforceClientId { get; set; }
        public static bool isStandardObject { get; set; }
        public static bool isIndicator { get; set; }
        public static string? FetchObjectsOrg { get; set; }
        public static bool ImportButtonClick { get; set; }
        public static bool SyncUpdatedObjectsToSalesforce { get; set; }
        public static string? InstanceUrl { get; set; }
        public static string? AccessToken { get; set; }
        public static string? SessionId { get; set; }
        public static string? SalesforceUtilities { get; set; }
        public static bool Isupdatefields { get; set; }
        public static bool IsDupilcate { get; set; }
        public static bool ObjLastopen { get; set; }
        public static bool CreateObjectResult { get; set; }
        public static string? tempFilePath { get; set; }
        public static string? ApiName {  get; set; }
        public static string? SalesforceInstanceUrl { get; set; }
        public static string? SalesforceRefreshToken {  get; set; }
        public static string? SalesforceAccessToken { get; set; }
        public static string? SalesforceDomainUrl { get; set; }
        public static string? FixedClientId { get; set; }
        public static string? FixedClientSecret { get; set; }
        public static string? LocationType { get;set; }
        public static string? ObjectName { get; set; }
        public static string? JWTClientId { get; set; }
        public static string? JWTUsername { get; set; }
        public static string? JWTCertificate { get; set; }
        public static string? JWTEndPointUrl { get; set; }

        public static string? SalesforceAlias { get; set; }

        public static string? SandboxOrgCount { get; set; }

        public static string? OthersCount { get; set; }

        public static List<string> SalesforceOrgsList { get; set; } = new List<string>();


    }

}
