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
        private const string armtoken = "";
        [TestMethod()]
        public void generateDefinitionTest()
        {
            
        }

        [TestMethod()]
        public void ConvertWithTokenTest()
        {
            LogicAppTemplate.TemplateGenerator generator = new TemplateGenerator(armtoken);
            var result = generator.ConvertWithToken(subscriptionId: "80d4fe69-c95b-4dd2-a938-9250f1c8ab03", resourceGroup: "TemplatesBrazilSouth", logicAppName: "AlertToText", bearerToken: armtoken).Result;
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.IsNotNull(result);
        }
    }
}