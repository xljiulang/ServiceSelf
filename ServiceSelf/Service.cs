using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;

namespace ServiceSelf
{
    /// <summary>
    /// 服务
    /// </summary>
    public abstract partial class Service
    {
        /// <summary>
        /// 获取服务名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 服务
        /// </summary>
        /// <param name="name">服务名</param> 
        public Service(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// 创建并启动服务
        /// </summary>
        /// <param name="filePath">文件完整路径</param>
        /// <param name="options">服务选项</param> 
        public abstract void CreateStart(string filePath, ServiceOptions options);

        /// <summary>
        /// 停止并删除服务
        /// </summary>
        public abstract void StopDelete();

        /// <summary>
        /// 读取服务进程的LoggingEventSource的日志
        /// </summary>
        /// <param name="callBack">日志回调</param>
        /// <returns></returns>
        public bool ReadLogs(Action<LogItem> callBack)
        {
            return this.TryGetProcessId(out var processId) && this.ReadLogs(processId, callBack);
        }

        /// <summary>
        /// 尝试获取服务的进程id
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        protected abstract bool TryGetProcessId(out int processId);

        /// <summary>
        /// 读取指定进程LoggingEventSource的日志
        /// https://learn.microsoft.com/zh-cn/dotnet/api/microsoft.extensions.logging.eventsource.loggingeventsource?view=dotnet-plat-ext-7.0
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        private bool ReadLogs(int processId, Action<LogItem> callBack)
        {
            var client = new DiagnosticsClient(processId);
            var provider = new EventPipeProvider("Microsoft-Extensions-Logging", EventLevel.LogAlways, (long)EventKeywords.All);
            using var session = client.StartEventPipeSession(provider, false);
            using var enventSource = new EventPipeEventSource(session.EventStream);

            enventSource.Dynamic.AddCallbackForProviderEvent(provider.Name, "FormattedMessage", traceEvent =>
            {
                var level = (LogLevel)traceEvent.PayloadByName("Level");
                var categoryName = (string)traceEvent.PayloadByName("LoggerName");
                var message = (string)traceEvent.PayloadByName("EventName");
                var log = new LogItem(level, categoryName, message);
                callBack.Invoke(log);
            });

            return enventSource.Process();
        }


        /// <summary>
        /// 创建服务对象
        /// </summary>
        /// <param name="name">服务名称</param>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public static Service Create(string name)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsService(name);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxService(name);
            }

            throw new PlatformNotSupportedException();
        }
    }
}
