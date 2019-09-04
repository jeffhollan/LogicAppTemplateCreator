using LogicAppTemplate.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
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

        [Parameter(
            Mandatory = false,
            HelpMessage = "How to handle KeyVault integration, default is None, available options None or Static, Static will generate parameters for static reference to KeyVault"
            )]
        public KeyVaultUsage KeyVault = KeyVaultUsage.None;

        [Parameter(Mandatory = false, HelpMessage = "If true, the default value for the parameters will be cleared")]
        public SwitchParameter ClearParameterValues;

        public enum KeyVaultUsage
        {
            None,
            Static
        }


        [Parameter(
           Mandatory = false,
           HelpMessage = "Whether to generate parameters whose default value is an ARM expression.  If not specified then will not generate parameters per original code"
           )]
        public string GenerateExpression;

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
            foreach (var param in logicAppTemplate["parameters"].Children<JProperty>())
            {
                // Don't create parameters that reference a ARM Template expression
                if (param.Value.Value<string>("type").Equals("string", StringComparison.CurrentCultureIgnoreCase) && param.Value.Value<string>("defaultValue") != null && param.Value.Value<string>("defaultValue").StartsWith("[") &&  string.IsNullOrEmpty(GenerateExpression))
                {
                    continue;
                }

                var obj = new JObject();
                if (KeyVaultUsage.Static == KeyVault && (string)logicAppTemplate["parameters"][param.Name]["type"] == "securestring")
                {
                    dynamic k = new ExpandoObject();
                    k.keyVault = new ExpandoObject();
                    k.keyVault.id = "/subscriptions/{subscriptionid}/resourceGroups/{resourcegroupname}/providers/Microsoft.KeyVault/vaults/{vault-name}";
                    k.secretName = param.Name.Replace("_","-"); //need replace the underscore since it is an eleigal character in keyvault
                    obj["reference"] = JObject.FromObject(k);
                }
                else if (ClearParameterValues)
                {
                    obj["value"] = JValue.Parse("[]");
                }
                else {
                    obj["value"] = logicAppTemplate["parameters"][param.Name]["defaultValue"];
                }

                paramTemplate.parameters.Add(param.Name, obj);
            }

            return JObject.FromObject(paramTemplate);
        }
    }
}
