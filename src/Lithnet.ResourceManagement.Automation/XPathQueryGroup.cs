using System.Collections.Generic;
using System.Linq;
using Client = Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public sealed class XPathQueryGroup : IXPathQueryObject
    {
        internal XPathQueryGroup(GroupOperator groupOperator, IEnumerable<IXPathQueryObject> queries)
        {
            this.GroupOperator = groupOperator;
            this.Queries = queries == null ? new List<IXPathQueryObject>() : queries.ToList();
        }

        public List<IXPathQueryObject> Queries { get; set; }

        public GroupOperator GroupOperator { get; set; }

        public bool Negate { get; set; }

        public string BuildQueryString()
        {
            return this.ToClientObject().BuildQueryString();
        }

        public override string ToString()
        {
            return this.BuildQueryString();
        }

        internal Client.XPathQueryGroup ToClientObject()
        {
            if (this.Queries == null)
            {
                throw new System.InvalidOperationException("The XPath query group's Queries collection cannot be null.");
            }

            if (this.Queries.Any(t => t == null))
            {
                throw new System.InvalidOperationException("The XPath query group's Queries collection cannot contain null entries.");
            }

            Client.IXPathQueryObject[] queries = this.Queries
                .Select(XPathObjectConverter.ToClientQueryObject)
                .ToArray();

            Client.XPathQueryGroup group = new Client.XPathQueryGroup(
                (Client.GroupOperator)this.GroupOperator,
                queries);

            group.Negate = this.Negate;
            return group;
        }
    }
}
