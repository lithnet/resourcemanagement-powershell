using Lithnet.ResourceManagement.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation.ChangeObserver
{
    public class ObserverSetting
    {
        public string ExportDirectory { get; set; }

        public ChangeModeType ChangeMode { get; set; }

        public TimeSpan ChangeDetectionInterval { get; set; }

        public List<ObserverObjectSetting> ObserverObjectSettings { get; set; }

    }

    [Cmdlet(VerbsCommon.New, "RMObserverSetting")]
    public class NewObserverSetting : PSCmdlet
    {
        [Parameter(ValueFromPipeline = true, Mandatory = false, Position = 1)]
        public string ExportDirectory { get; set; }

        [Parameter(ValueFromPipeline = true, Mandatory = true, Position = 2)]
        
        public ChangeModeType ChangeMode { get; set; }

        [Parameter(ValueFromPipeline = true, Mandatory = true, Position = 3)]
        public TimeSpan ChangeDetectionInterval { get; set; }

        [Parameter(ValueFromPipeline = true, Mandatory = true, Position = 4)]
        public ObserverObjectSetting[] ObserverConfigurations { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject(
                new ObserverSetting()
                {
                    ExportDirectory = ExportDirectory,
                    ChangeMode = ChangeMode,
                    ChangeDetectionInterval = ChangeDetectionInterval,
                    ObserverObjectSettings = ObserverConfigurations?.ToList()
                }
                );
        }
    }


    [Cmdlet(VerbsData.Save, "RMObserverSetting")]
    public class SaveObserverSetting : PSCmdlet
    {
        [Parameter(ValueFromPipeline = true, Mandatory = true, Position = 1)]
        public ObserverSetting Controller { get; set; }

        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 2)]
        public string File { get; set; }

        protected override void ProcessRecord()
        {

            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented
            };

            using (StreamWriter sw = new StreamWriter(File))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, Controller);
            }
        }
    }


    [Cmdlet(VerbsData.Import, "RMObserverSetting")]
    public class ImportObserverSetting : PSCmdlet
    {
        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 1)]
        public string File { get; set; }

        protected override void ProcessRecord()
        {
            using (StreamReader sr = System.IO.File.OpenText(File))
            {

                this.WriteObject(
                    JsonConvert.DeserializeObject<ObserverSetting>(sr.ReadToEnd()));                    
            }
        }
    }
}
