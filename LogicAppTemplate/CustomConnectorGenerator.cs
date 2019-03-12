using LogicAppTemplate.Templates;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.Globalization;
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


        public async Task<JObject> generateDefinition(JObject resource)
        {
            ARMTemplateClass template = new ARMTemplateClass();
            template.AddParameter("customConnectorApiVersion", "string", "1.0.0.0");
            var name = template.AddParameter("customConnectorName", "string", resource.Value<string>("name"));
            var location = template.AddParameter("customConnectorLocation", "string", "[resourceGroup().location]");

            var uri = resource["properties"]["apiDefinitions"].Value<string>("modifiedSwaggerUrl");

            var swaggerRawDefinition = GetCustomConnectorSwagger(uri);
            JObject swaggerDefinition = JObject.Parse(swaggerRawDefinition);
            var host = swaggerDefinition.Value<string>("host");

            //replace the host with a parameter to allow the scenarios where the host could be different between environments
            var serviceHost = template.AddParameter("serviceHost", "string", host);
            swaggerRawDefinition.Replace(host, template.WrapParameterName(serviceHost));
            swaggerRawDefinition.Replace("/{connectionId}", string.Empty);
            swaggerDefinition = JObject.Parse(swaggerRawDefinition);

         
            JObject resourceObject = new JObject();

            JObject propertiesObject = new JObject();

            JObject backendObject = new JObject();
            backendObject.Add("serviceUrl", template.WrapParameterName(serviceHost));   // $"[parameters('{serviceHost}')]");
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