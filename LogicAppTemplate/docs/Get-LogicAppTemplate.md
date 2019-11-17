---
external help file: LogicAppTemplate.dll-Help.xml
Module Name: LogicAppTemplate
online version:
schema: 2.0.0
---

# Get-LogicAppTemplate

## SYNOPSIS
Generate LogicApp ARM template

## SYNTAX

```
Get-LogicAppTemplate -LogicApp <String> -ResourceGroup <String> -SubscriptionId <String> [-TenantName <String>]
 [-Token <String>] [-ClaimsDump <String>] [-DebugOutPutFolder <String>] [-DiagnosticSettings]
 [-IncludeInitializeVariable] [-FixedFunctionAppName] [-GenerateHttpTriggerUrlOutput] [-StripPassword] [-DisabledState] [<CommonParameters>]
```

## DESCRIPTION
Generate a valid ARM template from a LogicApp directly from the Azure Portal.

There has been a change from previous version on parameters that where Boolean are now SwitchParameter there will be an error when you run it the first time.
Error is easy fixed, in your script just remove the $true part in your command se example bellow:
```powershell
 -DiagnosticSettings $true 
 ```
 To:
 ```powershell
 -DiagnosticSettings
 ```

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-LogicAppTemplate -LogicApp "AwesomeLogicApp" -ResourceGroup "LogicAppsDEV" -SubscriptionId "5c92054d-fab6-4dd7-9195-15cd935fa0a4"
```

This will connect to the Azure Portal, ask for your credentails like you are used to.  
It will switch context to the supplied subscription id ("5c92054d-fab6-4dd7-9195-15cd935fa0a4").  
The subscription must exists in your default / standard AAD.  
It will locate the "LogicAppsDEV" ressource group.  
Inside the ressource group it will locate the "AwesomeLogicApp" logic app.

The cmdlet will output the entire json string to the pipeline / console.  
  
### Example 2
```powershell
PS C:\> Get-LogicAppTemplate -LogicApp "AwesomeLogicApp" -ResourceGroup "LogicAppsDEV" -SubscriptionId "5c92054d-fab6-4dd7-9195-15cd935fa0a4" | Out-File -FilePath "c:\temp\AwesomeLogicApp.json" -Encoding utf8
```

This will connect to the Azure Portal, ask for your credentails like you are used to.  
It will switch context to the supplied subscription id ("5c92054d-fab6-4dd7-9195-15cd935fa0a4").  
The subscription must exists in your default / standard AAD.  
It will locate the "LogicAppsDEV" ressource group.  
Inside the ressource group it will locate the "AwesomeLogicApp" logic app.

The output from Get-LogicAppTemplate is piped to Out-File.  
The file will saved to "c:\temp\AwesomeLogicApp.json".  
The file is saved with utf8 encoding.  

### Example 3
```powershell
PS C:\> Get-LogicAppTemplate -LogicApp "AwesomeLogicApp" -ResourceGroup "LogicAppsDEV" -SubscriptionId "5c92054d-fab6-4dd7-9195-15cd935fa0a4" -TenantName "contoso.onmicrosoft.com"
```

This will connect to the Azure Portal, ask for your credentails like you are used to.  
It will use the "contoso.onmicrosoft.com" as the tenant name while looking for the subscriptionId.  
It will switch context to the supplied subscription id ("5c92054d-fab6-4dd7-9195-15cd935fa0a4").  
The subscription must exists in your default / standard AAD.  
It will locate the "LogicAppsDEV" ressource group.  
Inside the ressource group it will locate the "AwesomeLogicApp" logic app.

The cmdlet will output the entire json string to the pipeline / console.
  
### Example 4
```powershell
PS C:\> Get-LogicAppTemplate -LogicApp "AwesomeLogicApp" -ResourceGroup "LogicAppsDEV" -SubscriptionId "5c92054d-fab6-4dd7-9195-15cd935fa0a4" -Token "eyJ0eXAi......."
```

This will connect to the Azure Portal, and use the provided token to authenticate to gain access.
It will switch context to the supplied subscription id ("5c92054d-fab6-4dd7-9195-15cd935fa0a4").  
The subscription must exists in your default / standard AAD.  
It will locate the "LogicAppsDEV" ressource group.  
Inside the ressource group it will locate the "AwesomeLogicApp" logic app.

The cmdlet will output the entire json string to the pipeline / console.
  
## PARAMETERS

### -LogicApp
Name of the Logic App

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ResourceGroup
Name of the Resource Group where the Logic App is stored

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SubscriptionId
The SubscriptionId that the resource group is located in

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TenantName
Name of the Tenant that the subscription exists in

This parameter is important when you are trying to access a Logic App from another Azure AD (AAD) tenant than your own primary AAD tenant

E.g. "contoso.onmicrosoft.com"

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Token
A valied bearer token used to authenticate against the Azure Portal

The value must only be the raw token and NOT contain the "Bearer " part of a token

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ClaimsDump
Piped input from armclient

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -DiagnosticSettings
Instructs the cmdlet to included diagnostic in the ARM template

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeInitializeVariable
Initialize Variable actions will be included in the ARM template

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FixedFunctionAppName
Function App gets a static name

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GenerateHttpTriggerUrlOutput
Generate an output variable with the http trigger url, usuable in linked templates when url is needed in nested templates.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StripPassword
Passwords will be stripped out of the output

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DisabledState
The Logic App will be set to Disabled in the ARM Template and won't be automatically run when deployed

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DebugOutPutFolder
If set, result from rest interface will be saved to this folder

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```


### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

## OUTPUTS

### System.Object
## NOTES
If you want to avoid signing into the Azure Portal, you could spend some time on getting familiar with the ARMClient project.  
This will enable you to get a valid token for your personal user account.

https://github.com/projectkudu/ARMClient

## RELATED LINKS

