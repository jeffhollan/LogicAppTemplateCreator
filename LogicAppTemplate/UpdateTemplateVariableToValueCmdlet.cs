using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogicAppTemplate.Models;
using Newtonsoft.Json;
using System.Dynamic;
using System.IO;

namespace LogicAppTemplate
{
    [Cmdlet(VerbsData.Update, "TemplateVariableReferenceToValue", ConfirmImpact = ConfirmImpact.None)]
    public class UpdateTemplateVariableReferenceToValue : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            HelpMessage = "The path to the template file"
            )]
        [Alias("Path")]
        public string TemplateFile;

        [Parameter(
            Mandatory = true,
            HelpMessage = "Name of the variable reference to switch out"
            )]
        [Alias("Name")]
        public string Variable;
        
        [Parameter(
            Mandatory = true,
            HelpMessage = "Value that should be inserted instead of the current variable reference"
            )]
        public string Value;

        public UpdateTemplateVariableReferenceToValue()
        {

        }

        public UpdateTemplateVariableReferenceToValue(string TemplateFile, string Variable, string Value)
        {
            this.TemplateFile = TemplateFile;
            this.Variable = Variable;
            this.Value = Value;
        }

        public JObject UpdateTemplateVariable(string logicAppTemplateJson)
        {
            string pattern = string.Format("@{{variables('{0}')}}", Variable);
            return JObject.Parse(logicAppTemplateJson.Replace(pattern, Value));
        }

        protected override void ProcessRecord()
        {
            string logicappTemplate = File.ReadAllText(TemplateFile);
            JObject result = UpdateTemplateVariable(logicappTemplate);
            WriteObject(result.ToString());
        }
    }
}
