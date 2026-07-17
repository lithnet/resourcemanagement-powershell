using System;
using System.Collections;
using System.Management.Automation;

namespace Lithnet.ResourceManagement.Automation
{
    /// <summary>
    /// Unwraps a PSObject wrapper from a parameter value (and, for a collection parameter, from each
    /// element) before it is bound. PowerShell wraps a value passed to a parameter declared as
    /// <see cref="object"/> in a <see cref="PSObject"/>, because there is no target type for the
    /// binder to coerce to. A cmdlet that accepts a genuinely polymorphic value (for example an
    /// identifier that may be a Guid, a string or a UniqueIdentifier) must therefore unwrap before it
    /// type-tests the value, or the tests silently miss and the value is dropped. Parameters that have
    /// a single concrete type do not need this - declare the real type instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class UnwrapPSObjectAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            if (inputData == null)
            {
                return null;
            }

            object value = Unwrap(inputData);

            if (value is IList list && !(value is byte[]) && !(value is string))
            {
                object[] result = new object[list.Count];

                for (int i = 0; i < list.Count; i++)
                {
                    result[i] = Unwrap(list[i]);
                }

                return result;
            }

            return value;
        }

        private static object Unwrap(object value)
        {
            PSObject psObject = value as PSObject;

            if (psObject != null)
            {
                return psObject.BaseObject;
            }

            return value;
        }
    }
}
