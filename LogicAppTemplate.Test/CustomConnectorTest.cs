using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class CustomConnectorTest
    {

        private JObject GetLDAPTemplate()
        {
            var generator = new TemplateGenerator("INT0012C.Workday.Rehire.Leavers", "c107df29-a4af-4bc9-a733-f88f0eaa4296", "blobtest", new MockResourceCollector("BlobConnector"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void GenerateLDAPTemplate()
        {
            var defintion = GetLDAPTemplate();
            Assert.IsNotNull(defintion);
        }
        [TestMethod]
        public void GetLDAPConnection()
        {
            var defintion = GetLDAPTemplate();

            var connection = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Web/connections" && jj.Value<string>("name") == "[parameters('LDAPAdapter_name')]").First();
            Assert.AreEqual("2016-06-01", connection.Value<string>("apiVersion"));
            Assert.AreEqual("[parameters('logicAppLocation')]", connection.Value<string>("location"));
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('LDAPAdapter-ResourceGroup'),'/providers/Microsoft.Web/customApis/',parameters('LDAPAdapter_name'),'')]", connection["properties"]["api"].Value<string>("id"));

            Assert.AreEqual("[parameters('LDAPAdapter_displayName')]", connection["properties"].Value<string>("displayName"));
            Assert.AreEqual("[parameters('LDAPAdapter_authType')]", connection["properties"]["parameterValues"].Value<string>("authType"));
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('LDAPAdapter_gatewayresourcegroup'),'/providers/Microsoft.Web/connectionGateways/',parameters('LDAPAdapter_gatewayname'))]", connection["properties"]["parameterValues"]["gateway"].Value<string>("id"));

        }        

        [TestMethod]
        public void ShouldBuildCustomConnectorARMTemplate()
        {
            AzureResourceCollector resourceCollector = new AzureResourceCollector();
            resourceCollector.Login(string.Empty);

            

            CustomConnectorGenerator generator = new CustomConnectorGenerator("CC-HR-RSU-Denodo--Dev", "40034f3a-ee77-47f3-919a-486648748bbc", "RG-IT-CoreServices-EAI-Dev", resourceCollector);

            //string raw = "{  \"properties\": {    \"runtimeUrls\": [      \"https://flow-vp45yy5lcy54c-mwh-apim-runtime.westus2.environments.microsoftazurelogicapps.net/apim/7590515e6faf4937928967565606c41d\",      \"http://flow-vp45yy5lcy54c-mwh-apim-runtime.westus2.environments.microsoftazurelogicapps.net/apim/7590515e6faf4937928967565606c41d\"    ],    \"capabilities\": [],    \"description\": \"\",    \"displayName\": \"CC-HR-RSU-Denodo--Dev\",    \"iconUri\": \"/Content/retail/assets/default-connection-icon.d269a5b2275fe149967a9c567c002697.2.svg\",    \"apiDefinitions\": {      \"originalSwaggerUrl\": \"https://wawsprodmwh2031882chub8.blob.core.windows.net/api-swagger-files/flow-vp45yy5lcy54c-mwh-apim/7590515e6faf4937928967565606c41d.json_original?sv=2017-04-17&sr=b&sig=teaqvfjD9j0hSGn4ti%2FGd%2BsMPEKZphBuLbEc3q79%2BM4%3D&se=2019-03-12T21%3A18%3A33Z&sp=r\",      \"modifiedSwaggerUrl\": \"https://wawsprodmwh2031882chub8.blob.core.windows.net/api-swagger-files/flow-vp45yy5lcy54c-mwh-apim/7590515e6faf4937928967565606c41d.json?sv=2017-04-17&sr=b&sig=6U%2BE09HseJu4yejYvh3P382z0l2YqLIHQj3H4B1x4cM%3D&se=2019-03-12T21%3A18%3A33Z&sp=r\"    },    \"apiType\": \"Rest\",    \"wsdlDefinition\": {},    \"integrationServiceEnvironment\": {      \"name\": \"ISE-IT-CoreServices-EAI\",      \"id\": \"/subscriptions/40034f3a-ee77-47f3-919a-486648748bbc/resourceGroups/RG-IT-CoreServices-EAI-Prod/providers/Microsoft.Logic/integrationServiceEnvironments/ISE-IT-CoreServices-EAI\",      \"type\": \"Microsoft.Logic/integrationServiceEnvironments\"    }  },  \"id\": \"/subscriptions/40034f3a-ee77-47f3-919a-486648748bbc/resourceGroups/RG-IT-CoreServices-EAI-Dev/providers/Microsoft.Web/customApis/CC-HR-RSU-Denodo--Dev\",  \"name\": \"CC-HR-RSU-Denodo--Dev\",  \"type\": \"Microsoft.Web/customApis\",  \"location\": \"westus2\"}";
            //var result = generator.generateDefinition(JObject.Parse(raw)).GetAwaiter().GetResult();
            var result = generator.GenerateTemplate().GetAwaiter().GetResult();
            Assert.IsNotNull(result);
        }
    }
}
