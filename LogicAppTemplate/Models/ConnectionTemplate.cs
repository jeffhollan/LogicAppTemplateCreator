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
            api = new Api { id = apiId };
        }
        public Api api { get; set; }
        public string displayName { get; set; }
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
                return "2015-08-01-preview";
            } }
        public string location { get {
                return "[resourceGroup().location]";
            } }
        public string name { get {
                return $"[parameters('{connectionName}Name')]";
            } }
        public Properties properties { get; set; }
    }
}
