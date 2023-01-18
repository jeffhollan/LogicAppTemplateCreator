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
        public Tuple<string, string> Provider
        {
            get
            {
                for (var i = 0; i < splittedId.Length; i++)
                {
                    if (splittedId[i] == "providers")
                    {
                        return new Tuple<string, string>(splittedId[i+1], splittedId[i+2]);
                    }
                }
                return null;
            }
            set
            {
                for (var i = 0; i < splittedId.Length; i++)
                {
                    if (splittedId[i] == "providers")
                    {
                        splittedId[i + 1] = value.Item1;
                        splittedId[i + 2] = value.Item2;
                    }
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

        public string ValueAfter(String type)
        {
            var rest = this.splittedId.SkipWhile(s => s != type).Skip(1);            
            return rest.FirstOrDefault();            
        }

        public void ReplaceValueAfter(String type, string value)
        {
            var position = this.splittedId.TakeWhile(s => s != type).Count() + 1;
            if (position < this.splittedId.Length)
                this.splittedId[position] = value;
        }

        private string[] splittedId;
        public AzureResourceId (string resourceid)
        {
            this.splittedId = resourceid.Split('/');
        }

        public override string ToString()
        {
            return splittedId.Aggregate( (a,n) => { return a + '/' + n; } ).ToString();
        }
    }
}
