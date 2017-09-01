using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogicAppTemplate
{
    [Cmdlet(VerbsCommon.Add, "DeploymentVSProject", ConfirmImpact = ConfirmImpact.None)]
    public class VSProjectGenerator: Cmdlet
    {
        [Parameter(
    Mandatory = true,
    HelpMessage = "Resources Directory"
    )]
        public string SourceDir;
        private string DeployProject;

        public VSProjectGenerator()
        {

        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            string[] fileList = Directory.GetFiles(SourceDir, "*.deployproj");
            if (fileList != null && fileList.Length > 0)
            {
                DeployProject = fileList[0];
            }
            else
            {
                InitializeSourceDir();
            }
        }

        protected override void ProcessRecord()
        {
            string projectfile = Path.Combine(SourceDir, DeployProject);
            WriteDebug(projectfile);
            XNamespace ns = @"http://schemas.microsoft.com/developer/msbuild/2003";
            XDocument project = XDocument.Load(projectfile);
            WriteDebug(project.Root.ToString());
            XElement projectID = project.Descendants(ns + "ProjectGuid").FirstOrDefault();
            if (projectID != null)
            { 
                projectID.Value = Guid.NewGuid().ToString();
            }
            XElement itemGroups = project.Descendants(ns + "ItemGroup").Where(xl => !xl.HasAttributes).FirstOrDefault();
            if (itemGroups != null)
            {
                WriteDebug("Inside Loop");
                List<string> dirList = Directory.GetDirectories(SourceDir).ToList();
                WriteDebug(dirList.Count.ToString());
                foreach (string dir in dirList)
                {
                    string folder = dir.Replace(SourceDir, "").Replace(@"\","");
                    string template = string.Format(@"{0}\{0}.json", folder);
                    string parameter =string.Format(@"{0}\{0}.parameters.json", folder);
                    XElement templateXML = new XElement(ns + "Content", new XAttribute("Include", template));
                    XElement parameterXML = new XElement(ns + "Content", new XAttribute("Include", parameter));

                    itemGroups.Add(templateXML);
                    itemGroups.Add(parameterXML);
                }

                project.Save(projectfile);
            }
        }

        private void InitializeSourceDir()
        {
            string[] folder = SourceDir.Split('\\');
            DeployProject = String.Format(@"{0}.deployproj", folder.Last());
            string targetFile = "Deployment.targets";
            string powershellFile = "Deploy-AzureResourceGroup.ps1";

            CreateFile(DeployProject, SourceDir, "DeployProjectTemplate.deployproj");
            CreateFile(targetFile, SourceDir, targetFile);
            CreateFile(powershellFile, SourceDir, powershellFile);
        }

        private void CreateFile(string fileName, string sourceDir, string template)
        {
            string resource = String.Format(@"LogicAppTemplate.Templates.VSProject.{0}", template);
            StreamReader reader = new StreamReader(GetEmbededFileStream(resource));
            WriteDebug(Path.Combine(SourceDir, fileName));
            StreamWriter writer = new StreamWriter(Path.Combine(SourceDir, fileName), true);
            writer.Write(reader.ReadToEnd());
            writer.Flush();
            writer.Close();
        }

        private Stream GetEmbededFileStream(string resourceName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            WriteDebug(resourceName);
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            return stream;
        }
    }


}
