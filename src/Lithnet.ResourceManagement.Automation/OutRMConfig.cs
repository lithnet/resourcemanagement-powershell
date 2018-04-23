using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsData.Out, "RMConfig")]
    public class OutRMConfig : PSCmdlet
    {        
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 1)]
        public ConfigFile ConfigFile { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = false, Position = 2)]
        public string FilePath { get; set; }

        [Parameter(Mandatory = false, ValueFromPipeline = false, Position = 3)]
        public string[] AttributeSeparations { get; set; }

        private string exportDirectory;
        private string fileName;
        protected override void BeginProcessing()
        {
            this.WriteVerbose("Exporting rules file");

            ConfigSyncControl.CurrentConfig = ConfigFile;
            ConfigSyncControl.CurrentPath = FilePath;
            //ConfigSyncControl.Preview = this.Preview;

            exportDirectory = Path.GetDirectoryName(FilePath);
            fileName = Path.GetFileName(FilePath);
        }

        protected override void ProcessRecord()
        {
            XmlSerializer s = new XmlSerializer(typeof(ConfigFile));

            if (AttributeSeparations != null)
            {
                foreach (ResourceOperation rops in ConfigFile.Operations)
                {
                    foreach (string a in AttributeSeparations)
                    {
                        var aops = rops.AttributeOperations.FindAll(o => o.Name == a);

                        if (aops.Count == 1)
                        {
                            string attributDirectory = Path.Combine(exportDirectory, aops[0].Name);
                            if (!Directory.Exists(attributDirectory))
                                Directory.CreateDirectory(attributDirectory);

                            File.WriteAllText(
                                Path.Combine(attributDirectory,fileName),
                                aops[0].Value);

                            aops[0].ValueType = AttributeValueType.File;
                            aops[0].Value = Path.Combine(
                                    @".\",
                                    aops[0].Name,
                                    fileName);
                        }
                        else if (aops.Count > 1)
                            this.WriteWarning(string.Format("AttributeSeparation on MultiValue Attribute ({0}) is not supported.", aops[0].Name));
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(FilePath))
            {                
                s.Serialize(sw,ConfigFile);
            }
        }
    }
}
