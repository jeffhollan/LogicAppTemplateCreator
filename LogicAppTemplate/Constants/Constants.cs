using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate
{
    public static class Constants
    {
        internal static readonly string deploymentSchema = @"https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#";
     


        internal static readonly string deploymentTemplate = @"{
  ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""parameters"": {
    ""logicAppName"": {
      ""type"": ""string""
    }
  },
  ""variables"": {
  },
  ""resources"": [
    {
      ""type"": ""Microsoft.Logic/workflows"",
      ""apiVersion"": ""2016-06-01"",
      ""name"": ""[parameters('logicAppName')]"",
      ""dependsOn"": [],
      ""location"": ""[resourceGroup().location]"",
      ""properties"": {
        ""definition"": { },
        ""parameters"": { }
      }
    }
  ],
  ""outputs"": { }
}
";

        public static string AuthString = "https://login.windows.net/common/oauth2/authorize";
        public static string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        public static string ResourceUrl = "https://management.core.windows.net/";
        public static string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";
    }
}

