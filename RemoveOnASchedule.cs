using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Justrebl.Utils;


namespace AzFuncAzTagRemover
{
    public class RemoveOnASchedule
    {
        //Default Const Values for the tags to look for in resources to be deleted
        private const string TAG_KEY_MARKER = "env";
        private const string TAG_VALUE_MARKER = "trash";
        //TODO: Implement delay before deletion of resources
        private const string DELETE_BY_KEY = "DeleteBy";
        private const bool CASE_SENSITIVE_TAGS = false;

        //Private Fields necessary for proper execution of the function 
        private readonly ILogger _logger;
        private readonly ArmClient _armClient;
        private readonly KeyValuePair<string,string> _targetTag;
        private readonly bool _caseSensitiveTags;
        private readonly string? _userAssignedIdentity;
        private readonly string? _tenantId;

        public RemoveOnASchedule(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RemoveOnASchedule>();

            /* 
             * Optional: 
             * Retrieving the identity to be used for authenticating to Azure
             */
            //TODO: Validate the userAssignedIdentity authentication process 
            _userAssignedIdentity = Environment.GetEnvironmentVariable("UserAssignedIdentity");

            /* 
             * Optional: 
             * Retrieving the tenantId to be used for authenticating to Azure
             * Useful for identities with access to multiple Azure AD tenants
             */ 
            _tenantId = Environment.GetEnvironmentVariable("TenantId");

            //Define the tag that needs to be checked as a target for removal
            string targetTagKey = Environment.GetEnvironmentVariable("TargetTagKey") ?? TAG_KEY_MARKER;
            string targetTagValue = Environment.GetEnvironmentVariable("TargetTagValue") ?? TAG_VALUE_MARKER;
            _targetTag = new KeyValuePair<string, string>(targetTagKey, targetTagValue);

            //Set the need for case sensitive tags parsing, defaults to false if not set properly 
            try {
                _caseSensitiveTags = Environment.GetEnvironmentVariable("CaseSensitiveTags").ParseBool();
            }
            // Will set default value if CaseSensitiveTags is empty or not a valid boolean value
            catch (NullReferenceException) { _caseSensitiveTags = CASE_SENSITIVE_TAGS; }
            catch (ArgumentException) { _caseSensitiveTags = CASE_SENSITIVE_TAGS;
                _logger.LogError($"Unable to parse the value of the CaseSensitiveTags environment variable, setting to default value : {CASE_SENSITIVE_TAGS}.");
            }

            //Required : https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme?view=azure-dotnet
            //Details on Authenticating to Azure using DefaultAzureCredential : https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            
            if(_tenantId != null && _userAssignedIdentity != null)
                { _armClient = new ArmClient(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = _userAssignedIdentity, TenantId = _tenantId }));}
            else if (_tenantId != null)
                { _armClient = new ArmClient(new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = _tenantId }));}
            else 
                { _armClient = new ArmClient(new DefaultAzureCredential()); }

            _logger.LogInformation(@$"Azure Function executing with the following parameters : 
                Target Tag: <{_targetTag.Key}:{_targetTag.Value}>
                Case Sensitive: {_caseSensitiveTags}
                User Assigned Identity: { (_userAssignedIdentity is null ? "Manually Specified" : "Not Specified" )}
                Tenant Id: {_tenantId}");
        }

        [Function("RemoveOnASchedule")]
        public async void Run([TimerTrigger("%CronSchedule%")] MyInfo myTimer)
        {
            _logger.LogInformation($"Azure Resource Cleaner started at : {DateTime.Now}");

            //TODO: Improve by filtering directly on tag keys and values, based on a genuine REST API Request as explained here : 
            //https://learn.microsoft.com/en-us/rest/api/resources/resources/list#uri-parameters

            //Retrieve subscriptions attached to an authenticated identity
            SubscriptionCollection subCollection = _armClient.GetSubscriptions();

            //Retrieve the desired subscriptionIds from Env Var
            string? subscriptions = Environment.GetEnvironmentVariable("SubscriptionIds");
            
            //TODO: Manage Exceptions
            if (subscriptions == null)
            {
                _logger.LogError("One or more Subscription Ids, comma separated values, are required for this function to process as expected. Aborting any further action."); 
                return;
            }

            //Split the subscription Ids based on a separator "," and the value of the Subscription Ids Environment Variable
            string[] subscriptionIds = subscriptions.Split(separator: ",", options: StringSplitOptions.TrimEntries);

            //For each subscription specified in the environment variable, execute the function below
            foreach (string subscriptionId in subscriptionIds)
            {
                //Connect to the subscription being processed in the loop 
                SubscriptionResource subscription = await subCollection.GetAsync(subscriptionId);

                //Retrieve a list of resource groups defined in the said subscription
                ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
                
                _logger.LogInformation( @$"=========================
                                        Processing subscription named: {subscription.Data.DisplayName}
                                        =========================");
                //For each resource group in the subscription 
                foreach (var resourceGroup in resourceGroups)
                {
                    var resources = resourceGroup.GetGenericResources();

                    foreach(var resource in resources)
                    {
                        //Identify resources with the same tag and value as targeted : 
                        // Case sensitive or Non case sensitive comparison based on configuration 
                        if (
                            //If the resource contains the target tag and value pair in a non case sensitive manner    
                            (!_caseSensitiveTags && resource.Data.Tags.Contains(_targetTag, new NonCaseSensitiveKeyValuePairComparer()))
                            
                            //Or if tag & value pair need to be case sensitive and the resource contains the target tag and value pair
                            || _caseSensitiveTags && resource.Data.Tags.Contains(_targetTag)) {
                            _logger.LogInformation($"Resource to be deleted as it contains the {{ {_targetTag.Key}:{_targetTag.Value}}} Pair : {resource.Data.Name}");
                        }
                    }
                    //TODO: replace with a REST API call that will extract all the resources with a tag specified.
                    //RESOURCE: More details here : https://github.com/Azure/azure-sdk-for-net/blob/main/doc/dev/mgmt_quickstart.md#managing-existing-resources-by-id
                    //Log it's name in the stdout
                    _logger.LogInformation($"Processing Resource Group named: {resourceGroup.Data.Name}");
                }
            }
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
