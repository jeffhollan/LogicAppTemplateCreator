using System.Collections.Generic;

namespace LogicAppTemplate.Models
{
    public class DiagnosticSettingsTemplate
    {
        private string parameterName;

        public DiagnosticSettingsTemplate(string parameterName)
        {
            dependsOn = new List<object>();
            properties = new DiagnosticSettingsProperties(parameterName);
            this.parameterName = parameterName;
        }

        public string type
        {
            get { return "providers/diagnosticSettings"; }
        }

        public string name
        {
            get { return "[concat('Microsoft.Insights/', parameters('" + parameterName + "'))]"; }
        }

        public List<object> dependsOn { get; set; }

        public string apiVersion
        {
            get { return "2017-05-01-preview"; }
        }

        public DiagnosticSettingsProperties properties { get; set; }
    }

    public class DiagnosticSettingsProperties
    {
        private string parameterName;
        public DiagnosticSettingsProperties(string parameterName)
        {
            this.parameterName = parameterName;
            logs = new List<Log>();
            metrics = new List<Metric>();

            logs.Add(new Log());
            metrics.Add(new Metric());
        }
        public string name
        {
            get { return "[parameters('" + parameterName + "')]"; }
        }

        public string workspaceId
        {
            get { return "[concat('/subscriptions/', subscription().subscriptionId, '/resourceGroups/', parameters('" + Constants.DsResourceGroup + "'), '/providers/Microsoft.OperationalInsights/workspaces/', parameters('" + Constants.DsWorkspaceName + "'))]"; }
        }

        public List<Log> logs { get; set; }

        public List<Metric> metrics { get; set; }
    }

    public class Log
    {
        public Log()
        {
            retentionPolicy = new RetentionPolicy()
            {
                days = "[parameters('" + Constants.DsLogsRetentionPolicyDays + "')]",
                enabled = "[parameters('" + Constants.DsLogsRetentionPolicyEnabled + "')]"
            };
        }
        public string category { get { return "WorkflowRuntime"; } }
        public string enabled
        {
            get { return "[parameters('" + Constants.DsLogsEnabled + "')]"; }
        }

        public RetentionPolicy retentionPolicy { get; }
    }

    public class Metric
    {
        public Metric()
        {
            retentionPolicy = new RetentionPolicy()
            {
                days = "[parameters('" + Constants.DsMetricsRetentionPolicyDays + "')]",
                enabled = "[parameters('" + Constants.DsMetricsRetentionPolicyEnabled + "')]"
            };
        }
        public string category { get { return "AllMetrics"; } }
        public string enabled
        {
            get { return "[parameters('" + Constants.DsMetricsEnabled + "')]"; }
        }
        public RetentionPolicy retentionPolicy { get; set; }
    }

    public class RetentionPolicy
    {
        public string days { get; set; }
        public string enabled { get; set; }
    }
}