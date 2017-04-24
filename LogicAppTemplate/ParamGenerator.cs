using LogicAppTemplate.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate
{
    [Cmdlet(VerbsCommon.Get, "ParameterTemplate", ConfirmImpact = ConfirmImpact.None)]
    public class ParamGenerator : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            HelpMessage = "The path to the template file"
            )]
        public string TemplateFile;


        private ParameterTemplate paramTemplate;
        public ParamGenerator()
        {

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "LogicAppTemplate.Templates.paramTemplate.json";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                paramTemplate = JsonConvert.DeserializeObject<ParameterTemplate>(reader.ReadToEnd());
            }
        }

        protected override void ProcessRecord()
        {

            var logicappTemplate = JObject.Parse(File.ReadAllText(TemplateFile));
            var result = CreateParameterFileFromTemplate(logicappTemplate);
            

            WriteObject(result.ToString());
        }

        public JObject CreateParameterFileFromTemplate(JObject logicAppTemplate)
        {
            foreach(var param in logicAppTemplate["parameters"].Children<JProperty>())
            {
                var obj = new JObject();
                obj["value"] = logicAppTemplate["parameters"][param.Name]["defaultValue"];
                paramTemplate.parameters.Add(param.Name, obj);
            }

            return JObject.FromObject(paramTemplate);
        }
    }
}
