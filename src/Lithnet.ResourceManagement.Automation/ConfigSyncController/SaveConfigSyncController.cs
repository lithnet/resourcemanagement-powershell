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
    [Cmdlet(VerbsData.Save, "ConfigSyncController")]
    public class SaveConfigSyncController : PSCmdlet
    {
        [Parameter(ValueFromPipeline = true, Mandatory = true, Position = 1)]
        public ConfigSyncController Controller { get; set; }

        [Parameter(ValueFromPipeline = true, Mandatory = false, Position = 2)]
        public string FilePath { get; set; }

        protected override void ProcessRecord()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(FilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, Controller);
            }
        }
    }
}
