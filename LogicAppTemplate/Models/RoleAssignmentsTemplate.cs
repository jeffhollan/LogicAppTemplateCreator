using LogicAppTemplate.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Newtonsoft.Json.Serialization;

namespace LogicAppTemplate.Models
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class RoleAssignmentsProperties
    {
        public string PrincipalType { get; set; } = "ServicePrincipal";
        
        public string RoleDefinitionId { get; set; }
        
        public string PrincipalId { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Scope { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class RoleAssignmentsTemplate
    {
        public string Type => "Microsoft.Authorization/roleAssignments";
        
        public string ApiVersion => "2022-04-01";
        
        public string Name { get; set; }
        
        public string Scope { get; set; }
        
        public RoleAssignmentsProperties Properties { get; set; }

        public JObject GenerateJObject(Func<string,string,string,string> addTemplateParameter)
        {
            var resourceId = new AzureResourceId(Properties.Scope);

            var resourceGroupParameterName = addTemplateParameter($"{resourceId.Provider.Item2}_ResourceGroupName", "string", resourceId.ResourceGroupName);
            var roleAssignmentsResourceName = addTemplateParameter($"{resourceId.Provider.Item2}_Name", "string", resourceId.ResourceName);

            var retVal = new RoleAssignmentsTemplate
            {
                Name = $"[guid(parameters('{resourceGroupParameterName}'), parameters('logicAppName'), '{new AzureResourceId(Properties.RoleDefinitionId).ResourceName}')]",                    
                Scope = $"[concat('/{resourceId.Provider.Item1}/{resourceId.Provider.Item2}/', parameters('{roleAssignmentsResourceName}'))]",
                Properties = new RoleAssignmentsProperties
                {
                    RoleDefinitionId = $"[concat(subscription().Id, '/providers/Microsoft.Authorization/roleDefinitions/{new AzureResourceId(Properties.RoleDefinitionId).ResourceName}')]",
                    PrincipalId = "[reference(resourceId(resourceGroup().name, 'Microsoft.Logic/workflows', parameters('logicAppName')), '2019-05-01', 'Full').identity.principalId]"
                }
            };

            return JObject.FromObject(retVal);
        }
    }
}
