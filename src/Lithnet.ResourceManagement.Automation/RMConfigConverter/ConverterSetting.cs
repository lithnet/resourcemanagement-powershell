using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation.RMConfigConverter
{
    public class ConverterSetting
    {
        public List<ObjectSetting> Configurations { get; set; }
    }


    [Cmdlet(VerbsCommon.New, "RMConverterSetting")]
    public class NewConverterSetting : PSCmdlet
    {
        [Parameter(ParameterSetName = "NewMany", Mandatory = true, Position = 1)]
        public ObjectSetting[] ObjectSettings { get; set; }

        [Parameter(ParameterSetName = "NewOne", Mandatory = true, Position = 1)]
        public string ObjectType { get; set; }

        [Parameter(ParameterSetName = "NewOne", Mandatory = true, Position = 2)]
        public string[] AnchorAttributes { get; set; }

        [Parameter(ParameterSetName = "NewOne", Mandatory = false, Position = 3)]
        public string IDPrefix { get; set; }

        [Parameter(ParameterSetName = "NewOne", Mandatory = false, Position = 4)]
        public SwitchParameter ReferenceResolution { get; set; }

        [Parameter(ParameterSetName = "NewOne", Mandatory = false, Position = 5)]
        public SwitchParameter IncludeDefaultAttributes { get; set; }

        [Parameter(ParameterSetName = "NewOne", Mandatory = false, Position = 6)]
        public SwitchParameter IncludeEmptyAttributeValues { get; set; }

        [Parameter(ParameterSetName = "NewOne", Mandatory = false, Position = 7)]
        public List<string> AttributExclusions { get; set; }

        [Parameter(ParameterSetName = "NewOne", Mandatory = false, Position = 8)]
        public ObjectExclusion[] ObjectSpecificExlusions { get; set; }


        protected override void ProcessRecord()
        {
            if (ObjectSettings != null)
                this.WriteObject(
                    new ConverterSetting()
                    {
                        Configurations = ObjectSettings.ToList()
                    });
            else
            {
                ObjectSetting config = new ObjectSetting()
                {
                    AnchorAttributes = AnchorAttributes?.ToList(),
                    AttributExclusions = AttributExclusions?.ToList(),
                    IDPrefix = IDPrefix,
                    IncludeDefaultAttributes = IncludeDefaultAttributes.IsPresent,
                    IncludeEmptyAttributeValues = IncludeEmptyAttributeValues.IsPresent,
                    ObjectSpecificExlusions = ObjectSpecificExlusions?.ToList(),
                    ObjectType = ObjectType,
                    ReferenceResolution = ReferenceResolution.IsPresent
                };

                this.WriteObject(
                 new ConverterSetting()
                 {
                     Configurations = new List<ObjectSetting>() { config }
                 });
            }

        }
    }


    [Cmdlet(VerbsData.Save, "RMConverterSetting")]
    public class SaveConverterSetting : PSCmdlet
    {
        [Parameter(ValueFromPipeline = true, Mandatory = true, Position = 1)]
        public ConverterSetting ConverterSettings { get; set; }

        [Parameter(ValueFromPipeline = true, Mandatory = false, Position = 2)]
        public string FilePath { get; set; }

        protected override void ProcessRecord()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(FilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, ConverterSettings);
            }
        }
    }

    [Cmdlet(VerbsData.Import, "RMConverterSetting")]
    public class ImportConverterSetting : PSCmdlet
    {
        [Parameter(ValueFromPipeline = false, Mandatory = false, Position = 2)]
        public string File { get; set; }

        protected override void ProcessRecord()
        {
            using (StreamReader sr = System.IO.File.OpenText(File))
            {
                this.WriteObject(
                    JsonConvert.DeserializeObject<ConverterSetting>(sr.ReadToEnd()));
            }
        }
    }
}
