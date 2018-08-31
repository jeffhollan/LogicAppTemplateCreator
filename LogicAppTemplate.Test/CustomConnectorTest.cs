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
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('LDAPAdapter-ResourceGroup'),'/providers/Microsoft.Web/customApis/LDAPAdapter')]", connection["properties"]["api"].Value<string>("id"));

            Assert.AreEqual("[parameters('LDAPAdapter_displayName')]", connection["properties"].Value<string>("displayName"));
            Assert.AreEqual("[parameters('LDAPAdapter_authType')]", connection["properties"]["parameterValues"].Value<string>("authType"));
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('LDAPAdapter_gatewayresourcegroup'),'/providers/Microsoft.Web/connectionGateways/',parameters('LDAPAdapter_gatewayname'))]", connection["properties"]["parameterValues"]["gateway"].Value<string>("id"));

        }        
    }
}
