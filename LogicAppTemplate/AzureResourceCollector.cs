using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogicAppTemplate
{
    public class AzureResourceCollector : IResourceCollector
    {

        public string DebugOutputFolder = "";
        public string token;


        public AzureResourceCollector()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        }
        public string Login(string tenantName)
        {
            string authstring = Constants.AuthString;
            if (!string.IsNullOrEmpty(tenantName))
            {
                authstring = authstring.Replace("common", tenantName);
            }
            AuthenticationContext ac = new AuthenticationContext(authstring, true);

            var ar = ac.AcquireTokenAsync(Constants.ResourceUrl, Constants.ClientId, new Uri(Constants.RedirectUrl), new PlatformParameters(PromptBehavior.RefreshSession)).GetAwaiter().GetResult();
            token = ar.AccessToken;
            return token;
        }
        private static HttpClient client = new HttpClient() { BaseAddress = new Uri("https://management.azure.com") };

        public async Task<JObject> GetResource(string resourceId, string apiVersion = null, string suffix = "")
        {
            return JObject.Parse(await GetRawResource(resourceId, apiVersion, suffix));
        }
        public async Task<string> GetRawResource(string resourceId, string apiVersion = null, string suffix = "")
        {
            if (resourceId.ToLower().Contains("integrationserviceenvironment"))
            {
                apiVersion = "2018-07-01-preview";
            }

            string url = resourceId + (string.IsNullOrEmpty(apiVersion) ? "" : "?api-version=" + apiVersion) + (string.IsNullOrEmpty(suffix) ? "" : $"&{suffix}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception("Reesource Not found, resource: " + resourceId);
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new Exception("Authorization failed, httpstatus: " + response.StatusCode);
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(DebugOutputFolder))
            {
                System.IO.File.WriteAllText(DebugOutputFolder + "\\" + resourceId.Split('/').SkipWhile((a) => { return a != "providers" && a != "integrationAccounts"; }).Aggregate<string>((b, c) => { return b + "-" + c; }) + ".json", responseContent);
            }
            return responseContent;
        }
    }
}