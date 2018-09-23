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

        public TemplateGenerator(string LogicApp, string SubscriptionId, string ResourceGroup, IResourceCollector resourceCollector)
        {
            this.SubscriptionId = SubscriptionId;
            this.ResourceGroup = ResourceGroup;
            this.LogicApp = LogicApp;
            this.resourceCollector = resourceCollector;
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "LogicAppTemplate.Templates.starterTemplate.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                template = JsonConvert.DeserializeObject<DeploymentTemplate>(reader.ReadToEnd());
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

            template.parameters["logicAppName"]["defaultValue"] = definition.Value<string>("name");

            workflowTemplateReference = template.resources.Where(t => ((string)t["type"]) == "Microsoft.Logic/workflows").FirstOrDefault();

            // WriteVerbose("Upgrading connectionId paramters...");
            var modifiedDefinition = definition["properties"]["definition"].ToString().Replace(@"['connectionId']", @"['connectionId']");
            // WriteVerbose("Removing API Host references...");

            workflowTemplateReference["properties"]["definition"] = handleActions(JObject.Parse(modifiedDefinition), (JObject)definition["properties"]["parameters"]);

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
                /*               
                "Billogram": {
                "connectionId": "/subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/INT001.Invoice/providers/Microsoft.Web/connections/Billogram",
                "connectionName": "Billogram",
                "id": "/subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/Messaging/providers/Microsoft.Web/customApis/Billogram"
                } 
                 */

                string name = connectionProperty.Name;
                string connectionId = connectionProperty.First.Value<string>("connectionId");
                string id = connectionProperty.First.Value<string>("id");
                string connectionName = connectionProperty.First["connectionName"] != null ? connectionProperty.First.Value<string>("connectionName"): connectionId.Split('/').Last();

                var cid = apiIdTemplate(id);
                string concatedId = $"[concat('{cid.ToString()}')]";
                //fixes old templates where name sometimes is missing

                var connectionNameParam = AddTemplateParameter($"{connectionName}_name", "string", connectionName);
                workflowTemplateReference["properties"]["parameters"]["$connections"]["value"][name] = JObject.FromObject(new
                {
                    id = concatedId, 
                    connectionId = $"[resourceId('Microsoft.Web/connections', parameters('{connectionNameParam}'))]",
                    connectionName = $"[parameters('{connectionNameParam}')]"
                });
                
                /*
                "connections_Billogram_id": {
                    "defaultValue": "/subscriptions/cb693348-19cf-42ee-9a63-1969d567f333/resourceGroups/Shared/providers/Microsoft.Web/customApis/Billogram",
                    "type": "String"
                }
                "workflows_INT001.Invoice_id": {
                    "defaultValue": "/subscriptions/cb693348-19cf-42ee-9a63-1969d567f333/resourceGroups/Shared/providers/Microsoft.Web/customApis/Billogram",
                    "type": "String"
                }
                        "connections_Billogram_name": {
                    "defaultValue": "Billogram",
                    "type": "String"
                },
                {
                    "comments": "Generalized from resource: '/subscriptions/cb693348-19cf-42ee-9a63-1969d567f333/resourceGroups/INT001.Invoice/providers/Microsoft.Web/connections/Billogram'.",
                    "type": "Microsoft.Web/connections",
                    "name": "[parameters('connections_Billogram_name')]",
                    "apiVersion": "2016-06-01",
                    "location": "westeurope",
                    "scale": null,
                    "properties": {
                        "displayName": "[parameters('connections_Billogram_name')]",
                        "customParameterValues": {},
                        "api": {
                            "id": "[parameters('connections_Billogram_id')]"
                        }
                    },
                    "dependsOn": []
                }
                  "parameters": {
                    "$connections": {
                        "value": {
                            "Billogram": {
                                "connectionId": "[resourceId('Microsoft.Web/connections', parameters('connections_Billogram_name'))]",
                                "connectionName": "Billogram",
                                "id": "[parameters('workflows_INT001.Invoice_id')]"
                            }
                        }
                    }
                }           
                 */
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
            if(apiId.Contains("/managedApis/"))
            {
                rid.ReplaceValueAfter("locations", "',parameters('logicAppLocation'),'");                
            }else
            {
                
                string resourcegroupValue = LogicAppResourceGroup == rid.ResourceGroupName ? "[resourceGroup().name]" : rid.ResourceGroupName;
                string resourcegroupParameterName = AddTemplateParameter(apiId.Split('/').Last() + "-ResourceGroup", "string", resourcegroupValue);
                rid.ResourceGroupName = $"',parameters('{resourcegroupParameterName}'),'";
            }
            return rid; 
        }



        private static string regextoresourcegroup = @"\/subscriptions\/(?<subscription>[0-9a-zA-Z-]*)\/resourceGroups\/(?<resourcegroup>[\w-_d]*)\/";
        private JToken handleActions(JObject definition, JObject parameters)
        {
            foreach (JProperty action in definition["actions"])
            {
                var type = action.Value.SelectToken("type").Value<string>().ToLower();
                //if workflow fix so links are dynamic.
                if (type == "workflow")
                {
                    var curr = ((JObject)definition["actions"][action.Name]["inputs"]["host"]["workflow"]).Value<string>("id");
                    ///subscriptions/fakeecb73-15f5-4c85-bb3e-fakeecb73/resourceGroups/myresourcegrp/providers/Microsoft.Logic/workflows/INT0020-All-Users-Batch2
                    var wid = new AzureResourceId(curr);                    
                    string resourcegroupValue = LogicAppResourceGroup == wid.ResourceGroupName ? "[resourceGroup().name]" : wid.ResourceGroupName;
                    string resourcegroupParameterName = AddTemplateParameter(action.Name + "-ResourceGroup", "string", resourcegroupValue);
                    string wokflowParameterName = AddTemplateParameter(action.Name + "-LogicAppName", "string", wid.ResourceName);
                    string workflowid = $"[concat('/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('{resourcegroupParameterName}'),'/providers/Microsoft.Logic/workflows/',parameters('{wokflowParameterName}'))]";
                    definition["actions"][action.Name]["inputs"]["host"]["workflow"]["id"] = workflowid;
                    //string result = "[concat('" + rgx.Replace(matches.Groups[1].Value, "',subscription().subscriptionId,'") + + "']";
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
                                        meta.Parent.Parent["inputs"]["path"] = "[concat('" + action.Value.SelectToken("inputs.path").ToString().Replace($"'{base64string}'", "',base64(parameters('" + param + "')),'") + "')]";
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

                                        var token = action.Value.SelectToken("inputs.path");

                                        var replaced = token.Value<string>().Replace($"'{base64string}'", $"', parameters('__apostrophe'), base64(parameters('{param}')), parameters('__apostrophe'), '");
                                        var newValue = $"[concat('{replaced}')]";

                                        meta.Parent.Parent["inputs"]["path"] = newValue;

                                        AddTemplateParameter("__apostrophe", "string", "'");
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
                                        try
                                        {

                                            var base64string = ((JProperty)meta.First).Name;
                                            var param = AddParameterForMetadataBase64(meta, trigger.Name + "-folderPath");
                                            meta.Parent.Parent["inputs"]["queries"]["folderId"] = "[base64(parameters('" + param + "'))]";
                                        }
                                        catch (FormatException ex)
                                        {

                                            //folderid is not a valid base64 so we are skipping it for now
                                            /*var path = ((JProperty)meta.First).Value.ToString();
                                             var param = AddTemplateParameter(trigger.Name + "-folderPath","string",path);
                                             meta[((JProperty)meta.First).Name] = $"[parameters('{param}')]";*/
                                        }
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

        private string AddParameterForMetadataBase64(JObject meta, string parametername)
        {
            var base64string = ((JProperty)meta.First).Name;
            var path = Encoding.UTF8.GetString(Convert.FromBase64String(base64string));
            var param = AddTemplateParameter(parametername, "string", path);
            meta.Remove(((JProperty)meta.First).Name);
            meta.Add("[base64(parameters('" + param + "'))]", JToken.Parse("\"[parameters('" + param + "')]\""));

            return param;
        }

        private void HandledMetaDataFilePaths(JObject definition, JProperty action)
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

      
        
        public JObject generateConnectionTemplate(JObject connectionResource, JObject connectionInstance, string connectionName,string concatedId, string connectionNameParam)
        {
            //create template
            var connectionTemplate = new Models.ConnectionTemplate(connectionNameParam, concatedId);           
            //displayName            
            connectionTemplate.properties.displayName = $"[parameters('{AddTemplateParameter(connectionName+ "_displayName", "string", (string)connectionInstance["properties"]["displayName"])}')]";
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


                        if (parameter.Name == "accessKey" && concatedId.EndsWith("/azureblob')]"))
                        {
                            connectionParameters.Add(parameter.Name, $"[listKeys(resourceId('Microsoft.Storage/storageAccounts', parameters('{connectionName}_accountName')), providers('Microsoft.Storage', 'storageAccounts').apiVersions[0]).keys[0].value]");
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
