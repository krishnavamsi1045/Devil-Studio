using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ModelManager.Model;

public interface ISalesforceService
{
    Task<ValidationMessage> GetAccessTokenAsync(string username, string password, string consumerKey, string consumerSecret,string endPointUrl);
    //Task<List<salesforceObject>> RetrieveStandardObjectsAsync(string instanceUrl, string accessToken);

    //Task<List<salesforceObject>> RetrieveAllCustomDataAsync(string instanceUrl, string accessToken);
    Task<List<salesforceObject>> RetrieveObjectsAsync(string instanceUrl, string accessToken);
    ObservableCollection<salesforceObject> SalesforceObjects { get; }
    //Task<SalesforceObject> GetObjectByApiNameAsync(string instanceUrl, string accessToken, string apiName);
    //Task<List<SalesforceObject>> SyncAllSalesforceObjectsAsync(string instanceUrl, string accessToken);
}
