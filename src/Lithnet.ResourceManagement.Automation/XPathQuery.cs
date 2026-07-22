using Client = Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public sealed class XPathQuery : IXPathQueryObject
    {
        private readonly Client.AttributeType attributeType;

        internal XPathQuery(Client.AttributeTypeDefinition attribute, ComparisonOperator queryOperator, object value, bool negate)
        {
            this.AttributeName = attribute.SystemName;
            this.attributeType = attribute.Type;
            this.Operator = queryOperator;
            this.Value = value;
            this.Negate = negate;
        }

        public bool Negate { get; set; }

        public ComparisonOperator Operator { get; set; }

        public string AttributeName { get; set; }

        public object Value { get; set; }

        public string BuildQueryString()
        {
            return this.ToClientObject().BuildQueryString();
        }

        public override string ToString()
        {
            return this.BuildQueryString();
        }

        internal Client.XPathQuery ToClientObject()
        {
            return new Client.XPathQuery(
                this.AttributeName,
                this.attributeType,
                (Client.ComparisonOperator)this.Operator,
                XPathObjectConverter.ToClientValue(this.Value),
                this.Negate);
        }
    }
}
