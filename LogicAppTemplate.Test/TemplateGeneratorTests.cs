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
using LogicAppTemplate.Test;

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
            /* LogicAppTemplate.TemplateGenerator generator = new TemplateGenerator(armtoken);
             var result = generator.ConvertWithToken(subscriptionId: "80d4fe69-c95b-4dd2-a938-9250f1c8ab03", resourceGroup: "Foo", logicAppName: "Bar", bearerToken: armtoken).Result;
             Console.WriteLine(result.ToString(Newtonsoft.Json.Formatting.Indented));
             Assert.IsInstanceOfType(result, typeof(JObject));
             Assert.IsNotNull(result);*/
        }


        [TestMethod()]
        public void TestWorkflow()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.WorkflowTest.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual("[resourceGroup().name]", defintion["parameters"]["INT0014-NewHires-ResourceGroup"]["defaultValue"]);
            Assert.AreEqual("[resourceGroup().location]", defintion["parameters"]["logicAppLocation"]["defaultValue"]);
            Assert.AreEqual("INT0014-NewHires-Trigger", defintion["parameters"]["logicAppName"]["defaultValue"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('INT0014-NewHires-ResourceGroup'),'/providers/Microsoft.Logic/workflows/',parameters('INT0014-NewHires-LogicAppName'))]", defintion["resources"][0]["properties"]["definition"]["actions"]["INT0014-NewHires"]["inputs"]["host"]["workflow"]["id"]);
        }

        [TestMethod()]
        public void TestWorkflowOtherResourceGroup()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.WorkflowTestOtherResourcegroup.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.IsNotNull(defintion["parameters"]["INT0014-NewHires-ResourceGroup"]);
            Assert.AreEqual("[resourceGroup().location]", defintion["parameters"]["logicAppLocation"]["defaultValue"]);
            Assert.AreEqual("INT0014-NewHires-Trigger", defintion["parameters"]["logicAppName"]["defaultValue"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('INT0014-NewHires-ResourceGroup'),'/providers/Microsoft.Logic/workflows/',parameters('INT0014-NewHires-LogicAppName'))]", defintion["resources"][0]["properties"]["definition"]["actions"]["INT0014-NewHires"]["inputs"]["host"]["workflow"]["id"]);
        }

        [TestMethod()]
        public void TestAPIM()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.APIM.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();
            //check parameters
            Assert.AreEqual("Api-Default-West-Europe", defintion["parameters"]["apimResourceGroup"]["defaultValue"]);
            Assert.AreEqual("apiminstancename", defintion["parameters"]["apimInstanceName"]["defaultValue"]);
            Assert.AreEqual("58985740990a990dd41e5392", defintion["parameters"]["api_58985740990a990dd41e5392_name"]["defaultValue"]);
            Assert.AreEqual("8266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey"]["defaultValue"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('api_58985740990a990dd41e5392_name'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["subscriptionKey"]);
        }


        [TestMethod()]
        public void TestAPIMMultipleSame()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.APIMMultipleSame.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual("Api-Default-West-Europe", defintion["parameters"]["apimResourceGroup"]["defaultValue"]);
            Assert.AreEqual("apiminstancename", defintion["parameters"]["apimInstanceName"]["defaultValue"]);
            Assert.AreEqual("58985740990a990dd41e5392", defintion["parameters"]["api_58985740990a990dd41e5392_name"]["defaultValue"]);
            Assert.AreEqual("8266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey"]["defaultValue"]);

            //check parameters 2 is null
            Assert.IsNull(defintion["parameters"]["apimResourceGroup2"]);
            Assert.IsNull(defintion["parameters"]["apimInstanceName2"]);
            Assert.IsNull(defintion["parameters"]["apimApiId2"]);
            Assert.IsNull(defintion["parameters"]["apimSubscriptionKey2"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('api_58985740990a990dd41e5392_name'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["subscriptionKey"]);
            //check upload Attachment 2
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('api_58985740990a990dd41e5392_name'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["subscriptionKey"]);
        }


        [TestMethod()]
        public void TestAPIMMultipleDiffrent()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.APIMMultipleDiffrent.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual("Api-Default-West-Europe", defintion["parameters"]["apimResourceGroup"]["defaultValue"]);
            Assert.AreEqual("apiminstancename", defintion["parameters"]["apimInstanceName"]["defaultValue"]);
            Assert.AreEqual("58985740990a990dd41e5392", defintion["parameters"]["api_58985740990a990dd41e5392_name"]["defaultValue"]);
            Assert.AreEqual("8266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey"]["defaultValue"]);

            //check Upload Attachment
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('api_58985740990a990dd41e5392_name'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment"]["inputs"]["subscriptionKey"]);

            //check parameters 2
            Assert.AreEqual("APIintegration", defintion["parameters"]["apimResourceGroup2"]["defaultValue"]);
            Assert.AreEqual("otherapiminstancename", defintion["parameters"]["apimInstanceName2"]["defaultValue"]);
            Assert.AreEqual("78985740990a990dd41e5392", defintion["parameters"]["api_78985740990a990dd41e5392_name"]["defaultValue"]);
            Assert.AreEqual("F266eb865e6c440eb007067773e6890b", defintion["parameters"]["apimSubscriptionKey2"]["defaultValue"]);
            //check upload Attachment 2
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup2'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName2'),'/apis/', parameters('api_78985740990a990dd41e5392_name'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey2')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment2"]["inputs"]["subscriptionKey"]);


            //check parameters 3 is null
            Assert.IsNull(defintion["parameters"]["apimResourceGroup3"]);
            Assert.IsNull(defintion["parameters"]["apimInstanceName3"]);
            Assert.IsNull(defintion["parameters"]["apimApiId3"]);
            Assert.IsNull(defintion["parameters"]["apimSubscriptionKey3"]);

            //check upload Attachment3 should be same as 1
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('api_58985740990a990dd41e5392_name'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment3"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[parameters('apimSubscriptionKey')]", defintion["resources"][0]["properties"]["definition"]["actions"]["UploadAttachment3"]["inputs"]["subscriptionKey"]);
        }
        [TestMethod()]
        public void TestIFStatements()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.complex-logicapp-if.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters

            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('INT002_Create_Actioncode-ResourceGroup'),'/providers/Microsoft.Logic/workflows/',parameters('INT002_Create_Actioncode-LogicAppName'))]", defintion["resources"][0]["properties"]["definition"]["actions"]["Choose_external_procedure"]["actions"]["INT002_Create_Actioncode"]["inputs"]["host"]["workflow"]["id"]);
            //check nested nested action

        }

        [TestMethod()]
        public void TestSwitchStatements()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.complex-logicapp-switch.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual("[", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Switch"]["cases"]["Case"].Value<String>("case"));

            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('api_58778a86990a990f5c794e48_name'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Switch"]["default"]["actions"]["INT002_Create_Actioncode_2"]["inputs"]["api"]["id"]);
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/', parameters('apimResourceGroup'),'/providers/Microsoft.ApiManagement/service/', parameters('apimInstanceName'),'/apis/', parameters('api_58778a86990a990f5c794e48_name'),'')]", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Switch"]["cases"]["Case"]["actions"]["For_each"]["actions"]["INT002_Create_Actioncode"]["inputs"]["api"]["id"]);
            //check nested nested action

        }

        [TestMethod()]
        public void ParameterTestTypeObject()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.parameter-test-object.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            Assert.IsNull(defintion["parameters"]["ShouldNotExist"]);

            Assert.IsNotNull(defintion["parameters"]["paramismanager"]);
            Assert.AreEqual(572, (int)defintion["parameters"]["paramismanager"]["defaultValue"]["0"]);
            Assert.AreEqual(571, (int)defintion["parameters"]["paramismanager"]["defaultValue"]["1"]);
            Assert.AreEqual(572, (int)defintion["parameters"]["paramismanager"]["defaultValue"]["No"]);
            Assert.AreEqual(571, (int)defintion["parameters"]["paramismanager"]["defaultValue"]["Yes"]);
            Assert.AreEqual("object", defintion["parameters"]["paramismanager"]["type"]);

        }

        [TestMethod()]
        public void TestTriggerWithGateway()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.file-test-trigger-gateway.json");

            var generator = new TemplateGenerator("", "fakeb73-d0ff-455d-a2bf-eae0b300696d", "", null);
            var defintion = generator.generateDefinition(JObject.Parse(content), false).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual(defintion["parameters"]["When_a_file_is_createdFrequency"]["defaultValue"], "Minute");
            Assert.AreEqual(defintion["parameters"]["When_a_file_is_createdInterval"]["defaultValue"], 3);
            Assert.AreEqual(defintion["parameters"]["filesystem-1_name"]["defaultValue"], "filesystem-1");


            //check nested nested action
            Assert.AreEqual("[parameters('When_a_file_is_createdFrequency')]", defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["recurrence"]["frequency"]);
            Assert.AreEqual("[parameters('When_a_file_is_createdInterval')]", defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["recurrence"]["interval"]);

            //make sure no depends on is added
            Assert.AreEqual(0, defintion["resources"][0]["dependsOn"].Count());

            //File trigger parameters and base64 handling
            Assert.IsNotNull(defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["metadata"]["[base64(parameters('When_a_file_is_created-folderPath'))]"]);
            Assert.AreEqual("[parameters('When_a_file_is_created-folderPath')]", defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["metadata"]["[base64(parameters('When_a_file_is_created-folderPath'))]"]);
            Assert.AreEqual("[base64(parameters('When_a_file_is_created-folderPath'))]", defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["inputs"]["queries"]["folderId"]);
        }

        [TestMethod()]
        public void TestFileLogicApp()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.file-test-readfolder.json");

            var generator = new TemplateGenerator("", "fakeecb73-d0ff-455d-a2bf-eae0b300696d", "", null);
            var defintion = generator.generateDefinition(JObject.Parse(content), false).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual(defintion["parameters"]["RecurrenceFrequency"]["defaultValue"], "Minute");
            Assert.AreEqual(defintion["parameters"]["RecurrenceInterval"]["defaultValue"], 3);
            Assert.AreEqual(defintion["parameters"]["filesystem-1_name"]["defaultValue"], "filesystem-1");


            //check nested nested action
            Assert.AreEqual("[parameters('RecurrenceFrequency')]", defintion["resources"][0]["properties"]["definition"]["triggers"]["Recurrence"]["recurrence"]["frequency"]);
            Assert.AreEqual("[parameters('RecurrenceInterval')]", defintion["resources"][0]["properties"]["definition"]["triggers"]["Recurrence"]["recurrence"]["interval"]);

            //make sure no depends on is added
            Assert.AreEqual(0, defintion["resources"][0]["dependsOn"].Count());

            //File trigger parameters and base64 handling
            Assert.IsNotNull(defintion["resources"][0]["properties"]["definition"]["actions"]["List_files_in_folder"]["metadata"]["[base64(parameters('List_files_in_folder-folderPath'))]"]);
            Assert.AreEqual("[parameters('List_files_in_folder-folderPath')]", defintion["resources"][0]["properties"]["definition"]["actions"]["List_files_in_folder"]["metadata"]["[base64(parameters('List_files_in_folder-folderPath'))]"]);
            Assert.AreEqual("[concat('/datasets/default/folders/@{encodeURIComponent(encodeURIComponent(', parameters('__apostrophe'), base64(parameters('List_files_in_folder-folderPath')), parameters('__apostrophe'), '))}')]", defintion["resources"][0]["properties"]["definition"]["actions"]["List_files_in_folder"]["inputs"]["path"]);
            //Assert.AreEqual("[base64(parameters('When_a_file_is_created-folderPath'))]", defintion["resources"][0]["properties"]["definition"]["triggers"]["When_a_file_is_created"]["inputs"]["queries"]["folderId"]);
        }

        [TestMethod()]
        public void TestIntegrationAccount()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.IntegrationAccount-FlatFileAndTransform.json");

            var generator = new TemplateGenerator("", "", "", null);

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

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual(defintion["parameters"]["HTTP-URI"]["defaultValue"], "http://google.se");


        }

        [TestMethod()]
        public void TestExtractVariablesNotSet()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.Http-Variable.json");

            var generator = new TemplateGenerator("", "", "", null);
            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            Assert.IsNull(defintion["parameters"]["Initialize_variable-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-Array-Value-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-Boolean-False-Value"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Boolean-False-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-Boolean-False-Expression-Value"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Boolean-False-Expression-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-Boolean-True-Value"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Boolean-True-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-Boolean-True-Expression-Value"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Boolean-True-Expression-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-Float-0.01-Value"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Float-0.01-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-Integer-0-Value"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Integer-0-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-Object-Value-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-String-Null-Code-Value"]);
            Assert.IsNull(defintion["parameters"]["Initialize-String-Null-Code-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-String-Null-Expression-Value"]);
            Assert.IsNull(defintion["parameters"]["Initialize-String-Null-Expression-Value"]);

            Assert.IsNull(defintion["parameters"]["Initialize-Array-NoValue"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Float-NoValue"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Integer-NoValue"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Object-NoValue"]);
            Assert.IsNull(defintion["parameters"]["Initialize-String-NoValue"]);
        }



        [TestMethod()]
        public void TestManagedIdentityNotSet()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.HTTP-basic.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            Assert.IsNull(defintion["resources"][0]["identity"]);
        }
        [TestMethod()]
        public void TestManagedIdentityForced()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.HTTP-basic.json");

            var generator = new TemplateGenerator("", "", "", null);
            generator.ForceManagedIdentity = true;

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            Assert.IsNotNull(defintion["resources"][0]["identity"]);
        }


        [TestMethod()]
        public void TestManagedIdentityFromResource()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.ManagedIdentity.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            Assert.IsNotNull(defintion["resources"][0]["identity"]);
        }

        [TestMethod()]
        public void TestExtractVariablesSet()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.Http-Variable.json");

            var generator = new TemplateGenerator("", "", "", null);
            generator.IncludeInitializeVariable = true;

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters
            Assert.AreEqual(defintion["parameters"]["Initialize_variable-Value"]["defaultValue"], "https://www.nationalbanken.dk/");

            Assert.AreEqual(defintion["parameters"]["Initialize-Array-Value-Value"]["type"], "array");

            Assert.AreEqual(defintion["parameters"]["Initialize-Boolean-False-Value"]["type"], "bool");
            Assert.AreEqual(defintion["parameters"]["Initialize-Boolean-False-Value"]["defaultValue"], false);

            Assert.AreEqual(defintion["parameters"]["Initialize-Boolean-False-Expression-Value"]["type"], "string");
            Assert.AreEqual(defintion["parameters"]["Initialize-Boolean-False-Expression-Value"]["defaultValue"], "@false");

            Assert.AreEqual(defintion["parameters"]["Initialize-Boolean-True-Value"]["type"], "bool");
            Assert.AreEqual(defintion["parameters"]["Initialize-Boolean-True-Value"]["defaultValue"], true);

            Assert.AreEqual(defintion["parameters"]["Initialize-Boolean-True-Expression-Value"]["type"], "string");
            Assert.AreEqual(defintion["parameters"]["Initialize-Boolean-True-Expression-Value"]["defaultValue"], "@true");

            Assert.AreEqual(defintion["parameters"]["Initialize-Float-0.01-Value"]["type"], "string");
            Assert.AreEqual(defintion["parameters"]["Initialize-Float-0.01-Value"]["defaultValue"], "0.01");

            Assert.AreEqual(defintion["parameters"]["Initialize-Integer-0-Value"]["type"], "int");
            Assert.AreEqual(defintion["parameters"]["Initialize-Integer-0-Value"]["defaultValue"], 0);

            Assert.AreEqual(defintion["parameters"]["Initialize-Object-Value-Value"]["type"], "object");

            Assert.AreEqual(defintion["parameters"]["Initialize-String-Null-Code-Value"]["type"], "string");
            Assert.AreEqual(defintion["parameters"]["Initialize-String-Null-Code-Value"]["defaultValue"], "@null");

            Assert.AreEqual(defintion["parameters"]["Initialize-String-Null-Expression-Value"]["type"], "string");
            Assert.AreEqual(defintion["parameters"]["Initialize-String-Null-Expression-Value"]["defaultValue"], "@{null}");

            Assert.IsNull(defintion["parameters"]["Initialize-Array-NoValue"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Float-NoValue"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Integer-NoValue"]);
            Assert.IsNull(defintion["parameters"]["Initialize-Object-NoValue"]);
            Assert.IsNull(defintion["parameters"]["Initialize-String-NoValue"]);
        }

        [TestMethod()]
        public void TestHTTPAuthentication()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.HTTP-Authentication.json");

            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            //check parameters basic auth
            Assert.AreEqual(defintion["parameters"]["HTTP-Password"]["defaultValue"], "bbb");
            Assert.AreEqual(defintion["parameters"]["HTTP-Username"]["defaultValue"], "aa");
            Assert.AreEqual(defintion["parameters"]["HTTP-URI"]["defaultValue"], "http://google.se");
            Assert.AreEqual(defintion["resources"][0]["properties"]["definition"]["actions"]["HTTP"]["inputs"]["uri"], "[parameters('HTTP-URI')]");

            //check parameters certificate auth
            Assert.AreEqual(defintion["parameters"]["HTTP_2-Password"]["defaultValue"], "mypassword");
            Assert.AreEqual(defintion["parameters"]["HTTP_2-Pfx"]["defaultValue"], "pfxcontent");
            Assert.AreEqual(defintion["resources"][0]["properties"]["definition"]["actions"]["HTTP_2"]["inputs"]["uri"], "[parameters('HTTP_2-URI')]");

            //check parameters oauth AAD
            Assert.AreEqual(defintion["parameters"]["HTTP_3-Audience"]["defaultValue"], "myaudience");
            Assert.AreEqual(defintion["parameters"]["HTTP_3-Authority"]["defaultValue"], "https://login.microsoft.com/my");
            Assert.AreEqual(defintion["parameters"]["HTTP_3-ClientId"]["defaultValue"], "myclientid");
            Assert.AreEqual(defintion["parameters"]["HTTP_3-Secret"]["defaultValue"], "mysecret");
            Assert.AreEqual(defintion["parameters"]["HTTP_3-Tenant"]["defaultValue"], "mytenant");
            Assert.AreEqual(defintion["parameters"]["HTTP_3-URI"]["defaultValue"], "http://google.se/w2");
            Assert.AreEqual(defintion["resources"][0]["properties"]["definition"]["actions"]["HTTP_3"]["inputs"]["uri"], "[parameters('HTTP_3-URI')]");

            //check parameters Raw
            Assert.AreEqual(defintion["parameters"]["HTTP_4-Raw"]["defaultValue"], "myauthheader");
            Assert.AreEqual(defintion["resources"][0]["properties"]["definition"]["actions"]["HTTP_4"]["inputs"]["uri"], "[parameters('HTTP_4-URI')]");
        }
        /** Removed due to Changes in version, need to extract new files 
        [TestMethod]
        public void GenerateFileSystemGatewayConnectionTemplate()
        {
            var apiresource = JObject.Parse(GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.ApiSource.filegateway.json"));
            var apiresourceInstance = JObject.Parse(GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.ApiSource.filegatewayInstance.json"));
            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateConnectionTemplate(apiresource, apiresourceInstance, "filesystem", "[concat('/subscriptions/', subscription().subscriptionId, '/providers/Microsoft.Web/locations/', parameters('logicAppLocation'), '/managedApis/filesystem')]", "filesystem_name");

            var template = generator.GetTemplate();
            Assert.AreEqual("windows", template.parameters["filesystem_authType"]["defaultValue"]);
            Assert.AreEqual("Root folder path (examples: \\\\MACHINE\\myShare or C:\\myShare)", template.parameters["filesystem_rootfolder"]["metadata"]["description"]);

            Assert.AreEqual("[parameters('filesystem_rootfolder')]", defintion["properties"]["parameterValues"]["rootfolder"]);
            Assert.AreEqual("[parameters('filesystem_authType')]", defintion["properties"]["parameterValues"]["authType"]);
            Assert.AreEqual("[parameters('filesystem_username')]", defintion["properties"]["parameterValues"]["username"]);
            Assert.AreEqual("[parameters('filesystem_password')]", defintion["properties"]["parameterValues"]["password"]);
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('filesystem_gatewayresourcegroup'),'/providers/Microsoft.Web/connectionGateways/',parameters('filesystem_gatewayname'))]", defintion["properties"]["parameterValues"]["gateway"]["id"]);

            Assert.AreEqual("[parameters('filesystem_name')]", defintion["name"]);
            Assert.AreEqual("[parameters('filesystem_displayName')]", defintion["properties"]["displayName"]);

            Assert.AreEqual("Malos-LogicApp2015", template.parameters["filesystem_gatewayname"]["defaultValue"]);
            Assert.AreEqual("OnPremDataGateway", template.parameters["filesystem_gatewayresourcegroup"]["defaultValue"]);

            Assert.AreEqual("File System", template.parameters["filesystem_displayName"]["defaultValue"]);
            Assert.AreEqual("[concat('/subscriptions/', subscription().subscriptionId, '/providers/Microsoft.Web/locations/', parameters('logicAppLocation'), '/managedApis/filesystem')]", defintion["properties"]["api"]["id"]);

        } **/

        /** Removed due to Changes in version, need to extract new files 
        [TestMethod]
        public void GenerateSQLGatewayConnectionTemplate()
        {
        var apiresource = JObject.Parse(GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.ApiSource.sqlgateway.json"));
        var apiresourceInstance = JObject.Parse(GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.ApiSource.sqlgatewayInstance.json"));
        var generator = new TemplateGenerator("", "", "", null);

        var defintion = generator.generateConnectionTemplate(apiresource, apiresourceInstance, "sql", "[concat('/subscriptions/', subscription().subscriptionId, '/providers/Microsoft.Web/locations/', parameters('logicAppLocation'), '/managedApis/', 'sql')]", "sql_name");

        var template = generator.GetTemplate();
        Assert.AreEqual("windows", template.parameters["sql_authType"]["defaultValue"]);
        Assert.AreEqual("windows", template.parameters["sql_authType"]["allowedValues"][0]);
        Assert.AreEqual("Username credential", template.parameters["sql_username"]["metadata"]["description"]);

        Assert.AreEqual("[parameters('sql_server')]", defintion["properties"]["parameterValues"]["server"]);
        Assert.AreEqual("[parameters('sql_database')]", defintion["properties"]["parameterValues"]["database"]);
        Assert.AreEqual("[parameters('sql_authType')]", defintion["properties"]["parameterValues"]["authType"]);
        Assert.AreEqual("[parameters('sql_username')]", defintion["properties"]["parameterValues"]["username"]);
        Assert.AreEqual("[parameters('sql_password')]", defintion["properties"]["parameterValues"]["password"]);
        Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('sql_gatewayresourcegroup'),'/providers/Microsoft.Web/connectionGateways/',parameters('sql_gatewayname'))]", defintion["properties"]["parameterValues"]["gateway"]["id"]);

        Assert.AreEqual("[parameters('sql_name')]", defintion["name"]);
        Assert.AreEqual("[parameters('sql_displayName')]", defintion["properties"]["displayName"]);

        Assert.AreEqual("SQL server OnPrem", template.parameters["sql_displayName"]["defaultValue"]);
        Assert.AreEqual("[concat('/subscriptions/', subscription().subscriptionId, '/providers/Microsoft.Web/locations/', parameters('logicAppLocation'), '/managedApis/', 'sql')]", defintion["properties"]["api"]["id"]);

        }
        **/

        [TestMethod]
        public void GenerateSQLCloudConnectionTemplate()
        {
            var apiresource = JObject.Parse(GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.ApiSource.sqlcloud.json"));
            var apiresourceInstance = JObject.Parse(GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.ApiSource.sqlcloudInstance.json"));
            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateConnectionTemplate(apiresource, apiresourceInstance, "sql-1", "[concat('/subscriptions/', subscription().subscriptionId, '/providers/Microsoft.Web/locations/', parameters('logicAppLocation'), '/managedApis/', 'sql')]", "sql-1_name");

            var template = generator.GetTemplate();
            Assert.IsNull(template.parameters["sql-1_authType"]);
            Assert.AreEqual("Username credential", template.parameters["sql-1_username"]["metadata"]["description"]);

            Assert.AreEqual("[parameters('sql-1_server')]", defintion["properties"]["parameterValues"]["server"]);
            Assert.AreEqual("[parameters('sql-1_database')]", defintion["properties"]["parameterValues"]["database"]);
            Assert.IsNull(defintion["properties"]["parameterValues"]["authType"]);
            Assert.AreEqual("[parameters('sql-1_username')]", defintion["properties"]["parameterValues"]["username"]);
            Assert.AreEqual("[parameters('sql-1_password')]", defintion["properties"]["parameterValues"]["password"]);
            Assert.IsNull(defintion["properties"]["parameterValues"]["gateway"]);

            Assert.AreEqual("[parameters('sql-1_name')]", defintion["name"]);
            Assert.AreEqual("[parameters('sql-1_displayName')]", defintion["properties"]["displayName"]);

            Assert.AreEqual("SQL Azure", template.parameters["sql-1_displayName"]["defaultValue"]);
            Assert.AreEqual("[concat('/subscriptions/', subscription().subscriptionId, '/providers/Microsoft.Web/locations/', parameters('logicAppLocation'), '/managedApis/', 'sql')]", defintion["properties"]["api"]["id"]);

        }

        [TestMethod]
        public void GenerateBlobConnectionTemplate()
        {
            var apiresource = JObject.Parse(GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.ApiSource.azureblob.json"));
            var apiresourceInstance = JObject.Parse(GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.ApiSource.azureblobinstance.json"));
            var generator = new TemplateGenerator("", "", "", null);

            var defintion = generator.generateConnectionTemplate(apiresource, apiresourceInstance, "azureblob", "[concat('/subscriptions/', subscription().subscriptionId, '/providers/Microsoft.Web/locations/', parameters('logicAppLocation'), '/managedApis/azureblob')]", "azureblob_name");

            var template = generator.GetTemplate();

            Assert.AreEqual("[parameters('azureblob_name')]", defintion["name"]);
            Assert.AreEqual("[parameters('azureblob_displayName')]", defintion["properties"]["displayName"]);

            Assert.AreEqual("[parameters('azureblob_accountName')]", defintion["properties"]["parameterValues"]["accountName"]);
            Assert.AreEqual("[listKeys(resourceId(parameters('azureblob_resourceGroupName'),'Microsoft.Storage/storageAccounts', parameters('azureblob_accountName')), '2018-02-01').keys[0].value]", defintion["properties"]["parameterValues"]["accessKey"]);
            Assert.AreEqual("[concat('/subscriptions/', subscription().subscriptionId, '/providers/Microsoft.Web/locations/', parameters('logicAppLocation'), '/managedApis/azureblob')]", defintion["properties"]["api"]["id"]);
        }

        [TestMethod()]
        public void TestBlobConnectionWithDifferentNameThanConnectionName()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.AzureBlobWithDifferentNameThanConnectionName.json");

            var generator = new TemplateGenerator("lanme", "fakeee-15f5-4c85-bb3e-1e108dc79b00", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content), false).GetAwaiter().GetResult();

            Assert.IsNotNull(defintion["resources"][0]["properties"]["parameters"]["$connections"]["value"]["azureblob_1"]);
        }

        [TestMethod()]
        public void TestBlobAction()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.AzureBlob.json");

            var generator = new TemplateGenerator("lanme", "fakeee-15f5-4c85-bb3e-1e108dc79b00", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content), false).GetAwaiter().GetResult();

            Assert.AreEqual("[parameters('Get_blob_content-path')]", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Get_blob_content"]["metadata"]["[base64(parameters('Get_blob_content-path'))]"]);
            Assert.AreEqual("[concat('/datasets/default/files/@{encodeURIComponent(encodeURIComponent(', parameters('__apostrophe'), base64(parameters('Get_blob_content-path')), parameters('__apostrophe'), '))}/content')]", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Get_blob_content"]["inputs"]["path"]);
            Assert.AreEqual("'", defintion["parameters"]["__apostrophe"]["defaultValue"].Value<string>());

            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/providers/Microsoft.Web/locations/',parameters('logicAppLocation'),'/managedApis/azureblob')]", defintion["resources"][0]["properties"]["parameters"]["$connections"]["value"]["azureblob"]["id"]);
            Assert.AreEqual("[resourceId('Microsoft.Web/connections', parameters('azureblob_name'))]", defintion["resources"][0]["properties"]["parameters"]["$connections"]["value"]["azureblob"]["connectionId"]);
            Assert.AreEqual("[parameters('azureblob_name')]", defintion["resources"][0]["properties"]["parameters"]["$connections"]["value"]["azureblob"]["connectionName"]);
        }

        [TestMethod()]
        public void TestMissingParameter()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.misingparameter.json");

            var generator = new TemplateGenerator("", "FAKEd0d-bcaf-4a6f-b27a-9fe3f331692c", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content), false).GetAwaiter().GetResult();

            //Assert.AreEqual("[parameters('Get_blob_content-path')]", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Get_blob_content"]["metadata"]["[base64(parameters('Get_blob_content-path'))]"]);
            //Assert.AreEqual("/datasets/default/files/@{encodeURIComponent(encodeURIComponent(base64(parameters('Get_blob_content-path'))))}/content", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Get_blob_content"]["inputs"]["path"]);
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('FC_ExtractFilePathInfos-ResourceGroup'),'/providers/Microsoft.Web/sites/',parameters('FC_ExtractFilePathInfos-FunctionApp'),'/functions/',parameters('FC_ExtractFilePathInfos-FunctionName'))]", defintion["resources"][0]["properties"]["definition"]["actions"]["Scope"]["actions"]["FC_ExtractFilePathInfos"]["inputs"]["function"]["id"]);
        }

        [TestMethod()]
        public void TestFileTriggerAndMore()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.file-test-triggerandmore.json");

            var generator = new TemplateGenerator("", "c107df29-a4af-4bc9-a733-f88f0eaa4296", "", null);

            var defintion = generator.generateDefinition(JObject.Parse(content), false).GetAwaiter().GetResult();

            //Assert.AreEqual("[parameters('Get_blob_content-path')]", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Get_blob_content"]["metadata"]["[base64(parameters('Get_blob_content-path'))]"]);
            //Assert.AreEqual("/datasets/default/files/@{encodeURIComponent(encodeURIComponent(base64(parameters('Get_blob_content-path'))))}/content", defintion["resources"][0]["properties"]["definition"]["actions"]["Condition"]["actions"]["Get_blob_content"]["inputs"]["path"]);

        }

        [TestMethod()]
        public void TestCustomConnector()
        {
            var generator = new TemplateGenerator("INT001.Invoice", "89d02439-770d-43f3-9e4a-8b910457a10c", "INT001.Invoice", new MockResourceCollector("CustomConnector"));

            var defintion = generator.GenerateTemplate().GetAwaiter().GetResult();

            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('Billogram-ResourceGroup'),'/providers/Microsoft.Web/customApis/',parameters('Billogram_api'),'')]", defintion["resources"][0]["properties"]["parameters"]["$connections"]["value"]["Billogram"]["id"]);

            Assert.AreEqual("Microsoft.Web/connections", defintion["resources"][1]["type"]);
            Assert.AreEqual("[parameters('logicAppLocation')]", defintion["resources"][1]["location"]);
            Assert.AreEqual("[parameters('Billogram_name')]", defintion["resources"][1]["name"]);
            //subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/Messaging/providers/Microsoft.Web/customApis/Billogram
            //subscriptions/fakeecb73-d0ff-455d-a2bf-eae0b300696d/providers/Microsoft.Web/locations/westeurope/managedApis/filesystem
            Assert.AreEqual("[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('Billogram-ResourceGroup'),'/providers/Microsoft.Web/customApis/',parameters('Billogram_api'),'')]", defintion["resources"][1]["properties"]["api"]["id"]);
        }

        [TestMethod()]
        public void TestDiagnosticSettings()
        {
            // /subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/INT001.Invoice/providers/Microsoft.Logic/workflows/INT001.Invoice/providers/microsoft.insights/diagnosticSettings/service
            var generator = new TemplateGenerator("INT001.Invoice", "89d02439-770d-43f3-9e4a-8b910457a10c", "INT001.Invoice", new MockResourceCollector("DiagnosticSettings"));
            generator.DiagnosticSettings = true;

            var defintion = generator.GenerateTemplate().GetAwaiter().GetResult();

            Assert.AreEqual("providers/diagnosticSettings", defintion["resources"][0]["resources"][0]["type"]);
            Assert.AreEqual("[concat('Microsoft.Insights/', parameters('diagnosticSettings_name'))]", defintion["resources"][0]["resources"][0]["name"]);
            Assert.AreEqual("[concat('/subscriptions/', subscription().subscriptionId, '/resourceGroups/', parameters('diagnosticSettings_resourceGroupName'), '/providers/Microsoft.OperationalInsights/workspaces/', parameters('diagnosticSettings_workspaceName'))]", defintion["resources"][0]["resources"][0]["properties"]["workspaceId"]);
            Assert.AreEqual("service", defintion["parameters"]["diagnosticSettings_name"]["defaultValue"]);
            Assert.AreEqual("twobits", defintion["parameters"]["diagnosticSettings_workspaceName"]["defaultValue"]);
            Assert.AreEqual("shared.infrastructure", defintion["parameters"]["diagnosticSettings_resourceGroupName"]["defaultValue"]);
            Assert.AreEqual("[parameters('logicAppName')]", defintion["resources"][0]["resources"][0]["dependsOn"][0]);

            // Logs
            Assert.AreEqual("[parameters('diagnosticSettings_logsEnabled')]", defintion["resources"][0]["resources"][0]["properties"]["logs"][0]["enabled"]);
            Assert.AreEqual("WorkflowRuntime", defintion["resources"][0]["resources"][0]["properties"]["logs"][0]["category"]);
            Assert.AreEqual(true, defintion["parameters"]["diagnosticSettings_logsEnabled"]["defaultValue"]);
            Assert.AreEqual(true, defintion["parameters"]["diagnosticSettings_logsRetentionPolicyEnabled"]["defaultValue"]);
            Assert.AreEqual(3, defintion["parameters"]["diagnosticSettings_logsRetentionPolicyDays"]["defaultValue"]);

            // Metrics
            Assert.AreEqual("[parameters('diagnosticSettings_metricsEnabled')]", defintion["resources"][0]["resources"][0]["properties"]["metrics"][0]["enabled"]);
            Assert.AreEqual("AllMetrics", defintion["resources"][0]["resources"][0]["properties"]["metrics"][0]["category"]);
            Assert.AreEqual(false, defintion["parameters"]["diagnosticSettings_metricsEnabled"]["defaultValue"]);
            Assert.AreEqual(false, defintion["parameters"]["diagnosticSettings_metricsRetentionPolicyEnabled"]["defaultValue"]);
            Assert.AreEqual(0, defintion["parameters"]["diagnosticSettings_metricsRetentionPolicyDays"]["defaultValue"]);

            Assert.AreEqual("[parameters('logicAppLocation')]", defintion["resources"][0]["location"]);
        }
        [TestMethod()]
        public void TestShouldNotIncludeIseEnvironment()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.WorkflowTest.json");

            var generator = new TemplateGenerator("", "", "", null);

            var definition = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            Assert.IsTrue(definition["parameters"].Children().Any(x => x.ToString().Contains("logicAppLocation")));
            Assert.IsFalse(definition["parameters"].Children().Any(x => x.ToString().Contains("integrationServiceEnvironmentName")));
            Assert.IsFalse(definition["parameters"].Children().Any(x => x.ToString().Contains("integrationServiceEnvironmentResourceGroup")));
            Assert.IsFalse(definition["resources"].First()["properties"].Children().Any(x => x.ToString().Contains("integrationServiceEnvironment")));
        }

        [TestMethod()]
        public void TestShouldIncludeIseEnvironment()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.ISE.json");

            var generator = new TemplateGenerator("", "", "", null);

            var definition = generator.generateDefinition(JObject.Parse(content)).GetAwaiter().GetResult();

            Assert.IsTrue(definition["parameters"].Children().Any(x => x.ToString().Contains("logicAppLocation")));
            Assert.IsTrue(definition["parameters"].Children().Any(x => x.ToString().Contains("integrationServiceEnvironmentName")));
            Assert.IsTrue(definition["parameters"].Children().Any(x => x.ToString().Contains("integrationServiceEnvironmentResourceGroup")));
            Assert.IsTrue(definition["parameters"].Children().Any(x => x.ToString().Contains("Environment_Tag")));
            Assert.IsTrue(definition["resources"].First()["properties"].Children().Any(x => x.ToString().Contains("integrationServiceEnvironment")));
            Assert.AreEqual(@"[concat('/subscriptions/',subscription().subscriptionId,'/resourcegroups/',parameters('integrationServiceEnvironmentResourceGroupName'),'/providers/Microsoft.Logic/integrationServiceEnvironments/',parameters('integrationServiceEnvironmentName'))]", definition["resources"].First()["properties"]["integrationServiceEnvironment"]["id"]);
            Assert.AreEqual(@"Microsoft.Logic/integrationServiceEnvironments", definition["resources"].First()["properties"]["integrationServiceEnvironment"]["type"]);
        }

        [TestMethod()]
        public void TestGenerateHttpTriggerUrlOutput()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.APIM.json");
            var generator = new TemplateGenerator("lanme", "fakeee-15f5-4c85-bb3e-1e108dc79b00", "", null) { GenerateHttpTriggerUrlOutput = true };
            var definition = generator.generateDefinition(JObject.Parse(content), false).GetAwaiter().GetResult();

            Assert.IsNotNull(definition?["outputs"]?["httpTriggerUrl"]);
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
