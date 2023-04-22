using Microsoft.Extensions.Logging;

namespace ServiceSelf
{
    /// <summary>
    /// 命名管道日志提供者
    /// </summary>
    sealed class NamedPipeLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new NamedPipeLogger(categoryName, NamedPipeClient.Current);
        }

        public void Dispose()
        {
        }
    }
}
