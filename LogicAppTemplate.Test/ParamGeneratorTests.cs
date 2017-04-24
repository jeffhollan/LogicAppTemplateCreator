using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Newtonsoft.Json.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class ParamGeneratorTests
    {
        [TestMethod]
        public void CreateParamGeneratorClass()
        {
            new ParamGenerator();
        }

        [TestMethod]
        public void GenerateParameterFileFromTemplate()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.paramGeneratorLogicAppTemplate.json");
            var generator = new ParamGenerator();

            var defintion = generator.CreateParameterFileFromTemplate(JObject.Parse(content));

            //check parameters
            Assert.IsNull(defintion["parameters"]["INT0014-NewHires-ResourceGroup"]);
            Assert.AreEqual("[resourceGroup().location]", defintion["parameters"]["logicAppLocation"]["value"]);
            Assert.AreEqual("INT0014-NewHires-Trigger", defintion["parameters"]["logicAppName"]["value"]);
        }


             //var resourceName = "LogicAppTemplate.Templates.starterTemplate.json";
        private static string GetEmbededFileContent(string resourceName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();


            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }

        }
    }
}
