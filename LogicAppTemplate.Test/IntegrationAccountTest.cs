using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LogicAppTemplate.Test
{
    [TestClass]
    public class IntegrationAccountTest
    {

        private JObject GetTemplate()
        {
            var generator = new TemplateGenerator("test", "9fake3d-3c94-40e9-b2cb-18921e5d6cfa", "LogicAppsDev", new MockResourceCollector("IntegrationAccountMaps"));

            return generator.GenerateTemplate().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void GenerateTemplate()
        {
            var defintion = GetTemplate();
            Assert.IsNotNull(defintion);
        }
        [TestMethod]
        public void IntegrationJsonMapTest()
        {
            var defintion = GetTemplate();

            var JsonMap = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/integrationAccounts/maps" && jj.Value<string>("name") == "[concat(parameters('IntegrationAccountName'), '/' ,parameters('Transform_JSON_to_JSON-MapName'))]").First();
            Assert.AreEqual("[parameters('logicAppLocation')]", JsonMap.Value<string>("location"));
            Assert.AreEqual("2016-06-01", JsonMap.Value<string>("apiVersion"));
            Assert.AreEqual("Liquid", JsonMap["properties"].Value<string>("mapType"));
            Assert.AreEqual("{\r\n\\\"product_id\\\": \\\"{{content.prodId}}\\\",\r\n\\\"product_name\\\": \\\"{{content.prodName}}\\\"\r\n}", JsonMap["properties"].Value<string>("content"));
            Assert.AreEqual("Liquid", JsonMap["text"].Value<string>("contentType"));

        }
        [TestMethod]
        public void IntegrationXsltMapTest()
        {
            var defintion = GetTemplate();

            var map = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/integrationAccounts/maps" && jj.Value<string>("name") == "[concat(parameters('IntegrationAccountName'), '/' ,parameters('Transform_XML-MapName'))]").First();
            Assert.AreEqual("[parameters('logicAppLocation')]", map.Value<string>("location"));
            Assert.AreEqual("2016-06-01", map.Value<string>("apiVersion"));
            Assert.AreEqual("Xslt", map["properties"].Value<string>("mapType"));
            Assert.IsNotNull(map["properties"].Value<string>("parametersSchema"));
            Assert.AreEqual("<?xml version=\\\"1.0\\\" encoding=\\\"utf-8\\\"?>\r\n<xsl:stylesheet version=\\\"1.0\\\" xmlns:xsl=\\\"http://www.w3.org/1999/XSL/Transform\\\"\r\n    xmlns:msxsl=\\\"urn:schemas-microsoft-com:xslt\\\" exclude-result-prefixes=\\\"msxsl\\\">\r\n\r\n\t<xsl:output method=\\\"xml\\\" indent=\\\"yes\\\"/>\r\n\t<xsl:param name=\\\"userPrincipalName\\\"/>\r\n\t<xsl:param name=\\\"employeeID\\\"/>\r\n\t<xsl:param name=\\\"givenName\\\"/>\r\n\t<xsl:param name=\\\"SN\\\"/>\r\n\t<xsl:param name=\\\"middleName\\\"/>\r\n\t<xsl:param name=\\\"displayName\\\"/>\r\n\t<xsl:param name=\\\"company\\\"/>\r\n\t<xsl:param name=\\\"alfaLavalMISALCompanyCode\\\"/>\r\n\t<xsl:param name=\\\"department\\\"/>\r\n\t<xsl:param name=\\\"departmentNumber\\\"/>\r\n\t<xsl:param name=\\\"alfaLavalResourceManager\\\"/>\r\n\t<!-- <xsl:param name=\\\"alfaLavalExternalCompanyEmail\\\"/> -->\r\n\t<xsl:param name=\\\"count\\\"/>\r\n\t<xsl:param name=\\\"employeeType\\\"/>\r\n\t<xsl:param name=\\\"c\\\"/>\r\n\t<xsl:param name=\\\"co\\\"/>\r\n\t<xsl:param name=\\\"l\\\"/>\r\n\t<xsl:param name=\\\"manager\\\"/>\r\n\t<xsl:param name=\\\"carLicense\\\"/>\r\n\t<xsl:param name=\\\"isManager\\\"/>\r\n\t<xsl:param name=\\\"gender\\\"/>\r\n\t<xsl:param name=\\\"SAMAccount\\\"/>\r\n\t<xsl:param name=\\\"OUCountry\\\"/>\r\n\t<xsl:param name=\\\"OUITSite\\\"/>\r\n  <xsl:param name=\\\"physicalDeliveryOfficeName\\\"/>\r\n\t<xsl:param name=\\\"title\\\"/>\r\n\t<xsl:template match=\\\"/*\\\">\r\n\t\t<ns0:LDAP xmlns:ns0=\\\"http://integrationsoftware.se/BizTalk/Adapters/LDAP/Request/1.0\\\">\r\n\t\t\t<Batches guid=\\\"B7A211D7-20F3-44BC-B078-7F1B6E1C047F\\\" returnResponseMessageOnExceptions=\\\"true\\\">\r\n\t\t\t\t<Batch guid=\\\"2B25B9E6-4AF2-4094-9AE7-E1E4C99B6C8D\\\" continueOnError =\\\"false\\\">\r\n\t\t\t\t\t<User>\r\n\t\t\t\t\t\t<With>\r\n\t\t\t\t\t\t\t<xsl:attribute name=\\\"OU\\\">\r\n\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"concat('OU=Users,', $OUITSite, ',', $OUCountry, ',', 'OU=AlfaLaval' )\\\"/>\r\n\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t<xsl:attribute name=\\\"DC\\\">\r\n\t\t\t\t\t\t\t\t<xsl:text>DC=alfadev,DC=org</xsl:text>\r\n\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t<xsl:attribute name=\\\"searchScope\\\">\r\n\t\t\t\t\t\t\t\t<xsl:text>Base</xsl:text>\r\n\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t<xsl:attribute name=\\\"returnProperties\\\">\r\n\t\t\t\t\t\t\t\t<xsl:text>userPrincipalName,mail</xsl:text>\r\n\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t<xsl:attribute name=\\\"expectedMatchCount\\\">\r\n\t\t\t\t\t\t\t\t<xsl:text>1</xsl:text>\r\n\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t</With>\r\n\t\t\t\t\t\t<Operations>\r\n\t\t\t\t\t\t\t<Create>\r\n\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">\r\n\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"concat($displayName, $count)\\\"/>\r\n\t\t\t\t\t\t\t\t</xsl:attribute>\t\t\t\t\t\t\t\r\n\t\t\t\t\t\t\t\t<Properties>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">sAMAccountName</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$SAMAccount\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">userPrincipalName</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$userPrincipalName\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">mail</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$userPrincipalName\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">employeeID</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$employeeID\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">givenName</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$givenName\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">SN</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$SN\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">middleName</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$middleName\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">displayName</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$displayName\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t  <Property>\r\n\t\t\t\t\t\t\t\t    <xsl:attribute name=\\\"name\\\">division</xsl:attribute>\r\n\t\t\t\t\t\t\t\t    <xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t      <xsl:value-of select=\\\"$company\\\"/>\r\n\t\t\t\t\t\t\t\t    </xsl:attribute>\r\n\t\t\t\t\t\t\t\t  </Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">company</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$alfaLavalMISALCompanyCode\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">department</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$department\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">departmentNumber</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$departmentNumber\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">employeeType</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$employeeType\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">c</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$c\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">co</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$co\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">l</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$l\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">carLicense</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$carLicense\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">title</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$title\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">primaryTelexNumber</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$gender\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t  <Property>\r\n\t\t\t\t\t\t\t\t    <xsl:attribute name=\\\"name\\\">physicalDeliveryOfficeName</xsl:attribute>\r\n\t\t\t\t\t\t\t\t    <xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t      <!--xsl:value-of select=\\\"concat($OUCountry, ' ' , $OUITSite)\\\" /-->\r\n\t\t\t\t\t\t\t\t      <xsl:value-of select=\\\"$physicalDeliveryOfficeName\\\"/>\r\n\t\t\t\t\t\t\t\t    </xsl:attribute>\r\n\t\t\t\t\t\t\t\t  </Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">alfaLavalStaffManager</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:choose>\r\n\t\t\t\t\t\t\t\t\t\t\t\t<xsl:when test=\\\"$isManager = 0\\\">FALSE</xsl:when>\r\n\t\t\t\t\t\t\t\t\t\t\t\t<xsl:otherwise>TRUE</xsl:otherwise>\r\n\t\t\t\t\t\t\t\t\t\t\t</xsl:choose>\r\n\t\t\t\t\t\t\t\t\t\t\t<!-- <xsl:value-of select=\\\"boolean($isManager)\\\"/> -->\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">manager</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$manager\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">userAccountControl</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">544</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property>\r\n\t\t\t\t\t\t\t\t\t<!-- \t\t\t\t\t\t\t\t\t<Property>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"name\\\">alfaLavalMISALCompanyCode</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t\t<xsl:attribute name=\\\"value\\\">\r\n\t\t\t\t\t\t\t\t\t\t\t<xsl:value-of select=\\\"$alfaLavalMISALCompanyCode\\\"/>\r\n\t\t\t\t\t\t\t\t\t\t</xsl:attribute>\r\n\t\t\t\t\t\t\t\t\t</Property> -->\r\n\t\t\t\t\t\t\t\t</Properties>\r\n\t\t\t\t\t\t\t</Create>\r\n\t\t\t\t\t\t</Operations>\r\n\t\t\t\t\t</User>\r\n\t\t\t\t</Batch>\r\n\t\t\t</Batches>\r\n\t\t</ns0:LDAP>\r\n\r\n\r\n\r\n\t</xsl:template>\r\n\r\n</xsl:stylesheet>\r\n", map["properties"].Value<string>("content"));
            Assert.AreEqual("Liquid", map["text"].Value<string>("contentType"));

        }

        [TestMethod]
        public void IntegrationAccountWorkflowTest()
        {
            var defintion = GetTemplate();

            var workflow = defintion.Value<JArray>("resources").Where(jj => jj.Value<string>("type") == "Microsoft.Logic/workflows" && jj.Value<string>("name") == "[parameters('logicAppName')]").First();
            Assert.AreEqual("[parameters('logicAppLocation')]", workflow.Value<string>("location"));
            Assert.AreEqual("2016-06-01", workflow.Value<string>("apiVersion"));

            var dependsOn = workflow.Value<JArray>("dependsOn");

            Assert.AreEqual(2, dependsOn.Count());

            Assert.AreEqual("[resourceId('Microsoft.Logic/integrationAccounts/maps', parameters('IntegrationAccountName'),parameters('Transform_JSON_to_JSON-MapName'))]", dependsOn[0].ToString());
            Assert.AreEqual("[resourceId('Microsoft.Logic/integrationAccounts/maps', parameters('IntegrationAccountName'),parameters('Transform_XML-MapName'))]", dependsOn[1].ToString());

            var jsonToJsonAction = workflow["properties"]["definition"]["actions"].Value<JObject>("Transform_JSON_to_JSON");
            Assert.AreEqual("Liquid", jsonToJsonAction.Value<string>("type"));
            Assert.AreEqual("JsonToJson", jsonToJsonAction.Value<string>("kind"));
            Assert.AreEqual("[parameters('Transform_JSON_to_JSON-MapName')]", jsonToJsonAction["inputs"]["integrationAccount"]["map"].Value<string>("name"));
        }
    }
}
