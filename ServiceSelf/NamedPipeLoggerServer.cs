using Google.Protobuf;
using System;
using System.IO;
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
        public static void ListenLogs(int processId, string? filter, Action<LogItem> callback)
        {
            using var inputStream = CreateInputStream(processId);
            while (inputStream.IsAtEnd == false)
            {
                var logItem = ReadLogItem(inputStream);
                if (logItem == null)
                {
                    break;
                }

                if (logItem.IsMatch(filter))
                {
                    callback.Invoke(logItem);
                }
            }
        }


        private static CodedInputStream CreateInputStream(int processId)
        {
            var pipeName = $"{nameof(ServiceSelf)}_{processId}";
            var pipeStream = new NamedPipeServerStream(pipeName);

            pipeStream.WaitForConnection();
            return new CodedInputStream(pipeStream, leaveOpen: false);
        }


        private static LogItem? ReadLogItem(CodedInputStream inputStream)
        {
            try
            {
                var logItem = new LogItem();
                inputStream.ReadMessage(logItem);
                return logItem;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
