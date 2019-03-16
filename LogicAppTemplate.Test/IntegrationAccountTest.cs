using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using System.IO;

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
            Assert.AreEqual("Microsoft.Logic/integrationAccounts/maps", JsonMap.Value<string>("type"));
            Assert.AreEqual("Liquid", JsonMap["properties"].Value<string>("mapType"));
            Assert.AreEqual("text/plain", JsonMap["properties"].Value<string>("contentType"));

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
            Assert.AreEqual("Microsoft.Logic/integrationAccounts/maps", map.Value<string>("type"));
            Assert.AreEqual("Xslt", map["properties"].Value<string>("mapType"));
            Assert.IsNotNull(map["properties"].Value<JObject>("parametersSchema"));            
            Assert.AreEqual("application/xml", map["properties"].Value<string>("contentType"));

        }

        [TestMethod]
        public void ShouldGenerateSchemaTemplate()
        {         

            var iaDefinition = JObject.Parse(GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.Samples.IntegrationAccountSchemas.SampleDefinition.json"));
            var rawSchema = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.Samples.IntegrationAccountSchemas.SampleMap.xsd");
            IntegrationAccountGenerator generator = new IntegrationAccountGenerator("ArtifactName",IntegrationAccountGenerator.ARtifactType.Schemas, "IntegrationAccountName", "FakeSubscriptionId", "FakeResourceGroup", new AzureResourceCollector());
            JObject generatedObject = generator.GenerateSchemaDefinition(iaDefinition, rawSchema).GetAwaiter().GetResult();
            Assert.IsNotNull(generatedObject);
            Assert.AreEqual("Microsoft.Logic/integrationAccounts/schemas", generatedObject["resources"].First.Value<string>("type"));
            Assert.AreEqual("Xml", generatedObject["resources"].First["properties"].Value<string>("schemaType"));
          
        }

        private static string GetEmbededFileContent(string resourceName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

    }
}
