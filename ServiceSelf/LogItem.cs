using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace ServiceSelf
{
    /// <summary>
    /// 日志记录
    /// </summary>
    public sealed class LogItem
    {
        /// <summary>
        /// 日志名称
        /// </summary>
        public string LoggerName { get; set; } = string.Empty;

        /// <summary>
        /// 级别
        /// </summary>
        public LogLevel Level { get; set; }  

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        public LogItem()
        {
        }

        /// <summary>
        /// 日志记录
        /// </summary> 
        /// <param name="logLevel"></param>
        /// <param name="loggerName"></param>
        /// <param name="message"></param>
        public LogItem(LogLevel logLevel, string loggerName, string message)
        {
            this.Level = logLevel;
            this.LoggerName = loggerName;
            this.Message = message;
        }

        /// <summary>
        /// 是否匹配过滤器
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns></returns>
        public bool IsMatch(ReadOnlySpan<char> filter)
        {
            return filter.IsEmpty ||
                this.Level.ToString().AsSpan().Equals(filter, StringComparison.InvariantCulture) ||
                this.LoggerName.AsSpan().Contains(filter, StringComparison.InvariantCulture) ||
                this.Message.AsSpan().Contains(filter, StringComparison.InvariantCulture);
        }

        /// <summary>
        /// 写入指定的TextWriter
        /// </summary>
        /// <param name="writer"></param>
        public void WriteTo(TextWriter writer)
        {
            writer.WriteLine($"{DateTimeOffset.Now:O} [{this.Level}]");
            writer.WriteLine(this.LoggerName);
            writer.WriteLine(this.Message);
            writer.WriteLine();
        }
    }
}
