using System;
using System.Threading.Tasks;
#if NET8_0_OR_GREATER
using System.Runtime.Loader;
#endif

namespace Lithnet.ResourceManagement.Automation
{
    internal static class ModuleExecutionContext
    {
        public static void Run(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

#if NET8_0_OR_GREATER
            AssemblyLoadContext context = GetLoadContext();

            using (context.EnterContextualReflection())
            {
                action();
            }
#else
            action();
#endif
        }

        public static async Task<T> RunAsync<T>(Func<Task<T>> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

#if NET8_0_OR_GREATER
            AssemblyLoadContext context = GetLoadContext();

            using (context.EnterContextualReflection())
            {
                return await action().ConfigureAwait(false);
            }
#else
            return await action().ConfigureAwait(false);
#endif
        }

#if NET8_0_OR_GREATER
        private static AssemblyLoadContext GetLoadContext()
        {
            AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(typeof(ModuleExecutionContext).Assembly);

            if (context == null)
            {
                throw new InvalidOperationException("The PowerShell module's assembly load context could not be determined. Runtime-generated dependencies cannot be loaded safely.");
            }

            return context;
        }
#endif
    }
}
