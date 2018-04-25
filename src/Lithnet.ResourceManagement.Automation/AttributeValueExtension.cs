using Lithnet.ResourceManagement.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.ResourceManagement.Automation
{
    public static class AttributeValueExtension
    {
        private const string FILTERHEADER = @"Dialect=""http://schemas.microsoft.com/2006/11/XPathFilterDialect""";
        public static bool IsFilter(this AttributeValue AttributeValue)
        {
            if (AttributeValue.Attribute.IsMultivalued)
                return false;
            return AttributeValue.StringValue.Contains(FILTERHEADER);
        }        
    }
}
