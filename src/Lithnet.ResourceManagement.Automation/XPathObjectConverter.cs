using System;
using System.Management.Automation;
using Client = Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    internal static class XPathObjectConverter
    {
        internal static Client.IXPathQueryObject ToClientQueryObject(IXPathQueryObject query)
        {
            if (query == null)
            {
                return null;
            }

            XPathQuery simpleQuery = query as XPathQuery;

            if (simpleQuery != null)
            {
                return simpleQuery.ToClientObject();
            }

            XPathQueryGroup queryGroup = query as XPathQueryGroup;

            if (queryGroup != null)
            {
                return queryGroup.ToClientObject();
            }

            throw new ArgumentException(
                $"Unsupported XPath query object type '{query.GetType().FullName}'. Create query objects with New-XPathQuery or New-XPathQueryGroup.",
                nameof(query));
        }

        internal static object ToClientValue(object value)
        {
            object unwrappedValue = UnwrapPSObject(value);
            XPathExpression expression = unwrappedValue as XPathExpression;

            if (expression != null)
            {
                return expression.ToClientObject();
            }

            return unwrappedValue;
        }

        internal static object UnwrapPSObject(object value)
        {
            PSObject wrappedObject = value as PSObject;

            if (wrappedObject != null)
            {
                return wrappedObject.BaseObject;
            }

            return value;
        }
    }
}
