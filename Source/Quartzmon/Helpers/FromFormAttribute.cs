#if NETFRAMEWORK

using System;

namespace Quartzmon.Helpers
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class FromFormAttribute : Attribute
    {
        // just dummy attribute for full .NET
    }
}

#endif