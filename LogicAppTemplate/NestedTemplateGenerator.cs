using LogicAppTemplate.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.Caching;

namespace LogicAppTemplate
{
    [Cmdlet(VerbsCommon.Get, "NestedResourceTemplate", ConfirmImpact = ConfirmImpact.None)]
    public class NestedTemplateGenerator : Cmdlet
    {

        [Parameter(
        Mandatory = true,
        HelpMessage = "Name of the Resource"
        )]
        public string ResourceName;

        [Parameter(
       Mandatory = true,
       HelpMessage = "Path of the Resource"
       )]
        public string ResourcePath;

        [Parameter(
       Mandatory = false,
       HelpMessage = "Existing Deployment Template definition"
       )]
        public string Template = "";

        [Parameter(Mandatory = false, HelpMessage = "Add Parameterlink")]
        public bool AddParameterlink = true;

        private static string NestedTemplateResourceUri = "[concat(parameters('repoBaseUrl'), '/{0}/{0}.json', parameters('_artifactsLocationSasToken'))]";
        private static string NestedTemplateParameterUri = "[concat(parameters('repoBaseUrl'), '/{0}/{0}.parameters.json', parameters('_artifactsLocationSasToken'))]";
        private DeploymentTemplate nestedtemplate;
        private MemoryCache cache;

        public NestedTemplateGenerator()
        {

        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            InitializeTemplate();
        }

        private void InitializeTemplate()
        {
            if (string.IsNullOrEmpty(Template))
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "LogicAppTemplate.Templates.nestedTemplateShell.json";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        Template = reader.ReadToEnd();
                    }
                }
                cache = new MemoryCache("powershell.arm");
                //MemoryCache.Default.Dispose();
            }
            else
            {

            }
            nestedtemplate = JsonConvert.DeserializeObject<DeploymentTemplate>(Template);
        }

        protected override void ProcessRecord()
        {
            var nestedresource = new Models.NestedResourceTemplate() { name = ResourceName };
            nestedresource.properties.templateLink.uri = String.Format(NestedTemplateResourceUri, ResourceName);

            if (AddParameterlink)
            {
                nestedresource.properties.parametersLink.uri = String.Format(NestedTemplateParameterUri, ResourceName);
            }
            else
            {
                nestedresource.properties.parametersLink = null;
                nestedresource.properties.parameters = new JObject();


                var fileName = Path.Combine(ResourcePath, Path.GetFileName(ResourcePath) + ".json");

                using (Stream stream = File.OpenRead(fileName))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var nestedTemplate2 = new JsonTextReader(reader);

                        JsonSerializer se = new JsonSerializer();
                        dynamic parsedData = se.Deserialize(nestedTemplate2);
                        JObject parameters = parsedData["parameters"];

                        foreach (JProperty parameter in parameters.Properties())
                        {

                            JProperty param = parameter.DeepClone() as JProperty;

                            if (nestedtemplate.parameters.ContainsKey(parameter.Name))
                            {
                                dynamic v1 = nestedtemplate.parameters[parameter.Name]; //.Value["defaultValue"]?.Value<string>();\
                                                                                        //v1.Properties[""];


                                if (parameter.Contains("defaultValue") && !JToken.DeepEquals(v1.defaultValue, parameter.Value["defaultValue"]))
                                {
                                    var paramName = GetUniqueParamName(parameter.Name);

                                    param = new JProperty($"{paramName}", parameter.Value);


                                    nestedtemplate.parameters.Add(param);
                                }
                            }
                            else
                            {
                                nestedtemplate.parameters.Add(param);
                            }

                            nestedresource.properties.parameters.Add(parameter.Name, JObject.FromObject(new { value = $"[parameters('{param.Name}')]" }));

                        }

                    }
                }


            }
            nestedtemplate.resources.Add(JObject.FromObject(nestedresource));


            var result = JObject.FromObject(nestedtemplate);
            Sort(result["parameters"]);
            WriteObject(result.ToString());
        }

        private string GetUniqueParamName(string name)
        {
            var uniqueName = name;
            var count = 0;
            while (nestedtemplate.parameters.ContainsKey(uniqueName))
            {
                count += 1;
                uniqueName = $"{name}_{count}";
            }

            return uniqueName;
        }

        protected override void EndProcessing()
        {


        }
        public class CountObject
        {
            public int Cache = -1;

            public int Increment()
            {
                Cache++;
                return Cache;
            }
        }
        private static void Sort(JToken jToken)
        {
            if (jToken == null)
            {
                return;
            }

            if (jToken is JObject)
            {
                var jObj = jToken as JObject;

                var props = jObj.Properties().ToList();
                foreach (var prop in props)
                {
                    prop.Remove();
                }

                foreach (var prop in props.OrderBy(p => p.Name))
                {
                    jObj.Add(prop);
                    if (prop.Value is JObject)
                        Sort((JObject)prop.Value);
                }
            }
        }
    }
}
