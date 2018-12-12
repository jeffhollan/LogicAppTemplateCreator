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
    [Cmdlet(VerbsCommon.Get, "CustomConnectorTemplate", ConfirmImpact = ConfirmImpact.None)]
    public class GeneratorCustomConnectorCmdlet : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Name of the CustomConnector")]
        public string CustomConnector;

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
            CustomConnectorGenerator generator = new CustomConnectorGenerator(CustomConnector, SubscriptionId, ResourceGroup,resourceCollector);

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
