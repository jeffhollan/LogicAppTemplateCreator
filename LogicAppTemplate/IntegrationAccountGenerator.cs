using LogicAppTemplate.Templates;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace LogicAppTemplate
{
    public class IntegrationAccountGenerator
    {
        private string integrationAccount;
        private IResourceCollector resourceCollector;
        private string resourceGroup;
        private string subscriptionId;
        private string artifactName;
        private ARtifactType type;
        public enum ARtifactType
        {
            Schemas,
            Maps
        }

        public IntegrationAccountGenerator(string artifactName, ARtifactType type, string integrationAccount, string subscriptionId, string resourceGroup, IResourceCollector resourceCollector)
        {
            this.integrationAccount = integrationAccount;
            this.subscriptionId = subscriptionId;
            this.resourceGroup = resourceGroup;
            this.resourceCollector = resourceCollector;
            this.type = type;
            this.artifactName = artifactName;
        }

        public async Task<JObject> GenerateTemplate()
        {
            JObject _definition = await resourceCollector.GetResource($"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Logic/integrationAccounts/{integrationAccount}/{type.ToString().ToLower()}/{artifactName}", "2018-07-01-preview");
            return await generateDefinition(_definition);
        }


        public async Task<JObject> generateDefinition(JObject resource)
        {
            ARMTemplateClass template = new ARMTemplateClass();
            var paramiaName = template.AddParameter("IntegrationAccountName", "string", integrationAccount);
            var uri = resource["properties"]["contentLink"].Value<string>("uri").Split('?');
            var rawresource = await resourceCollector.GetRawResource(uri[0], uri[1].Replace("api-version=", ""));

            var paramResourceName = template.AddParameter("name", "string", resource.Value<string>("name"));

            //create the resource and add to the template
            var obj = new IntegationAccountResource();
            obj.name = $"[concat(parameters('{paramiaName}'), '/' ,parameters('{paramResourceName}'))]";
            obj.type = resource.Value<string>("type");
            //add the current Integration Account parameter name
            var location = template.AddParameter("integrationAccountLocation", "string", "[resourceGroup().location]");
            obj.location = "[parameters('integrationAccountLocation')]";

            if (type == ARtifactType.Maps)
            {
                obj.properties["mapType"] = resource["properties"]["mapType"];
                obj.properties["parametersSchema"] = resource["properties"]["parametersSchema"];
                obj.properties["contentType"] = obj.properties.Value<string>("mapType") == "Liquid" ? "text/plain" : "application/xml";
            }
            else if (type == ARtifactType.Schemas)
            {
                obj.properties["schemaType"] = resource["properties"]["schemaType"];
                obj.properties["contentType"] = "application/xml";
            }

            obj.properties["content"] = rawresource;

            template.resources.Add(JObject.FromObject( obj));

            return JObject.FromObject(template);
        }
    }
}