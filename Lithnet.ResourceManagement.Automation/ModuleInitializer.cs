using System;
using System.Reflection;

/// <summary>
/// Used by the ModuleInit. All code inside the Initialize method is ran as soon as the assembly is loaded.
/// </summary>
public static class ModuleInitializer
{
    private static int loadAttempts;

    /// <summary>
    /// Initializes the module.
    /// </summary>
    public static void Initialize()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        if (args.Name.StartsWith("Microsoft.ResourceManagement"))
        {
            loadAttempts++;

            if (loadAttempts > 1)
            {
                return null;
            }

            try
            {
#pragma warning disable 618
                return Assembly.LoadWithPartialName("Microsoft.ResourceManagement");
#pragma warning restore 618
            }
            catch
            {
                try
                {
                    return Assembly.Load("Microsoft.ResourceManagement.dll");
                }
                catch
                {
                }
            }

        }

        return null;
    }
}