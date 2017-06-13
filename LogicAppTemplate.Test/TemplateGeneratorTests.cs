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
            Assert.IsNull(defintion["parameters"]["INT0014-NewHires-ResourceGroup"]);
            Assert.AreEqual("[resourceGroup().location]", defintion["parameters"]["logicAppLocation"]["defaultValue"]);
            Assert.AreEqual("INT0014-NewHires-Trigger", defintion["parameters"]["logicAppName"]["defaultValue"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', resourceGroup().name,'/providers/Microsoft.Logic/workflows/INT0014-NewHires')]", defintion["resources"][0]["properties"]["definition"]["actions"]["INT0014-NewHires"]["inputs"]["host"]["workflow"]["id"]);
        }

        [TestMethod()]
        public void TestWorkflowOtherResourceGroup()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.WorkflowTestOtherResourcegroup.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.IsNotNull(defintion["parameters"]["INT0014-NewHires-ResourceGroup"]);
            Assert.AreEqual("[resourceGroup().location]", defintion["parameters"]["logicAppLocation"]["defaultValue"]);
            Assert.AreEqual("INT0014-NewHires-Trigger", defintion["parameters"]["logicAppName"]["defaultValue"]);

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
            Assert.AreEqual("Api-Default-West-Europe", defintion["parameters"]["apimResourceGroup"]["defaultValue"]);
            Assert.AreEqual("apiminstancename", defintion["parameters"]["apimInstanceName"]["defaultValue"]);
            Assert.AreEqual("58985740990a990dd41e5392", defintion["parameters"]["apimApiId"]["defaultValue"]);
            Assert.AreEqual("8266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey"]["defaultValue"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["subscriptionKey"]);
        }


        [TestMethod()]
        public void TestAPIMMultipleSame()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.APIMMultipleSame.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual("Api-Default-West-Europe", defintion["parameters"]["apimResourceGroup"]["defaultValue"]);
            Assert.AreEqual("apiminstancename", defintion["parameters"]["apimInstanceName"]["defaultValue"]);
            Assert.AreEqual("58985740990a990dd41e5392", defintion["parameters"]["apimApiId"]["defaultValue"]);
            Assert.AreEqual("8266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey"]["defaultValue"]);

            //check parameters 2 is null
            Assert.IsNull(defintion["parameters"]["apimResourceGroup2"]);
            Assert.IsNull(defintion["parameters"]["apimInstanceName2"]);
            Assert.IsNull(defintion["parameters"]["apimApiId2"]);
            Assert.IsNull(defintion["parameters"]["apimSubscriptionKey2"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["subscriptionKey"]);
            //check upload Attachment 2
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["subscriptionKey"]);
        }


        [TestMethod()]
        public void TestAPIMMultipleDiffrent()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.APIMMultipleDiffrent.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual("Api-Default-West-Europe", defintion["parameters"]["apimResourceGroup"]["defaultValue"]);
            Assert.AreEqual("apiminstancename", defintion["parameters"]["apimInstanceName"]["defaultValue"]);
            Assert.AreEqual("58985740990a990dd41e5392", defintion["parameters"]["apimApiId"]["defaultValue"]);
            Assert.AreEqual("8266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey"]["defaultValue"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["subscriptionKey"]);

            //check parameters 2
            Assert.AreEqual("APIintegration", defintion["parameters"]["apimResourceGroup2"]["defaultValue"]);
            Assert.AreEqual("otherapiminstancename", defintion["parameters"]["apimInstanceName2"]["defaultValue"]);
            Assert.AreEqual("78985740990a990dd41e5392", defintion["parameters"]["apimApiId2"]["defaultValue"]);
            Assert.AreEqual("F266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey2"]["defaultValue"]);
            //check upload Attachment 2
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup2'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName2'),'/apis/', parameters('apimApiId2'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey2')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["subscriptionKey"]);


            //check parameters 3 is null
            Assert.IsNull(defintion["parameters"]["apimResourceGroup3"]);
            Assert.IsNull(defintion["parameters"]["apimInstanceName3"]);
            Assert.IsNull(defintion["parameters"]["apimApiId3"]);
            Assert.IsNull(defintion["parameters"]["apimSubscriptionKey3"]);
            
            //check upload Attachment3 should be same as 1
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment3"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment3"]["inputs"]["subscriptionKey"]);
        }
        [TestMethod()]
        public void TestIFStatements()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.complex-logicapp-if.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters

            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', resourceGroup().name,'/providers/Microsoft.Logic/workflows/INT002_Create_Actioncode')]", defintion["resources"][0]["properties"]["definition"]["actions"]["Choose_external_procedure"]["actions"]["INT002_Create_Actioncode"]["inputs"]["host"]["workflow"]["id"]);
            //check nested nested action

        }

        [TestMethod()]
        public void TestSwitchStatements()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.complex-logicapp-switch.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters

            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Switch"]["default"]["actions"]["INT002_Create_Actioncode_2"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('apimApiId'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Switch"]["cases"]["Case"]["actions"]["For_each"]["actions"]["INT002_Create_Actioncode"]["inputs"]["api"]["id"]);
            //check nested nested action

        }

        [TestMethod()]
        public void ParameterTestTypeObject()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.parameter-test-object.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            Assert.IsNull(defintion["parameters"]["ShouldNotExist"]);

            Assert.IsNotNull(defintion["parameters"]["paramismanager"]);
            Assert.AreEqual(572, (int)defintion["parameters"]["paramismanager"]["defaultValue"]["0"]);
            Assert.AreEqual(571, (int)defintion["parameters"]["paramismanager"]["defaultValue"]["1"]);
            Assert.AreEqual(572, (int)defintion["parameters"]["paramismanager"]["defaultValue"]["No"]);
            Assert.AreEqual(571, (int)defintion["parameters"]["paramismanager"]["defaultValue"]["Yes"]);
            Assert.AreEqual("Object", defintion["parameters"]["paramismanager"]["type"]);           

        }

        [TestMethod()]
        public void TestTriggerWithGateway()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.file-test-trigger-gateway.json");

            var generator = new TemplateGenerator();
            var defintion = generator.generateDefinition(JObject.Parse(content),false).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual(defintion["parameters"]["When_a_file_is_createdFrequency"]["defaultValue"],"Minute");
            Assert.AreEqual(defintion["parameters"]["When_a_file_is_createdInterval"]["defaultValue"], 3);
            Assert.AreEqual(defintion["parameters"]["filesystem_1"]["defaultValue"], "filesystem-1");


            //check nested nested action
            Assert.AreEqual("[parameters('When_a_file_is_createdFrequency')]", defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["recurrence"]["frequency"]);
            Assert.AreEqual("[parameters('When_a_file_is_createdInterval')]", defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["recurrence"]["interval"]);

            //make sure no depends on is added
            Assert.AreEqual(0,defintion["resources"][0]["dependsOn"].Count());

            //File trigger parameters and base64 handling
            Assert.IsNotNull(defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["metadata"]["[base64(parameters('When_a_file_is_created-folderPath'))]"]);
            Assert.AreEqual("[parameters('When_a_file_is_created-folderPath')]", defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["metadata"]["[base64(parameters('When_a_file_is_created-folderPath'))]"]);
            Assert.AreEqual("[base64(parameters('When_a_file_is_created-folderPath'))]", defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["inputs"]["queries"]["folderId"]);
        }

        [TestMethod()]
        public void TestFileLogicApp()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.file-test-readfolder.json");

            var generator = new TemplateGenerator();
            var defintion = generator.generateDefinition(JObject.Parse(content), false).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual(defintion["parameters"]["RecurrenceFrequency"]["defaultValue"], "Minute");
            Assert.AreEqual(defintion["parameters"]["RecurrenceInterval"]["defaultValue"], 3);
            Assert.AreEqual(defintion["parameters"]["filesystem_1"]["defaultValue"], "filesystem-1");


            //check nested nested action
            Assert.AreEqual("[parameters('RecurrenceFrequency')]", defintion["resources"][0]["properties"]["definition"]["triggers"]["Recurrence"]["recurrence"]["frequency"]);
            Assert.AreEqual("[parameters('RecurrenceInterval')]", defintion["resources"][0]["properties"]["definition"]["triggers"]["Recurrence"]["recurrence"]["interval"]);

            //make sure no depends on is added
            Assert.AreEqual(0, defintion["resources"][0]["dependsOn"].Count());

            //File trigger parameters and base64 handling
            Assert.IsNotNull(defintion["resources"][0]["properties"]["definition"]["actions"]["List_files_in_folder"]["metadata"]["[base64(parameters('List_files_in_folder-folderPath'))]"]);
            Assert.AreEqual("[parameters('List_files_in_folder-folderPath')]", defintion["resources"][0]["properties"]["definition"]["actions"]["List_files_in_folder"]["metadata"]["[base64(parameters('List_files_in_folder-folderPath'))]"]);
            //Assert.AreEqual("[base64(parameters('When_a_file_is_created-folderPath'))]", defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["inputs"]["queries"]["folderId"]);
        }

        [TestMethod()]
        public void TestIntegrationAccount()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.IntegrationAccount-FlatFileAndTransform.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual(defintion["parameters"]["IntegrationAccountName"]["defaultValue"], "ia");
            Assert.AreEqual(defintion["parameters"]["IntegrationAccountResourceGroupName"]["defaultValue"], "[resourceGroup().name]");
            Assert.AreEqual(defintion["parameters"]["Flat_File_Decoding-SchemaName"]["defaultValue"], "TEST-INT0021.Scorpio.DailyStatistics");
            Assert.AreEqual(defintion["parameters"]["Flat_File_Encoding-SchemaName"]["defaultValue"], "TEST-INT0021.Intime.DailyStatistics");
            Assert.AreEqual(defintion["parameters"]["Transform_XML-MapName"]["defaultValue"], "TEST-INT0021.DailyStatistics.Scorpio.To.Intime");


            //check nested nested action

        }


        [TestMethod()]
        public void TestHTTP()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.HTTP-basic.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual(defintion["parameters"]["HTTP-URI"]["defaultValue"], "http://google.se");


        }

        [TestMethod()]
        public void TestHTTPAuthentication()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.HTTP-Authentication.json");

            var generator = new TemplateGenerator();

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters basic auth
            Assert.AreEqual(defintion["parameters"]["HTTP-Password"]["defaultValue"], "bbb");
            Assert.AreEqual(defintion["parameters"]["HTTP-Username"]["defaultValue"], "aa");

            //check parameters certificate auth
            Assert.AreEqual(defintion["parameters"]["HTTP_2-Password"]["defaultValue"], "mypassword");
            Assert.AreEqual(defintion["parameters"]["HTTP_2-Pfx"]["defaultValue"], "pfxcontent");

            //check parameters oauth AAD
            Assert.AreEqual(defintion["parameters"]["HTTP_3-Audience"]["defaultValue"], "myaudience");
            Assert.AreEqual(defintion["parameters"]["HTTP_3-Authority"]["defaultValue"], "https://login.microsoft.com/my");
            Assert.AreEqual(defintion["parameters"]["HTTP_3-ClientId"]["defaultValue"], "myclientid");
            Assert.AreEqual(defintion["parameters"]["HTTP_3-Secret"]["defaultValue"], "mysecret");
            Assert.AreEqual(defintion["parameters"]["HTTP_3-Tenant"]["defaultValue"], "mytenant");

            //check parameters Raw
            Assert.AreEqual(defintion["parameters"]["HTTP_4-Raw"]["defaultValue"], "myauthheader");            
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