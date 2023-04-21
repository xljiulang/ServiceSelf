using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Tracing;

namespace ServiceSelf
{
    /// <summary>
    /// 服务进程LoggingEventSource的诊断客户端
    /// </summary>
    static class LoggingDiagnosticsClient
    {
        private const string EventName = "FormattedMessage";
        private const string ProviderName = "Microsoft-Extensions-Logging";

        /// <summary>
        /// 监听指定进程LoggingEventSource的日志
        /// https://learn.microsoft.com/zh-cn/dotnet/api/microsoft.extensions.logging.eventsource.loggingeventsource?view=dotnet-plat-ext-7.0
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="filter"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static bool ListenLogs(int processId, string? filter, Action<LogItem> callback)
        {
            var client = new DiagnosticsClient(processId);
            var provider = new EventPipeProvider(ProviderName, EventLevel.LogAlways, (long)EventKeywords.All);
            using var session = client.StartEventPipeSession(provider, false);
            using var enventSource = new EventPipeEventSource(session.EventStream);

            enventSource.Dynamic.AddCallbackForProviderEvent(ProviderName, EventName, TraceEventCallback);
            return enventSource.Process();

            void TraceEventCallback(TraceEvent traceEvent)
            {
                var level = (LogLevel)traceEvent.PayloadByName("Level");
                var loggerName = (string)traceEvent.PayloadByName("LoggerName");
                var message = (string)traceEvent.PayloadByName("EventName");

                var log = new LogItem(level, loggerName, message);
                if (log.IsMatch(filter))
                {
                    callback.Invoke(log);
                }
            }
        }
    }
}
