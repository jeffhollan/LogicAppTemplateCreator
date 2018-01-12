using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate
{
    public interface IResourceCollector
    {
        string Login(string tenantName);
        Task<JObject> GetResource(string resourceId, string suffix = "");
    }
}
