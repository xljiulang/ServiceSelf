using Google.Protobuf;
using System;
using System.IO.Pipes;

namespace ServiceSelf
{
    /// <summary>
    /// 命名管道日志服务端
    /// </summary>
    static class NamedPipeLoggerServer
    {
        /// <summary>
        /// 监听指定进程命名管道日志
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
            var inputStream = new CodedInputStream(serverStream, leaveOpen: true);

            while (serverStream.IsConnected)
            {
                var logItem = new LogItem();
                inputStream.ReadMessage(logItem);
                if (logItem.IsMatch(filter))
                {
                    callback.Invoke(logItem);
                }
            }

            return true;
        }
    }
}
