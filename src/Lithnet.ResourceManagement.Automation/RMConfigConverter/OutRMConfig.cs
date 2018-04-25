using Lithnet.ResourceManagement.Automation.RMConfigConverter;
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
        public ConfigFile RMConfig { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = false, Position = 2)]
        public string FilePath { get; set; }

        [Parameter(Mandatory = false, ValueFromPipeline = false, Position = 3)]
        public string[] AttributeSeparations { get; set; }

        
        protected override void BeginProcessing()
        {
            this.WriteVerbose("Exporting RMConfig file");
        }

        protected override void ProcessRecord()
        {
            Converter.SerializeConfigFile(RMConfig, AttributeSeparations?.ToList(), FilePath);
        }
    }
}
