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
            Assert.AreEqual("[listKeys(resourceId(parameters('azureblob_resourceGroupName'),'Microsoft.Storage/storageAccounts', parameters('azureblob_accountName')), '2018-02-01').keys[0].value]", connection["properties"]["parameterValues"].Value<string>("accessKey"));

        }

        [TestMethod]
        public void BloblWorkflowTest()
        {
            var defintion = GetTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();
            Assert.AreEqual("[parameters('logicAppLocation')]", workflow.Value<string>("location"));
            Assert.AreEqual("2016-06-01", workflow.Value<string>("apiVersion"));

            var dependsOn = workflow.Value<JArray>("dependsOn");

            Assert.AreEqual(2, dependsOn.Count());

            Assert.AreEqual("[resourceId('Microsoft.Web/connections', parameters('LDAPAdapter_name'))]", dependsOn[0].ToString());
            Assert.AreEqual("[resourceId('Microsoft.Web/connections', parameters('azureblob_name'))]", dependsOn[1].ToString());
        }
        [TestMethod]
        public void BlobActionTest()
        {
            var defintion = GetTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();            

            var actions = workflow["properties"]["definition"]["actions"];

            var blob = actions.Value<JObject>("Get_blob_content");

            var metadata = blob.Value<JObject>("metadata");
            Assert.AreEqual(1, metadata.Children().Count());
            Assert.AreEqual("[parameters('Get_blob_content-path')]", metadata.Value<string>("[base64(parameters('Get_blob_content-path'))]"));

            Assert.AreEqual("[concat('/datasets/default/files/@{encodeURIComponent(encodeURIComponent(', parameters('__apostrophe'), base64(parameters('Get_blob_content-path')), parameters('__apostrophe'), '))}/content')]", blob["inputs"].Value<string>("path"));



            var managerblob = actions.Value<JObject>("Get_manger_blob");

            var managermetadata = managerblob.Value<JObject>("metadata");
            Assert.AreEqual(1, managermetadata.Children().Count());
            Assert.AreEqual("[parameters('Get_manger_blob-path')]", managermetadata.Value<string>("[base64(parameters('Get_manger_blob-path'))]"));

            Assert.AreEqual("[concat('/datasets/default/files/@{encodeURIComponent(encodeURIComponent(', parameters('__apostrophe'), base64(parameters('Get_manger_blob-path')), parameters('__apostrophe'), '))}/content')]", managerblob["inputs"].Value<string>("path"));

        }
    }
}
