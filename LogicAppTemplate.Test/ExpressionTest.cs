using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class ExpressionTest
    {

        private JObject GetFirstTemplate()
        {
            var generator = new TemplateGenerator("INT0012C.Workday.Rehire.Leavers", "c107df29-a4af-4bc9-a733-f88f0eaa4296", "blobtest", new MockResourceCollector("BlobConnector"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void GenerateTemplatesTest()
        {
            var defintion = GetFirstTemplate();
            Assert.IsNotNull(defintion);
        }      
        [TestMethod]
        public void HandleBracketsExpressionTest()
        {
            var defintion = GetFirstTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();            

            var actions = workflow["properties"]["definition"]["actions"];

            var ifstatement = actions.Value<JObject>("Check_mail");

            Assert.AreEqual("If", ifstatement.Value<string>("type"));

            Assert.AreEqual("Succeeded", ifstatement["runAfter"]["Check_new_location_2"][0].Value<string>());

            var expression = ifstatement.Value<JObject>("expression");
            Assert.IsNotNull(expression);

            Assert.AreEqual(1, expression.Children().Count());

            Assert.AreEqual("[[]", expression["and"][0]["equals"][1].Value<string>());

        }
    }
}
