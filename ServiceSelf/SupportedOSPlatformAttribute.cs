#if NETCOREAPP3_1

namespace System.Runtime.Versioning
{
    sealed class SupportedOSPlatformAttribute : Attribute
    {
        public string PlatformName { get; }

        public SupportedOSPlatformAttribute(string platformName)
        {
            this.PlatformName = platformName;
        }
    }
}
#endif