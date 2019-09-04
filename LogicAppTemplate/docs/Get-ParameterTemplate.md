---
external help file: LogicAppTemplate.dll-Help.xml
Module Name: LogicAppTemplate
online version:
schema: 2.0.0
---

# Get-ParameterTemplate

## SYNOPSIS
Extract ARM template parameter file

## SYNTAX

```
Get-ParameterTemplate -TemplateFile <String> [-KeyVault <KeyVaultUsage>] [-GenerateExpression] [<CommonParameters>]
```

## DESCRIPTION
Extract a valid ARM template parameter file from a LogicApp template file

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-ParameterTemplate -TemplateFile "c:\temp\AwesomeLogicApp.json"
```

This will parse the LogicApp ARM template and extract the parameter file.  
It will read the "c:\temp\AwesomeLogicApp.json" file.  

The cmdlet will output the entire json string to the pipeline / console.  

### Example 2
```powershell
PS C:\> Get-ParameterTemplate -TemplateFile "c:\temp\AwesomeLogicApp.json" | Out-File -FilePath "c:\temp\AwesomeLogicApp_parameters.json" -Encoding utf8
```

This will parse the LogicApp ARM template and extract the parameter file.  
It will read the "c:\temp\AwesomeLogicApp.json" file.  

The output from Get-ParameterTemplate is piped to Out-File.  
The file will saved to "c:\temp\AwesomeLogicApp.json".  
The file is saved with utf8 encoding.  

## PARAMETERS

### -KeyVault
How to handle KeyVault integration, default is None, available options None or Static, Static will generate parameters for static reference to KeyVault

```yaml
Type: KeyVaultUsage
Parameter Sets: (All)
Aliases:
Accepted values: None, Static

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TemplateFile
The path to the LogicApp ARM template file that you want to extract the parameter file from

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

### -GenerateExpression
Whether to generate parameters whose default value is an ARM expression.  If not specified then will not generate parameters per original code.

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


### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
[Get-LogicAppTemplate]()
