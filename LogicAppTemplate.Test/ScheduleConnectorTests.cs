using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class ScheduleConnectorTests
    {
        private JObject GetTemplateTrigger(string templateName)
        {
            var generator = new TemplateGenerator(templateName, "9fake3d-3c94-40e9-b2cb-18921e5d6cfa", "LogicAppsDev", new MockResourceCollector("ScheduleConnector"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void RecurrenceHardcodedRequiredOnlyTriggerTest()
        {
            var defintion = GetTemplateTrigger("RecurrenceHardcodedRequiredOnly");
            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();
            var triggers = workflow["properties"]["definition"]["triggers"];
            var trigger = triggers.Value<JObject>("Recurrence");

            var recurrence = trigger.Value<JObject>("recurrence");

            //check parameters
            Assert.AreEqual("Minute", defintion["parameters"]["RecurrenceFrequency"]["defaultValue"]);
            Assert.AreEqual(3, defintion["parameters"]["RecurrenceInterval"]["defaultValue"]);

            //check trigger values
            Assert.AreEqual("[parameters('RecurrenceFrequency')]", recurrence["frequency"]);
            Assert.AreEqual("[parameters('RecurrenceInterval')]", recurrence["interval"]);
        }

        [TestMethod]
        public void RecurrenceHardcodedAllPropertiesTriggerTest()
        {
            var defintion = GetTemplateTrigger("RecurrenceHardcodedAll");
            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();
            var triggers = workflow["properties"]["definition"]["triggers"];
            var trigger = triggers.Value<JObject>("Recurrence");

            var recurrence = trigger.Value<JObject>("recurrence");

            //check parameters
            Assert.AreEqual("Minute", defintion["parameters"]["RecurrenceFrequency"]["defaultValue"]);
            Assert.AreEqual(3, defintion["parameters"]["RecurrenceInterval"]["defaultValue"]);
            Assert.AreEqual(new DateTime(2023, 07, 06, 14, 31, 12, DateTimeKind.Utc), defintion["parameters"]["RecurrenceStartTime"]["defaultValue"]);
            Assert.AreEqual("GMT Standard Time", defintion["parameters"]["RecurrenceTimeZone"]["defaultValue"]);

            //check trigger values
            Assert.AreEqual("[parameters('RecurrenceFrequency')]", recurrence["frequency"]);
            Assert.AreEqual("[parameters('RecurrenceInterval')]", recurrence["interval"]);
            Assert.AreEqual("[parameters('RecurrenceStartTime')]", recurrence["startTime"]);
            Assert.AreEqual("[parameters('RecurrenceTimeZone')]", recurrence["timeZone"]);
        }

        [TestMethod]
        public void RecurrenceParameterizedTriggerTest()
        {
            var defintion = GetTemplateTrigger("RecurrenceParameterized");
            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();
            var triggers = workflow["properties"]["definition"]["triggers"];
            var trigger = triggers.Value<JObject>("Recurrence");

            var recurrence = trigger.Value<JObject>("recurrence");

            //check trigger values
            Assert.AreEqual("@parameters('RecurrenceFrequency')", recurrence["frequency"]);
            Assert.AreEqual("@parameters('RecurrenceInterval')", recurrence["interval"]);
            Assert.AreEqual("@parameters('RecurrenceStartTime')", recurrence["startTime"]);
            Assert.AreEqual("@parameters('RecurrenceTimeZone')", recurrence["timeZone"]);
        }

        [TestMethod]
        public void SlidingWindowParameterizedTriggerTest()
        {
            var defintion = GetTemplateTrigger("SlidingWindowParameterized");
            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();
            var triggers = workflow["properties"]["definition"]["triggers"];
            var trigger = triggers.Value<JObject>("Sliding_Window");

            var recurrence = trigger.Value<JObject>("recurrence");

            //check trigger values
            Assert.AreEqual("@parameters('RecurrenceFrequency')", recurrence["frequency"]);
            Assert.AreEqual("@parameters('RecurrenceInterval')", recurrence["interval"]);
            Assert.AreEqual("@parameters('RecurrenceStartTime')", recurrence["startTime"]);
            Assert.AreEqual("@parameters('RecurrenceTimeZone')", recurrence["timeZone"]);
        }
    }
}
