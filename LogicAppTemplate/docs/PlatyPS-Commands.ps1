Import-Module platyPS

#Generate new markdown files.
New-MarkdownHelp -Module LogicAppTemplate -OutputFolder "C:\temp\LogicAppTemplate\docs"

#Update markdown files with new content.
#The module should be loaded before hand.
Update-MarkdownHelp -Path "C:\temp\LogicAppTemplate\docs"

#Generate the MAML file, which enable us to provide Get-Help content for binary cmdlets.
#Current directory is the root of the project "LogicAppTemplate" folder
New-ExternalHelp -Path ".\docs\" -OutputPath ".\docs\en-US\" -Encoding ([System.Text.Encoding]::UTF8) -Force

#Tailing double spaces ("  ") will generate newline in the help file
#double enter, but not tailing spaces, will generate a new paragraph in help file