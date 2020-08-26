namespace LogicAppTemplate
{
    public static class Constants
    {
        internal static readonly string deploymentSchema = @"https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#";
        internal static readonly string parameterSchema = @"https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#";

        public static string AuthString = "https://login.microsoftonline.com/common";
        public static string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        public static string ResourceUrl = "https://management.core.windows.net/";
        public static string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

        // Diagnostic Settings
        public static string DsPrefix = "diagnosticSettings_";
        public static string DsName = DsPrefix + "name";
        public static string DsResourceGroup = DsPrefix + "resourceGroupName";
        public static string DsWorkspaceName = DsPrefix + "workspaceName";
        public static string DsLogsEnabled = DsPrefix + "logsEnabled";
        public static string DsLogsRetentionPolicyEnabled = DsPrefix + "logsRetentionPolicyEnabled";
        public static string DsLogsRetentionPolicyDays = DsPrefix + "logsRetentionPolicyDays";
        public static string DsMetricsEnabled = DsPrefix + "metricsEnabled";
        public static string DsMetricsRetentionPolicyEnabled = DsPrefix + "metricsRetentionPolicyEnabled";
        public static string DsMetricsRetentionPolicyDays = DsPrefix + "metricsRetentionPolicyDays";

        // Managed Identity
        public static string UserAssignedIdentityParameterName = "UserAssignedIdentityName";
    }
}