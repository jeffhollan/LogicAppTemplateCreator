using System.IO;

namespace LogicAppTemplate.Test
{
    public static class Utils
    {

        //var resourceName = "LogicAppTemplate.Templates.starterTemplate.json";
        public static string GetEmbededFileContent(string resourceName)
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
