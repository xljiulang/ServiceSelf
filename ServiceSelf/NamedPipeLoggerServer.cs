using System;
using System.IO.Pipes;
using System.Text.Json;

namespace ServiceSelf
{
    /// <summary>
    /// 服务进程LoggingEventSource的诊断客户端
    /// </summary>
    static class NamedPipeLoggerServer
    {
        /// <summary>
        /// 监听指定进程LoggingEventSource的日志
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="filter"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static bool ListenLogs(int processId, string? filter, Action<LogItem> callback)
        {
            var pipeName = $"{nameof(ServiceSelf)}_{processId}";
            using var serverStream = new NamedPipeServerStream(pipeName);
            serverStream.WaitForConnection();

            var buffer = new byte[1024 * 1024];
            while (serverStream.IsConnected)
            {
                var length = serverStream.Read(buffer, 0, buffer.Length);
                var span = buffer.AsSpan(0, length);
                var logItem = JsonSerializer.Deserialize<LogItem>(span);
                if (logItem != null && logItem.IsMatch(filter))
                {
                    callback.Invoke(logItem);
                }
            }

            return true;
        }
    }
}
