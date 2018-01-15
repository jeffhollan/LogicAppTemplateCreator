using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate
{
    public class AzureResourceId
    {

        public string ResourceGroupName
        {
            get
            {
                if (this.splittedId.Length > 4)
                {
                    return this.splittedId[4];
                }
                return "";
            }
            set
            {
                if (this.splittedId.Length > 4)
                {
                    this.splittedId[4] = value;
                }
            }
        }
        public string SubscriptionId
        {
            get
            {
                if (this.splittedId.Length > 2)
                {
                    return this.splittedId[2];
                }
                return "";
            }
            set
            {
                if (this.splittedId.Length > 2)
                {
                    this.splittedId[2] = value;
                }
            }
        }
        public string ResourceName
        {
            get
            {
                return this.splittedId.Last();
            }
            set
            {
                this.splittedId[this.splittedId.Length - 1] = value;
            }
        }

        private string[] splittedId;
        public AzureResourceId (string resourceid)
        {
            this.splittedId = resourceid.Split('/');
        }


    }
}
