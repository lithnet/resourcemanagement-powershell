using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation
{

    [Cmdlet(VerbsCommon.New, "ConfigSyncConfiguration")]    
    public class NewConfigSyncConfiguration : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string ObjectType { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public string[] AnchorAttributes { get; set; }

        [Parameter(Mandatory = false, Position = 3)]
        public string IDPrefix { get; set; }
        
        [Parameter(Mandatory = false, Position = 4)]
        public SwitchParameter ReferenceResolution { get; set; }

        [Parameter(Mandatory = false, Position = 5)]
        public SwitchParameter IncludeDefaultAttributes { get; set; }

        [Parameter(Mandatory = false, Position = 6)]
        public SwitchParameter IncludeEmptyAttributeValues { get; set; }

        [Parameter(Mandatory = false, Position = 7)]
        public List<string> AttributExclusions { get; set; }

        [Parameter(Mandatory = false, Position = 8)]
        public ObjectExclusion[] ObjectSpecificExlusions { get; set; }


        protected override void ProcessRecord()
        {
            this.WriteObject(
                new ConfigSyncConfiguration()
                {
                    AnchorAttributes = AnchorAttributes?.ToList(),
                    AttributExclusions = AttributExclusions?.ToList(),
                    IDPrefix = IDPrefix,                    
                    IncludeDefaultAttributes = IncludeDefaultAttributes.IsPresent,
                    IncludeEmptyAttributeValues = IncludeEmptyAttributeValues.IsPresent,
                    ObjectSpecificExlusions = ObjectSpecificExlusions?.ToList(),
                    ObjectType = ObjectType,
                    ReferenceResolution = ReferenceResolution.IsPresent
                });
        }
    }
}
