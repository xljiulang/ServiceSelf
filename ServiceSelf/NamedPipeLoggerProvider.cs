using Microsoft.Extensions.Logging;

namespace ServiceSelf
{
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
