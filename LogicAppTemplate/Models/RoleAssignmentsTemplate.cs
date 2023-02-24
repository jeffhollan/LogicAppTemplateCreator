using LogicAppTemplate.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogicAppTemplate.Models
{

    public class RoleAssignmentsProperties
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string displayName { get; set; }

        public string roleDefinitionId { get; set; }

        public string principalId { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string scope { get; set; }
    }

    public class RoleAssignmentsTemplate
    {
        public string type => "Microsoft.Authorization/roleAssignments";

        public string apiVersion => "2022-04-01";

        public string name { get; set; }
        
        public string scope { get; set; }

        public RoleAssignmentsProperties properties { get; set; }

        public JObject ToJObject()
        {
            var retVal = new RoleAssignmentsTemplate
            {
                name = $"[guid(parameters('storageAccounts_ResourceGroupName'), parameters('logicAppName'), '{new AzureResourceId(properties.roleDefinitionId).ResourceName}')]",                    
                scope = scope,
                properties = new RoleAssignmentsProperties
                {
                    roleDefinitionId = $"[concat(subscription().Id, '/providers/Microsoft.Authorization/roleDefinitions/{new AzureResourceId(properties.roleDefinitionId).ResourceName}')]",
                    principalId = $"[reference(concat(resourceId('Microsoft.Logic/workflows', parameters('logicAppName')), '/providers/Microsoft.ManagedIdentity/Identities/default'), '2018-11-30').principalId]",
                    displayName = $"[concat('RoleAssignments_', guid(parameters('storageAccounts_ResourceGroupName'), parameters('logicAppName'), '{new AzureResourceId(properties.roleDefinitionId).ResourceName}'))]"

                }
            };

            return JObject.FromObject(retVal);
        }
    }
}
