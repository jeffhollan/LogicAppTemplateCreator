using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConverterLibrary
{
    public static class Constants
    {
        internal static readonly string deploymentSchema = @"https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#";
     


        internal static readonly string deploymentTemplate = @"{
  ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""parameters"": {
    ""svcPlanName"": {
      ""type"": ""string""
    },
    ""logicAppName"": {
      ""type"": ""string""
    },
    ""sku"": {
      ""type"": ""string"",
      ""defaultValue"": ""Standard"",
      ""allowedValues"": [
        ""Free"",
        ""Basic"",
        ""Standard"",
        ""Premium""
      ],
      ""metadata"": {
        ""description"": ""The pricing tier for the logic app.""
      }
    }
  },
  ""variables"": {
  },
  ""resources"": [
    {
      ""apiVersion"": ""2014-06-01"",
      ""name"": ""[parameters('svcPlanName')]"",
      ""type"": ""Microsoft.Web/serverfarms"",
      ""location"": ""[resourceGroup().location]"",
      ""properties"": {
        ""name"": ""[parameters('svcPlanName')]"",
        ""sku"": ""[parameters('sku')]"",
        ""workerSize"": ""0"",
        ""numberOfWorkers"": 1
      }
    },
    {
      ""type"": ""Microsoft.Logic/workflows"",
      ""apiVersion"": ""2015-08-01-preview"",
      ""name"": ""[parameters('logicAppName')]"",
      ""location"": ""[resourceGroup().location]"",
      ""dependsOn"": [ ""[resourceId('Microsoft.Web/serverfarms', parameters('svcPlanName'))]""  ],
      ""properties"": {
        ""sku"": {
          ""name"": ""[parameters('sku')]"",
          ""plan"": {
            ""id"": ""[concat(resourceGroup().id, '/providers/Microsoft.Web/serverfarms/',parameters('svcPlanName'))]""
          }
        },
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

