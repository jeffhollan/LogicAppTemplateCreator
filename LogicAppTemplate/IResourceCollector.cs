using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace LogicAppTemplate
{
    public interface IResourceCollector
    {
        string Login(string tenantName);
        Task<JObject> GetResource(string resourceId, string apiVersion, string suffix = "");
    }
}