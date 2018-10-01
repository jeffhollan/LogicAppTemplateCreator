using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LogicAppTemplate.Test
{
    public class MockResourceCollector : IResourceCollector
    {
        private string basepath = "";
        public MockResourceCollector(string basepath)
        {
            this.basepath = basepath;
        }
        public Task<JObject> GetResource(string resourceId,string suffix = "")
        {
            var t = new Task<JObject>(() => { return JObject.Parse(Utils.GetEmbededFileContent($"LogicAppTemplate.Test.TestFiles.Samples.{basepath}.{resourceId.Split('/').SkipWhile((a) => { return a != "providers" && a != "integrationAccounts"; }).Aggregate<string>((b, c) => { return b + "-" + c; })}.json")); });
            t.Start();
            return t;
        }
        public Task<string> GetRawResource(string resourceId, string apiversion = "", string suffix = "")
        {
            var t = new Task<string>(() => { return Utils.GetEmbededFileContent($"LogicAppTemplate.Test.TestFiles.Samples.{basepath}.{resourceId.Split('/').SkipWhile((a) => { return a != "providers" && a != "integrationAccounts"; }).Aggregate<string>((b, c) => { return b + "-" + c; })}.json");});
            t.Start();
            return t;
        }

        public Task<JObject> GetResource(string resourceId, string apiVersion, string suffix = "")
        {
            var t = new Task<JObject>(() => { return JObject.Parse(Utils.GetEmbededFileContent($"LogicAppTemplate.Test.TestFiles.Samples.{basepath}.{resourceId.Split('/').SkipWhile((a) => { return a != "providers" && a != "integrationAccounts"; }).Aggregate<string>((b, c) => { return b + "-" + c; })}.json")); });
            t.Start();
            return t;
        }

        public string Login(string tenantName)
        {
            return "mocked";
        }
    }
}
