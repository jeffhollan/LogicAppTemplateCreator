using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class StorageQueueConnectorTest
    {

        private JObject GetTemplate()
        {
            var generator = new TemplateGenerator("INT0091.MailboxErrorHandling", "9fake3d-3c94-42e9-b2cb-18921e5d6cfa", "LogicAppsDev", new MockResourceCollector("StorageQueuesConnector"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void GenreateTemplateTest()
        {
            var defintion = GetTemplate();
            Assert.IsNotNull(defintion);
        }
        [TestMethod]
        public void StorageQueueConnectionTest()
        {
            var defintion = GetTemplate();
            int i = 0;
            foreach (var connection in defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Web/connections" && jj.Value<string>("name") == "[parameters('azurequeues_name')]"))
            {
                Assert.AreEqual("[parameters('logicAppLocation')]", connection.Value<string>("location"));
                Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/providers/Microsoft.Web/locations/',parameters('logicAppLocation'),'/managedApis/azurequeues')]", connection["properties"]["api"].Value<string>("id"));
                if (i == 1)
                {
                    Assert.AreEqual("[parameters('azurequeues-1_displayName')]", connection["properties"].Value<string>("displayName"));
                    Assert.AreEqual("[parameters('azurequeues-1_storageaccount')]", connection["properties"]["parameterValues"].Value<string>("storageaccount"));
                    Assert.AreEqual("[listKeys(resourceId(parameters('azurequeues-1_resourceGroupName'),'Microsoft.Storage/storageAccounts', parameters('azurequeues-1_storageaccount')), '2018-02-01').keys[0].value]", connection["properties"]["parameterValues"].Value<string>("sharedkey"));
                }else
                {
                    Assert.AreEqual("[parameters('azurequeues_displayName')]", connection["properties"].Value<string>("displayName"));
                    Assert.AreEqual("[parameters('azurequeues_storageaccount')]", connection["properties"]["parameterValues"].Value<string>("storageaccount"));
                    Assert.AreEqual("[listKeys(resourceId(parameters('azurequeues_resourceGroupName'),'Microsoft.Storage/storageAccounts', parameters('azurequeues_storageaccount')), '2018-02-01').keys[0].value]", connection["properties"]["parameterValues"].Value<string>("sharedkey"));
                }
                i++;
            }
        }

        [TestMethod]
        public void WorkflowTest()
        {
            var defintion = GetTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();
            Assert.AreEqual("[parameters('logicAppLocation')]", workflow.Value<string>("location"));
            Assert.AreEqual("2016-06-01", workflow.Value<string>("apiVersion"));

            var dependsOn = workflow.Value<JArray>("dependsOn");

            Assert.AreEqual(2, dependsOn.Count());

            Assert.AreEqual("[resourceId('Microsoft.Web/connections', parameters('azurequeues_name'))]", dependsOn[0].ToString());
            Assert.AreEqual("[resourceId('Microsoft.Web/connections', parameters('azurequeues-1_name'))]", dependsOn[1].ToString());
        }
        [TestMethod]
        public void ActionTest()
        {
            var defintion = GetTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();            

            var actions = workflow["properties"]["definition"]["actions"];

            var deleteAction = actions.Value<JObject>("Delete_message");
            Assert.AreEqual("@parameters('$connections')['azurequeues_1']['connectionId']", deleteAction["inputs"]["host"]["connection"].Value<string>("name"));
            Assert.AreEqual("[concat('/@{encodeURIComponent(''', parameters('Delete_message-queuename'), ''')}/messages/@{encodeURIComponent(triggerBody()?[''MessageId''])}')]", deleteAction["inputs"].Value<string>("path"));
         
        }
    }
}
