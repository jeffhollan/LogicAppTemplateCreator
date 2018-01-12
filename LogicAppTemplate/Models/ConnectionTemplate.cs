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
        public Properties(string name, string apiId,string type = "")
        {
            displayName = name;
            if (type == "Microsoft.Web/customApis")
            {
                api = new Api { id = string.Format("[concat('/subscriptions/', subscription().subscriptionId,'/resourceGroups/', parameters('{1}_ResourceGroupName') ,'/providers/Microsoft.Web/customApis/', '{0}')]", apiId.Split('/').Last(),name.Split('_').First()) };
                /// subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/Messaging/providers/Microsoft.Web/customApis/Billogram
            }
            else
            {
                api = new Api { id = string.Format("[concat('/subscriptions/', subscription().subscriptionId, '/providers/Microsoft.Web/locations/', parameters('logicAppLocation'), '/managedApis/', '{0}')]", apiId.Split('/').Last()) };
            }
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
