using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogicAppTemplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using LogicAppTemplate.Models;

namespace LogicAppTemplate.Tests
{
    
    [TestClass()]
    public class TemplateGeneratorTests
    {
        private const string armtoken = "";
        [TestMethod()]
        public void generateDefinitionTest()
        {
            
        }

        [TestMethod()]
        public void ConvertWithTokenTest()
        {
            LogicAppTemplate.TemplateGenerator generator = new TemplateGenerator(armtoken);
            var result = generator.ConvertWithToken(subscriptionId: "80d4fe69-c95b-4dd2-a938-9250f1c8ab03", resourceGroup: "Foo", logicAppName: "Bar", bearerToken: armtoken).Result;
            Console.WriteLine(result.ToString(Newtonsoft.Json.Formatting.Indented));
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.IsNotNull(result);
        }


        [TestMethod()]
        public void TestWorkflow()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.WorkflowTest.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual("resourcegroupname", defintion["parameters"]["INT0014-NewHires-ResourceGroup"]["defaultValue"]);            

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('INT0014-NewHires-ResourceGroup'),'/providers/Microsoft.Logic/workflows/INT0014-NewHires')]", defintion["resources"][0]["properties"]["definition"]["actions"]["INT0014-NewHires"]["inputs"]["host"]["workflow"]["id"]);
        }

        [TestMethod()]
        public void TestAPIM()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.APIM.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();
            //check parameters
            Assert.AreEqual("Api-Default-West-Europe", defintion["parameters"]["apimLocation"]["defaultValue"]);
            Assert.AreEqual("apiminstancename", defintion["parameters"]["apimInstanceName"]["defaultValue"]);
            Assert.AreEqual("58985740990a990dd41e5392", defintion["parameters"]["apimApiId"]["defaultValue"]);
            Assert.AreEqual("8266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey"]["defaultValue"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimLocation'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["subscriptionKey"]);
        }


        [TestMethod()]
        public void TestAPIMMultipleSame()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.APIMMultipleSame.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual("Api-Default-West-Europe", defintion["parameters"]["apimLocation"]["defaultValue"]);
            Assert.AreEqual("apiminstancename", defintion["parameters"]["apimInstanceName"]["defaultValue"]);
            Assert.AreEqual("58985740990a990dd41e5392", defintion["parameters"]["apimApiId"]["defaultValue"]);
            Assert.AreEqual("8266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey"]["defaultValue"]);

            //check parameters 2 is null
            Assert.IsNull(defintion["parameters"]["apimLocation2"]);
            Assert.IsNull(defintion["parameters"]["apimInstanceName2"]);
            Assert.IsNull(defintion["parameters"]["apimApiId2"]);
            Assert.IsNull(defintion["parameters"]["apimSubscriptionKey2"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimLocation'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["subscriptionKey"]);
            //check upload Attachment 2
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimLocation'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["subscriptionKey"]);
        }


        [TestMethod()]
        public void TestAPIMMultipleDiffrent()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.APIMMultipleDiffrent.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual("Api-Default-West-Europe", defintion["parameters"]["apimLocation"]["defaultValue"]);
            Assert.AreEqual("apiminstancename", defintion["parameters"]["apimInstanceName"]["defaultValue"]);
            Assert.AreEqual("58985740990a990dd41e5392", defintion["parameters"]["apimApiId"]["defaultValue"]);
            Assert.AreEqual("8266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey"]["defaultValue"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimLocation'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["subscriptionKey"]);

            //check parameters 2
            Assert.AreEqual("APIintegration", defintion["parameters"]["apimLocation2"]["defaultValue"]);
            Assert.AreEqual("otherapiminstancename", defintion["parameters"]["apimInstanceName2"]["defaultValue"]);
            Assert.AreEqual("78985740990a990dd41e5392", defintion["parameters"]["apimApiId2"]["defaultValue"]);
            Assert.AreEqual("F266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey2"]["defaultValue"]);
            //check upload Attachment 2
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimLocation2'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName2'),'/apis/', parameters('apimApiId2'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey2')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["subscriptionKey"]);


            //check parameters 3 is null
            Assert.IsNull(defintion["parameters"]["apimLocation3"]);
            Assert.IsNull(defintion["parameters"]["apimInstanceName3"]);
            Assert.IsNull(defintion["parameters"]["apimApiId3"]);
            Assert.IsNull(defintion["parameters"]["apimSubscriptionKey3"]);
            
            //check upload Attachment3 should be same as 1
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimLocation'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment3"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment3"]["inputs"]["subscriptionKey"]);
        }
        [TestMethod()]
        public void TestIFStatements()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.complex-logicapp-if.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            //Assert.AreEqual("resourcegroupname", defintion["parameters"]["INT0014-NewHires-ResourceGroup"]["defaultValue"]);

            //check Upload Attachment
            //Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('INT0014-NewHires-ResourceGroup'),'/providers/Microsoft.Logic/workflows/INT0014-NewHires')]", defintion["resources"][0]["properties"]["definition"]["actions"]["INT0014-NewHires"]["inputs"]["host"]["workflow"]["id"]);
        }


        //var resourceName = "LogicAppTemplate.Templates.starterTemplate.json";
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