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
    [Cmdlet(VerbsCommon.Get, "NestedResourceTemplate", ConfirmImpact = ConfirmImpact.None)]
    public class NestedTemplateGenerator: Cmdlet
    {

        [Parameter(
        Mandatory = true,
        HelpMessage = "Name of the Resource"
        )]
        public string ResourceName;

        [Parameter(
       Mandatory = false,
       HelpMessage = "Existing Deployment Template definition"
       )]
        public string Template = "";

        private static string NestedTemplateResourceUri = "[concat(parameters('_artifactsLocation'), '/{0}/{0}.json', parameters('_artifactsLocationSasToken'))]";
        private static string NestedTemplateParameterUri = "[concat(parameters('_artifactsLocation'), '/{0}/{0}.parameters.json', parameters('_artifactsLocationSasToken'))]";
        private DeploymentTemplate nestedtemplate;

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
            }

            nestedtemplate = JsonConvert.DeserializeObject<DeploymentTemplate>(Template);
        }

        protected override void ProcessRecord()
        {
            var nestedresource = new Models.NestedResourceTemplate() { name = ResourceName};
            nestedresource.properties.templateLink.uri = String.Format(NestedTemplateResourceUri, ResourceName);
            nestedresource.properties.parametersLink.uri = String.Format(NestedTemplateParameterUri, ResourceName);

            nestedtemplate.resources.Add(JObject.FromObject(nestedresource));
            var result = JObject.FromObject(nestedtemplate);
            WriteObject(result.ToString());
        }


    }
}
