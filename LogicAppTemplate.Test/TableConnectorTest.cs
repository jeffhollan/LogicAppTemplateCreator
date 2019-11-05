using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class TableConnectorTest
    {
        private JObject GetTemplate()
        {
            var generator = new TemplateGenerator("addDataToTable", "c107df29-a4af-4bc9-a733-f88f0eaa4296", "blobtest", new MockResourceCollector("TableConnector"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void GenreateTemplateTest()
        {
            var defintion = GetTemplate();
            Assert.IsNotNull(defintion);
        }
        [TestMethod]
        public void ParameterTest()
        {
            var defintion = GetTemplate();

            var parameters = defintion.Value<JObject>("parameters");
            Assert.AreEqual("addDataToTable", parameters["logicAppName"].Value<string>("defaultValue"));

            Assert.AreEqual("[resourceGroup().location]", parameters["logicAppLocation"].Value<string>("defaultValue"));

            Assert.AreEqual("azuretables", parameters["azuretables_name"].Value<string>("defaultValue"));
            Assert.AreEqual("connectionblobtest", parameters["azuretables_displayName"].Value<string>("defaultValue"));
            Assert.AreEqual("blobwithsearch", parameters["azuretables_storageaccount"].Value<string>("defaultValue"));
            Assert.AreEqual("testdata", parameters["Insert_Entity-tablename"].Value<string>("defaultValue"));
            Assert.AreEqual("testdata", parameters["Get_entities-tablename"].Value<string>("defaultValue"));
            Assert.IsNull(parameters["azuretables_sharedkey"]);

        }
        [TestMethod]
        public void TableConnectionTest()
        {
            var defintion = GetTemplate();

            var connection = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Web/connections").First();

            Assert.AreEqual("[parameters('logicAppLocation')]", connection.Value<string>("location"));
            Assert.AreEqual("[parameters('azuretables_name')]", connection.Value<string>("name"));
            Assert.AreEqual("2016-06-01", connection.Value<string>("apiVersion"));
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/providers/Microsoft.Web/locations/',parameters('logicAppLocation'),'/managedApis/azuretables')]", connection["properties"]["api"].Value<string>("id"));

            Assert.AreEqual("[parameters('azuretables_displayName')]", connection["properties"].Value<string>("displayName"));
            Assert.AreEqual("[parameters('azuretables_storageaccount')]", connection["properties"]["parameterValues"].Value<string>("storageaccount"));
            Assert.AreEqual("[listKeys(resourceId(parameters('azuretables_resourceGroupName'),'Microsoft.Storage/storageAccounts', parameters('azuretables_accountName')), '2018-02-01').keys[0].value]", connection["properties"]["parameterValues"].Value<string>("sharedkey"));

        }

        [TestMethod]
        public void TableWorkflowTest()
        {
            var defintion = GetTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();
            Assert.AreEqual("[parameters('logicAppLocation')]", workflow.Value<string>("location"));
            Assert.AreEqual("2016-06-01", workflow.Value<string>("apiVersion"));

            var dependsOn = workflow.Value<JArray>("dependsOn");

            Assert.AreEqual(1, dependsOn.Count());

            Assert.AreEqual("[resourceId('Microsoft.Web/connections', parameters('azuretables_name'))]", dependsOn[0].ToString());            
        }
        [TestMethod]
        public void TableActionTest()
        {
            var defintion = GetTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();

            var actions = workflow["properties"]["definition"]["actions"];

            Assert.AreEqual("[concat('/Tables/@{encodeURIComponent(', parameters('__apostrophe'), parameters('Get_entities-tablename'), parameters('__apostrophe'), ')}/entities')]", actions.Value<JObject>("Get_entities")["inputs"].Value<string>("path"));


            Assert.AreEqual("[concat('/Tables/@{encodeURIComponent(', parameters('__apostrophe'), parameters('Insert_Entity-tablename'), parameters('__apostrophe'), ')}/entities')]", actions.Value<JObject>("Insert_Entity")["inputs"].Value<string>("path"));
          
        }
    }
}
