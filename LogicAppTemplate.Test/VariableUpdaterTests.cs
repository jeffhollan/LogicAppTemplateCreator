using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Newtonsoft.Json.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class UpdateTemplateVariableTests
    {
        [TestMethod]
        public void CreateUpdateTemplateVariableToValueClass()
        {
            new UpdateTemplateVariableReferenceToValue("","","");
        }

        [TestMethod]
        public void GenerateParameterFileFromTemplate()
        {
            var content = GetEmbededFileContent("LogicAppTemplate.Test.TestFiles.VariableReference.json");
            var generator = new UpdateTemplateVariableReferenceToValue("", "URL", "https://www.nationalbanken.dk/");

            var definition = generator.UpdateTemplateVariable(content);
            Assert.AreEqual(definition["parameters"]["HTTP-URI"]["defaultValue"], (JValue)"https://www.nationalbanken.dk/_vti_bin/DN/DataService.svc/CurrencyRatesXML?lang=da");
        }

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
