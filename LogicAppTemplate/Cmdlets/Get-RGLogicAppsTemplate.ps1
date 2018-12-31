<#
.Synopsis
   Create template Files for All logic Apps in a Resource Group.
.DESCRIPTION
   This script will create template files from all logic apps in a resource group and parameter files and saves them in folders defined as parameter.
   It assumes that the user has already been authenticated against Azure.
   It depends on the following components:
   - armclient
   - AzureRM
.EXAMPLE
   ./Get-RGLogicAppsTemplate.ps1 "My Subscription" "mytenant.onmicrosoft.com" "MyResourceGroup" "c:\mydestination"
.PARAMETER subscriptionname
    The subscription name where the resource group is deployed.
.PARAMETER tenantname
    The tenant associated to the subscription
.PARAMETER resourcegroup
    The Resource Group containing the Logic Apps to be extracted
.PARAMETER destination
    The local folder where the logic app templates will be created. if this folder doesn't exists it will be created. A new params folder will be created inside the destination folder.
#>
param([string] $subscriptionname, [string] $tenantname, [string] $resourcegroup, [string] $destination)

# Import the Logic Apps Template Module #
$module = resolve-path ".\..\bin\Debug\LogicAppTemplate.dll"
Import-Module $module

# Create required folders #
md -Force $destination | Out-Null

Write-Host "Acquiring access token. Please wait for the login scree and logon with a contributor user."

$rmAccount = Login-AzureRmAccount
$tenantId = (Get-AzureRmSubscription -SubscriptionName $subscriptionname).TenantId
$tokenCache = $rmAccount.Context.TokenCache
$cachedTokens = $tokenCache.ReadItems() `
        | where { $_.TenantId -eq $tenantId } `
        | Sort-Object -Property ExpiresOn -Descending
$accessToken = $cachedTokens[0].AccessToken

# Select the correct subscription #
Get-AzureRmSubscription -SubscriptionName $subscriptionname | Select-AzureRmSubscription | Out-Null

Write-Host

# Gets a list of logic app
Find-AzureRmResource -ResourceGroupNameContains $resourcegroup -ResourceType Microsoft.Logic/workflows | ForEach-Object{ 

	Write-Host $("Creating {0} Logic App Template" -f $_.Name)

	# Define the destination folder #
	$logicappfolder = [IO.Path]::GetFullPath((Join-Path $destination $_.Name))
	md $logicappfolder -Force | Out-Null
	
	# Define the destination file names #
	$destinationfile = $(Join-path $logicappfolder ($_.Name + ".json"))
	$destinationparmfile = $(Join-path $logicappfolder ($_.Name + ".parameters.json"))
	
	# Create Logic App Template #
	Get-LogicAppTemplate -LogicApp $_.Name -ResourceGroup $_.ResourceGroupName -SubscriptionId $_.SubscriptionId -TenantName $tenantname -Token $accessToken -Verbose | Out-File $destinationfile -Force
	
	# Generate the Parameter File #
	Get-ParameterTemplate -TemplateFile $destinationfile | Out-File $destinationparmfile -Force}

Write-Host
# Initialize Azure Deploy nested Templates variable #
$azuredeploytemplate = ""

Write-Host "Creating AzureDeploy ARM Template"

# Gets a list of resources to add to the nested template #
Get-ChildItem $destination -Directory | ForEach-object {

    # Adds the resource to the nested templates #
    $azuredeploytemplate = Get-NestedResourceTemplate -ResourceName $_.Name -Template $azuredeploytemplate}

#Save nested template to destination #
$azuredeploytemplate | Out-File $(Join-path $destination "azuredeploy.json") -Force

Write-Host

Write-Host "Creating AzureDeploy ARM Parameter Template"

#Generate an empty Azure Deploy Parameter

Get-EmptyParameterTemplate | Out-File $(Join-path $destination "azuredeploy.parameters.json") -Force

Write-Host

Write-Host "Creating Visual Studio Project"

Add-DeploymentVSProject -SourceDir $destination