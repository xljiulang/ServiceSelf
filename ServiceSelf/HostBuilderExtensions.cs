namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// IHostBuilder扩展
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// 为主机使用WindowsService和Systemd的生命周期
        /// </summary>
        /// <param name="hostBuilder"></param>
        public static void UseServiceSelf(this IHostBuilder hostBuilder)
        {
            hostBuilder.UseWindowsService().UseSystemd();
        }
    }
}
