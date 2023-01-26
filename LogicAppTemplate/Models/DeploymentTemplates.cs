using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate.Models
{

    public class DeploymentTemplatesProperties
    {
        public string mode { get; set; } = "Incremental";

        public DeploymentTemplate template { get; set; } = new DeploymentTemplate {resources = new List<JObject>()};
    }

    public class DeploymentTemplates
    {
        public DeploymentTemplates(string name, string resourceGroup)
        {
            this.name = name;
            this.resourceGroup = resourceGroup;
        }

        public string name { get; set; }

        public string resourceGroup { get; set; }
        
        public string type => "Microsoft.Resources/deployments";

        public string apiVersion => "2019-10-01";

        public DeploymentTemplatesProperties properties { get; set; } = new DeploymentTemplatesProperties();
        
        public JObject ToJObject()
        {
            return JObject.FromObject(this);
        }

        public void AddResource(JObject resource)
        {
            this.properties.template.resources.Add(resource);
        }
    }
}
