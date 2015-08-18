using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Lithnet.ResourceManagement.Automation
{
    public enum AttributeValueType
    {
        [XmlEnum("value")]
        Value = 0,

        [XmlEnum("file")]
        File = 1,

        [XmlEnum("ref")]
        Reference = 2,
        
        [XmlEnum("xmlref")]
        XmlReference = 3,

        [XmlEnum("filter")]
        XPathFilter = 4
    }
}
