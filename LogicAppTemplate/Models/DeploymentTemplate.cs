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
    public class DeploymentTemplate
    {

        [JsonProperty("$schema")]
        public string schema { get
            {
                return Constants.deploymentSchema;
            } }
        public string contentVersion { get
            {
                return "1.0.0.0";
            } }

        public JObject parameters { get; set; }
        public JObject variables { get; set; }

        public IList<JObject> resources { get; set; }

        public JObject outputs { get; set; }
    }
}
