using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Justrebl.Utils;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace AzFuncAzTagRemover
{
    public class RemoveOnASchedule
    {
        private readonly ConfigManager _configManager;
        //Private Fields necessary for proper execution of the function 
        private readonly ILogger _logger;
        private readonly ArmClient _armClient;

        public RemoveOnASchedule(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RemoveOnASchedule>();
            _configManager = new ConfigManager(_logger);

            if (_configManager._tenantId != null)
                { _armClient = new ArmClient(new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = _configManager._tenantId }));}
            else 
                //Select default tenant if not set explicitly 
                { _armClient = new ArmClient(new DefaultAzureCredential()); }
        }

        [Function("RemoveOnASchedule")]
        public async Task Run([TimerTrigger("%CronSchedule%")] MyInfo myTimer)
        {
            _logger.LogInformation($"Azure Resource Cleaner started at : {DateTime.Now}");

            KeyValuePairComparer keyValuePairComparer = new KeyValuePairComparer(_configManager._ignoreCase);
            
            DateTime deleteBy = DateTime.MinValue;
            string actualDeleteByKey = string.Empty;

            //TODO: Improve by filtering directly on tag keys and values, based on a genuine REST API Request as explained here : 
            //https://learn.microsoft.com/en-us/rest/api/resources/resources/list#uri-parameters
            //RESOURCE: More details here : https://github.com/Azure/azure-sdk-for-net/blob/main/doc/dev/mgmt_quickstart.md#managing-existing-resources-by-id
            
            //Retrieve subscriptions attached to an authenticated identity
            SubscriptionCollection subCollection = _armClient.GetSubscriptions();
            
            if ( _configManager._subscriptionIds is null || _configManager._subscriptionIds.Count() == 0)
            {
                _logger.LogError("One or more Subscription Ids, comma separated values, are required for this function to process as expected. Aborting any further action."); 
                //Exit the function
                return;
            }
            
            //For each subscription specified in the environment variable, execute the function below
            foreach (string subscriptionId in _configManager._subscriptionIds)
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
                        if (resource.Data.Tags.Contains(_configManager._targetTag, keyValuePairComparer)){                           
                            _logger.LogInformation($"{{{_configManager._targetTag.Key}:{_configManager._targetTag.Value}}} detected | Resource to be deleted : {resource.Data.Name}");
                            
                            //Check if the resource has a specific date to be deleted by
                            if(resource.Data.Tags.ContainsKey(_configManager._deleteByTagKey, _configManager._ignoreCase, out actualDeleteByKey)){
                                //If deleteby is set but cannot be parsed, then prevent resourse deletion for safety
                                if(!DateTime.TryParseExact(resource.Data.Tags[actualDeleteByKey], _configManager._dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out deleteBy)){
                                    _logger.LogError($"ABORT: \"{resource.Data.Name}\" won't be deleted for safety => Couldn't properly parse the '{_configManager._deleteByTagKey}' resource tag | Found: {resource.Data.Tags[actualDeleteByKey]} | Expected Date Format: {_configManager._dateTimeFormat}.");
                                    break;
                                }
                            }
                            else {
                                //If no deleteBy tag is set, or cannot retrieve it, then set it to MinValue to process deletion immediately
                                deleteBy = DateTime.MinValue;
                            }

                            //If date of delete by is in the future, then skip the resource deletion. 
                            try{
                                if(deleteBy.IsInTheFuture()){
                                    _logger.LogInformation($"SKIP: {resource.Data.Name} set to be deleted by : {deleteBy.ToString(_configManager._dateTimeFormat)}");
                                    break;
                                }
                            }
                            catch(ArgumentException e){_logger.LogError($"Error while comparing dates : {e.Message}"); break;}

                            //Process the actual resource deletion based on a deleteBy tag set to today/past, or if no deleteBy tag has been set at all
                                    _logger.LogInformation(
                                        $"{_configManager.executionMode.ToString().ToUpper()}: {resource.Data.Name} will be deleted now as {(deleteBy <= DateTime.Now ? "'Delete By' Tag is set to : " + deleteBy.ToShortDateString() : "No 'Delete By' Tag was set")})");

                            //Will process deletion based on the execution mode set in the configuration
                            switch(_configManager.executionMode){
                                case ExecutionMode.Audit: 
                                    _logger.LogInformation("AUDIT: No deletion actually processed");
                                    break;
                                case ExecutionMode.Notify:
                                    _logger.LogWarning("NOTIFY: Feature to be implemented");
                                    break;
                                case ExecutionMode.Delete:
                                    _logger.LogInformation($"DELETE: ... Starting on '{resource.Data.Name}' ... ");
                                    await resource.DeleteAsync(Azure.WaitUntil.Completed);
                                    _logger.LogInformation($"DELETE: ... '{resource.Data.Name}' is now deleted");
                                    break;
                            } 

                            //Reset deleteBy to MinValue for the next round of checks
                            deleteBy = DateTime.MinValue;
                        }
                    }
                }
            }
        }        
    }
}