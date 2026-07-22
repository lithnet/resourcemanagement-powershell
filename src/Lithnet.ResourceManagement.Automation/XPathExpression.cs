using Client = Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public class XPathExpression
    {
        internal XPathExpression(string objectType, IXPathQueryObject query, bool wrapFilterXml)
        {
            this.ObjectType = objectType;
            this.Query = query;
            this.WrapFilterXml = wrapFilterXml;
        }

        public string ObjectType { get; set; }

        public IXPathQueryObject Query { get; set; }

        public bool WrapFilterXml { get; set; }

        public override string ToString()
        {
            return this.ToClientObject().ToString();
        }

        internal virtual Client.XPathExpression ToClientObject()
        {
            return new Client.XPathExpression(
                this.ObjectType,
                XPathObjectConverter.ToClientQueryObject(this.Query),
                this.WrapFilterXml);
        }
    }
}
