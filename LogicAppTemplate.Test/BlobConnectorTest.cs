using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class BlobConnectorTest
    {

        private JObject GetTemplate()
        {
            var generator = new TemplateGenerator("INT0012C.Workday.Rehire.Leavers", "c107df29-a4af-4bc9-a733-f88f0eaa4296", "blobtest", new MockResourceCollector("BlobConnector"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void GenreateTemplateTest()
        {
            var defintion = GetTemplate();
            Assert.IsNotNull(defintion);
        }
        [TestMethod]
        public void BlobConnectionTest()
        {
            var defintion = GetTemplate();

            var connection = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Web/connections" && jj.Value<string>("name") == "[parameters('azureblob_name')]").First();
            Assert.AreEqual("[parameters('logicAppLocation')]", connection.Value<string>("location"));
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/providers/Microsoft.Web/locations/',parameters('logicAppLocation'),'/managedApis/azureblob')]", connection["properties"]["api"].Value<string>("id"));

            Assert.AreEqual("[parameters('azureblob_displayName')]", connection["properties"].Value<string>("displayName"));
            Assert.AreEqual("[parameters('azureblob_accountName')]", connection["properties"]["parameterValues"].Value<string>("accountName"));
            Assert.AreEqual("[listKeys(resourceId('Microsoft.Storage/storageAccounts', parameters('azureblob_accountName')), providers('Microsoft.Storage', 'storageAccounts').apiVersions[0]).keys[0].value]", connection["properties"]["parameterValues"].Value<string>("accessKey"));

        }
    }
}
