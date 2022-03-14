using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class DynamicsAxConnectorTests
    {
        private JObject GetTemplate()
        {
            var generator = new TemplateGenerator("INT117-LA-004-GetFundingsFromD365", "aabbccdd-9cb5-4652-b245-f1782aa43b25", "INT117-funding-RG", new MockResourceCollector("DynamicsAx"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void GenreateTemplateTest()
        {
            var defintion = GetTemplate();
            Assert.IsNotNull(defintion);
        }
        [TestMethod]
        public void ConnectorTest()
        {
            var defintion = GetTemplate();

            var connection = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Web/connections" && jj.Value<string>("name") == "[parameters('dynamicsax_name')]").First();
            Assert.AreEqual("[parameters('logicAppLocation')]", connection.Value<string>("location"));
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/providers/Microsoft.Web/locations/',parameters('logicAppLocation'),'/managedApis/dynamicsax')]", connection["properties"]["api"].Value<string>("id"));

            Assert.AreEqual("[parameters('dynamicsax_displayName')]", connection["properties"].Value<string>("displayName"));
            Assert.AreEqual("[parameters('dynamicsax_token:clientId')]", connection["properties"]["parameterValues"].Value<string>("token:clientId"));
            Assert.AreEqual("[parameters('dynamicsax_token:clientSecret')]", connection["properties"]["parameterValues"].Value<string>("token:clientSecret"));
            Assert.AreEqual("[parameters('dynamicsax_token:TenantId')]", connection["properties"]["parameterValues"].Value<string>("token:TenantId"));
            Assert.AreEqual("[parameters('dynamicsax_token:resourceUri')]", connection["properties"]["parameterValues"].Value<string>("token:resourceUri"));
            Assert.AreEqual("[parameters('dynamicsax_token:grantType')]", connection["properties"]["parameterValues"].Value<string>("token:grantType"));


        }

        [TestMethod]
        public void Workflowtest()
        {
            var defintion = GetTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();
            Assert.AreEqual("[parameters('logicAppLocation')]", workflow.Value<string>("location"));
            Assert.AreEqual("2019-05-01", workflow.Value<string>("apiVersion"));

            var dependsOn = workflow.Value<JArray>("dependsOn");

            Assert.AreEqual(2, dependsOn.Count());

            Assert.AreEqual("[resourceId('Microsoft.Web/connections', parameters('dynamicsax_name'))]", dependsOn[0].ToString());
            Assert.AreEqual("[resourceId('Microsoft.Web/connections', parameters('sql_name'))]", dependsOn[1].ToString());
        }
        [TestMethod]
        public void DynamicAXAction()
        {
            var defintion = GetTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();            

            var actions = workflow["properties"]["definition"]["actions"];

            var action = actions.Value<JObject>("Lists_items_present_in_table");

            var inputs = action.Value<JObject>("inputs");

            var path = inputs.Value<string>("path");
            //"/datasets/@{encodeURIComponent(encodeURIComponent('az-customer-d365.cloudax.dynamics.com'))}/tables/@{encodeURIComponent(encodeURIComponent('ProductCategoryAssignments'))}/items"
            //[concat('/datasets/@{encodeURIComponent(encodeURIComponent(', parameters('__apostrophe'), parameters('Lists_items_present_in_table-instance'), parameters('__apostrophe'), '))}/tables/@{encodeURIComponent(encodeURIComponent('ProductCategoryAssignments'))}/items')]
            Assert.AreEqual("[concat('/datasets/@{encodeURIComponent(encodeURIComponent(', parameters('__apostrophe'), parameters('Lists_items_present_in_table-instance'), parameters('__apostrophe'), '))}/tables/@{encodeURIComponent(encodeURIComponent(''ProductCategoryAssignments''))}/items')]", path);            

        }

        private JObject GetTemplateTrigger()
        {
            var generator = new TemplateGenerator("INT142-LA-002-partsProgramming", "aabbccdd-9cb5-4652-b245-aabbccddeeff", "INT142-partsProgramming-RG", new MockResourceCollector("DynamicAxTrigger"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void GenreateTemplateTriggerTest()
        {
            var defintion = GetTemplateTrigger();
            Assert.IsNotNull(defintion);
        }
        [TestMethod]
        public void ConnectorTriggerTest()
        {
            var defintion = GetTemplateTrigger();

            var connection = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Web/connections" && jj.Value<string>("name") == "[parameters('dynamicsax_name')]").First();
            Assert.AreEqual("[parameters('logicAppLocation')]", connection.Value<string>("location"));
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/providers/Microsoft.Web/locations/',parameters('logicAppLocation'),'/managedApis/dynamicsax')]", connection["properties"]["api"].Value<string>("id"));

            Assert.AreEqual("[parameters('dynamicsax_displayName')]", connection["properties"].Value<string>("displayName"));
            Assert.AreEqual("[parameters('dynamicsax_token:clientId')]", connection["properties"]["parameterValues"].Value<string>("token:clientId"));
            Assert.AreEqual("[parameters('dynamicsax_token:clientSecret')]", connection["properties"]["parameterValues"].Value<string>("token:clientSecret"));
            Assert.AreEqual("[parameters('dynamicsax_token:TenantId')]", connection["properties"]["parameterValues"].Value<string>("token:TenantId"));
            Assert.AreEqual("[parameters('dynamicsax_token:resourceUri')]", connection["properties"]["parameterValues"].Value<string>("token:resourceUri"));
            Assert.AreEqual("[parameters('dynamicsax_token:grantType')]", connection["properties"]["parameterValues"].Value<string>("token:grantType"));


        }

        [TestMethod]
        public void DynamicAXTrigger()
        {
            var defintion = GetTemplateTrigger();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();

            var triggers = workflow["properties"]["definition"]["triggers"];

            var trigger = triggers.Value<JObject>("When_a_Business_Event_occurs");

            var inputs = trigger.Value<JObject>("inputs");

            var path = inputs.Value<string>("path");
            Assert.AreEqual("[concat('/datasets/@{encodeURIComponent(encodeURIComponent(', parameters('__apostrophe'), parameters('When_a_Business_Event_occurs-instance'), parameters('__apostrophe'), '))}/subscribebusinessevent/@{encodeURIComponent(encodeURIComponent(''PER_ProdOrderStatusChangedPartsProgBusinessEvent''))}')]", path);

        }
    }
}
