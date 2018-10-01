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

    public class TemplateGenerator
    {

        private DeploymentTemplate template;
        private JObject workflowTemplateReference;

        private string LogicAppResourceGroup;

        IResourceCollector resourceCollector;
        private string SubscriptionId;
        private string ResourceGroup;
        private string LogicApp;
        private string IntegrationAccountId;
        private bool extractIntegrationAccountArtifacts = false;

        public TemplateGenerator(string LogicApp, string SubscriptionId, string ResourceGroup, IResourceCollector resourceCollector)
        {
            this.SubscriptionId = SubscriptionId;
            this.ResourceGroup = ResourceGroup;
            this.LogicApp = LogicApp;
            this.resourceCollector = resourceCollector;
            template = JsonConvert.DeserializeObject<DeploymentTemplate>(GetResourceContent("LogicAppTemplate.Templates.starterTemplate.json"));
        }

        private string GetResourceContent(string resourceName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public bool DiagnosticSettings { get; set; }

        public async Task<JObject> GenerateTemplate()
        {
            JObject _definition = await resourceCollector.GetResource($"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup}/providers/Microsoft.Logic/workflows/{LogicApp}", "2016-06-01");
            return await generateDefinition(_definition);
        }

        public async Task<JObject> generateDefinition(JObject definition, bool generateConnection = true)
        {
            var rid = new AzureResourceId(definition.Value<string>("id"));
            LogicAppResourceGroup = rid.ResourceGroupName;
            //Manage Integration account
            if (definition["properties"]["integrationAccount"] == null)
            {
                ((JObject)template.resources[0]["properties"]).Remove("integrationAccount");
                template.parameters.Remove("IntegrationAccountName");
                template.parameters.Remove("IntegrationAccountResourceGroupName");
            }
            else
            {
                template.parameters["IntegrationAccountName"]["defaultValue"] = definition["properties"]["integrationAccount"]["name"];
                IntegrationAccountId = definition["properties"]["integrationAccount"].Value<string>("id");
            }


            template.parameters["logicAppName"]["defaultValue"] = definition.Value<string>("name");

            workflowTemplateReference = template.resources.Where(t => ((string)t["type"]) == "Microsoft.Logic/workflows").FirstOrDefault();

            // WriteVerbose("Upgrading connectionId paramters...");
            var modifiedDefinition = definition["properties"]["definition"].ToString().Replace(@"['connectionId']", @"['connectionId']");
            // WriteVerbose("Removing API Host references...");

            var def = JObject.Parse(modifiedDefinition);
            //try fixing all properties that start with before handling connections etc[
            var ll = def.Value<JObject>("actions").Descendants().Where(dd => dd.Type == JTokenType.String && dd.Value<string>().StartsWith("[") && dd.Value<string>().EndsWith("]")).ToList();
            for (int i = 0; i < ll.Count(); i++)
            {
                var tofix = ll[i];
                var newValue = "[" + tofix.Value<string>();
                if (tofix.Parent.Type == JTokenType.Property)
                {
                    (tofix.Parent as JProperty).Value = newValue;
                }
                else
                {
                    var parent = tofix.Parent;
                    tofix.Remove();
                    parent.Add(newValue);
                }
            }

            workflowTemplateReference["properties"]["definition"] = handleActions(def, (JObject)definition["properties"]["parameters"]);

            // Diagnostic Settings
            if (DiagnosticSettings)
            {
                JToken resources = await handleDiagnosticSettings(definition);
                ((JArray)workflowTemplateReference["resources"]).Merge(resources);
            }

            // Remove resources if empty
            if (((JArray)workflowTemplateReference["resources"]).Count == 0)
            {
                workflowTemplateReference.Remove("resources");
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
                string name = connectionProperty.Name;
                string connectionId = connectionProperty.First.Value<string>("connectionId");
                string id = connectionProperty.First.Value<string>("id");
                string connectionName = connectionProperty.First["connectionName"] != null ? connectionProperty.First.Value<string>("connectionName") : connectionId.Split('/').Last();

                var cid = apiIdTemplate(id);
                string concatedId = $"[concat('{cid.ToString()}')]";
                //fixes old templates where name sometimes is missing

                var connectionNameParam = AddTemplateParameter($"{connectionName}_name", "string", connectionName);
                workflowTemplateReference["properties"]["parameters"]["$connections"]["value"][connectionName] = JObject.FromObject(new
                {
                    id = concatedId,
                    connectionId = $"[resourceId('Microsoft.Web/connections', parameters('{connectionNameParam}'))]",
                    connectionName = $"[parameters('{connectionNameParam}')]"
                });
              
                if (generateConnection)
                {
                    //get api definition
                    JObject apiResource = await resourceCollector.GetResource("https://management.azure.com" + id, "2016-06-01");
                    //get api instance data, sub,group,provider,name
                    JObject apiResourceInstance = await resourceCollector.GetResource("https://management.azure.com" + connectionId, "2016-06-01");
                    //add depends on to make sure that the api connection is created before the Logic App
                    ((JArray)workflowTemplateReference["dependsOn"]).Add($"[resourceId('Microsoft.Web/connections', parameters('{connectionNameParam}'))]");

                    // WriteVerbose($"Generating connection resource for {connectionName}....");
                    var connectionTemplate = generateConnectionTemplate(apiResource, apiResourceInstance, connectionName, concatedId, connectionNameParam);

                    template.resources.Insert(1, connectionTemplate);
                }
            }


            // WriteVerbose("Finalizing Template...");
            return JObject.FromObject(template);
        }

        private async Task<JToken> handleDiagnosticSettings(JObject definition)
        {
            JArray result = new JArray();

            // Get diagnostic settings 
            JObject resources = await resourceCollector.GetResource("https://management.azure.com" + definition.Value<string>("id") + "/providers/microsoft.insights/diagnosticSettings", "2017-05-01-preview");

            foreach (JObject resourceProperty in resources["value"])
            {
                string dsName = AddTemplateParameter(Constants.DsName, "string", resourceProperty["name"]);

                Match m = Regex.Match((string)resourceProperty["properties"]["workspaceId"], "resourceGroups/(.*)/providers/Microsoft.OperationalInsights/workspaces/(.*)", RegexOptions.IgnoreCase);

                string dsResourceGroup = AddTemplateParameter(Constants.DsResourceGroup, "string", m.Groups[1].Value);
                string dsWorkspaceId = AddTemplateParameter(Constants.DsWorkspaceName, "string", m.Groups[2].Value);

                string dsLogsEnabled = AddTemplateParameter(Constants.DsLogsEnabled, "bool", resourceProperty["properties"]["logs"][0]["enabled"]);
                string dsLogsRetentionPolicyEnabled = AddTemplateParameter(Constants.DsLogsRetentionPolicyEnabled, "bool", resourceProperty["properties"]["logs"][0]["retentionPolicy"]["enabled"]);
                string dsLogsRetentionPolicyDays = AddTemplateParameter(Constants.DsLogsRetentionPolicyDays, "int", resourceProperty["properties"]["logs"][0]["retentionPolicy"]["days"]);
                string dsMetricsEnabled = AddTemplateParameter(Constants.DsMetricsEnabled, "bool", resourceProperty["properties"]["metrics"][0]["enabled"]);
                string dsMetricsRetentionPolicyEnabled = AddTemplateParameter(Constants.DsMetricsRetentionPolicyEnabled, "bool", resourceProperty["properties"]["metrics"][0]["retentionPolicy"]["enabled"]);
                string dsMetricsRetentionPolicyDays = AddTemplateParameter(Constants.DsMetricsRetentionPolicyDays, "int", resourceProperty["properties"]["metrics"][0]["retentionPolicy"]["days"]);

                DiagnosticSettingsTemplate resource = new DiagnosticSettingsTemplate(dsName);
                resource.dependsOn.Add("[parameters('logicAppName')]");

                result.Add(JObject.FromObject(resource));
            }

            return result;
        }

        private AzureResourceId apiIdTemplate(string apiId)
        {
            var rid = new AzureResourceId(apiId);
            rid.SubscriptionId = "',subscription().subscriptionId,'";
            if (apiId.Contains("/managedApis/"))
            {
                rid.ReplaceValueAfter("locations", "',parameters('logicAppLocation'),'");
            }
            else
            {

                string resourcegroupValue = LogicAppResourceGroup == rid.ResourceGroupName ? "[resourceGroup().name]" : rid.ResourceGroupName;
                string resourcegroupParameterName = AddTemplateParameter(apiId.Split('/').Last() + "-ResourceGroup", "string", resourcegroupValue);
                rid.ResourceGroupName = $"',parameters('{resourcegroupParameterName}'),'";
            }
            return rid;
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
                    
                    var wid = new AzureResourceId(curr);
                    string resourcegroupValue = LogicAppResourceGroup == wid.ResourceGroupName ? "[resourceGroup().name]" : wid.ResourceGroupName;
                    string resourcegroupParameterName = AddTemplateParameter(action.Name + "-ResourceGroup", "string", resourcegroupValue);
                    string wokflowParameterName = AddTemplateParameter(action.Name + "-LogicAppName", "string", wid.ResourceName);
                    string workflowid = $"[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('{resourcegroupParameterName}'),'/providers/Microsoft.Logic/workflows/',parameters('{wokflowParameterName}'))]";
                    definition["actions"][action.Name]["inputs"]["host"]["workflow"]["id"] = workflowid;
                    
                }
                else if (type == "apimanagement")
                {
                    var apiId = ((JObject)definition["actions"][action.Name]["inputs"]["api"]).Value<string>("id");
                    var aaid = new AzureResourceId(apiId);


                    aaid.SubscriptionId = "',subscription().subscriptionId,'";
                    aaid.ResourceGroupName = "', parameters('" + AddTemplateParameter("apimResourceGroup", "string", aaid.ResourceGroupName) + "'),'";
                    aaid.ReplaceValueAfter("service", "', parameters('" + AddTemplateParameter("apimInstanceName", "string", aaid.ValueAfter("service")) + "'),'");
                    aaid.ReplaceValueAfter("apis", "', parameters('" + AddTemplateParameter("apimApiId", "string", aaid.ValueAfter("apis")) + "'),'");
                    apiId = "[concat('" + aaid.ToString() + "')]";

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
                else if (type == "xslt" || type == "liquid")
                {
                    var mapname = ((JObject)definition["actions"][action.Name]["inputs"]["integrationAccount"]["map"]).Value<string>("name");
                    var mapParameterName = AddTemplateParameter(action.Name + "-MapName", "string", mapname);
                    definition["actions"][action.Name]["inputs"]["integrationAccount"]["map"]["name"] = "[parameters('" + mapParameterName + "')]";
                    //Get the map
                    if (extractIntegrationAccountArtifacts)
                    {
                        var mapresource = resourceCollector.GetResource(IntegrationAccountId + "/maps/" + mapname, "2018-07-01-preview").Result;

                        var uri = mapresource["properties"]["contentLink"].Value<string>("uri").Split('?');
                        var map = resourceCollector.GetRawResource(uri[0], uri[1].Replace("api-version=", "")).Result;

                        //create the resource and add to the template
                        var newResource = JObject.Parse(GetResourceContent("LogicAppTemplate.Templates.integrationAccountMap.json"));
                        //add the current Integration Account parameter name
                        newResource["name"] = $"[concat(parameters('IntegrationAccountName'), '/' ,parameters('{mapParameterName}'))]";

                        newResource["properties"]["mapType"] = mapresource["properties"]["mapType"];
                        newResource["properties"]["parametersSchema"] = mapresource["properties"]["parametersSchema"];

                        newResource["properties"]["content"] = map.Replace("\"", "\\\"");
                        newResource["properties"]["contentType"] = "text";

                        //add dependson
                        if (template.resources.First()["dependsOn"] == null)
                        {
                            template.resources.First()["dependsOn"] = new JArray();
                        }
                    ((JArray)template.resources.First()["dependsOn"]).Add($"[resourceId('{newResource.Value<string>("type")}', parameters('IntegrationAccountName'),parameters('{mapParameterName}'))]");
                        template.resources.Add(newResource);
                    }
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
                    var faid = new AzureResourceId(curr);

                    var resourcegroupValue = LogicAppResourceGroup == faid.ResourceGroupName ? "[resourceGroup().name]" : faid.ResourceGroupName;

                    faid.SubscriptionId = "',subscription().subscriptionId,'";
                    faid.ResourceGroupName = "',parameters('" + AddTemplateParameter(action.Name + "-ResourceGroup", "string", resourcegroupValue) + "'),'";
                    faid.ReplaceValueAfter("sites", "',parameters('" + AddTemplateParameter(action.Name + "-FunctionApp", "string", faid.ValueAfter("sites")) + "'),'");
                    faid.ReplaceValueAfter("functions", "',parameters('" + AddTemplateParameter(action.Name + "-FunctionName", "string", faid.ValueAfter("functions")) + "')");

                    definition["actions"][action.Name]["inputs"]["function"]["id"] = "[concat('" + faid.ToString() + ")]";
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
                        var connectioname = (JObject)definition["actions"][action.Name]["inputs"]["host"]["connection"].Value<string>("name");

                        var getConnectionNameType = this.GetConnectionTypeName(connection, parameters);

                        switch (getConnectionNameType)
                        {
                            case "filesystem":
                                {
                                    var newValue = AddParameterForMetadataBase64((JObject)action.Value, action.Name + "-folderPath", action.Value["inputs"].Value<string>("path"));
                                    action.Value["inputs"]["path"] = newValue;

                                    break;
                                }
                            case "azuretables":
                                {
                                    var inputs = action.Value.Value<JObject>("inputs");
                                    var path = inputs.Value<string>("path");
                                    var m = Regex.Match(path, @"/Tables/@{encodeURIComponent\('(.*)'\)");
                                    if (m.Groups.Count > 1)
                                    {
                                        var tablename = m.Groups[1].Value;
                                        var param = AddTemplateParameter(action.Name + "-tablename", "string", tablename);                                        
                                        inputs["path"] = "[concat('" + path.Replace($"'{tablename}'", $"', parameters('__apostrophe'), parameters('{param}'), parameters('__apostrophe'), '") + "')]";
                                        AddTemplateParameter("__apostrophe", "string", "'");
                                    }

                                    break;
                                }
                            case "azureblob":
                                {
                                    var newValue = AddParameterForMetadataBase64((JObject)action.Value, action.Name + "-path", action.Value["inputs"].Value<string>("path"));
                                    action.Value["inputs"]["path"] = newValue;
                                    break;
                                }
                            case "azurequeues":
                                {
                                    var inputs = action.Value.Value<JObject>("inputs");
                                    var path = inputs.Value<string>("path");
                                    var m = Regex.Match(path, @"/@{encodeURIComponent\('(.*)'\)");
                                    if (m.Groups.Count > 1)
                                    {
                                        var queuename = m.Groups[1].Value;
                                        var param = AddTemplateParameter(action.Name + "-queuename", "string", queuename);
                                        inputs["path"] = "[concat('" + path.Replace("'","''").Replace($"'{queuename}'", $"'', parameters('{param}'), ''") + "')]";
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

                                    try
                                    {
                                        var newValue = AddParameterForMetadataBase64((JObject)trigger.Value, trigger.Name + "-folderPath", trigger.Value["inputs"]["queries"].Value<string>("folderId"));
                                        trigger.Value["inputs"]["queries"]["folderId"] = newValue;
                                    }
                                    catch (FormatException ex)
                                    {

                                        //folderid is not a valid base64 so we are skipping it for now
                                        /*var path = ((JProperty)meta.First).Value.ToString();
                                         var param = AddTemplateParameter(trigger.Name + "-folderPath","string",path);
                                         meta[((JProperty)meta.First).Name] = $"[parameters('{param}')]";*/
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
                            if (DateTime.TryParse(value, out date))
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

        private string AddParameterForMetadataBase64(JObject action, string parametername, string currentValue)
        {
            var meta = action.Value<JObject>("metadata");

            if (meta == null)
                return "";

            var inputs = action.Value<JObject>("inputs");

            var base64string = "";
            if (meta.Children().Count() == 1)
            {
                base64string = ((JProperty)meta.First).Name;
            }
            else
            {
                foreach (var property in meta.Children<JProperty>())
                {
                    if (currentValue.Contains(property.Name))
                    {
                        base64string = property.Name;
                        break;
                    }
                }
            }
            var param = AddTemplateParameter(parametername, "string", Base64Decode(base64string));
            meta.RemoveAll();
            meta.Add("[base64(parameters('" + param + "'))]", JToken.Parse("\"[parameters('" + param + "')]\""));
            if(currentValue == base64string)
            {
                return $"[base64(parameters('{param}'))]";
            }
            var replaced = currentValue.Replace($"'{base64string}'", $"', parameters('__apostrophe'), base64(parameters('{param}')), parameters('__apostrophe'), '");

            var newValue = $"[concat('{replaced}')]";
            AddTemplateParameter("__apostrophe", "string", "'");

            return newValue;
        }

        private string Base64Decode(string base64string)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64string));
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

        private string AddTemplateParameter(string paramname, string type, object defaultvalue)
        {
            return AddTemplateParameter(paramname, type, new JProperty("defaultValue", defaultvalue));
        }


        private string AddTemplateParameter(string paramname, string type, JProperty defaultvalue)
        {
            string realParameterName = paramname;
            JObject param = new JObject();
            param.Add("type", JToken.FromObject(type));
            param.Add(defaultvalue);

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



        public JObject generateConnectionTemplate(JObject connectionResource, JObject connectionInstance, string connectionName, string concatedId, string connectionNameParam)
        {
            //create template
            var connectionTemplate = new Models.ConnectionTemplate(connectionNameParam, concatedId);
            //displayName            
            connectionTemplate.properties.displayName = $"[parameters('{AddTemplateParameter(connectionName + "_displayName", "string", (string)connectionInstance["properties"]["displayName"])}')]";
            JObject connectionParameters = new JObject();

            bool useGateway = connectionInstance["properties"]["nonSecretParameterValues"]["gateway"] != null;


            //add all parameters
            if (connectionResource["properties"]["connectionParameters"] != null)
            {
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


                        if ( (parameter.Name == "accessKey" && concatedId.EndsWith("/azureblob')]") ) || parameter.Name == "sharedkey" && concatedId.EndsWith("/azuretables')]"))
                        {
                            connectionParameters.Add(parameter.Name, $"[listKeys(resourceId('Microsoft.Storage/storageAccounts', parameters('{connectionName}_accountName')), '2018-02-01').keys[0].value]");
                        }else if ( parameter.Name == "sharedkey" && concatedId.EndsWith("/azurequeues')]"))
                        {
                            connectionParameters.Add(parameter.Name, $"[listKeys(resourceId('Microsoft.Storage/storageAccounts', parameters('{connectionName}_storageaccount')), '2018-02-01').keys[0].value]");
                        }
                        else if (concatedId.EndsWith("/azureeventgridpublish')]"))
                        {
                            var url = connectionInstance["properties"]["nonSecretParameterValues"].Value<string>("endpoint");
                            var location = connectionInstance.Value<string>("location");
                            url = url.Replace("https://", "");
                            var site = url.Substring(0, url.LastIndexOf("." + location));

                            var param = AddTemplateParameter($"{connectionInstance.Value<string>("name")}_instancename", "string", site);

                            if (parameter.Name == "endpoint")
                            {
                                connectionParameters.Add(parameter.Name, $"[reference(concat('Microsoft.EventGrid/topics/',parameters('{param}')),'2018-01-01').endpoint]");
                            }
                            else if (parameter.Name == "api_key")
                            {
                                connectionParameters.Add(parameter.Name, $"[listKeys(resourceId('Microsoft.EventGrid/topics',parameters('{param}')),'2018-01-01').key1]");
                            }
                            
                            
                        }
                        else
                        {
                            //todo check this!
                            var addedparam = AddTemplateParameter($"{connectionName}_{parameter.Name}", (string)(parameter.Value)["type"], connectionInstance["properties"]["nonSecretParameterValues"][parameter.Name]);
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
            }

            if (useGateway)
            {
                var currentvalue = (string)connectionInstance["properties"]["nonSecretParameterValues"]["gateway"]["id"];
                var rid = new AzureResourceId(currentvalue);
                var gatewayname = AddTemplateParameter($"{connectionName}_gatewayname", "string", rid.ResourceName);
                var resourcegroup = AddTemplateParameter($"{connectionName}_gatewayresourcegroup", "string", rid.ResourceGroupName);

                var gatewayobject = new JObject();
                gatewayobject["id"] = $"[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('{resourcegroup}'),'/providers/Microsoft.Web/connectionGateways/',parameters('{gatewayname}'))]";
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
