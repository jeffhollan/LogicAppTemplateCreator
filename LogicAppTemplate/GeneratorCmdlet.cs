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

        [Parameter(Mandatory = false, HelpMessage = "If supplied, diagnostic settings will be included in the ARM template")]
        public SwitchParameter DiagnosticSettings;

        [Parameter(Mandatory = false, HelpMessage = "If supplied, Initialize Variable actions will be included in the ARM template")]
        public SwitchParameter IncludeInitializeVariable;

        [Parameter(Mandatory = false, HelpMessage = "If supplied, the functionApp gets a static name")]
        public SwitchParameter FixedFunctionAppName;

        [Parameter(Mandatory = false, HelpMessage = "If supplied, generate an output variable with the trigger url.")]
        public SwitchParameter GenerateHttpTriggerUrlOutput;

        [Parameter(Mandatory = false, HelpMessage = "If supplied, the passwords will be stripped out of the output")]
        public SwitchParameter StripPassword;

        [Parameter(Mandatory = false, HelpMessage = "If supplied, the LA ARM Template will be set to Disabled and won't be automatically run when deployed")]
        public SwitchParameter DisabledState;

        [Parameter(Mandatory = false, HelpMessage = "If supplied, Managed Identity for the Logic App will be set in the ARM template")]
        public SwitchParameter ForceManagedIdentity;

        [Parameter(Mandatory = false, HelpMessage = "If supplied, Connections for the Logic App will not be output in the ARM template")]
        public SwitchParameter DisableConnectionGeneration;


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
                Token = GetClipboardText().Replace("Bearer ", "");
                resourceCollector.token = Token;
            }
            else
            {
                return;
            }

            TemplateGenerator generator = new TemplateGenerator(LogicApp, SubscriptionId, ResourceGroup, resourceCollector, StripPassword, DisabledState)
            {
                DiagnosticSettings = this.DiagnosticSettings,
                GenerateHttpTriggerUrlOutput = this.GenerateHttpTriggerUrlOutput,
                IncludeInitializeVariable = this.IncludeInitializeVariable,
                ForceManagedIdentity = this.ForceManagedIdentity,
                FixedFunctionAppName = FixedFunctionAppName,
                DisableConnectionsOutput = this.DisableConnectionGeneration
            };

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
        private string GetClipboardText()
        {
            string strClipboard = string.Empty;

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    strClipboard = Clipboard.GetText();
                    if (strClipboard != string.Empty)
                        return strClipboard;

                    System.Threading.Thread.Sleep(10);
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    //fix for OpenClipboard Failed (Exception from HRESULT: 0x800401D0 (CLIPBRD_E_CANT_OPEN))
                    //https://stackoverflow.com/questions/12769264/openclipboard-failed-when-copy-pasting-data-from-wpf-datagrid
                    //https://stackoverflow.com/questions/68666/clipbrd-e-cant-open-error-when-setting-the-clipboard-from-net
                    if (ex.ErrorCode == -2147221040)
                        System.Threading.Thread.Sleep(10);
                    else
                        throw new Exception("Unable to get Clipboard text.");
                }
            }

            return strClipboard;
        }
    }
}
