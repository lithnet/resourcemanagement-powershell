using Lithnet.ResourceManagement.Automation.Enums;
using Lithnet.ResourceManagement.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Lithnet.ResourceManagement.Automation.RMConfigConverter
{
    [Cmdlet(VerbsData.ConvertTo, "RMConfig")]
    public class ConvertToRMConfig : PSCmdlet
    {

        [Parameter(ParameterSetName = "ConvertResource", ValueFromPipeline = true, Mandatory = true, Position = 1)]
        [Parameter(ParameterSetName = "ConvertResourceReferenceResolution", ValueFromPipeline = true, Mandatory = true, Position = 1)]
        public RmaObject[] Resources { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", ValueFromPipeline = false, Mandatory = true, Position = 2)]
        public string[] AnchorAttributes { get; set; }

        [Parameter(ParameterSetName = "ConvertResourceReferenceResolution", Mandatory = true, ValueFromPipeline = false, Position = 3)]
        public ConverterSetting RMConverterSetting { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", ValueFromPipeline = false, Mandatory = false, Position = 3)]
        public string IDPrefix { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", ValueFromPipeline = false, Mandatory = false, Position = 4)]
        public ObjectExclusion[] RMConverterObjectExclusions { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", Mandatory = false, ValueFromPipeline = false, Position = 5)]
        public string[] AttributExclusions { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", Mandatory = false, ValueFromPipeline = false, Position = 6)]
        public bool IncludeDefaultAttributes { get; set; }

        [Parameter(ParameterSetName = "ConvertResource", Mandatory = false, ValueFromPipeline = false, Position = 7)]
        public bool IncludeEmptyAttributeValues { get; set; }

        private Converter rmConfigConverter;

        protected override void BeginProcessing()
        {
            if (RMConverterSetting == null)
            {
                ObjectSetting s = new ObjectSetting()
                {
                    
                    AnchorAttributes = AnchorAttributes.ToList(),
                    AttributExclusions = AttributExclusions.ToList(),
                    IDPrefix = IDPrefix,
                    IncludeDefaultAttributes = IncludeDefaultAttributes,
                    IncludeEmptyAttributeValues = IncludeEmptyAttributeValues,
                    ObjectSpecificExlusions = RMConverterObjectExclusions.ToList(),
                    ObjectType = Resources[0].InternalObject.ObjectTypeName
                };

                RMConverterSetting = new ConverterSetting()
                {
                    Configurations = new List<ObjectSetting>() { s }
                };
            }

            rmConfigConverter = new Converter(RMConverterSetting);
        }

        protected override void ProcessRecord()
        {
            int count = 0;
            foreach (ResourceObject r in Resources.Select(t => t.InternalObject))
            {
                ProgressRecord p = new ProgressRecord(
                                                    0,
                                                    string.Format("Processing converting..."),
                                                    string.Format("Processing {0}", r.ObjectID))
                {
                    PercentComplete = (count / Resources.Length) * 100
                };

                if (!rmConfigConverter.TryAddResourceOperation(r))
                {
                    this.WriteWarning(String.Format(
                        "Ressource Object {0} was filtered by the configuration",
                        r.ObjectID));
                }

                this.WriteProgress(p);
            }
        }

        protected override void EndProcessing()
        {
            WriteObject(rmConfigConverter.GetConfigFile());
        }

        private void ResourceOperation_LogEvent(object sender, string e)
        {
            this.builder.AppendLine(e);
        }

        private StringBuilder builder = new StringBuilder();

    }
}
