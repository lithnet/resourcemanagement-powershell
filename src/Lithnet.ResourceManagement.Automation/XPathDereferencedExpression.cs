using Client = Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public sealed class XPathDereferencedExpression : XPathExpression
    {
        internal XPathDereferencedExpression(string objectType, string dereferenceAttribute, IXPathQueryObject query, bool wrapFilterXml)
            : base(objectType, query, wrapFilterXml)
        {
            this.DereferenceAttribute = dereferenceAttribute;
        }

        public string DereferenceAttribute { get; set; }

        internal override Client.XPathExpression ToClientObject()
        {
            return new Client.XPathDereferencedExpression(
                this.ObjectType,
                this.DereferenceAttribute,
                XPathObjectConverter.ToClientQueryObject(this.Query),
                this.WrapFilterXml);
        }
    }
}
