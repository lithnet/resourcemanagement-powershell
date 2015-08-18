using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsData.Import, "RMConfig")]
    public class ImportRMConfig : PSCmdlet
    {
        [Parameter(ValueFromPipeline = false, Mandatory = true, Position = 1)]
        public string File { get; set; }

        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 2)]
        public SwitchParameter Preview { get; set; }

        private ConfigFile config;

        protected override void BeginProcessing()
        {
            this.WriteVerbose("Loading rules fine");
            XmlSerializer s = new XmlSerializer(typeof(ConfigFile));

            string filename;
            string basePath = this.SessionState.Path.CurrentFileSystemLocation.Path;
            if (Path.IsPathRooted(this.File))
            {
                filename = this.File;
            }
            else
            {
                filename = Path.Combine(basePath, this.File);
            }

            StreamReader sr = System.IO.File.OpenText(filename);
            XmlReader xr = XmlReader.Create(sr);
            this.config = (ConfigFile)s.Deserialize(xr);
            this.config.Variables.LoadFileVariables(basePath);
            ResourceOperation.LogEvent += ResourceOperation_LogEvent;

            ConfigSyncControl.CurrentConfig = this.config;
            ConfigSyncControl.CurrentPath = basePath;
            ConfigSyncControl.Preview = this.Preview;
        }

        protected override void ProcessRecord()
        {
            int count = 0;

            foreach (ResourceOperation op in this.config.Operations)
            {
                ProgressRecord p = new ProgressRecord(0, string.Format("Processing import operations{0}...", this.Preview ? " (preview)" : string.Empty)
                    , string.Format("Processing {0}", op.ID));
                p.PercentComplete = (count / this.config.Operations.Count) * 100;
                this.WriteProgress(p);
                op.ExecuteOperation();
                this.WriteVerbose(this.builder.ToString());
                this.builder = new StringBuilder();
            }
        }

        protected override void EndProcessing()
        {
            ConfigSyncControl.CurrentConfig = null;
        }

        private void ResourceOperation_LogEvent(object sender, string e)
        {
            this.builder.AppendLine(e);
        }

        private StringBuilder builder = new StringBuilder();
    }
}