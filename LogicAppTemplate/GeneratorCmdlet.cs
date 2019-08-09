using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogicAppTemplate
{
    [Cmdlet(VerbsCommon.Get, "LogicAppTemplate", ConfirmImpact = ConfirmImpact.None)]
    public class GeneratorCmdlet : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Name of the Logic App")]
        public string LogicApp;

        [Parameter(Mandatory = true, HelpMessage = "Name of the Resource Group")]
        public string ResourceGroup;

        [Parameter(Mandatory = true, HelpMessage = "The SubscriptionId")]
        public string SubscriptionId;

        [Parameter(Mandatory = false, HelpMessage = "Name of the Tenant i.e. contoso.onmicrosoft.com")]
        public string TenantName = "";

        [Parameter(Mandatory = false, HelpMessage = "A Bearer token value")]
        public string Token = "";

        [Parameter(Mandatory = false, HelpMessage = "Piped input from armclient", ValueFromPipeline = true)]
        public string ClaimsDump;

        [Parameter(Mandatory = false, HelpMessage = "If set, result from rest interface will be saved to this folder")]
        public string DebugOutPutFolder = "";

        [Parameter(Mandatory = false, HelpMessage = "If true, diagnostic settings will be included in the ARM template")]
        public bool DiagnosticSettings = false;

        [Parameter(Mandatory = false, HelpMessage = "If true, the passwords will be stripped out of the output")]
        public bool StripPassword = false;

        [Parameter(Mandatory = false, HelpMessage = "If true, the LA ARM Template will be set to Disabled and won't be automatically run when deployed")]
        public bool DisableState = false;

        protected override void ProcessRecord()
        {
            AzureResourceCollector resourceCollector = new AzureResourceCollector();

            if (!string.IsNullOrEmpty(DebugOutPutFolder))
                resourceCollector.DebugOutputFolder = DebugOutPutFolder;

            if (ClaimsDump == null)
            {
                if (String.IsNullOrEmpty(Token))
                {
                    Token = resourceCollector.Login(TenantName);
                }
                else
                {
                    resourceCollector.token = Token;
                }
            }
            else if (ClaimsDump.Contains("Token copied"))
            {
                Token = Clipboard.GetText().Replace("Bearer ", "");
                resourceCollector.token = Token;
            }
            else
            {
                return;
            }
            TemplateGenerator generator = new TemplateGenerator(LogicApp, SubscriptionId, ResourceGroup, resourceCollector,StripPassword, DisableState);
            generator.DiagnosticSettings = DiagnosticSettings;

            try
            {

                JObject result = generator.GenerateTemplate().Result;
                WriteObject(result.ToString());
            }
            catch (Exception ex)
            {
                if (ex is AggregateException)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Aggregation exception thrown, se following exceptions for more information");
                    AggregateException ae = (AggregateException)ex;
                    foreach (var e in ae.InnerExceptions)
                    {
                        sb.AppendLine($"Exception: {e.Message}");
                        sb.AppendLine($"{e.StackTrace}");
                        sb.AppendLine("-------------------------------------------");
                    }
                    throw new Exception($"Aggregation Exception thrown, {ae.Message}, first Exception message is: {ae.InnerExceptions.First().Message}, for more information read the output file.");
                }
                else
                {
                    throw ex;
                }
            }
        }
    }
}
