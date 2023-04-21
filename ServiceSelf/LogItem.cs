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
        /// 级别
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// 容器名称
        /// </summary>
        public string CategoryName { get; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 日志记录
        /// </summary> 
        /// <param name="level"></param>
        /// <param name="categoryName"></param>
        /// <param name="message"></param>
        public LogItem(LogLevel level, string categoryName, string message)
        {
            this.Level = level;
            this.CategoryName = categoryName;
            this.Message = message;
        }

        /// <summary>
        /// 写入指定的TextWriter
        /// </summary>
        /// <param name="writer"></param>
        public void WriteTo(TextWriter writer)
        {
            writer.WriteLine($"{DateTimeOffset.Now:O} [{this.Level}]");
            writer.WriteLine(this.CategoryName);
            writer.WriteLine(this.Message);
            writer.WriteLine();
        }
    }
}
