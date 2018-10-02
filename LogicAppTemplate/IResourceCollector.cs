using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace LogicAppTemplate
{
    public interface IResourceCollector
    {
        string Login(string tenantName);
        Task<JObject> GetResource(string resourceId, string apiVersion = null, string suffix = "");

        Task<string> GetRawResource(string resourceId, string apiVersion = null, string suffix = "");
        
    }
}