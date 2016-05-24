## Logic App Template Creator Script

This is a simple PowerShell script module I wrote to convert Logic Apps into a template that can be included in a deployment.  

### How to use

Clone the project, open, and build.

Open PowerShell and Import the module:

`Import-Module C:\{pathToSolution}\LogicAppTemplateCreator\LogicAppTemplate\bin\Debug\ConverterLibrary.dll`

Run the PowerShell command `Get-LogicAppTemplate`.  You can pipe the output as needed, and recommended you pipe in a token from `armclient`

`armclient token 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 | Get-LogicAppTemplate -LogicApp MyApp -ResourceGroup Integrate2016 -SubscriptionId 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 -Verbose | Out-File C:\template.json`

### Specifications

| Parameter | Description | Required |
| --------- | ---------- | -------|
| LogicApp | The name of the Logic App | true |
| ResourceGroup | The name of the Resource Group | true |
| SubscriptionId | The subscription Id for the resource | true |
| Token | An AAD Token to access the resources - should not include `Bearer`, only the token | false |
| ClaimsDump | A dump of claims piped in from `armclient` - should not be manually set | false |
