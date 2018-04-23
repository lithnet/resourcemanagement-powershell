using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsData.Import, "ConfigSyncController")]
    public class ImportConfigSyncController : PSCmdlet
    {
        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 2)]
        public string FilePath { get; set; }

        protected override void ProcessRecord()
        {
            using (StreamReader sr = System.IO.File.OpenText(FilePath))
            {
                this.WriteObject(
                    JsonConvert.DeserializeObject<ConfigSyncController>(sr.ReadToEnd()));
            }
        }
    }
}
