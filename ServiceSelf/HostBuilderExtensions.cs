using Microsoft.Extensions.Logging;

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
        /// <param name="enableLogs">是否启用日志，如果为true，当调用logs子命令时，将在控制台实时输出服务进程写入LoggingFactory的日志</param>
        /// <returns></returns>
        public static IHostBuilder UseServiceSelf(this IHostBuilder hostBuilder, bool enableLogs = true)
        {
            if (enableLogs == true)
            {
                hostBuilder.ConfigureLogging(builder => builder.AddEventSourceLogger());
            }
            return hostBuilder.UseWindowsService().UseSystemd();
        }
    }
}
