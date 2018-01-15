using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogicAppTemplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate.Tests
{
    [TestClass()]
    public class AzureResourceIdTests
    {
        [TestMethod()]
        public void AzureResourceIdGetSubscriptionTest()
        {
            string reourceid = "/subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/INT001.Invoice/providers/Microsoft.Web/connections/Billogram";
            var ari = new AzureResourceId(reourceid);
            Assert.AreEqual("89d02439-770d-43f3-9e4a-8b910457a10c", ari.SubscriptionId);

        }
        [TestMethod()]
        public void AzureResourceIdGetResourceGroupNameTest()
        {
            string reourceid = "/subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/INT001.Invoice/providers/Microsoft.Web/connections/Billogram";
            var ari = new AzureResourceId(reourceid);
            Assert.AreEqual("INT001.Invoice", ari.ResourceGroupName);

        }

        [TestMethod()]
        public void AzureResourceIdGetResourceName()
        {
            string reourceid = "/subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/INT001.Invoice/providers/Microsoft.Web/connections/Billogram";
            var ari = new AzureResourceId(reourceid);
            Assert.AreEqual("Billogram", ari.ResourceName);

        }
    }
}