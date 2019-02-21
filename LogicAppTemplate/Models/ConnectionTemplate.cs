using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate.Models
{
    public class Api
    {
        public string id { get; set; }
    }

    

    public class Properties
    {
        public Properties(string name, string apiId)
        {
            displayName = name;
            api = new Api { id = apiId};
        }
        public Api api { get; set; }
        public string displayName { get; set; }

        //only fill connectionParameters when source not empty, otherwise saved credentials will be lost.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public dynamic parameterValues { get; set; }
    }

    public class ConnectionTemplate
    {
        private string connectionName = "";
        public ConnectionTemplate(string name, string apiId)
        {
            connectionName = name;
            properties = new Properties(name, apiId);
        }
        public string type { get {
                return "Microsoft.Web/connections";
            } }
        public string apiVersion { get {
                return "2016-06-01";
            } }
        public string location { get {
                return "[parameters('logicAppLocation')]";
            } }
        public string name { get {
                return $"[parameters('{connectionName}')]";
            } }
        public Properties properties { get; set; }
    }
}
