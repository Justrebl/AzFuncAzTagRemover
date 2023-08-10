using Justrebl.Utils;
using Microsoft.Extensions.Logging;

namespace AzFuncAzTagRemover

{
    public class ConfigManager
    {
                //Default Const Values for the tags to look for in resources to be deleted
        private const string DEFAULT_TAG_KEY = "env";
        private const string DEFAULT_TAG_VALUE = "trash";
        //TODO: Implement delay before deletion of resources
        private const string DEFAULT_DELETE_BY_KEY = "DeleteBy";
        private const string DEFAULT_DATETIMEFORMAT="dd/MM/yyyy";
        private const bool IGNORE_TAG_CASE = true;

        private readonly ILogger _logger;
        public readonly string? _tenantId;
        public readonly string[] _subscriptionIds;
        public readonly string[] _resourceGroupNames;
        public readonly KeyValuePair<string, string> _targetTag;
        public readonly bool _ignoreCase;
        public readonly int _delayBeforeDeletion;
        public readonly string _deleteByTagKey; 
        public readonly string _dateTimeFormat;
        public readonly ExecutionMode executionMode;

        public ConfigManager(ILogger logger)
        {
            _logger = logger;

            /* 
             * Mandatory: 
             * Retrieving the tenantId to be used for authenticating to Azure
             * Useful for identities with access to multiple Azure AD tenants
             */ 
            _tenantId = Environment.GetEnvironmentVariable("TenantId");

            //If _tenantID not set, will abort execution
            if(string.IsNullOrEmpty(_tenantId))
            {
                _logger.LogCritical("TenantId environment variable not set, aborting execution.");
                throw new System.ArgumentNullException("TenantId environment variable not set, aborting execution.");
            }

            /* 
             * Optional:
             * Retrieve the subscriptionIds to be processed by the function
             * Not setting this variable will result in all subscriptions being processed
             * Split the subscription Ids based on a separator "," and the value of the Subscription Ids Environment Variable
             */
            _subscriptionIds = Environment.GetEnvironmentVariable("SubscriptionIds")?.Split(separator: ",", options: StringSplitOptions.TrimEntries) ?? Array.Empty<string>();

            /*
             * Optional:
             * Retrieve the resource group names to be processed by the function*
             * Not setting this variable will result in all resource groups being processed
             */
            _resourceGroupNames = Environment.GetEnvironmentVariable("ResourceGroupNames")?.Split(separator: ",", options: StringSplitOptions.TrimEntries) ?? Array.Empty<string>();
            

            //Define the tag that needs to be checked as a target for removal
            string targetTagKey = Environment.GetEnvironmentVariable("TargetTagKey") ?? DEFAULT_TAG_KEY;
            string targetTagValue = Environment.GetEnvironmentVariable("TargetTagValue") ?? DEFAULT_TAG_VALUE;
            _targetTag = new KeyValuePair<string, string>(targetTagKey, targetTagValue);
            
            _deleteByTagKey = Environment.GetEnvironmentVariable("DeleteByTagKey") ?? DEFAULT_DELETE_BY_KEY;

            //Set the need for case sensitive tags parsing, defaults to false if not set properly 
            try {
                _ignoreCase = Environment.GetEnvironmentVariable("IgnoreTagCase").ParseBool();
            }
            // Will set default value if CaseSensitiveTags is empty or not a valid boolean value
            catch (NullReferenceException) { _ignoreCase = IGNORE_TAG_CASE; }
            catch (ArgumentException) { _ignoreCase = IGNORE_TAG_CASE;
                _logger.LogError($"Unable to parse the value of the CaseSensitiveTags environment variable, setting to default value : {IGNORE_TAG_CASE}.");
            }

            _dateTimeFormat = Environment.GetEnvironmentVariable("DateTimeFormat") ?? DEFAULT_DATETIMEFORMAT;

            //If ExecutionMode cannot be parsed, set to default value 'Audit'
            if(!Enum.TryParse(Environment.GetEnvironmentVariable("ExecutionMode"), ignoreCase: true, out executionMode)){
                _logger.LogWarning($"Unable to parse the value of the ExecutionMode environment variable, setting to default value : {ExecutionMode.Audit}.");
                executionMode = ExecutionMode.Audit;
            }

            _logger.LogInformation(@$"Azure Function executing with the following parameters : 
                Execution Mode: {executionMode}
                Target Tag: <{_targetTag.Key}:{_targetTag.Value}>
                Target Tag Case Sensitive: {_ignoreCase}
                Delete By Tag : {_deleteByTagKey}
                Date Time Format : {_dateTimeFormat}
                
                Tenant Id: {_tenantId}");
        }
    }
}