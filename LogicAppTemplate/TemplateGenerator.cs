using LogicAppTemplate.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace LogicAppTemplate
{

    public class TemplateGenerator
    {

        private DeploymentTemplate template;
        private JObject workflowTemplateReference;

        private string LogicAppResourceGroup;
        private bool stripPassword = false;
        IResourceCollector resourceCollector;
        private string SubscriptionId;
        private string ResourceGroup;
        private string LogicApp;
        private string IntegrationAccountId;
        private bool extractIntegrationAccountArtifacts = false;
        private bool disabledState = false;

        public TemplateGenerator(string LogicApp, string SubscriptionId, string ResourceGroup, IResourceCollector resourceCollector, bool stripPassword = false, bool disabledState = false)
        {
            this.SubscriptionId = SubscriptionId;
            this.ResourceGroup = ResourceGroup;
            this.LogicApp = LogicApp;
            this.resourceCollector = resourceCollector;
            this.stripPassword = stripPassword;
            this.disabledState = disabledState;
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

        public bool AccessControl { get; set; }
        public bool ForceAccessControl { get; set; }
        public bool DiagnosticSettings { get; set; }
        public bool IncludeInitializeVariable { get; set; }
        public bool FixedFunctionAppName { get; set; }
        public bool GenerateHttpTriggerUrlOutput { get; set; }
        public bool ForceManagedIdentity { get; set; }
        public bool DisableConnectionsOutput { get; set; }
        public bool DisableTagParameters { get; set; }
        public bool DisableFunctionNameParameters { get; set; }
        public bool IncludeEvaluatedRecurrence { get; set; }
        public bool SkipOauthConnectionAuthorization { get; set; }
        public bool UseServiceBusDisplayName { get; set; }
        public bool OnlyParameterizeConnections = false;
        public bool GenerateManagedIdentityRoleAssignment { get; set; }
        public bool ParameterizeApiDefinitionUrl { get; set; }

        public async Task<JObject> GenerateTemplate()
        {
            JObject _definition = await resourceCollector.GetResource($"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup}/providers/Microsoft.Logic/workflows/{LogicApp}", "2019-05-01");
            return await generateDefinition(_definition, !DisableConnectionsOutput);
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
                var IntegrationAccountAzureResourceId = new AzureResourceId(IntegrationAccountId);
                if (IntegrationAccountAzureResourceId.ResourceGroupName.ToLower() != rid.ResourceGroupName.ToLower())
                {
                    template.parameters["IntegrationAccountResourceGroupName"]["defaultValue"] = IntegrationAccountAzureResourceId.ResourceGroupName;
                }
            }

            //ISE
            if (definition["properties"]["integrationServiceEnvironment"] == null)
            {
                ((JObject)template.resources[0]["properties"]).Remove("integrationServiceEnvironment");
                template.parameters.Remove("integrationServiceEnvironmentName");
                template.parameters.Remove("integrationServiceEnvironmentResourceGroupName");
            }
            else
            {
                template.parameters["integrationServiceEnvironmentName"]["defaultValue"] = definition["properties"]["integrationServiceEnvironment"]["name"];
                AzureResourceId iseId = new AzureResourceId(definition["properties"]["integrationServiceEnvironment"].Value<string>("id"));
                template.parameters["integrationServiceEnvironmentResourceGroupName"]["defaultValue"] = iseId.ResourceGroupName;
            }

            if (disabledState)
            {
                ((JObject)template.resources[0]["properties"]).Add("state", "Disabled");
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

            if (definition.ContainsKey("tags"))
            {
                JToken tags = await HandleTags(definition);
                if (tags.HasValues)
                {
                    workflowTemplateReference.Add("tags", tags);
                }
            }

            // Access Control
            var accessControl = (JObject)definition["properties"]["accessControl"];
            if ((ForceAccessControl || AccessControl) && (accessControl != null && accessControl.ToString() != "{}"))
            {

                if ((accessControl["triggers"]?["allowedCallerIpAddresses"] as JArray)?.Any() is true)
                {
                    var triggerAllowedCallerIpAddresses = AddTemplateParameter("trigger_allowedCallerIpAddresses", "array", accessControl["triggers"]["allowedCallerIpAddresses"]);
                    accessControl["triggers"]["allowedCallerIpAddresses"] = $"[parameters('{triggerAllowedCallerIpAddresses}')]";
                }

                if ((accessControl?["contents"]?["allowedCallerIpAddresses"] as JArray)?.Any() is true)
                {
                    var contentsAllowedCallerIpAddresses = AddTemplateParameter("contents_allowedCallerIpAddresses", "array", accessControl["contents"]["allowedCallerIpAddresses"]);
                    accessControl["contents"]["allowedCallerIpAddresses"] = $"[parameters('{contentsAllowedCallerIpAddresses}')]";
                }

                if ((accessControl["actions"]?["allowedCallerIpAddresses"] as JArray)?.Any() is true)
                {
                    var actionAllowedCallerIpAddresses = AddTemplateParameter("action_allowedCallerIpAddresses", "array", accessControl["actions"]["allowedCallerIpAddresses"]);
                    accessControl["actions"]["allowedCallerIpAddresses"] = $"[parameters('{actionAllowedCallerIpAddresses}')]";
                }

                workflowTemplateReference["properties"]["accessControl"] = accessControl;
            }
            else if (ForceAccessControl && accessControl == null || accessControl?.ToString() == "{}")
            {
                //Check for triggerKind eventgrid. In that case we don't want to add whitelisting, because we will get handshake errors 
                var triggerKind = ((JProperty)workflowTemplateReference["properties"]?["definition"]?["triggers"]?.FirstOrDefault())?.Value["kind"]?.Value<string>();
                if (triggerKind != "EventGrid")
                {
                    workflowTemplateReference["properties"]["accessControl"] = JObject.Parse(@"{""triggers"":{""allowedCallerIpAddresses"":[]},""actions"":{""allowedCallerIpAddresses"":[]}}");
                }

            }

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

                    template.parameters.Add(name, JObject.FromObject(new { type = parameter.Value["type"].Value<string>().ToLower(), defaultValue = (parameter.Value["type"].Value<string>().ToLower() == "securestring") ? string.Empty : parameter.Value["defaultValue"] }));
                    parameter.Value["defaultValue"] = "[parameters('" + name + "')]";
                }
            }

            var managedIdentity = (JObject)definition["identity"];

            JObject userAssignedIdentities = null;
            if (ForceManagedIdentity || (managedIdentity != null && managedIdentity.Value<string>("type") == "SystemAssigned"))
            {
                template.resources[0].Add("identity", JObject.Parse("{'type': 'SystemAssigned'}"));
            }
            else if (managedIdentity != null && managedIdentity.Value<string>("type") == "UserAssigned")
            {
                //Extract user assigned managed identity info
                var identities = ((JObject)managedIdentity["userAssignedIdentities"]).Properties().ToList();
                var identity = new AzureResourceId(identities[0].Name);

                //Add ARM parameter to configure the user assigned identity
                template.parameters.Add(Constants.UserAssignedIdentityParameterName, JObject.FromObject(new { type = "string", defaultValue = identity.ResourceName }));

                //When the identity exists in a different resourcegroup add this as parameter and in the resourceId() function
                var identityResourceGroupAddtion = "";
                if (LogicAppResourceGroup != identity.ResourceGroupName)
                {
                    identityResourceGroupAddtion = $"parameters('{Constants.UserAssignedIdentityParameterName}_resourceGroup'),";
                    template.parameters.Add($"{Constants.UserAssignedIdentityParameterName}_resourceGroup", JObject.FromObject(new { type = "string", defaultValue = identity.ResourceGroupName }));
                }


                //Create identity object for ARM template
                userAssignedIdentities = new JObject();
                userAssignedIdentities.Add($"[resourceId({identityResourceGroupAddtion}'Microsoft.ManagedIdentity/userAssignedIdentities/', parameters('{Constants.UserAssignedIdentityParameterName}'))]", JObject.FromObject(new { }));

                var userAssignedIdentity = new JObject();
                userAssignedIdentity.Add("type", "UserAssigned");
                userAssignedIdentity.Add("userAssignedIdentities", userAssignedIdentities);

                //Add identity object to Logic App resource
                template.resources[0].Add("identity", userAssignedIdentity);
            }

            //Add ManagedIdentityRoleAssignment
            if (GenerateManagedIdentityRoleAssignment && managedIdentity?.Value<string>("type") == "SystemAssigned")
            {
                var principalId = managedIdentity.Value<string>("principalId");
                var filter = $"assignedTo('{principalId}')";
                var roles = (await resourceCollector.GetRoles($"subscriptions/{rid.SubscriptionId}", filter)).ToList();

                if (roles.Any())
                {
                    foreach (var roleByScope in roles.Select(t => t.ToObject<Models.RoleAssignmentsTemplate>()).GroupBy(s => s.Properties.Scope))
                    {
                        //create template
                        var scope = new AzureResourceId(roleByScope.Key);
                        var roleAssignmentsResourceGroupName = AddTemplateParameter($"{scope.Provider.Item2}_ResourceGroupName", "string", scope.ResourceGroupName);

                        var deploymentTemplate = new DeploymentTemplates($"[concat(parameters('logicAppName'), '_roles')]", $"[parameters('{roleAssignmentsResourceGroupName}')]");

                        foreach (var roleAssignmentTemplate in roleByScope)
                        {
                            deploymentTemplate.AddResource(roleAssignmentTemplate.GenerateJObject(AddTemplateParameter, roleAssignmentsResourceGroupName));
                        }

                        template.resources.Add(deploymentTemplate.ToJObject());
                    }
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

                var connectionProperties = connectionProperty.First["connectionProperties"] as JObject;
                if (connectionProperties != null)
                {
                    //replace identity value with parsed one for userAssignedIdentities
                    if (connectionProperties.ContainsKey("authentication"))
                    {
                        if (connectionProperties["authentication"]?["identity"]?.Value<string>()?.Contains("userAssignedIdentities") == true)
                        {
                            connectionProperties["authentication"] = JObject.FromObject(new
                            {
                                type = connectionProperties["authentication"]?["type"],
                                identity = ((JProperty)userAssignedIdentities.First).Name
                            });
                        }
                    }
                }

                //fixes old templates where name sometimes is missing

                var connectionNameParam = AddTemplateParameter($"{connectionName}_name", "string", connectionName);

                AzureResourceId cid;

                // Check if id contains different parameter than connectionname
                var idarray = id.Split('/');
                string candidate = idarray.Last();
                string type = idarray[idarray.Count() - 2];
                if (type.Equals("customApis"))
                {
                    var idparam = AddTemplateParameter($"{connectionName}_api", "string", candidate);
                    cid = apiIdTemplate(id, idparam);
                }
                else
                {
                    cid = apiIdTemplate(id, connectionNameParam);
                }
                string concatedId = $"[concat('{cid.ToString()}')]";

                workflowTemplateReference["properties"]["parameters"]["$connections"]["value"][name] = JObject.FromObject(new
                {
                    id = concatedId,
                    connectionId = $"[resourceId('Microsoft.Web/connections', parameters('{connectionNameParam}'))]",
                    connectionName = $"[parameters('{connectionNameParam}')]"
                });

                //Add connectionProperties if not null
                if (connectionProperties != null)
                {
                    ((JObject)workflowTemplateReference["properties"]["parameters"]["$connections"]["value"][name])
                        .Add("connectionProperties", connectionProperties);
                }

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
            JObject resources = await resourceCollector.GetResource("https://management.azure.com" + definition.Value<string>("id") + "/providers/microsoft.insights/diagnosticSettings", "2021-05-01-preview");

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

        private AzureResourceId apiIdTemplate(string apiId, string connectionNameParameter)
        {
            var rid = new AzureResourceId(apiId);
            rid.SubscriptionId = "',subscription().subscriptionId,'";
            if (apiId.Contains("/managedApis/"))
            {
                rid.ReplaceValueAfter("locations", "',parameters('logicAppLocation'),'");
            }
            else
            {
                if (apiId.Contains("customApis"))
                {
                    rid.ReplaceValueAfter("customApis", "',parameters('" + connectionNameParameter + "'),'");
                }
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
                else if (type == "initializevariable" && IncludeInitializeVariable && definition["actions"][action.Name]["inputs"]["variables"][0]["value"] != null)
                {
                    var variableType = definition["actions"][action.Name]["inputs"]["variables"][0]["type"];
                    string paramType = string.Empty;

                    //missing securestring & secureObject
                    switch (variableType.Value<string>())
                    {
                        case "Array":
                        case "Object":
                        case "String":
                            paramType = variableType.Value<string>().ToLower();
                            break;
                        case "Boolean":
                            paramType = "bool";
                            break;
                        case "Float":
                            paramType = "string";
                            break;
                        case "Integer":
                            paramType = "int";
                            break;
                        default:
                            paramType = "string";
                            break;
                    }

                    //Arrays and Objects can't be expressions 
                    if (definition["actions"][action.Name]["inputs"]["variables"][0]["value"].Type != JTokenType.Array
                        && definition["actions"][action.Name]["inputs"]["variables"][0]["value"].Type != JTokenType.Object)
                    {
                        //If variable is an expression OR float, we need to change the type of the parameter to string
                        if (definition["actions"][action.Name]["inputs"]["variables"][0].Value<string>("value").StartsWith("@")
                            || variableType.Value<string>() == "Float")
                        {
                            definition["actions"][action.Name]["inputs"]["variables"][0]["value"] = "[parameters('" + AddTemplateParameter(action.Name + "-Value", "string", ((JObject)definition["actions"][action.Name]["inputs"]["variables"][0]).Value<string>("value")) + "')]";
                        }
                        else
                        {
                            //Same as the one from in the outer if sentence
                            definition["actions"][action.Name]["inputs"]["variables"][0]["value"] = "[parameters('" + AddTemplateParameter(action.Name + "-Value", paramType, definition["actions"][action.Name]["inputs"]["variables"][0]["value"]) + "')]";
                        }
                    }
                    else
                    {
                        definition["actions"][action.Name]["inputs"]["variables"][0]["value"] = "[parameters('" + AddTemplateParameter(action.Name + "-Value", paramType, definition["actions"][action.Name]["inputs"]["variables"][0]["value"]) + "')]";
                    }
                }
                else if (type == "apimanagement")
                {
                    var apiId = ((JObject)definition["actions"][action.Name]["inputs"]["api"]).Value<string>("id");
                    var aaid = new AzureResourceId(apiId);


                    aaid.SubscriptionId = "',subscription().subscriptionId,'";
                    aaid.ResourceGroupName = "', parameters('" + AddTemplateParameter("apimResourceGroup", "string", aaid.ResourceGroupName) + "'),'";
                    aaid.ReplaceValueAfter("service", "', parameters('" + AddTemplateParameter("apimInstanceName", "string", aaid.ValueAfter("service")) + "'),'");
                    aaid.ReplaceValueAfter("apis", "', parameters('" + AddTemplateParameter($"api_{aaid.ValueAfter("apis")}_name", "string", aaid.ValueAfter("apis")) + "'),'");
                    apiId = "[concat('" + aaid.ToString() + "')]";

                    definition["actions"][action.Name]["inputs"]["api"]["id"] = apiId;

                    //handle subscriptionkey if not parematrized
                    var subkey = ((JObject)definition["actions"][action.Name]["inputs"]).Value<string>("subscriptionKey");
                    if (subkey != null && !Regex.Match(subkey, @"parameters\('(.*)'\)").Success)
                    {
                        definition["actions"][action.Name]["inputs"]["subscriptionKey"] = "[parameters('" + AddTemplateParameter("apimSubscriptionKey", "string", subkey) + "')]";
                    }
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
                }
                else if (type == "http")
                {
                    var apiDefinitionUrl = definition["actions"]?[action.Name]?["metadata"]?["apiDefinitionUrl"];
                    if (ParameterizeApiDefinitionUrl)
                    {
                        if (apiDefinitionUrl != null)
                        {
                            //get hostname from apiDefinitionUrl
                            var apiDefinitionUri =
                                new Uri(((JObject)definition["actions"][action.Name]["metadata"]).Value<string>(
                                    "apiDefinitionUrl"));
                            var apiDefinitionHostname = apiDefinitionUri.GetLeftPart(UriPartial.Authority);
                            var apiDefinitionHostnameParam =
                                AddTemplateParameter(action.Name + "-ApiDefinitionUrlHostname", "string",
                                    apiDefinitionHostname);
                            var apiDefinitionUriPathAndQuery = apiDefinitionUri.AbsolutePath;
                            if (!string.IsNullOrEmpty(apiDefinitionUri.Query))
                            {
                                var queryDictionary = HttpUtility.ParseQueryString(apiDefinitionUri.Query);

                                var querySeperator = "?";
                                foreach (string key in queryDictionary)
                                {
                                    var value = queryDictionary.Get(key);
                                    var queryParameterName = AddTemplateParameter(action.Name + "-ApiDefinition-" + key,
                                        "string", value);

                                    apiDefinitionUriPathAndQuery += querySeperator + key + "=', parameters('" +
                                                                    queryParameterName + "')";
                                    querySeperator = "&";
                                }

                            }
                            else
                            {
                                apiDefinitionUriPathAndQuery += "'";
                            }

                            definition["actions"][action.Name]["metadata"]["apiDefinitionUrl"] =
                                "[concat(parameters('" + apiDefinitionHostnameParam + "'), '" +
                                apiDefinitionUriPathAndQuery + ")]";

                            var apiUri =
                                new Uri(((JObject)definition["actions"][action.Name]["inputs"]).Value<string>("uri"));
                            var apiHostname = apiDefinitionUri.GetLeftPart(UriPartial.Authority);
                            var apiHostnameParam = apiDefinitionHostname == apiHostname
                                ? apiDefinitionHostnameParam
                                : AddTemplateParameter(action.Name + "-UriHostname", "string", apiHostname);

                            var pathAndQuery = apiUri.PathAndQuery.Replace("'", "', parameters('__apostrophe'), '");

                            definition["actions"][action.Name]["inputs"]["uri"] = "[concat(parameters('" +
                                apiHostnameParam + "'), '" + pathAndQuery + "')]";
                            AddTemplateParameter("__apostrophe", "string", "'");
                        }

                        //only add when not parameterized yet
                        else if (!Regex.IsMatch(((JObject)definition["actions"][action.Name]["inputs"]).Value<string>("uri"), @"parameters\('.*'\)"))
                            definition["actions"][action.Name]["inputs"]["uri"] = "[parameters('" + AddTemplateParameter(action.Name + "-URI", "string", ((JObject)definition["actions"][action.Name]["inputs"]).Value<string>("uri")) + "')]";

                    }
                    else
                    //only add when not parameterized yet
                    if (!Regex.IsMatch(((JObject)definition["actions"][action.Name]["inputs"]).Value<string>("uri"), @"parameters\('.*'\)"))
                        definition["actions"][action.Name]["inputs"]["uri"] = "[parameters('" + AddTemplateParameter(action.Name + "-URI", "string", ((JObject)definition["actions"][action.Name]["inputs"]).Value<string>("uri")) + "')]";



                    //var metadata = ((JObject)definition["actions"][action.Name]["inputs"]["api"]).Value<string>("id");


                    var authenticationObj = (JObject)definition["actions"][action.Name]["inputs"]["authentication"];
                    if (authenticationObj != null)
                    {
                        var authType = authenticationObj.Value<string>("type");
                        if ("Basic".Equals(authType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            //only add when not parameterized yet
                            if (!Regex.IsMatch(((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("password"), @"parameters\('.*'\)"))
                                definition["actions"][action.Name]["inputs"]["authentication"]["password"] = "[parameters('" + AddTemplateParameter(action.Name + "-Password", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("password")) + "')]";
                            //only add when not parameterized yet
                            if (!Regex.IsMatch(((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("username"), @"parameters\('.*'\)"))
                                definition["actions"][action.Name]["inputs"]["authentication"]["username"] = "[parameters('" + AddTemplateParameter(action.Name + "-Username", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("username")) + "')]";
                        }
                        else if ("ClientCertificate".Equals(authType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            definition["actions"][action.Name]["inputs"]["authentication"]["password"] = "[parameters('" + AddTemplateParameter(action.Name + "-Password", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("password")) + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["pfx"] = "[parameters('" + AddTemplateParameter(action.Name + "-Pfx", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("pfx")) + "')]";
                        }
                        else if ("ActiveDirectoryOAuth".Equals(authType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            //only add when not parameterized yet
                            if (!Regex.IsMatch(((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("audience"), @"parameters\('.*'\)"))
                                definition["actions"][action.Name]["inputs"]["authentication"]["audience"] = "[parameters('" + AddTemplateParameter(action.Name + "-Audience", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("audience")) + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["authority"] = "[parameters('" + AddTemplateParameter(action.Name + "-Authority", "string", (((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("authority")) ?? "") + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["clientId"] = "[parameters('" + AddTemplateParameter(action.Name + "-ClientId", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("clientId")) + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["secret"] = "[parameters('" + AddTemplateParameter(action.Name + "-Secret", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("secret")) + "')]";
                            definition["actions"][action.Name]["inputs"]["authentication"]["tenant"] = "[parameters('" + AddTemplateParameter(action.Name + "-Tenant", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("tenant")) + "')]";
                        }
                        else if ("Raw".Equals(authType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            definition["actions"][action.Name]["inputs"]["authentication"]["value"] = "[parameters('" + AddTemplateParameter(action.Name + "-Raw", "string", ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("value")) + "')]";
                        }
                        else if ("ManagedServiceIdentity".Equals(authType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            var msiAudience = ((JObject)definition["actions"][action.Name]["inputs"]["authentication"]).Value<string>("audience");

                            //only add when not parameterized yet
                            //audience is not mandatory!
                            if (msiAudience != null && !Regex.IsMatch(msiAudience, @"parameters\('.*'\)"))
                            {
                                definition["actions"][action.Name]["inputs"]["authentication"]["audience"] = "[parameters('" + AddTemplateParameter(action.Name + "-Audience", "string", msiAudience) + "')]";
                            }

                            if (definition["actions"][action.Name]["inputs"]["authentication"]["identity"] != null)
                            {

                                //When the identity exists in a different resourcegroup add this as parameter and in the resourceId() function
                                var identityResource = new AzureResourceId(definition["actions"][action.Name]["inputs"]["authentication"]["identity"].Value<string>());
                                var identityResourceGroupAddtion = "";
                                if (LogicAppResourceGroup != identityResource.ResourceGroupName)
                                {
                                    identityResourceGroupAddtion = $"parameters('{Constants.UserAssignedIdentityParameterName}_resourceGroup'),";
                                    //template.parameters.Add($"{Constants.UserAssignedIdentityParameterName}_resourceGroup", JObject.FromObject(new { type = "string", defaultValue = identityResource.ResourceGroupName }));
                                }

                                //User Assigned Identity
                                definition["actions"][action.Name]["inputs"]["authentication"]["identity"] = $"[resourceId({identityResourceGroupAddtion}'Microsoft.ManagedIdentity/userAssignedIdentities/', parameters('{Constants.UserAssignedIdentityParameterName}'))]";
                            }
                        }

                    }
                }
                else if (type == "function")
                {
                    var curr = ((JObject)definition["actions"][action.Name]["inputs"]["function"]).Value<string>("id");
                    var faid = new AzureResourceId(curr);

                    var resourcegroupValue = LogicAppResourceGroup == faid.ResourceGroupName ? "[resourceGroup().name]" : faid.ResourceGroupName;


                    faid.SubscriptionId = "',subscription().subscriptionId,'";
                    faid.ResourceGroupName = "',parameters('" + AddTemplateParameter((FixedFunctionAppName ? "FunctionApp-" : action.Name + "-") + "ResourceGroup", "string", resourcegroupValue) + "'),'";
                    faid.ReplaceValueAfter("sites", "',parameters('" + AddTemplateParameter((FixedFunctionAppName ? "" : action.Name + "-") + "FunctionApp", "string", faid.ValueAfter("sites")) + "'),'");

                    if (DisableFunctionNameParameters)
                    {
                        faid.ReplaceValueAfter("functions", faid.ValueAfter("functions") + "'");
                    }
                    else
                    {
                        faid.ReplaceValueAfter("functions", "',parameters('" + AddTemplateParameter((FixedFunctionAppName ? "" : action.Name + "-") + "FunctionName", "string", faid.ValueAfter("functions")) + "')");
                    }
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
                        var connectioname = definition["actions"][action.Name]["inputs"]["host"]["connection"].Value<string>("name");

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
                            case "dynamicsax":
                                {
                                    var inputs = action.Value.Value<JObject>("inputs");
                                    var path = inputs.Value<string>("path").Replace("'", "''");
                                    var pathsubsets = path.Split('/');
                                    var dataset = pathsubsets[2];

                                    var m = Regex.Match(dataset, @"\(''(.*)''\)");
                                    if (m.Groups.Count > 1)
                                    {
                                        var datasetName = m.Groups[1].Value;
                                        var param = AddTemplateParameter(action.Name + "-instance", "string", datasetName);
                                        inputs["path"] = "[concat('" + path.Replace($"''{datasetName}''", $"', parameters('__apostrophe'), parameters('{param}'), parameters('__apostrophe'), '") + "')]";
                                        AddTemplateParameter("__apostrophe", "string", "'");
                                    }

                                    break;
                                }
                            case "commondataservice":
                            case "dynamicscrmonline":
                                {
                                    var inputs = action.Value.Value<JObject>("inputs");
                                    var path = inputs.Value<string>("path").Replace("'", "''");
                                    var pathsubsets = Regex.Split(path, "(?<!/)/(?!/)");
                                    var dataset = "";

                                    var datasetIndex = Array.FindIndex(pathsubsets, pathsubset => pathsubset == "datasets") + 1;

                                    if (datasetIndex < pathsubsets.Length)
                                    {
                                        dataset = pathsubsets[datasetIndex];
                                    }

                                    var m = Regex.Match(dataset, @"\(''(.*)''\)");
                                    if (m.Groups.Count > 1)
                                    {
                                        var datasetName = m.Groups[1].Value;
                                        var param = AddTemplateParameter(action.Name + "-environment", "string", datasetName);
                                        inputs["path"] = "[concat('" + path.Replace($"''{datasetName}''", $"', parameters('__apostrophe'), parameters('{param}'), parameters('__apostrophe'), '") + "')]";
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
                                        inputs["path"] = "[concat('" + path.Replace("'", "''").Replace($"'{queuename}'", $"'', parameters('{param}'), ''") + "')]";
                                    }
                                    break;
                                }
                            case "azuredatafactory":
                                {
                                    AddTemplateParameter("__apostrophe", "string", "'");
                                    var path = action.Value["inputs"].Value<string>("path");

                                    var splittedPath = path.Split('/');
                                    splittedPath[2] = "@{encodeURIComponent(', parameters('__apostrophe'),subscription().subscriptionId, parameters('__apostrophe'),')}";

                                    var rgMatch = Regex.Match(splittedPath[4], "('(?<name>.*)')");
                                    var rgName = rgMatch.Groups["name"].Value;

                                    AddTemplateParameter("__apostrophe", "string", "'");
                                    splittedPath[4] = "@{encodeURIComponent(', parameters('__apostrophe'),parameters('" + AddTemplateParameter(action.Name + "_ADF__ResourceGroup", "string", rgName) + "'), parameters('__apostrophe'),')}";

                                    var adfMatch = Regex.Match(splittedPath[8], "('(?<name>.*)')");
                                    var adfName = adfMatch.Groups["name"].Value;

                                    splittedPath[8] = "@{encodeURIComponent(', parameters('__apostrophe'),parameters('" + AddTemplateParameter(action.Name + "_ADF__Instance", "string", adfName) + "'), parameters('__apostrophe'),')}";

                                    var pipelineMatch = Regex.Match(splittedPath[10], "('(?<name>.*)')");
                                    var pipelineName = pipelineMatch.Groups["name"].Value;

                                    splittedPath[10] = "@{encodeURIComponent(', parameters('__apostrophe'),parameters('" + AddTemplateParameter(action.Name + "_ADF__Pipeline", "string", pipelineName) + "'), parameters('__apostrophe'),')}";

                                    //"/subscriptions/@{encodeURIComponent('04a4ca35-9cb5-4652-b245-f1782aa43b25')}/resourcegroups/@{encodeURIComponent('azne-per-adf-dev02-rg')}/providers/Microsoft.DataFactory/factories/@{encodeURIComponent('azne-per-adf-dev02')}/pipelines/@{encodeURIComponent('INT159-FinancialData-Cognos')}/CreateRun",
                                    //replace for gui
                                    action.Value["inputs"]["path"] = "[concat('" + string.Join("/", splittedPath) + "')]";


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
                            case "dynamicsax":
                                {

                                    var inputs = trigger.Value.Value<JObject>("inputs");
                                    var path = inputs.Value<string>("path").Replace("'", "''");
                                    var pathsubsets = path.Split('/');
                                    var dataset = pathsubsets[2];

                                    var m = Regex.Match(dataset, @"\(''(.*)''\)");
                                    if (m.Groups.Count > 1)
                                    {
                                        var datasetName = m.Groups[1].Value;
                                        var param = AddTemplateParameter(trigger.Name + "-instance", "string", datasetName);
                                        inputs["path"] = "[concat('" + path.Replace($"''{datasetName}''", $"', parameters('__apostrophe'), parameters('{param}'), parameters('__apostrophe'), '") + "')]";
                                        AddTemplateParameter("__apostrophe", "string", "'");
                                    }

                                    break;
                                }
                            case "commondataservice":
                            case "dynamicscrmonline":
                                {

                                    var inputs = trigger.Value.Value<JObject>("inputs");
                                    var path = inputs.Value<string>("path").Replace("'", "''");
                                    var pathsubsets = Regex.Split(path, "(?<!/)/(?!/)");
                                    var dataset = "";

                                    var datasetIndex = Array.FindIndex(pathsubsets, pathsubset => pathsubset == "datasets") + 1;

                                    if (datasetIndex < pathsubsets.Length)
                                    {
                                        dataset = pathsubsets[datasetIndex];
                                    }

                                    var m = Regex.Match(dataset, @"\(''(.*)''\)");
                                    if (m.Groups.Count > 1)
                                    {
                                        var datasetName = m.Groups[1].Value;
                                        var param = AddTemplateParameter(trigger.Name + "-environment", "string", datasetName);
                                        inputs["path"] = "[concat('" + path.Replace($"''{datasetName}''", $"', parameters('__apostrophe'), parameters('{param}'), parameters('__apostrophe'), '") + "')]";
                                        AddTemplateParameter("__apostrophe", "string", "'");
                                    }

                                    break;
                                }
                            case "azureeventgrid":
                                {
                                    var ri = new AzureResourceId(trigger.Value["inputs"]["body"]["properties"].Value<string>("topic"));
                                    AddTemplateParameter("__apostrophe", "string", "'");
                                    var path = trigger.Value["inputs"].Value<string>("path");
                                    path = path.Replace("'", "', parameters('__apostrophe'),'");

                                    //replace for gui
                                    trigger.Value["inputs"]["path"] = "[concat('" + path.Replace($"'{ri.SubscriptionId}'", $"subscription().subscriptionId") + "')]";

                                    ri.SubscriptionId = "',subscription().subscriptionId,'";
                                    ri.ResourceGroupName = "',parameters('" + AddTemplateParameter(ri.ResourceName + "_ResourceGroup", "string", ri.ResourceGroupName) + "'),'";
                                    ri.ResourceName = "',parameters('" + AddTemplateParameter(ri.ResourceName + "_Name", "string", ri.ResourceName) + "')";
                                    //replace for topic
                                    trigger.Value["inputs"]["body"]["properties"]["topic"] = "[concat('" + ri.ToString() + ")]";
                                    break;
                                }
                        }
                    }



                    //promote parameters for reccurence settings
                    var recurrence = trigger.Value.SelectToken("recurrence");
                    if (recurrence != null)
                    {
                        if (ShouldAddTemplateParameter(recurrence["frequency"]))
                        {
                            definition["triggers"][trigger.Name]["recurrence"]["frequency"] = "[parameters('" + this.AddTemplateParameter(trigger.Name + "Frequency", "string", recurrence.Value<string>("frequency")) + "')]";
                        }
                        if (ShouldAddTemplateParameter(recurrence["interval"]))
                        {
                            definition["triggers"][trigger.Name]["recurrence"]["interval"] = "[parameters('" + this.AddTemplateParameter(trigger.Name + "Interval", "int", new JProperty("defaultValue", recurrence.Value<int>("interval"))) + "')]";
                        }
                        if (ShouldAddTemplateParameter(recurrence["startTime"]))
                        {
                            definition["triggers"][trigger.Name]["recurrence"]["startTime"] = "[parameters('" + this.AddTemplateParameter(trigger.Name + "StartTime", "string", recurrence["startTime"]) + "')]";
                        }
                        if (ShouldAddTemplateParameter(recurrence["timeZone"]))
                        {
                            definition["triggers"][trigger.Name]["recurrence"]["timeZone"] = "[parameters('" + this.AddTemplateParameter(trigger.Name + "TimeZone", "string", recurrence.Value<string>("timeZone")) + "')]";
                        }
                        if (ShouldAddTemplateParameter(recurrence["schedule"]))
                        {
                            definition["triggers"][trigger.Name]["recurrence"]["schedule"] = "[parameters('" + this.AddTemplateParameter(trigger.Name + "Schedule", "Object", new JProperty("defaultValue", recurrence["schedule"])) + "')]";
                        }
                    }

                    if (!IncludeEvaluatedRecurrence)
                    {
                        //remove the evaluatedRecurrence, we don't need it in the source
                        var evaluatedRecurrence = trigger.Value.SelectToken("evaluatedRecurrence");
                        if (evaluatedRecurrence != null)
                        {
                            ((JObject)definition["triggers"][trigger.Name]).Remove("evaluatedRecurrence");
                        }
                    }

                    //parse query parameters
                    var queries = trigger.Value.SelectToken("inputs.queries");
                    if (queries != null)
                    {
                        foreach (var jToken in queries)
                        {
                            try
                            {
                                var query = jToken as JProperty;
                                var queryValue = query?.Value?.Value<string>();

                                //[ at the beginning has to be escaped with a extra [
                                if (query.HasValues && queryValue.StartsWith("["))
                                {
                                    definition["triggers"][trigger.Name]["inputs"]["queries"][query.Name] = queryValue = "[" + queryValue;
                                }
                            }
                            catch (FormatException ex)
                            {
                            }
                        }

                        break;
                    }

                    var type = trigger.Value.SelectToken("type")?.Value<string>()?.ToLower();
                    switch (type)
                    {
                        // http trigger
                        case "request":
                            {
                                var kind = trigger.Value.SelectToken("kind")?.Value<string>()?.ToLower();
                                if (kind == "http")
                                {
                                    if (this.GenerateHttpTriggerUrlOutput)
                                    {
                                        var outputValue = JObject.FromObject(new
                                        {
                                            type = "string",
                                            value = $"[listCallbackURL(concat(resourceId(resourceGroup().name,'Microsoft.Logic/workflows/', parameters('logicAppName')), '/triggers/{trigger.Name}'), '2016-06-01').value]"
                                        });

                                        this.template.outputs.Add("httpTriggerUrl", outputValue);
                                    }
                                }

                                break;
                            }
                        // apimanagement trigger
                        case "apimanagement":
                            {
                                var apiId = ((JObject)definition["triggers"][trigger.Name]["inputs"]["api"]).Value<string>("id");
                                var aaid = new AzureResourceId(apiId)
                                {
                                    SubscriptionId = "',subscription().subscriptionId,'"
                                };

                                aaid.ResourceGroupName = "', parameters('" + AddTemplateParameter("apimResourceGroup", "string", aaid.ResourceGroupName) + "'),'";
                                aaid.ReplaceValueAfter("service", "', parameters('" + AddTemplateParameter("apimInstanceName", "string", aaid.ValueAfter("service")) + "'),'");
                                aaid.ReplaceValueAfter("apis", "', parameters('" + AddTemplateParameter($"api_{aaid.ValueAfter("apis")}_name", "string", aaid.ValueAfter("apis")) + "'),'");
                                apiId = "[concat('" + aaid.ToString() + "')]";

                                definition["triggers"][trigger.Name]["inputs"]["api"]["id"] = apiId;

                                //handle subscriptionkey if not parematrized
                                var subkey = ((JObject)definition["triggers"][trigger.Name]["inputs"]).Value<string>("subscriptionKey");
                                if (subkey != null && !Regex.Match(subkey, @"parameters\('(.*)'\)").Success)
                                {
                                    definition["triggers"][trigger.Name]["inputs"]["subscriptionKey"] = "[parameters('" + AddTemplateParameter("apimSubscriptionKey", "string", subkey) + "')]";
                                }

                                break;
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
                return currentValue;

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
            if (currentValue == base64string)
            {
                return $"[base64(parameters('{param}'))]";
            }
            var replaced = currentValue.Replace("'", "''").Replace($"{base64string}", $"', base64(parameters('{param}')), '");

            var newValue = $"[concat('{replaced}')]";
            //AddTemplateParameter("__apostrophe", "string", "'");

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
                var match = Regex.Match(name, @"@parameters\('\$connections'\)\['(?<connectionname>\w*)'");
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

            if (this.stripPassword && (paramname.EndsWith("Username", StringComparison.InvariantCultureIgnoreCase) || paramname.EndsWith("Password", StringComparison.InvariantCultureIgnoreCase)))
            {
                defaultvalue.Value = "*Stripped*";
            }
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
            //parameterValueSet
            connectionTemplate.properties.parameterValueSet = connectionInstance["properties"]?["parameterValueSet"];

            JObject connectionParameters = new JObject();

            bool useGateway = connectionInstance["properties"]?["parameterValueSet"]?["values"]?["gateway"] != null ||
                connectionInstance["properties"]?["nonSecretParameterValues"]?["gateway"] != null;

            if (useGateway == false)
            {
                useGateway = connectionInstance["properties"]?["nonSecretParameterValues"]?["gateway"] != null;
            }
            var instanceResourceId = new AzureResourceId(connectionInstance.Value<string>("id"));

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

                        if (OnlyParameterizeConnections == false && concatedId.EndsWith("/azureblob')]") && connectionInstance["properties"]["parameterValueSet"]?["name"].Value<string>() == "managedIdentityAuth")
                        {
                            //ignore
                        }
                        else if (OnlyParameterizeConnections == false && concatedId.EndsWith("/sql')]") && connectionInstance["properties"]["parameterValueSet"]?["name"].Value<string>() == "oauthMI")
                        {
                            //ignore
                        }
                        else if (OnlyParameterizeConnections == false && parameter.Name == "accessKey" && concatedId.EndsWith("/azureblob')]"))
                        {
                            //handle different resourceGroups

                            connectionParameters.Add(parameter.Name, $"[listKeys(resourceId(parameters('{AddTemplateParameter(connectionName + "_resourceGroupName", "string", instanceResourceId.ResourceGroupName)}'),'Microsoft.Storage/storageAccounts', parameters('{connectionName}_accountName')), '2018-02-01').keys[0].value]");
                        }
                        else if (OnlyParameterizeConnections == false && concatedId.EndsWith("/azuretables')]") && connectionInstance["properties"]["parameterValueSet"]?["name"].Value<string>() == "managedIdentityAuth")
                        {
                            //ignore
                        }
                        else if (OnlyParameterizeConnections == false && parameter.Name == "sharedkey" && concatedId.EndsWith("/azuretables')]"))
                        {
                            //handle different resourceGroups

                            connectionParameters.Add(parameter.Name, $"[listKeys(resourceId(parameters('{AddTemplateParameter(connectionName + "_resourceGroupName", "string", instanceResourceId.ResourceGroupName)}'),'Microsoft.Storage/storageAccounts', parameters('{connectionName}_storageaccount')), '2018-02-01').keys[0].value]");
                        }
                        else if (OnlyParameterizeConnections == false && (parameter.Name == "sharedkey" && concatedId.EndsWith("/azurequeues')]")))
                        {
                            connectionParameters.Add(parameter.Name, $"[listKeys(resourceId(parameters('{AddTemplateParameter(connectionName + "_resourceGroupName", "string", instanceResourceId.ResourceGroupName)}'),'Microsoft.Storage/storageAccounts', parameters('{connectionName}_storageaccount')), '2018-02-01').keys[0].value]");
                        }
                        else if (OnlyParameterizeConnections == false && concatedId.EndsWith("/servicebus')]") && connectionInstance["properties"]["parameterValueSet"]?["name"].Value<string>() == "managedIdentityAuth")
                        {
                            //Check for namespaceEndpoint property exist and is not null
                            var namespaceEndpoint = connectionInstance["properties"]?["parameterValueSet"]?["values"]?["namespaceEndpoint"]?["value"];
                            if (namespaceEndpoint != null)
                            {
                                var namespaceEndpointUri = new Uri(namespaceEndpoint.Value<string>()); //example: sb://{serviceNamespace}.servicebus.windows.net

                                var namespaces_Name_param = AddTemplateParameter($"namespaces_Name", "string", namespaceEndpointUri.Host.Replace(".servicebus.windows.net", ""));
                                if (namespaces_Name_param != null)
                                {
                                    connectionInstance["properties"]["parameterValueSet"]["values"]["namespaceEndpoint"]["value"] = $"[concat('{namespaceEndpointUri.Scheme}://', parameters('{namespaces_Name_param}'), '.servicebus.windows.net')]";
                                }
                            }
                        }
                        else if (OnlyParameterizeConnections == false && concatedId.EndsWith("/keyvault')]") && connectionInstance["properties"]["parameterValueSet"]?["name"].Value<string>() == "oauthMI")
                        {
                            if (parameter.Name != "vaultName")
                            {
                                continue;
                            }

                            //Check for vaultName property exist and is not null
                            var vaultName_param = AddTemplateParameter($"vaultName", "string", connectionInstance["properties"]?["parameterValueSet"]?["values"]?["vaultName"]?["value"]);
                            if (vaultName_param != null)
                            {
                                connectionInstance["properties"]["parameterValueSet"]["values"]["vaultName"]["value"] = $"[parameters('{vaultName_param}')]";
                            }
                        }
                        else if (OnlyParameterizeConnections == false && concatedId.EndsWith("/servicebus')]"))
                        {
                            var serviceBus_displayName = (string)connectionInstance["properties"]?["displayName"];
                            if (string.IsNullOrEmpty(serviceBus_displayName) || !UseServiceBusDisplayName)
                            {
                                serviceBus_displayName = "servicebus";
                            }

                            var namespace_param = AddTemplateParameter($"{serviceBus_displayName}_namespace_name", "string", "REPLACE__servicebus_namespace");
                            var sb_resource_group_param = AddTemplateParameter($"{serviceBus_displayName}_resourceGroupName", "string", "REPLACE__servicebus_rg");
                            var servicebus_auth_name_param = AddTemplateParameter($"servicebus_accessKey_name", "string", "RootManageSharedAccessKey");

                            connectionParameters.Add(parameter.Name, $"[listkeys(resourceId(parameters('{sb_resource_group_param}'),'Microsoft.ServiceBus/namespaces/authorizationRules', parameters('{namespace_param}'), parameters('{servicebus_auth_name_param}')), '2017-04-01').primaryConnectionString]");

                        }
                        else if (OnlyParameterizeConnections == false && concatedId.EndsWith("/azureeventgridpublish')]"))
                        {
                            var url = connectionInstance["properties"]["nonSecretParameterValues"].Value<string>("endpoint");
                            var location = connectionInstance.Value<string>("location");
                            url = url.Replace("https://", "");
                            var site = url.Substring(0, url.IndexOf("."));

                            var param = AddTemplateParameter($"{connectionInstance.Value<string>("name")}_instancename", "string", site);

                            if (parameter.Name == "endpoint")
                            {
                                connectionParameters.Add(parameter.Name, $"[reference(resourceId(parameters('{AddTemplateParameter(connectionName + "_resourceGroupName", "string", instanceResourceId.ResourceGroupName)}'),'Microsoft.EventGrid/topics',parameters('{param}')),'2018-01-01').endpoint]");
                            }
                            else if (parameter.Name == "api_key")
                            {
                                connectionParameters.Add(parameter.Name, $"[listKeys(resourceId(parameters('{AddTemplateParameter(connectionName + "_resourceGroupName", "string", instanceResourceId.ResourceGroupName)}'),'Microsoft.EventGrid/topics',parameters('{param}')),'2018-01-01').key1]");
                            }


                        }
                        //check for the existence of token:TenantId to determine the connectoins uses Oauth
                        else if (SkipOauthConnectionAuthorization && parameter.Name.Equals("token:TenantId"))
                        {
                            //skip because otherwise authenticated connection has to be authenticated again.
                        }
                        else
                        {
                            //todo check this!
                            object parameterValue = null;
                            if (parameter.Name.Equals("token:TenantId"))
                            {
                                parameterValue = "[subscription().tenantId]";
                            }

                            //check for hidden constraint do not skip token parameters for client credential services like eventgrid
                            else if (!parameter.Name.StartsWith("token:") && (parameter.Value["uiDefinition"]["constraints"]["hidden"]?.Value<bool>() ?? false))
                            {
                                continue;
                            }
                            else if (connectionInstance["properties"]["nonSecretParameterValues"] != null)
                            {
                                parameterValue = connectionInstance["properties"]["nonSecretParameterValues"][parameter.Name];
                            }
                            else if (concatedId.EndsWith("/managedApis/sql')]") && parameter.Name == "authType")
                            {
                                var parameterName = connectionInstance["properties"]["parameterValueSet"]?["name"].Value<string>();
                                parameterValue = (parameterName == "windowsAuthentication") ? "windows" : "basic";
                            }
                            else
                            {
                                parameterValue = connectionInstance["properties"]["parameterValueSet"]?["values"]?[parameter.Name]?["value"];
                            }

                            var addedparam = AddTemplateParameter($"{connectionName}_{parameter.Name}", (string)(parameter.Value)["type"], parameterValue);
                            connectionParameters.Add(parameter.Name, $"[parameters('{addedparam}')]");

                            //If has an enum
                            if (parameter.Value["allowedValues"] != null)
                            {
                                var array = new JArray();
                                foreach (var allowedValue in parameter.Value["allowedValues"])
                                {
                                    array.Add(allowedValue["value"].Value<string>().Replace("none", "anonymous"));
                                }
                                template.parameters[addedparam]["allowedValues"] = array;
                                if (parameter.Value["allowedValues"].Count() == 1)
                                {
                                    template.parameters[addedparam]["defaultValue"] = parameter.Value["allowedValues"][0]["value"].Value<string>().Replace("none", "anonymous");
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
                    else
                    {
                    }
                }
            }

            if (useGateway)
            {
                string currentvalue = "";
                if (connectionInstance["properties"]["nonSecretParameterValues"] != null)
                {
                    currentvalue = (string)connectionInstance["properties"]["nonSecretParameterValues"]["gateway"]["id"];

                }
                else if (connectionInstance["properties"]["parameterValueSet"]["values"]["gateway"]["value"].HasValues)
                {
                    currentvalue = (string)connectionInstance["properties"]["parameterValueSet"]["values"]["gateway"]["value"]["id"];
                }

                var rid = new AzureResourceId(currentvalue);
                var gatewayname = AddTemplateParameter($"{connectionName}_gatewayname", "string", rid.ResourceName);
                var resourcegroup = AddTemplateParameter($"{connectionName}_gatewayresourcegroup", "string", rid.ResourceGroupName);

                var subscriptionId = "subscription().subscriptionId";
                if (instanceResourceId.SubscriptionId != rid.SubscriptionId)
                {
                    subscriptionId = $"parameters('{AddTemplateParameter($"{connectionName}_gatewaysubscriptionId", "string", rid.SubscriptionId)}')";
                }
                var gatewayobject = new JObject();


                gatewayobject["id"] = $"[concat('/subscriptions/',{subscriptionId},'/resourceGroups/',parameters('{resourcegroup}'),'/providers/Microsoft.Web/connectionGateways/',parameters('{gatewayname}'))]";
                connectionParameters.Add("gateway", gatewayobject);
                useGateway = true;

            }
            //only fill connectionParameters when source not empty, otherwise saved credentials will be lost.
            if (connectionParameters.HasValues)
                connectionTemplate.properties.parameterValues = connectionParameters;

            return JObject.FromObject(connectionTemplate);
        }


        private async Task<JObject> HandleTags(JObject definition)
        {
            JObject result = new JObject();

            foreach (var property in definition["tags"].ToObject<JObject>().Properties())
            {
                if (DisableTagParameters)
                {
                    result.Add(property.Name, property.Value.ToString());
                }
                else
                {
                    var parm = AddTemplateParameter(property.Name + "_Tag", "string", property.Value.ToString());
                    result.Add(property.Name, $"[parameters('{parm}')]");
                }
            }

            return result;
        }

        private static bool ShouldAddTemplateParameter(JToken token)
        {
            return token != null && (token.Type != JTokenType.String || !Regex.IsMatch(token.Value<string>(), @"parameters\('.*'\)"));
        }

        public DeploymentTemplate GetTemplate()
        {
            return template;
        }

    }
}
