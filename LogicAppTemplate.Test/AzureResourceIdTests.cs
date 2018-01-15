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

        [TestMethod()]
        public void AzureResourceIdGetNameBasedOnTypeName()
        {
            //subscriptions/fakeecb73-d0ff-455d-a2bf-eae0b300696d/providers/Microsoft.Web/locations/westeurope/managedApis/filesystem
            string reourceid = "subscriptions/fakeecb73-d0ff-455d-a2bf-eae0b300696d/providers/Microsoft.Web/locations/westeurope/managedApis/filesystem";
            var ari = new AzureResourceId(reourceid);
            Assert.AreEqual("westeurope", ari.ValueAfter("locations"));
            Assert.AreEqual(null, ari.ValueAfter("a"));

        }

        [TestMethod()]
        public void AzureResourceIdToString()
        {
            string reourceid = "/subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/INT001.Invoice/providers/Microsoft.Web/connections/Billogram";
            var ari = new AzureResourceId(reourceid);
            Assert.AreEqual(reourceid, ari.ToString());

        }

        [TestMethod()]
        public void AzureResourceIdReplaceAndToString()
        {
            string reourceid = "/subscriptions/89d02439-770d-43f3-9e4a-8b910457a10c/resourceGroups/INT001.Invoice/providers/Microsoft.Web/connections/Billogram";
            var ari = new AzureResourceId(reourceid);
            ari.SubscriptionId = "',subscription().subscriptionId,'";
            ari.ResourceGroupName = "',parameters('rgname-something'),'";
            Assert.AreEqual("/subscriptions/',subscription().subscriptionId,'/resourceGroups/',parameters('rgname-something'),'/providers/Microsoft.Web/connections/Billogram", ari.ToString());
        }

        [TestMethod()]
        public void AzureResourceIdReplaceValueAfterAndToString()
        {
            string reourceid = "subscriptions/fakeecb73-d0ff-455d-a2bf-eae0b300696d/providers/Microsoft.Web/locations/westeurope/managedApis/filesystem";
            var ari = new AzureResourceId(reourceid);
            ari.ReplaceValueAfter("locations", "',parameters('logicapplocation'),'");

            Assert.AreEqual("subscriptions/fakeecb73-d0ff-455d-a2bf-eae0b300696d/providers/Microsoft.Web/locations/',parameters('logicapplocation'),'/managedApis/filesystem", ari.ToString());
        }

    }
}