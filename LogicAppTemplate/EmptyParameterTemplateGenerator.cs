using LogicAppTemplate.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Management.Automation;

namespace LogicAppTemplate
{
    [Cmdlet(VerbsCommon.Get, "EmptyParameterTemplate", ConfirmImpact = ConfirmImpact.None)]
    public class EmptyParameterTemplateGenerator : Cmdlet

    {
        private ParameterTemplate paramtemplate;
        public EmptyParameterTemplateGenerator()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "LogicAppTemplate.Templates.paramTemplate.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                paramtemplate = JsonConvert.DeserializeObject<ParameterTemplate>(reader.ReadToEnd());
            }
        }

        protected override void ProcessRecord()
        {
            var result = JObject.FromObject(paramtemplate);
            WriteObject(result.ToString());
        }
    }
}
