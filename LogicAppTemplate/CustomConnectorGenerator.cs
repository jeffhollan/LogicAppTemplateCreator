using LogicAppTemplate.Templates;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace LogicAppTemplate
{
    public class CustomConnectorGenerator
    {
        private IResourceCollector resourceCollector;
        private string resourceGroup;
        private string subscriptionId;
        private string customConnectorName;

        public CustomConnectorGenerator(string customConnectorName,string subscriptionId, string resourceGroup, IResourceCollector resourceCollector)
        {
            this.customConnectorName = customConnectorName;
            this.subscriptionId = subscriptionId;
            this.resourceGroup = resourceGroup;
            this.resourceCollector = resourceCollector;            
        }

        public async Task<JObject> GenerateTemplate()
        {

            JObject _definition = await resourceCollector.GetResource($"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/customApis/{customConnectorName}", "2018-07-01-preview");
            return await generateDefinition(_definition);
        }


        public async Task<JObject> generateDefinition(JObject resource)
        {
            ARMTemplateClass template = new ARMTemplateClass();            



            return JObject.FromObject(template);
        }
    }
}