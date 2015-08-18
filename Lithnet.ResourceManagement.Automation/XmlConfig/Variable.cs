using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Lithnet.ResourceManagement.Automation
{
    public class Variable
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        [XmlIgnore]
        public string ExpandedValue
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(this.Value);
            }
        }
    }
}
