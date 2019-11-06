using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class BlobConnectorTest2
    {

        private JObject GetTemplate()
        {
            var generator = new TemplateGenerator("INT0040.HireNew", "9fake3d-3c94-40e9-b2cb-18921e5d6cfa", "LogicAppsDev", new MockResourceCollector("BlobConnector2"));

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



            var blobadsearch = actions.Value<JObject>("Get_Full_AD_Search");

            var managermetadata = blobadsearch.Value<JObject>("metadata");
            
            Assert.AreEqual("[parameters('Get_Full_AD_Search-path')]", managermetadata.Value<string>("[base64(parameters('Get_Full_AD_Search-path'))]"));

            Assert.AreEqual("[concat('/datasets/default/files/@{encodeURIComponent(encodeURIComponent(', parameters('__apostrophe'), base64(parameters('Get_Full_AD_Search-path')), parameters('__apostrophe'), '))}/content')]", blobadsearch["inputs"].Value<string>("path"));
            Assert.AreEqual(1, managermetadata.Children().Count());
        }
    }
}
