using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogicAppTemplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LogicAppTemplate.Tests
{
    
    [TestClass()]
    public class TemplateGeneratorTests
    {
        private const string armtoken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IlliUkFRUlljRV9tb3RXVkpLSHJ3TEJiZF85cyIsImtpZCI6IlliUkFRUlljRV9tb3RXVkpLSHJ3TEJiZF85cyJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuY29yZS53aW5kb3dzLm5ldC8iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWF0IjoxNDY5MjEwNzA1LCJuYmYiOjE0NjkyMTA3MDUsImV4cCI6MTQ2OTIxNDYwNSwiX2NsYWltX25hbWVzIjp7Imdyb3VwcyI6InNyYzEifSwiX2NsYWltX3NvdXJjZXMiOnsic3JjMSI6eyJlbmRwb2ludCI6Imh0dHBzOi8vZ3JhcGgud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3L3VzZXJzL2VlZWZkOTVhLWI3YzktNGM2Yi05NjAxLTdmM2RlY2Q5MGY1YS9nZXRNZW1iZXJPYmplY3RzIn19LCJhY3IiOiIxIiwiYW1yIjpbIndpYSIsIm1mYSJdLCJhcHBpZCI6IjE5NTBhMjU4LTIyN2ItNGUzMS1hOWNmLTcxNzQ5NTk0NWZjMiIsImFwcGlkYWNyIjoiMCIsImZhbWlseV9uYW1lIjoiSG9sbGFuIiwiZ2l2ZW5fbmFtZSI6IkplZmYiLCJpbl9jb3JwIjoidHJ1ZSIsImlwYWRkciI6IjEzMS4xMDcuMTYwLjEwOSIsIm5hbWUiOiJKZWZmIEhvbGxhbiIsIm9pZCI6ImVlZWZkOTVhLWI3YzktNGM2Yi05NjAxLTdmM2RlY2Q5MGY1YSIsIm9ucHJlbV9zaWQiOiJTLTEtNS0yMS0yMTI3NTIxMTg0LTE2MDQwMTI5MjAtMTg4NzkyNzUyNy0xMjE5MTU0NCIsInB1aWQiOiIxMDAzQkZGRDg2NDk4M0UyIiwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwic3ViIjoiUVBWMkhwaEhFUDFZdFVmNHI2MGp6Q3JHY0dEUnlzcTBkUkk4WlJWaVlGdyIsInRpZCI6IjcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0NyIsInVuaXF1ZV9uYW1lIjoiamVob2xsYW5AbWljcm9zb2Z0LmNvbSIsInVwbiI6ImplaG9sbGFuQG1pY3Jvc29mdC5jb20iLCJ2ZXIiOiIxLjAifQ.icv3jOI5jda3G6gJSC1jUswFZwg8FBXp5d0qISs52D6vUmpiGniPBaKkbhR2rGGHO9tEySNyWPAIHB_MDaTtcHGeDgelImjw0NCWXv3-daHoipuJHu6m5mCk4DAJlRrSNpGgj-WrxQnBc-z9VXF9-f5xwEJVAyVoRkI6daKG7YYJEZ03lIJXt6s1rZsEpDEdBzyYN6K91Mqr0oRy6UrlbLHa_jM-Vlw8ib340dkOOfckmi-uaQ063NjlIQYtpKTXg0100b69P_y3fhhw3VBvnz_U_WlCXrqZG4t3VJeD_xEgZGnCeCkkV62etnvyEVCXEHZcr4DLzPzp0TKwIRoh6A";

        [TestMethod()]
        public void generateDefinitionTest()
        {
            
        }

        [TestMethod()]
        public void ConvertWithTokenTest()
        {
            LogicAppTemplate.TemplateGenerator generator = new TemplateGenerator(armtoken);
            var result = generator.ConvertWithToken(subscriptionId: "80d4fe69-c95b-4dd2-a938-9250f1c8ab03", resourceGroup: "Premium", logicAppName: "TweetAnalysis", bearerToken: armtoken).Result;
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.IsNotNull(result);
        }
    }
}