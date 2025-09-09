using Microsoft.Extensions.Logging;
using System;

namespace ServiceSelf
{
    /// <summary>
    /// 命名管道日志
    /// </summary>
    sealed class NamedPipeLogger : ILogger
    {
        private readonly string categoryName;
        private readonly NamedPipeClient pipeClient;

        public NamedPipeLogger(string categoryName, NamedPipeClient pipeClient)
        {
            this.categoryName = categoryName;
            this.pipeClient = pipeClient;
        }

#if NET8_0_OR_GREATER
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }
#else
        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }
#endif

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && this.pipeClient.CanWrite;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (this.IsEnabled(logLevel))
            {
                var logItem = new LogItem
                {
                    LoggerName = this.categoryName,
                    Level = (int)logLevel,
                    Message = formatter(state, exception)
                };

                this.pipeClient.Write(logItem);
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
