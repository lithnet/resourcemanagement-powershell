using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation.RMConfigConverter
{
    public class ObjectExclusion
    {
        public Dictionary<string,string> AnchorKeyValueList { get; set; }
        public List<string> AttributExclusions { get; set; }
    }

    [Cmdlet(VerbsCommon.New, "RMConverterObjectExclusion")]
    public class NewObjectExclusion : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 1)]
        public Hashtable AnchorKeyValueList { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public string[] AttributExclusions { get; set; }



        protected override void ProcessRecord()
        {
            this.WriteObject(
                new ObjectExclusion()
                {
                    AnchorKeyValueList = AnchorKeyValueList
                                                .Cast<DictionaryEntry>()
                                                .ToDictionary(d => d.Key.ToString(), d => d.Value.ToString()),
                    AttributExclusions = AttributExclusions.ToList(),
                });
        }
    }
}
