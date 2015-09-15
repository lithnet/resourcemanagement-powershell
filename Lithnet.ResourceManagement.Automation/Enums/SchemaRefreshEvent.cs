using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Lithnet.ResourceManagement.Automation
{
    [Flags]
    public enum SchemaRefreshEvent
    {
        [XmlEnum("none")]
        None = 0,

        [XmlEnum("before-operation")]
        BeforeOperation = 1,

        [XmlEnum("after-operation")]
        AfterOperation = 2
    }
}
