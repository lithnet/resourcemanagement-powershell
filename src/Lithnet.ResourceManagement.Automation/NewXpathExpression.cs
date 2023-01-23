using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Management.Automation;
using System.Collections;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    [Cmdlet(VerbsCommon.New, "XPathExpression")]
    public class NewXPathExpression : PSCmdlet //, IDynamicParameters
    {
        [Parameter(Mandatory = true, Position = 1)]
        public string ObjectType {get;set;}
        //{
        //    get
        //    {
        //        return (string)this.MyInvocation.BoundParameters["ObjectType"];
        //    }
        //}

        [Parameter(Mandatory = false, Position = 2, ValueFromPipeline = true)]
        public object QueryObject { get; set; }

        [Parameter(Mandatory = false, Position = 3)]
        public string DereferenceAttribute { get; set; }

        [Parameter(Mandatory = false, Position = 4)]
        public SwitchParameter WrapFilterXml { get; set; }

        protected override void ProcessRecord()
        {
            if (this.DereferenceAttribute == null)
            {
                this.WriteObject(new XPathExpression(this.ObjectType, (IXPathQueryObject)this.QueryObject, this.WrapFilterXml.IsPresent));
            }
            else
            {
                this.WriteObject(new XPathDereferencedExpression(this.ObjectType, this.DereferenceAttribute, (IXPathQueryObject)this.QueryObject, this.WrapFilterXml.IsPresent));
            }
        }

        //public object GetDynamicParameters()
        //{
        //    var runtimeDefinedParameterDictionary = new RuntimeDefinedParameterDictionary();
        //    RuntimeDefinedParameter parameter = RmcWrapper.GetObjectTypeParameter("ObjectType", true, 1, true, null);
            
        //    runtimeDefinedParameterDictionary.Add(parameter.Name, parameter);

        //    return runtimeDefinedParameterDictionary;
        //}
    }
}
