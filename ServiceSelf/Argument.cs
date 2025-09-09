using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ServiceSelf
{
    /// <summary>
    /// 启动参数
    /// </summary>
    public sealed class Argument
    {
        private readonly string argument;

        /// <summary>
        /// 启动参数
        /// </summary>
        /// <param name="argument">参数</param>
        public Argument(string argument)
        {
            this.argument = argument;
        }

        /// <summary>
        /// 启动参数
        /// </summary>
        /// <param name="name">参数名</param>
        /// <param name="value">参数值</param>
        public Argument(string name, object? value)
        {
            this.argument = @$"{name}=""{value}""";
        }

        /// <summary>
        /// 转换为文本
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.argument;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string? GetValueOrDefault(IEnumerable<string> arguments, string name)
        {
            return TryGetValue(arguments, name, out var value) ? value : default;
        }

        /// <summary>
        /// 尝试解析值
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetValue(IEnumerable<string> arguments, string name, [MaybeNullWhen(false)] out string value)
        {
            var prefix = $"{name}=".AsSpan();
            foreach (var argument in arguments)
            {
                if (TryGetValue(argument, prefix, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// 尝试解析值
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="prefix"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryGetValue(ReadOnlySpan<char> argument, ReadOnlySpan<char> prefix, [MaybeNullWhen(false)] out string value)
        {
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == false)
            {
                value = null;
                return false;
            }

            var valueSpan = argument[prefix.Length..];
            if (valueSpan.Length > 1 && valueSpan[0] == '"' && valueSpan[^1] == '"')
            {
                valueSpan = valueSpan[1..^1];
            }

            value = valueSpan.ToString();
            return true;
        }
    }
}
