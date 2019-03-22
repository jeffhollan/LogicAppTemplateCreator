using LogicAppTemplate.Templates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate
{
    public class CustomConnectorGenerator
    {
        private IResourceCollector resourceCollector;
        private string resourceGroup;
        private string subscriptionId;
        private string customConnectorName;

        public CustomConnectorGenerator(string customConnectorName,string subscriptionId, string resourceGroup, IResourceCollector resourceCollector)
        {
            this.customConnectorName = customConnectorName;
            this.subscriptionId = subscriptionId;
            this.resourceGroup = resourceGroup;
            this.resourceCollector = resourceCollector;            
        }

        public async Task<JObject> GenerateTemplate()
        {

            JObject _definition = await resourceCollector.GetResource($"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/customApis/{customConnectorName}", "2018-07-01-preview");
            return await generateDefinition(_definition);
        }

        // the apiDefinition parameter allows dependency injection for unit testing
        public async Task<JObject> generateDefinition(JObject resource, string apiDefinition = null)
        {
            ARMTemplateClass template = new ARMTemplateClass();
            template.AddParameter("customConnectorApiVersion", "string", "1.0.0.0");
            var name = template.AddParameter("customConnectorName", "string", resource.Value<string>("name"));
            var location = template.AddParameter("customConnectorLocation", "string", "[resourceGroup().location]");

            var uri = resource["properties"]["apiDefinitions"].Value<string>("modifiedSwaggerUrl");
            
            string swaggerRawDefinition = apiDefinition ?? GetCustomConnectorSwagger(uri);

            JObject preParsedswaggerDefinition = JObject.Parse(swaggerRawDefinition);
            var host = preParsedswaggerDefinition.Value<string>("host");

            //replace the host with a parameter to allow the scenarios where the host could be different between environments
            var serviceHost = template.AddParameter("serviceHost", "string", host);
            var scheme = "http";
            if (preParsedswaggerDefinition.ContainsKey("schemes"))
            {
                scheme = preParsedswaggerDefinition["schemes"].Children().First().Value<string>();
            }
            var backendService = template.AddParameter("backendService", "string",scheme + "://" + host + preParsedswaggerDefinition.Value<string>("basePath"));
            swaggerRawDefinition = swaggerRawDefinition.Replace(host, template.WrapParameterName(serviceHost));

            // handle connectionID
            swaggerRawDefinition= swaggerRawDefinition.Replace(@"/{connectionId}", string.Empty);
            JObject swaggerDefinition = JObject.Parse(swaggerRawDefinition);
            // remove all connectionID parameters
            if (swaggerDefinition.ToString().Contains("connectionId"))
            {
                // note: only Json.Net 12.0.1 supports XPath like below, hence guarding with a try/catch just in case of older version of the libray
                try
                {                    
                    foreach (var token in swaggerDefinition["paths"].SelectTokens(@"$..[?(@.name === 'connectionId')]").ToList())
                    {
                        token.Remove();
                    }
                }
                catch (JsonException) { }
            }
            

            JObject resourceObject = new JObject();
            JObject propertiesObject = new JObject();
            

            // handle ISE 

            JToken origIseToken = resource["properties"].SelectToken("integrationServiceEnvironment");
            if (origIseToken != null)
            {
                var iseEnvName = template.AddParameter("ISEEnvironmentName", "string", origIseToken.Value<string>("name"));
                

                JObject iseObject = new JObject();
                AzureResourceId iseId = new AzureResourceId(origIseToken.Value<string>("id"));
                //iseObject.Add("id", origIseToken.Value<string>("id"));
                var iseEnvResouceGroup = template.AddParameter("ISEEnvironmentResourceGroup", "string", iseId.ResourceGroupName);
                iseId.SubscriptionId = "subscription().subscriptionId";
                iseId.ResourceGroupName = template.WrapParameterName(iseEnvResouceGroup);
                iseId.ResourceName = template.WrapParameterName(iseEnvName);

                iseObject.Add("id", $"[concat('/subscriptions/', subscription().subscriptionId, '/resourceGroups/',parameters('{iseEnvResouceGroup}'), '/providers/Microsoft.Logic/integrationServiceEnvironments/', parameters('{iseEnvName}'))]");
                
                iseObject.Add("name", template.WrapParameterName(iseEnvName));
                iseObject.Add("type", origIseToken.Value<string>("type"));

                propertiesObject.Add("integrationServiceEnvironment", iseObject);
            }
            
            if (resource["properties"].SelectToken("connectionParameters") != null)
            {
                propertiesObject.Add("connectionParameters", resource["properties"]["connectionParameters"]);
            }

            JObject backendObject = new JObject();
            backendObject.Add("serviceUrl", template.WrapParameterName(backendService));   // $"[parameters('{serviceHost}')]");
            propertiesObject.Add("backendService", backendObject);

            propertiesObject.Add("swagger",swaggerDefinition);
            propertiesObject.Add("description", resource["properties"].Value<string>("description"));
            propertiesObject.Add("displayname", template.WrapParameterName(name));
            propertiesObject.Add("iconUri", resource["properties"].Value<string>("iconUri"));

            resourceObject.Add("apiVersion", "2016-06-01");
            resourceObject.Add("location", template.WrapParameterName(location));
            resourceObject.Add("name", template.WrapParameterName(name));
            resourceObject.Add("type", resource.Value<string>("type"));
            resourceObject.Add("properties", propertiesObject);

            template.resources.Add(resourceObject);
            
            return JObject.FromObject(template);
        }

        public string GetCustomConnectorSwagger(string url)
        {
            string x_ms_version = "2011-08-18";
            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            NameValueCollection requestHeaders = new NameValueCollection();
            var requestDate = DateTime.UtcNow;
            //Add request headers. Please note that since we're using SAS URI, we don't really need "Authorization" header.
            requestHeaders.Add("x-ms-date", string.Format(CultureInfo.InvariantCulture, "{0:R}", requestDate));
            requestHeaders.Add("x-ms-version", x_ms_version);
            request.Headers.Add(requestHeaders);
            var response = request.GetResponseAsync();
            var encoding = ASCIIEncoding.ASCII;
            using (var reader = new System.IO.StreamReader(response.GetAwaiter().GetResult().GetResponseStream(), encoding))
            {
                return reader.ReadToEnd();
            }
            
        }
    }
}