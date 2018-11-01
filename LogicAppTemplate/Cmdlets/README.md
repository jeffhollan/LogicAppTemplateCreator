##Description
   
This script will create template files from all logic apps in a resource group and parameter files and saves them in folders defined as parameter.

It depends on the following components:

   - [AzureRM](https://docs.microsoft.com/en-us/powershell/azure/install-azurerm-ps?view=azurermps-4.2.0 "AzureRM")

##Parameters

**subscriptionname**

    The subscription name where the resource group is deployed.

**tenantname**

    The tenant associated to the subscription

**resourcegroup**

    The Resource Group containing the Logic Apps to be extracted

**destination**

    The local folder where the logic app templates will be created. if this folder doesn't exists it will be created. A new params folder will be created inside the destination folder.

##Usage Example

	./Get-RGLogicAppsTemplate.ps1 "My Subscription" "mytenant.onmicrosoft.com" "MyResourceGroup" "c:\mydestination"

This cmdlet expects the user to be authenticated against Azure. Authentication can be done using *Login-AzureRmAccount*

	Login-AzureRmAccount
	./Get-RGLogicAppsTemplate.ps1 "My Subscription" "mytenant.onmicrosoft.com" "MyResourceGroup" "c:\mydestination"
