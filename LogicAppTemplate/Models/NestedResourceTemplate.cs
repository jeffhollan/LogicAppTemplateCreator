using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace LogicAppTemplate.Models
{
    public class TemplateLink
    {
        public string contentVersion
        {
            get
            {
                return "1.0.0.0";
            }
        }
        public string uri { get; set; }
    }

    public class TemplateProperties
    {
        public string mode
        {
            get
            {
                return "Incremental";
            }
        }
        public TemplateLink templateLink { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TemplateLink parametersLink { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public JObject parameters { get; set; }
    }

    public class NestedResourceTemplate
    {
        public string name { get; set; }
        public string type
        {
            get
            {
                return "Microsoft.Resources/deployments";
            }
        }
        public string apiVersion
        {
            get
            {
                return "2016-09-01";
            }
        }
        public List<object> dependsOn { get; set; }
        public TemplateProperties properties { get; set; }
        public IList<JObject> resources { get; set; }

        public NestedResourceTemplate()
        {
            properties = new TemplateProperties();
            dependsOn = new List<object>();
            properties.parametersLink = new TemplateLink();
            properties.templateLink = new TemplateLink();
        }
    }
}