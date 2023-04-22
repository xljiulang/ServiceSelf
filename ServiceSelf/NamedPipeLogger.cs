using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace ServiceSelf
{
    sealed class NamedPipeLogger : ILogger
    {
        private readonly string categoryName;
        private readonly NamedPipeClient pipeClient;

        public NamedPipeLogger(string categoryName, NamedPipeClient pipeClient)
        {
            this.categoryName = categoryName;
            this.pipeClient = pipeClient;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && this.pipeClient.CanWrite;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (this.IsEnabled(logLevel))
            {
                var message = formatter(state, exception);
                var logItem = new LogItem(logLevel, this.categoryName, message);
                var json = JsonSerializer.SerializeToUtf8Bytes(logItem);
                this.pipeClient.Write(json);
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
