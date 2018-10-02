using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class IntegrationAccountTest
    {

        private JObject GetTemplate()
        {
            var generator = new IntegrationAccountGenerator("NotificationMap", IntegrationAccountGenerator.ARtifactType.Maps, "IntegrationAccountDev", "9fake3d-3c94-40e9-b2cb-18921e5d6cfa", "LogicAppsDev", new MockResourceCollector("IntegrationAccountMaps"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }
        private JObject GetXsltTemplate()
        {
            var generator = new IntegrationAccountGenerator("TEST_Create_User", IntegrationAccountGenerator.ARtifactType.Maps, "IntegrationAccountDev", "9fake3d-3c94-40e9-b2cb-18921e5d6cfa", "LogicAppsDev", new MockResourceCollector("IntegrationAccountMaps"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void GenerateTemplate()
        {
            var defintion = GetTemplate();
            Assert.IsNotNull(defintion);
        }
        [TestMethod]
        public void IntegrationJsonMapTest()
        {
            var defintion = GetTemplate();

            Assert.AreEqual("NotificationMap", defintion["parameters"]["name"]["defaultValue"]);
            Assert.AreEqual("[resourceGroup().location]", defintion["parameters"]["integrationAccountLocation"]["defaultValue"]);
            Assert.AreEqual("IntegrationAccountDev", defintion["parameters"]["IntegrationAccountName"]["defaultValue"]);

            var JsonMap = defintion.Value<JArray>("resources").First();
            Assert.AreEqual("[parameters('integrationAccountLocation')]", JsonMap.Value<string>("location"));
            Assert.AreEqual("2016-06-01", JsonMap.Value<string>("apiVersion"));
            Assert.AreEqual("Liquid", JsonMap["properties"].Value<string>("mapType"));
            Assert.AreEqual("text", JsonMap["properties"].Value<string>("contentType"));

        }
        [TestMethod]
        public void IntegrationXsltMapTest()
        {
            var defintion = GetXsltTemplate();

            Assert.AreEqual("TEST_Create_User", defintion["parameters"]["name"]["defaultValue"]);
            Assert.AreEqual("[resourceGroup().location]", defintion["parameters"]["integrationAccountLocation"]["defaultValue"]);
            Assert.AreEqual("IntegrationAccountDev", defintion["parameters"]["IntegrationAccountName"]["defaultValue"]);

            var map = defintion.Value<JArray>("resources").First();
            Assert.AreEqual("[parameters('integrationAccountLocation')]", map.Value<string>("location"));
            Assert.AreEqual("2016-06-01", map.Value<string>("apiVersion"));
            Assert.AreEqual("Xslt", map["properties"].Value<string>("mapType"));
            Assert.IsNotNull(map["properties"].Value<JObject>("parametersSchema"));            
            Assert.AreEqual("text", map["properties"].Value<string>("contentType"));

        }
    }
}
