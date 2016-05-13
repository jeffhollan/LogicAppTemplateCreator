using ConverterLibrary.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ConverterLibrary
{
    [Cmdlet(VerbsCommon.Get, "LogicAppTemplate", ConfirmImpact = ConfirmImpact.None)]
    public class Converter : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            HelpMessage = "Name of the Logic App"
            )]
        public string LogicApp;
        [Parameter(
            Mandatory = true,
            HelpMessage = "Name of the Resource Group"
            )]
        public string ResourceGroup;
        [Parameter(
            Mandatory = true,
            HelpMessage = "Azure Subscription Id"
            )]
        public string SubscriptionId;
        [Parameter(
            Mandatory = false,
            HelpMessage = "Authorization Bearer Token",
            ValueFromPipeline = true
            )]
        public string Token = "";
        private DeploymentTemplate template;
        private JObject workflowTemplateReference;


        public Converter()
        {       
                template = JsonConvert.DeserializeObject<DeploymentTemplate>(Constants.deploymentTemplate);
        }

        public Converter(string token) : this()
        {
            Token = token;
        }

        protected override void ProcessRecord()
        {
            if (String.IsNullOrEmpty(Token))
            {
                AuthenticationContext ac = new AuthenticationContext(Constants.AuthString, true);
                var ar = ac.AcquireToken(Constants.ResourceUrl, Constants.ClientId, new Uri(Constants.RedirectUrl), PromptBehavior.Always);


                Token = ar.AccessToken;

                WriteVerbose("Retrieved Token: " + Token);
            }
            var result = ConvertWithToken(SubscriptionId, ResourceGroup, LogicApp, Token).Result;
            WriteObject(result.ToString());
        }

        
        public async Task<JObject> ConvertWithToken(string subscriptionId, string resourceGroup, string logicAppName, string bearerToken)
        {
            SubscriptionId = subscriptionId;
            ResourceGroup = resourceGroup;
            LogicApp = logicAppName;
            WriteVerbose("Retrieving Definition....");
            JObject _definition = await getDefinition();
            WriteVerbose("Converting definition to template");
            return await convertTemplate(_definition);
        }

        private async Task<JObject> getDefinition()
        {

                string url = $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup}/providers/Microsoft.Logic/workflows/{LogicApp}?api-version=2015-08-01-preview";
                WriteVerbose("Doing a GET to: " + url);
                var request = HttpWebRequest.Create(url);
                 request.Headers[HttpRequestHeader.Authorization] = "Bearer " + Token;

                var logicAppRequest = request.GetResponse();
                 var stream = logicAppRequest.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                var logicApp = reader.ReadToEnd();
                WriteVerbose("Got definition");
                return JObject.Parse(logicApp);
            
        }

        private async Task<JObject> convertTemplate(JObject definition)
        {
            
            return  await createConnections(definition);
        }


        public async Task<JObject> createConnections(JObject definition)
        {
            workflowTemplateReference = template.resources.Where(t => ((string)t["type"]) == "Microsoft.Logic/workflows").FirstOrDefault();

            WriteVerbose("Upgrading connectionId paramters...");
            var modifiedDefinition = definition["properties"]["definition"].ToString().Replace(@"['connectionId']", @"['connectionId']");
            WriteVerbose("Removing API Host references...");

            workflowTemplateReference["properties"]["definition"] = removeApiFromActions(JObject.Parse(modifiedDefinition));

            JObject connections = (JObject)definition["properties"]["parameters"]["$connections"];

            WriteVerbose("Checking connections...");
            if (connections == null)
                return JObject.FromObject(template);

            workflowTemplateReference["properties"]["parameters"]["$connections"] = new JObject(new JProperty("value", new JObject()));
            foreach (JProperty connectionProperty in connections["value"])
            {
                WriteVerbose($"Parameterizing {connectionProperty.Name}");
                string connectionName = connectionProperty.Name;
                var conn = (JObject)connectionProperty.Value;
                var apiId = conn["id"] != null ? conn["id"] :
                            conn["api"]["id"] != null ? conn["api"]["id"] : null;
                if (apiId == null)
                    throw new NullReferenceException($"Connection {connectionName} is missing an id");

                 workflowTemplateReference["properties"]["parameters"]["$connections"]["value"][connectionName] = JObject.FromObject(new {
                    id =  apiIdTemplate((string)apiId),
                    connectionId = $"[resourceId('Microsoft.Web/connections', parameters('{connectionName}Name'))]" 
                });
                ((JArray)workflowTemplateReference["dependsOn"]).Add($"[resourceId('Microsoft.Web/connections', parameters('{connectionName}Name'))]");


                JObject apiResource = await generateConnectionResource(connectionName, (string)apiId);
                WriteVerbose($"Generating connection resource for {connectionName}....");
                var connectionTemplate = generateConnectionTemplate(connectionName, apiResource, apiIdTemplate((string)apiId));
                
                template.resources.Insert(1, connectionTemplate);
                template.parameters.Add(connectionName + "Name", JObject.FromObject(new { type = "string" }));
                

            }
            WriteVerbose("Finalizing Template...");
            return JObject.FromObject(template);

        }

        private JToken removeApiFromActions(JObject definition)
        {
            foreach(JProperty action in definition["actions"])
            {
                var api = action.Value.SelectToken("inputs.host.api");
                if (api != null)
                    ((JObject)definition["actions"][action.Name]["inputs"]["host"]).Remove("api");
            }

            foreach (JProperty trigger in definition["triggers"])
            {
                var api = trigger.Value.SelectToken("inputs.host.api");
                if (api != null)
                    ((JObject)definition["triggers"][trigger.Name]["inputs"]["host"]).Remove("api");
            }
            return definition;
        }

        private string apiIdTemplate(string apiId)
        {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"\/locations\/(.*)\/managedApis");
            apiId = r.Replace(apiId, @"/locations/', resourceGroup().location, '/managedApis");
            return apiId.Insert(0, "[concat('") + "')]";
        }

        private async Task<JObject> generateConnectionResource(string connectionName, string apiId)
        {

            string url = "https://management.azure.com" + apiId + "?api-version=2015-08-01-preview";
            WriteVerbose("Doing a GET to: " + url);
            var request = HttpWebRequest.Create(url);
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + Token;

            var logicAppRequest = request.GetResponse();
            var stream = logicAppRequest.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            var apiResource = reader.ReadToEnd();
            WriteVerbose("Got api");
            return JObject.Parse(apiResource);

        }

        private JObject generateConnectionTemplate(string connectionName, JObject connectionResource, string apiId)
        {
            var connectionTemplate = new Models.ConnectionTemplate(connectionName, apiId);
            JObject connectionParameters = new JObject();
            foreach(JProperty parameter in connectionResource["properties"]["connectionParameters"])
            {
                if ((string)(parameter.Value)["type"] != "oauthSetting")
                {
                    connectionParameters.Add(parameter.Name, $"[parameters('{connectionName + parameter.Name}')]");
                    template.parameters.Add(connectionName + parameter.Name, JObject.FromObject(new { type = parameter.Value["type"] }));
                }
            }
            connectionTemplate.properties.parameterValues = connectionParameters;
            return JObject.FromObject(connectionTemplate);
        }
    }
}
