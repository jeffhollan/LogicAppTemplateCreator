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
            //WriteInformation("Starting up instance",new string[0]);

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

                    var ar = ac.AcquireTokenAsync(Constants.ResourceUrl, Constants.ClientId, new Uri(Constants.RedirectUrl), new PlatformParameters(PromptBehavior.Auto)).GetAwaiter().GetResult();
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

        public async Task<JObject> generateDefinition(JObject definition, bool generateConnection = true)
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

            workflowTemplateReference["properties"]["definition"] = handleActions(JObject.Parse(modifiedDefinition), (JObject)definition["properties"]["parameters"]);



            if (definition["properties"]["integrationAccount"] == null)
            {
                ((JObject)template.resources[0]["properties"]).Remove("integrationAccount");
                template.parameters.Remove("IntegrationAccountName");
                template.parameters.Remove("IntegrationAccountResourceGroupName");
            }
            else
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
                string connectionNameProperty = (string)conn["connectionName"] ?? ((string)conn["connectionId"]).Split('/').Last();
                workflowTemplateReference["properties"]["parameters"]["$connections"]["value"][connectionName] = JObject.FromObject(new
                {
                    id = apiIdTemplate((string)apiId),
                    connectionId = $"[resourceId('Microsoft.Web/connections', parameters('{connectionNameProperty}_name'))]",
                    connectionName = $"[parameters('{connectionNameProperty}_name')]"
                });
                AddTemplateParameter($"{connectionNameProperty}_name", "string", (string)connectionNameProperty);

                if (generateConnection)
                {
                    //get api definition
                    JObject apiResource = await generateConnectionResource((string)apiId);

                    var connectionId = (string)conn["connectionId"];
                    //get api instance data, sub,group,provider,name
                    JObject apiResourceInstance = await generateConnectionResource(connectionId);
                    //add depends on to make sure that the api connection is created before the Logic App
                    ((JArray)workflowTemplateReference["dependsOn"]).Add($"[resourceId('Microsoft.Web/connections', parameters('{connectionName}_name'))]");

                    // WriteVerbose($"Generating connection resource for {connectionName}....");
                    var connectionTemplate = generateConnectionTemplate(apiResource, apiResourceInstance, (string)apiId);

                    template.resources.Insert(1, connectionTemplate);
                }
            }


            // WriteVerbose("Finalizing Template...");
            return JObject.FromObject(template);

        }

        private JToken handleActions(JObject definition, JObject parameters)
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
                        curr = curr.Replace(matches.Groups["resourcegroup"].Value, "', resourceGroup().name,'");
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
                    apiId = apiId.Replace(matches.Groups["resourcegroup"].Value, "', parameters('" + AddTemplateParameter("apimResourceGroup", "string", matches.Groups["resourcegroup"].Value) + "'),'");
                    apiId = apiId.Replace(matches.Groups["apim"].Value, "', parameters('" + AddTemplateParameter("apimInstanceName", "string", matches.Groups["apim"].Value) + "'),'");
                    apiId = apiId.Replace(matches.Groups["apiId"].Value, "', parameters('" + AddTemplateParameter("apimApiId", "string", matches.Groups["apiId"].Value) + "'),'");
                    apiId = "[concat('" + apiId + "')]";

                    definition["actions"][action.Name]["inputs"]["api"]["id"] = apiId;

                    //handle subscriptionkey
                    var subkey = ((JObject)definition["actions"][action.Name]["inputs"]).Value<string>("subscriptionKey");
                    definition["actions"][action.Name]["inputs"]["subscriptionKey"] = "[parameters('" + AddTemplateParameter("apimSubscriptionKey", "string", subkey) + "')]";
                }
                else if (type == "if")
                {
                    definition["actions"][action.Name] = handleActions(definition["actions"][action.Name].ToObject<JObject>(), parameters);
                    //else

                    if (definition["actions"][action.Name]["else"] != null && definition["actions"][action.Name]["else"]["actions"] != null)
                        definition["actions"][action.Name]["else"] = handleActions(definition["actions"][action.Name]["else"].ToObject<JObject>(), parameters);
                }
                else if (type == "scope" || type == "foreach" || type == "until")
                {
                    definition["actions"][action.Name] = handleActions(definition["actions"][action.Name].ToObject<JObject>(), parameters);
                }
                else if (type == "switch")
                {
                    //handle default if exists
                    if (definition["actions"][action.Name]["default"] != null && definition["actions"][action.Name]["default"]["actions"] != null)
                        definition["actions"][action.Name]["default"] = handleActions(definition["actions"][action.Name]["default"].ToObject<JObject>(), parameters);

                    foreach (var switchcase in definition["actions"][action.Name]["cases"].Children<JProperty>())
                    {
                        definition["actions"][action.Name]["cases"][switchcase.Name] = handleActions(definition["actions"][action.Name]["cases"][switchcase.Name].ToObject<JObject>(), parameters);
                    }
                }
                else if (type == "flatfiledecoding" || type == "flatfileencoding")
                {
                    definition["actions"][action.Name]["inputs"]["integrationAccount"]["schema"]["name"] = "[parameters('" + AddTemplateParameter(action.Name + "-SchemaName", "string", ((JObject)definition["actions"][action.Name]["inputs"]["integrationAccount"]["schema"]).Value<string>("name")) + "')]";
                }
                else if (type == "xslt")
                {
                    definition["actions"][action.Name]["inputs"]["integrationAccount"]["map"]["name"] = "[parameters('" + AddTemplateParameter(action.Name + "-MapName", "string", ((JObject)definition["actions"][action.Name]["inputs"]["integrationAccount"]["map"]).Value<string>("name")) + "')]";
                }
                else if (type == "http")
                {
                    definition["actions"][action.Name]["inputs"]["uri"] = "[parameters('" + AddTemplateParameter(action.Name + "-URI", "string", ((JObject)definition["actions"][action.Name]["inputs"]).Value<string>("uri")) + "')]";

                    var authenticationObj = (JObject)definition["actions"][action.Name]["inputs"]["authentication"];
                    if (authenticationObj != null)
                    {
                        var authType = authenticationObj.Value<string>("type");
                        if ("Basic".Equals(authType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            definition["actions"][action.Name]["inputs"]["authentication"]["password"] = "[parameters('" + AddTemplateParameter(action.Name + "-Password", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("password")) + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["username"] = "[parameters('" + AddTemplateParameter(action.Name + "-Username", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("username")) + "')]";
                        }
                        else if ("ClientCertificate".Equals(authType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            definition["actions"][action.Name]["inputs"]["authentication"]["password"] = "[parameters('" + AddTemplateParameter(action.Name + "-Password", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("password")) + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["pfx"] = "[parameters('" + AddTemplateParameter(action.Name + "-Pfx", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("pfx")) + "')]";
                        }
                        else if ("ActiveDirectoryOAuth".Equals(authType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            definition["actions"][action.Name]["inputs"]["authentication"]["audience"] = "[parameters('" + AddTemplateParameter(action.Name + "-Audience", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("audience")) + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["authority"] = "[parameters('" + AddTemplateParameter(action.Name + "-Authority", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("authority")) + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["clientId"] = "[parameters('" + AddTemplateParameter(action.Name + "-ClientId", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("clientId")) + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["secret"] = "[parameters('" + AddTemplateParameter(action.Name + "-Secret", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("secret")) + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["tenant"] = "[parameters('" + AddTemplateParameter(action.Name + "-Tenant", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("tenant")) + "')]";
                        }
                        else if ("Raw".Equals(authType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            definition["actions"][action.Name]["inputs"]["authentication"]["value"] = "[parameters('" + AddTemplateParameter(action.Name + "-Raw", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("value")) + "')]";
                        }
                    }
                }
                else if (type == "function")
                {
                    var curr = ((JObject)definition["actions"][action.Name]["inputs"]["function"]).Value<string>("id");

                    Regex rgx = new Regex(@"\/subscriptions\/(?<subscription>[0-9a-zA-Z-]*)\/resourceGroups\/(?<resourcegroup>[a-zA-Z0-9-]*)\/providers\/Microsoft.Web\/sites\/(?<functionApp>[a-zA-Z0-9-]*)\/functions\/(?<functionName>.*)");
                    var matches = rgx.Match(curr);

                    curr = curr.Replace(matches.Groups["subscription"].Value, "',subscription().subscriptionId,'");

                    if (LogicAppResourceGroup == matches.Groups["resourcegroup"].Value)
                    {
                        curr = curr.Replace(matches.Groups["resourcegroup"].Value, "', resourceGroup().name,'");
                    }
                    else
                    {
                        curr = curr.Replace(matches.Groups["resourcegroup"].Value, "', parameters('" + AddTemplateParameter(action.Name + "-ResourceGroup", "string", matches.Groups["resourcegroup"].Value) + "'),'");
                    }

                    curr = curr.Replace(matches.Groups["functionApp"].Value, "', parameters('" + AddTemplateParameter(action.Name + "-FunctionApp", "string", matches.Groups["functionApp"].Value) + "'),'");
                    curr = curr.Replace(matches.Groups["functionName"].Value, "', parameters('" + AddTemplateParameter(action.Name + "-FunctionName", "string", matches.Groups["functionName"].Value) + "'),'");

                    curr = "[concat('" + curr + "')]";

                    definition["actions"][action.Name]["inputs"]["function"]["id"] = curr;
                }
                else
                {
                    var api = action.Value.SelectToken("inputs.host.api");
                    if (api != null)
                        ((JObject)definition["actions"][action.Name]["inputs"]["host"]).Remove("api");

                    //get the type:
                    //handle connection
                    var connection = action.Value.SelectToken("inputs.host.connection");
                    if (connection != null)
                    {
                        var getConnectionNameType = this.GetConnectionTypeName(connection, parameters);

                        switch (getConnectionNameType)
                        {
                            case "filesystem":
                                {
                                    //var metadata = action.Value.SelectToken("metadata");
                                    var meta = ((JObject)definition["actions"][action.Name]["metadata"]);
                                    if (meta != null)
                                    {
                                        var base64string = ((JProperty)meta.First).Name;
                                        var param = AddParameterForMetadataBase64(meta, action.Name + "-folderPath");
                                        meta.Parent.Parent["inputs"]["path"] = action.Value.SelectToken("inputs.path").ToString().Replace($"'{base64string}'", "base64(parameters('" + param + "'))");                           
                                    }
                                    break;
                                }
                            case "azureblob":
                                {
                                    var meta = ((JObject)definition["actions"][action.Name]["metadata"]);
                                    if (meta != null)
                                    {
                                        var base64string = ((JProperty)meta.First).Name;
                                        var param = AddParameterForMetadataBase64(meta, action.Name + "-path");
                                        meta.Parent.Parent["inputs"]["path"] = action.Value.SelectToken("inputs.path").ToString().Replace($"'{base64string}'", "base64(parameters('" + param + "'))");
                                    }
                                    break;
                                }
                        }
                    }
                }
            }

            //when in if statements triggers is not there
            if (definition["triggers"] != null)
            {
                foreach (JProperty trigger in definition["triggers"])
                {
                    //handle api 
                    var api = trigger.Value.SelectToken("inputs.host.api");
                    if (api != null)
                        ((JObject)definition["triggers"][trigger.Name]["inputs"]["host"]).Remove("api");

                    //handle connection
                    var connection = trigger.Value.SelectToken("inputs.host.connection");
                    if (connection != null)
                    {
                        var getConnectionNameType = this.GetConnectionTypeName(connection, parameters);

                        switch (getConnectionNameType)
                        {
                            case "filesystem":
                                {
                                    var meta = ((JObject)trigger.Value["metadata"]);
                                    if (meta != null)
                                    {
                                        var base64string = ((JProperty)meta.First).Name;
                                        var param = AddParameterForMetadataBase64(meta, trigger.Name + "-folderPath");
                                        meta.Parent.Parent["inputs"]["queries"]["folderId"] = "[base64(parameters('" + param + "'))]";
                                    }                                   
                                    break;
                                }
                        }
                    }

                    //promote parameters for reccurence settings
                    var recurrence = trigger.Value.SelectToken("recurrence");
                    if (recurrence != null)
                    {
                        definition["triggers"][trigger.Name]["recurrence"]["frequency"] = "[parameters('" + this.AddTemplateParameter(trigger.Name + "Frequency", "string", recurrence.Value<string>("frequency")) + "')]";
                        definition["triggers"][trigger.Name]["recurrence"]["interval"] = "[parameters('" + this.AddTemplateParameter(trigger.Name + "Interval", "int", new JProperty("defaultValue", recurrence.Value<int>("interval"))) + "')]";
                        if (recurrence["startTime"] != null)
                        {
                            string value = recurrence.Value<string>("startTime");
                            DateTime date;
                            if(DateTime.TryParse(value,out date))
                            {
                                value = date.ToString("O");
                            }
                            definition["triggers"][trigger.Name]["recurrence"]["startTime"] = "[parameters('" + this.AddTemplateParameter(trigger.Name + "StartTime", "string", value) + "')]";
                        }
                        if (recurrence["timeZone"] != null)
                        {
                            definition["triggers"][trigger.Name]["recurrence"]["timeZone"] = "[parameters('" + this.AddTemplateParameter(trigger.Name + "TimeZone", "string", recurrence.Value<string>("timeZone")) + "')]";
                        }
                        if (recurrence["schedule"] != null)
                        {
                            definition["triggers"][trigger.Name]["recurrence"]["schedule"] = "[parameters('" + this.AddTemplateParameter(trigger.Name + "Schedule", "Object", new JProperty("defaultValue", recurrence["schedule"])) + "')]";                            
                        }
                    }
                }
            }

            return definition;
        }

        private string AddParameterForMetadataBase64(JObject meta,string parametername)
        {
            var base64string = ((JProperty)meta.First).Name;
            var path = Encoding.UTF8.GetString(Convert.FromBase64String(base64string));
            var param = AddTemplateParameter(parametername, "string", path);
            meta.Remove(((JProperty)meta.First).Name);
            meta.Add("[base64(parameters('" + param + "'))]", JToken.Parse("\"[parameters('" + param + "')]\""));
            
            return param;
        }

        private void HandledMetaDataFilePaths(JObject definition,JProperty action)
        {
            var metadata = action.Value.SelectToken("metadata");
            if (metadata != null)
            {

                byte[] data = Convert.FromBase64String(((JProperty)metadata.First).Name);
                var folderpath = Encoding.UTF8.GetString(data);

                var param = AddTemplateParameter(action.Name + "-folderPath", "string", folderpath);
                //remove the metadata tag associated with the folderid
                var meta = ((JObject)definition["actions"][action.Name]["metadata"]);
                meta.Remove(((JProperty)metadata.First).Name);
                meta.Add("[base64(parameters('" + param + "'))]", JToken.Parse("\"[parameters('" + param + "')]\""));

                definition["actions"][action.Name]["inputs"]["path"] = "[concat('/datasets/default/folders/@{encodeURIComponent(''',base64(parameters('" + param + "')),''')}')]";
            }
        }


        private string GetConnectionTypeName(JToken ConnectionToken, JObject parameters)
        {
            //try to find the connection and understand special handling
            var name = ConnectionToken.Value<string>("name");
            if (name != null && name.StartsWith("@parameters('$connections')"))
            {
                var match = Regex.Match(name, @"@parameters\('\$connections'\)\['(?<connectionname>\w*)");
                if (match.Success)
                {
                    var path = "$connections.value." + match.Groups["connectionname"].Value;
                    var paramConnection = parameters.SelectToken(path);
                    if (paramConnection != null)
                    {
                        //if filesystem the path is base64 necoded and need to be set as parameter
                        var id = paramConnection.Value<string>("id");
                        if (!string.IsNullOrEmpty(id))
                            return id.Split('/').Last();
                    }
                }
            }

            return null;
        }

        private string AddTemplateParameter(string paramname, string type, string defaultvalue)
        {
            return AddTemplateParameter(paramname, type, new JProperty("defaultValue", defaultvalue));
        }


        private string AddTemplateParameter(string paramname, string type, JProperty defaultvalue)
        {
            string realParameterName = paramname;
            JObject param = new JObject();
            param.Add("type", JToken.FromObject(type));
            param.Add(defaultvalue);

            if (!string.IsNullOrEmpty(defaultvalue.Value.ToString()) && type.Equals("string",StringComparison.CurrentCultureIgnoreCase))
            {
                foreach (var c in template.parameters)
                {
                    if (c.Value.Value<string>("type").Equals("string", StringComparison.CurrentCultureIgnoreCase) &&  c.Value.Value<string>("defaultValue").Equals(defaultvalue.Value.ToString()))
                    {
                        return c.Key;
                    }
                }
            }

            if (template.parameters[paramname] == null)
            {
                template.parameters.Add(paramname, param);
            }
            else
            {
                if (!template.parameters[paramname].Value<string>("defaultValue").Equals(defaultvalue.Value.ToString()))
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

        private async Task<JObject> generateConnectionResource(string apiId)
        {

            string url = "https://management.azure.com" + apiId + "?api-version=2016-06-01";
            // WriteVerbose("Doing a GET to: " + url);
            var request = HttpWebRequest.Create(url);
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + Token;

            var logicAppRequest = await request.GetResponseAsync();
            var stream = logicAppRequest.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            var apiResource = reader.ReadToEnd();
            // WriteVerbose("Got api");
            return JObject.Parse(apiResource);

        }

        public JObject generateConnectionTemplate(JObject connectionResource, JObject connectionInstance, string apiId)
        {
            //create template
            string connectionName = (string)connectionInstance["name"];
            var connectionTemplate = new Models.ConnectionTemplate(AddTemplateParameter($"{connectionName}_name", "string", (string)connectionInstance["name"]), apiId);
            //displayName            
            connectionTemplate.properties.displayName = $"[parameters('{AddTemplateParameter(connectionName + "_displayName", "string", (string)connectionInstance["properties"]["displayName"])}')]";
            JObject connectionParameters = new JObject();

            bool useGateway = connectionInstance["properties"]["nonSecretParameterValues"]["gateway"] != null;


            //add all parameters

            foreach (JProperty parameter in connectionResource["properties"]["connectionParameters"])
            {
                if ((string)(parameter.Value)["type"] != "oauthSetting")
                {
                    //we are not handling parameter gatewaySetting
                    if ((string)parameter.Value["type"] == "gatewaySetting")
                        continue;
                    if (parameter.Value["uiDefinition"]["constraints"]["capability"] != null)
                    {
                        var match = parameter.Value["uiDefinition"]["constraints"]["capability"].FirstOrDefault(cc => (string)cc == "gateway" && useGateway || (string)cc == "cloud" && !useGateway);
                        if (match == null)
                            continue;
                    }

                    
                    if (parameter.Name == "accessKey" && apiId.EndsWith("azureblob"))
                    {
                        connectionParameters.Add(parameter.Name, $"[listKeys(resourceId('Microsoft.Storage/storageAccounts', parameters('{connectionName}_accountName')), providers('Microsoft.Storage', 'storageAccounts').apiVersions[0]).keys[0].value]");
                    }
                    else {
                        var currentvalue = (string)connectionInstance["properties"]["nonSecretParameterValues"][parameter.Name] ?? "";
                        var addedparam = AddTemplateParameter($"{connectionName}_{parameter.Name}", (string)(parameter.Value)["type"], currentvalue);
                        connectionParameters.Add(parameter.Name, $"[parameters('{addedparam}')]");

                        //If has an enum
                        if (parameter.Value["allowedValues"] != null)
                        {
                            var array = new JArray();
                            foreach (var allowedValue in parameter.Value["allowedValues"])
                            {
                                array.Add(allowedValue["value"]);
                            }
                            template.parameters[addedparam]["allowedValues"] = array;
                            if (parameter.Value["allowedValues"].Count() == 1)
                            {
                                template.parameters[addedparam]["defaultValue"] = parameter.Value["allowedValues"][0]["value"];
                            }
                        }

                        if (parameter.Value["uiDefinition"]["description"] != null)
                        {
                            //add meta data
                            template.parameters[addedparam]["metadata"] = new JObject();
                            template.parameters[addedparam]["metadata"]["description"] = parameter.Value["uiDefinition"]["description"];
                        }
                    }
                }
            }

            if (useGateway)
            {
                var currentvalue = (string)connectionInstance["properties"]["nonSecretParameterValues"]["gateway"]["id"];
                ///subscriptions/c107df29-a4af-4bc9-a733-f88f0eaa4296/resourceGroups/OnPremDataGateway/providers/Microsoft.Web/connectionGateways/Malos-LogicApp2015
                Regex rgx = new Regex(@"\/subscriptions\/(?<subscription>[0-9a-zA-Z-]*)\/resourceGroups\/(?<resourcegroup>[a-zA-Z0-9-]*)\/providers\/Microsoft.Web\/connectionGateways\/(?<gatewayname>[a-zA-Z0-9-]*)");
                var matches = rgx.Match(currentvalue);

                var gatewayname = AddTemplateParameter($"{connectionName}_gatewayname", "string", matches.Groups["gatewayname"].Value);
                var resourcegroup = AddTemplateParameter($"{connectionName}_gatewayresourcegroup", "string", matches.Groups["resourcegroup"].Value);
                var gatewayobject = new JObject();
                gatewayobject["id"] = $"[concat('subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('{resourcegroup}'),'/providers/Microsoft.Web/connectionGateways/',parameters('{gatewayname}'))]";
                connectionParameters.Add("gateway", gatewayobject);
                useGateway = true;
            }

            connectionTemplate.properties.parameterValues = connectionParameters;
            return JObject.FromObject(connectionTemplate);
        }

        public DeploymentTemplate GetTemplate()
        {
            return template;
        }

    }
}
