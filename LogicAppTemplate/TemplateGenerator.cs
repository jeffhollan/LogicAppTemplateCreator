using LogicAppTemplate.Models;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogicAppTemplate
{
    [Cmdlet(VerbsCommon.Get, "LogicAppTemplate", ConfirmImpact = ConfirmImpact.None)]
    public class TemplateGenerator : PSCmdlet
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
            HelpMessage = "The SubscriptionId"
            )]
        public string SubscriptionId;
        [Parameter(
            Mandatory = false,
            HelpMessage = "Name of the Tenant i.e. contoso.onmicrosoft.com"
            )]
        public string TenantName = "";
        [Parameter(
            Mandatory = false,
            HelpMessage = "A Bearer token value"
        )]
        public string Token = "";
        [Parameter(
            Mandatory = false,
            HelpMessage = "Piped input from armclient",
            ValueFromPipeline = true
        )]

        public string ClaimsDump;
        private DeploymentTemplate template;
        private JObject workflowTemplateReference;

        private string LogicAppResourceGroup;


        public TemplateGenerator()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "LogicAppTemplate.Templates.starterTemplate.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                template = JsonConvert.DeserializeObject<DeploymentTemplate>(reader.ReadToEnd());
            }

        }

        public TemplateGenerator(string token) : this()
        {
            Token = token;
        }

        protected override void ProcessRecord()
        {
            if (ClaimsDump == null)
            {
                // WriteVerbose("No armclient token piped through.  Attempting to authenticate");
                if (String.IsNullOrEmpty(Token))
                {
                    string authstring = Constants.AuthString;
                    if (!string.IsNullOrEmpty(TenantName))
                    {
                        authstring = authstring.Replace("common", TenantName);
                    }
                    AuthenticationContext ac = new AuthenticationContext(authstring, true);

                    var ar = ac.AcquireToken(Constants.ResourceUrl, Constants.ClientId, new Uri(Constants.RedirectUrl), PromptBehavior.Auto);
                    Token = ar.AccessToken;
                    // WriteVerbose("Retrieved Token: " + Token);
                }
            }
            else if (ClaimsDump.Contains("Token copied"))
            {
                Token = Clipboard.GetText().Replace("Bearer ", "");
                // WriteVerbose("Got token from armclient: " + Token);
            }
            else
            {
                return;
            }
            var result = ConvertWithToken(SubscriptionId, ResourceGroup, LogicApp, Token).Result;

            WriteObject(result.ToString());
        }


        public async Task<JObject> ConvertWithToken(string subscriptionId, string resourceGroup, string logicAppName, string bearerToken)
        {
            SubscriptionId = subscriptionId;
            ResourceGroup = resourceGroup;
            LogicApp = logicAppName;
            // WriteVerbose("Retrieving Definition....");
            JObject _definition = getDefinition();
            // WriteVerbose("Converting definition to template");
            return await generateDefinition(_definition);
        }

        private JObject getDefinition()
        {

            string url = $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup}/providers/Microsoft.Logic/workflows/{LogicApp}?api-version=2016-06-01";
            // WriteVerbose("Doing a GET to: " + url);
            var request = HttpWebRequest.Create(url);
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + Token;

            var logicAppRequest = request.GetResponse();
            var stream = logicAppRequest.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            var logicApp = reader.ReadToEnd();
            // WriteVerbose("Got definition");
            return JObject.Parse(logicApp);

        }


        public async Task<JObject> generateDefinition(JObject definition)
        {
            Regex rgx = new Regex(@"\/subscriptions\/(?<subscription>[0-9a-zA-Z-]*)\/resourceGroups\/(?<resourcegroup>[a-zA-Z0-9-]*)");
            var matches = rgx.Match(definition.Value<string>("id"));
            LogicAppResourceGroup = matches.Groups["resourcegroup"].Value;

            template.parameters["logicAppName"]["defaultValue"] = definition.Value<string>("name");

            workflowTemplateReference = template.resources.Where(t => ((string)t["type"]) == "Microsoft.Logic/workflows").FirstOrDefault();

            // WriteVerbose("Upgrading connectionId paramters...");
            var modifiedDefinition = definition["properties"]["definition"].ToString().Replace(@"['connectionId']", @"['connectionId']");
            // WriteVerbose("Removing API Host references...");
            //template.parameters["logicAppLocation"]["defaultValue"] = definition["location"];

            workflowTemplateReference["properties"]["definition"] = handleActions(JObject.Parse(modifiedDefinition));



            if (definition["properties"]["integrationAccount"] == null)
            {
                ((JObject)template.resources[0]["properties"]).Remove("integrationAccount");
                template.parameters.Remove("IntegrationAccountName");
                template.parameters.Remove("IntegrationAccountResourceGroupName");
            }else
            {
                template.parameters["IntegrationAccountName"]["defaultValue"] = definition["properties"]["integrationAccount"]["name"];
            }



            JObject connections = (JObject)definition["properties"]["parameters"]["$connections"];

            foreach (JProperty parameter in workflowTemplateReference["properties"]["definition"]["parameters"])
            {
                if (!parameter.Name.StartsWith("$"))
                {
                    var name = "param" + parameter.Name;
                    template.parameters.Add(name, JObject.FromObject(new { type = parameter.Value["type"], defaultValue = parameter.Value["defaultValue"] }));
                    parameter.Value["defaultValue"] = "[parameters('" + name + "')]";
                }
            }



            // WriteVerbose("Checking connections...");
            if (connections == null)
                return JObject.FromObject(template);

            workflowTemplateReference["properties"]["parameters"]["$connections"] = new JObject(new JProperty("value", new JObject()));
            foreach (JProperty connectionProperty in connections["value"])
            {
                // WriteVerbose($"Parameterizing {connectionProperty.Name}");
                string connectionName = connectionProperty.Name;
                var conn = (JObject)connectionProperty.Value;
                var apiId = conn["id"] != null ? conn["id"] :
                            conn["api"]["id"] != null ? conn["api"]["id"] : null;
                if (apiId == null)
                    throw new NullReferenceException($"Connection {connectionName} is missing an id");

                workflowTemplateReference["properties"]["parameters"]["$connections"]["value"][connectionName] = JObject.FromObject(new
                {
                    id = apiIdTemplate((string)apiId),
                    connectionId = $"[resourceId('Microsoft.Web/connections', parameters('{connectionName}'))]"
                });
                ((JArray)workflowTemplateReference["dependsOn"]).Add($"[resourceId('Microsoft.Web/connections', parameters('{connectionName}'))]");

                JObject apiResource = await generateConnectionResource(connectionName, (string)apiId);
                // WriteVerbose($"Generating connection resource for {connectionName}....");
                var connectionTemplate = generateConnectionTemplate(connectionName, apiResource, apiIdTemplate((string)apiId));

                template.resources.Insert(1, connectionTemplate);
                template.parameters.Add(connectionName, JObject.FromObject(new { type = "string", defaultValue = conn["connectionName"] }));
            }


            // WriteVerbose("Finalizing Template...");
            return JObject.FromObject(template);

        }

        private JToken handleActions(JObject definition)
        {
            foreach (JProperty action in definition["actions"])
            {
                var type = action.Value.SelectToken("type").Value<string>().ToLower();
                //if workflow fix so links are dynamic.
                if (type == "workflow")
                {
                    var curr = ((JObject)definition["actions"][action.Name]["inputs"]["host"]["workflow"]).Value<string>("id");

                    Regex rgx = new Regex(@"\/subscriptions\/(?<subscription>[0-9a-zA-Z-]*)\/resourceGroups\/(?<resourcegroup>[a-zA-Z0-9-]*)");
                    var matches = rgx.Match(curr);

                    curr = curr.Replace(matches.Groups["subscription"].Value, "',subscription().subscriptionId,'");

                    if (LogicAppResourceGroup == matches.Groups["resourcegroup"].Value)
                    {
                        curr = curr.Replace(matches.Groups["resourcegroup"].Value, "', resourceGroup().id,'");
                    }
                    else
                    {
                        curr = curr.Replace(matches.Groups["resourcegroup"].Value, "', parameters('" + AddTemplateParameter(action.Name + "-ResourceGroup", "string", matches.Groups["resourcegroup"].Value) + "'),'");
                    }
                    curr = "[concat('" + curr + "')]";

                    definition["actions"][action.Name]["inputs"]["host"]["workflow"]["id"] = curr;
                    //string result = "[concat('" + rgx.Replace(matches.Groups[1].Value, "',subscription().subscriptionId,'") + + "']";
                }
                else if (type == "apimanagement")
                {
                    var apiId = ((JObject)definition["actions"][action.Name]["inputs"]["api"]).Value<string>("id");

                    Regex rgx = new Regex(@"\/subscriptions\/(?<subscription>[0-9a-zA-Z-]*)\/resourceGroups\/(?<resourcegroup>[a-zA-Z0-9-]*).*\/service\/(?<apim>[a-zA-Z0-9]*)\/apis\/(?<apiId>[0-9a-zA-Z]*)");
                    var matches = rgx.Match(apiId);

                    apiId = apiId.Replace(matches.Groups["subscription"].Value, "',subscription().subscriptionId,'");
                    apiId = apiId.Replace(matches.Groups["resourcegroup"].Value, "', parameters('" + AddTemplateParameter("apimLocation", "string", matches.Groups["resourcegroup"].Value) + "'),'");
                    apiId = apiId.Replace(matches.Groups["apim"].Value, "', parameters('" + AddTemplateParameter("apimInstanceName", "string", matches.Groups["apim"].Value) + "'),'");
                    apiId = apiId.Replace(matches.Groups["apiId"].Value, "', parameters('" + AddTemplateParameter("apimApiId", "string", matches.Groups["apiId"].Value) + "'),'");
                    apiId = "[concat('" + apiId + "')]";

                    definition["actions"][action.Name]["inputs"]["api"]["id"] = apiId;

                    //handle subscriptionkey
                    var subkey = ((JObject)definition["actions"][action.Name]["inputs"]).Value<string>("subscriptionKey");
                    definition["actions"][action.Name]["inputs"]["subscriptionKey"] = "[parameters('" + AddTemplateParameter("apimSubscriptionKey", "string", subkey) + "')]";
                }
                else if (type == "if" || type == "scope" || type == "foreach" || type == "until" )
                {
                    definition["actions"][action.Name]["actions"] = handleActions(definition["actions"][action.Name].ToObject<JObject>());
                }
                else
                {
                    var api = action.Value.SelectToken("inputs.host.api");
                    if (api != null)
                        ((JObject)definition["actions"][action.Name]["inputs"]["host"]).Remove("api");
                    //get the type:
                }
            }

            //when in if statements triggers is not there
            if (definition["triggers"] != null)
            {
                foreach (JProperty trigger in definition["triggers"])
                {
                    var api = trigger.Value.SelectToken("inputs.host.api");
                    if (api != null)
                        ((JObject)definition["triggers"][trigger.Name]["inputs"]["host"]).Remove("api");
                }
            }

            return definition;
        }

        private string AddTemplateParameter(string paramname,string type, string defaultvalue)
        {
            string realParameterName = paramname;
            JObject param = new JObject();
            param.Add("type", JToken.FromObject(type));
            param.Add("defaultValue", JToken.FromObject(defaultvalue));

            if (template.parameters[paramname] == null)
            {
                template.parameters.Add(paramname, param);
            }
            else
            {
                if (template.parameters[paramname].Value<string>("defaultValue") != defaultvalue)
                {
                    foreach (var p in template.parameters)
                    {
                        if (p.Key.StartsWith(paramname))
                        {
                            for (int i = 2; i < 100; i++)
                            {
                                realParameterName = paramname + i.ToString();
                                if (template.parameters[realParameterName] == null)
                                {
                                    template.parameters.Add(realParameterName, param);
                                    return realParameterName;
                                }
                            }
                        }
                    }
                }
            }
            return realParameterName;
        }

        private string apiIdTemplate(string apiId)
        {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"\/locations\/(.*)\/managedApis");
            apiId = r.Replace(apiId, @"/locations/', parameters('logicAppLocation'), '/managedApis");
            r = new System.Text.RegularExpressions.Regex(@"(.*)\/providers");
            apiId = r.Replace(apiId, @"subscription().id,'/providers");
            return apiId.Insert(0, "[concat(") + "')]";
        }

        private async Task<JObject> generateConnectionResource(string connectionName, string apiId)
        {

            string url = "https://management.azure.com" + apiId + "?api-version=2016-06-01";
            // WriteVerbose("Doing a GET to: " + url);
            var request = HttpWebRequest.Create(url);
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + Token;

            var logicAppRequest = request.GetResponse();
            var stream = logicAppRequest.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            var apiResource = reader.ReadToEnd();
            // WriteVerbose("Got api");
            return JObject.Parse(apiResource);

        }

        private JObject generateConnectionTemplate(string connectionName, JObject connectionResource, string apiId)
        {
            var connectionTemplate = new Models.ConnectionTemplate(connectionName, apiId);
            JObject connectionParameters = new JObject();
            foreach (JProperty parameter in connectionResource["properties"]["connectionParameters"])
            {
                if ((string)(parameter.Value)["type"] != "oauthSetting")
                {
                    //Filter out gateway stuff - can't export to template yet
                    //TODO
                    if ((string)parameter.Value["type"] == "gatewaySetting")
                        continue;

                    if (((JArray)parameter.Value["uiDefinition"]["constraints"]["capability"]) != null &&
                        ((JArray)parameter.Value["uiDefinition"]["constraints"]["capability"]).Count == 1
                        && (string)((JArray)parameter.Value["uiDefinition"]["constraints"]["capability"])[0] == "gateway")
                        continue;

                    connectionParameters.Add(parameter.Name, $"[parameters('{connectionName + parameter.Name}')]");
                    template.parameters.Add(connectionName + parameter.Name, JObject.FromObject(new { type = parameter.Value["type"] }));
                    //If has an enum
                    if (parameter.Value["allowedValues"] != null)
                    {
                        template.parameters[connectionName + parameter.Name]["allowedValues"] = parameter.Value["allowedValues"];
                    }
                    //If an optional parameter
                    if ((bool)(parameter.Value)["uiDefinition"]["constraints"]["required"] == false)
                    {
                        template.parameters[connectionName + parameter.Name]["defaultValue"] = "";
                    }
                }
            }
            connectionTemplate.properties.parameterValues = connectionParameters;
            return JObject.FromObject(connectionTemplate);
        }


    }
}
