using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.New, "ConfigSyncController")]
    public class NewConfigSyncController : PSCmdlet
    {
        [Parameter(ParameterSetName = "NewMany", Mandatory = true, Position = 1)]
        public ConfigSyncConfiguration[] Configurations { get; set; }

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
            if (Configurations != null)
                this.WriteObject(
                    new ConfigSyncController()
                    {
                        Configurations = Configurations.ToList()
                    });
            else
            {
                ConfigSyncConfiguration config = new ConfigSyncConfiguration()
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
                 new ConfigSyncController()
                 {
                     Configurations = new List<ConfigSyncConfiguration>() { config }
                 });
            }

        }
    }
}
