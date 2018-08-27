using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class TableConnectorTest
    {
        [TestMethod]
        public void TestTableConnector()
        {
            var generator = new TemplateGenerator("addDataToTable", "c107df29-a4af-4bc9-a733-f88f0eaa4296", "blobtest", new MockResourceCollector("TableConnector"));

            var defintion = generator.GenerateTemplate().GetAwaiter().GetResult();

            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('Billogram-ResourceGroup'),'/providers/Microsoft.Web/customApis/Billogram')]", defintion["resources"][0]["properties"]["parameters"]["$connections"]["value"]["Billogram"]["id"]);

            Assert.AreEqual("Microsoft.Web/connections", defintion["resources"][1]["type"]);
            Assert.AreEqual("[parameters('logicAppLocation')]", defintion["resources"][1]["location"]);
            Assert.AreEqual("[parameters('Billogram_name')]", defintion["resources"][1]["name"]);
            //subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/Messaging/providers/Microsoft.Web/customApis/Billogram
            //subscriptions/fakeecb73-d0ff-455d-a2bf-eae0b300696d/providers/Microsoft.Web/locations/westeurope/managedApis/filesystem
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('Billogram-ResourceGroup'),'/providers/Microsoft.Web/customApis/Billogram')]", defintion["resources"][1]["properties"]["api"]["id"]);
        }
    }
}
