using Microsoft.Extensions.Logging;
using System;

namespace ServiceSelf
{
    /// <summary>
    /// 命名管道日志提供者
    /// </summary>
    sealed class NamedPipeLoggerProvider : ILoggerProvider
    {
        private static readonly NamedPipeClient pipeClient = CreateNamedPipeClient();

        /// <summary>
        /// 创建与当前processId对应的NamedPipeClient
        /// </summary>
        /// <returns></returns>
        private static NamedPipeClient CreateNamedPipeClient()
        {
#if NET6_0_OR_GREATER
            var processId = Environment.ProcessId;
#else
            var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
#endif
            var pipeName = $"{nameof(ServiceSelf)}_{processId}";
            return new NamedPipeClient(pipeName);
        }

        /// <summary>
        /// 创建日志
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new NamedPipeLogger(categoryName, pipeClient);
        }

        public void Dispose()
        {
        }
    }
}
