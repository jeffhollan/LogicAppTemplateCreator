using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplate.Templates
{
    public class ARMTemplateClass
    {
        [JsonProperty("$schema")]
        public string schema
        {
            get
            {
                return Constants.deploymentSchema;
            }
        }
        public string contentVersion
        {
            get
            {
                return "1.0.0.0";
            }
        }

        public JObject parameters { get; set; }
        public JObject variables { get; set; }

        public IList<JObject> resources { get; set; }

        public JObject outputs { get; set; }

        public ARMTemplateClass()
        {
            parameters = new JObject();
            variables = new JObject();
            resources = new List<JObject>();
            outputs = new JObject();

        }

        public static ARMTemplateClass FromString(string template)
        {
            return JsonConvert.DeserializeObject<ARMTemplateClass>(template);
        }


        public void RemoveResources_OfType(string type)
        {
            var resources = this.resources.Where(rr => rr.Value<string>("type") == type);
            int count = resources.Count();
            for (int i = 0; i < count; i++)
            {
                RemoveResource(resources.First());
            }
        }


        private void RemoveResource(JObject resource)
        {
            this.parameters.Remove(resource.Value<string>("name").Replace("[parameters('", "").Replace("')]", ""));
            this.resources.Remove(resource);
        }

        public string AddParameter(string paramname, string type, object defaultvalue)
        {
            return AddParameter(paramname, type, new JProperty("defaultValue", defaultvalue));
        }


        public string AddParameter(string paramname, string type, JProperty defaultvalue)
        {
            string realParameterName = paramname;
            JObject param = new JObject();
            param.Add("type", JToken.FromObject(type));
            param.Add(defaultvalue);

            if (this.parameters[paramname] == null)
            {
                this.parameters.Add(paramname, param);
            }
            else
            {
                if (!this.parameters[paramname].Value<string>("defaultValue").Equals(defaultvalue.Value.ToString()))
                {
                    foreach (var p in this.parameters)
                    {
                        if (p.Key.StartsWith(paramname))
                        {
                            for (int i = 2; i < 100; i++)
                            {
                                realParameterName = paramname + i.ToString();
                                if (this.parameters[realParameterName] == null)
                                {
                                    this.parameters.Add(realParameterName, param);
                                    return realParameterName;
                                }
                            }
                        }
                    }
                }
            }
            return realParameterName;
        }

        public string AddVariable(string variablename, string value)
        {
            string realVariableName = variablename;

            if (this.variables[variablename] == null)
            {
                this.variables.Add(variablename, value);
            }
            else
            {
                foreach (var p in this.variables)
                {
                    if (p.Key.StartsWith(variablename))
                    {
                        for (int i = 2; i < 100; i++)
                        {
                            realVariableName = variablename + i.ToString();
                            if (this.variables[realVariableName] == null)
                            {
                                this.variables.Add(realVariableName, value);
                                return realVariableName;
                            }
                        }
                    }
                }
            }
            return realVariableName;
        }


        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string WrapParameterName(string paramname)
        {
            return "[parameters('" + paramname + "')]";
        }
        public string RemoveWrapParameter(string parameterstring)
        {
            return parameterstring.Replace("[parameters('", "").Replace("')]", "");
        }

        private string GetParameterName(string name, string ending)
        {
            if (!name.StartsWith("[parameters('"))
                return name;

            var tmpname = RemoveWrapParameter(name);
            if (!string.IsNullOrEmpty(ending) && tmpname.Contains(ending))
            {
                tmpname = tmpname.Substring(0, tmpname.IndexOf("_name"));
            }
            return tmpname;
        }

        public void AddParameterFromObject(JObject obj, string propertyName, string propertyType, string paramNamePrefix = "")
        {

            var propValue = (string)obj[propertyName];
            if (propValue == null || (propValue.StartsWith("[") && propValue.EndsWith("]")))
                return;
            obj[propertyName] = WrapParameterName(this.AddParameter(paramNamePrefix + "_" + propertyName, propertyType, obj[propertyName]));
        }
    }
}
