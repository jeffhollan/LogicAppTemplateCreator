## Logic App Template Creator Script

This is a simple PowerShell script module I wrote to convert Logic Apps into a template that can be included in a deployment.  

### How to use

Clone the project, open, and build.

Open PowerShell and Import the module:

`Import-Module C:\{pathToSolution}\LogicAppTemplateCreator\LogicAppTemplate\bin\Debug\LogicAppTemplate.dll`

Run the PowerShell command `Get-LogicAppTemplate`.  You can pipe the output as needed, and recommended you pipe in a token from `armclient`

`armclient token 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 | Get-LogicAppTemplate -LogicApp MyApp -ResourceGroup Integrate2016 -SubscriptionId 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 -Verbose | Out-File C:\template.json`

Example when user is connected to multitenants:
`Get-LogicAppTemplate -LogicApp MyApp -ResourceGroup Integrate2016 -SubscriptionId 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 -TenantName contoso.onmicrosoft.com`

### Specifications

| Parameter | Description | Required |
| --------- | ---------- | -------|
| LogicApp | The name of the Logic App | true |
| ResourceGroup | The name of the Resource Group | true |
| SubscriptionId | The subscription Id for the resource | true |
| TenantName | Name of the Tenant i.e. contoso.onmicrosoft.com | false |
| Token | An AAD Token to access the resources - should not include `Bearer`, only the token | false |
| ClaimsDump | A dump of claims piped in from `armclient` - should not be manually set | false |


After extraction a parameters file can be created off the LogicAppTemplate. (works on any ARM template file):
`Get-ParameterTemplate -TemplateFile $filenname | Out-File 'paramfile.json'`

For extraction with KeyVault reference liks created use: (only static reference)
`Get-ParameterTemplate -TemplateFile $filenname -KeyVault Static | Out-File $filennameparam`

### Specifications

| Parameter | Description | Required |
| --------- | ---------- | -------|
| TemplateFile | File path to the template file | true |
| KeyVault | Enum describing how to hanndle KeyVault possible values Static Noce, default None | false |
