using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate.Models
{
    public class ParameterTemplate
    {
        [JsonProperty("$schema")]
        public string schema
        {
            get
            {
                return Constants.parameterSchema;
            }
        }
        public string contentVersion
        {
            get
            {
                return "1.0.0.0";
            }
        }

        public JObject parameters { get; set; }
    }
}
