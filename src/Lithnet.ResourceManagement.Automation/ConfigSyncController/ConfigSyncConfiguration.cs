using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation
{
    public class ConfigSyncConfiguration
    {
        public string ObjectType { get; set; }
        public List<string> AnchorAttributes { get; set; }

        public string IDPrefix { get; set; }
        
        public bool ReferenceResolution { get; set; }

        public bool IncludeDefaultAttributes { get; set; }

        public bool IncludeEmptyAttributeValues { get; set; }

        public List<string> AttributExclusions { get; set; }

        public List<ObjectExclusion> ObjectSpecificExlusions { get; set; }        
    }
}
