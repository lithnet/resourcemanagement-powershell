using Client = Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public sealed class XPathQuery : IXPathQueryObject
    {
        private readonly Client.AttributeType attributeType;

        internal XPathQuery(Client.AttributeTypeDefinition attribute, ComparisonOperator queryOperator, object value, bool negate)
        {
            Client.XPathQueryValidator.Validate(
                attribute,
                (Client.ComparisonOperator)queryOperator,
                value,
                negate);

            this.AttributeName = attribute.SystemName;
            this.attributeType = attribute.Type;
            this.Operator = queryOperator;
            this.Value = value;
            this.Negate = negate;
        }

        public bool Negate { get; private set; }

        public ComparisonOperator Operator { get; private set; }

        public string AttributeName { get; private set; }

        public object Value { get; private set; }

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
