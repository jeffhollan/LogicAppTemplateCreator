using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class EventGridConnectorTest
    {
        private JObject GetTemplate()
        {
            var generator = new TemplateGenerator("INT0005.Publish", "9fake3d-3c94-40e9-b2cb-18921e5d6cfa", "LogicAppsDev", new MockResourceCollector("EventGridActionConnector"));

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
            Assert.AreEqual("INT0005.Publish", parameters["logicAppName"].Value<string>("defaultValue"));

            Assert.AreEqual("[resourceGroup().location]", parameters["logicAppLocation"].Value<string>("defaultValue"));

            Assert.AreEqual("[resourceGroup().name]", parameters["ConvertXMLToJSON-ResourceGroup"].Value<string>("defaultValue"));
            Assert.AreEqual("INT0072-GetGenericModelDev", parameters["ConvertXMLToJSON-FunctionApp"].Value<string>("defaultValue"));
            Assert.AreEqual("ConvertXMLToJSON", parameters["ConvertXMLToJSON-FunctionName"].Value<string>("defaultValue"));
            Assert.AreEqual("[resourceGroup().name]", parameters["PGPDecrypt-ResourceGroup"].Value<string>("defaultValue"));
            Assert.AreEqual("FunctionsDev", parameters["PGPDecrypt-FunctionApp"].Value<string>("defaultValue"));
            Assert.AreEqual("PGPDecrypt", parameters["PGPDecrypt-FunctionName"].Value<string>("defaultValue"));
            Assert.AreEqual("https://keyvaultdev.vault.azure.net/secrets/mykey", parameters["paramprivatekeysecretid"].Value<string>("defaultValue"));
            Assert.AreEqual("azureeventgridpublish", parameters["azureeventgridpublish_name"].Value<string>("defaultValue"));
            Assert.AreEqual("PublishMasterData", parameters["azureeventgridpublish_displayName"].Value<string>("defaultValue"));
            Assert.IsNull(parameters["azureeventgridpublish_endpoint"]);

            Assert.IsNull(parameters["azureeventgridpublish_api_key"]);

        }
        [TestMethod]
        public void EventGridConnectionTest()
        {
            var defintion = GetTemplate();

            var connection = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Web/connections").First();

            Assert.AreEqual("[parameters('logicAppLocation')]", connection.Value<string>("location"));
            Assert.AreEqual("[parameters('azureeventgridpublish_name')]", connection.Value<string>("name"));
            Assert.AreEqual("2016-06-01", connection.Value<string>("apiVersion"));
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/providers/Microsoft.Web/locations/',parameters('logicAppLocation'),'/managedApis/azureeventgridpublish')]", connection["properties"]["api"].Value<string>("id"));

            Assert.AreEqual("[parameters('azureeventgridpublish_displayName')]", connection["properties"].Value<string>("displayName"));
            Assert.AreEqual("[listKeys(resourceId(parameters('azureeventgridpublish_resourceGroupName'),'Microsoft.EventGrid/topics',parameters('azureeventgridpublish_instancename')),'2018-01-01').key1]", connection["properties"]["parameterValues"].Value<string>("api_key"));
            Assert.AreEqual("[reference(resourceId(parameters('azureeventgridpublish_resourceGroupName'),'Microsoft.EventGrid/topics',parameters('azureeventgridpublish_instancename')),'2018-01-01').endpoint]", connection["properties"]["parameterValues"].Value<string>("endpoint"));

        }
        [TestMethod]
     public void URLTest()
        {
            string url = "https://masterdatapublication.northeurope-1.eventgrid.azure.net/api/events";
            string location = "northeurope";
            url = url.Replace("https://", "");
            var site = url.Substring(0, url.LastIndexOf("." + location));

            Assert.AreEqual("masterdatapublication", site);
        }
        [TestMethod]
        public void EventGridActionTest()
        {
            var defintion = GetTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();

            var actions = workflow["properties"]["definition"]["actions"];

            Assert.AreEqual("post", actions.Value<JObject>("Publish_Event")["inputs"].Value<string>("method"));
            Assert.AreEqual("/eventGrid/api/events", actions.Value<JObject>("Publish_Event")["inputs"].Value<string>("path"));
          
        }
    }
}
