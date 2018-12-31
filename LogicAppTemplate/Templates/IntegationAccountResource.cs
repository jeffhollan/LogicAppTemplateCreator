using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate.Templates
{
    public class IntegationAccountResource
    {
        public string name {get;set;}
        public string type { get; set; }

        public string apiVersion { get; set; }
        public string location { get; set; }
        public JObject tags { get; set; }
        public JObject properties { get; set; }

        public IntegationAccountResource()
        {
            tags = new JObject();
            properties = new JObject();
            apiVersion = "2016-06-01";         
        }

        public static ARMTemplateClass FromString(string template)
        {
            return JsonConvert.DeserializeObject<ARMTemplateClass>(template);
        }


        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
