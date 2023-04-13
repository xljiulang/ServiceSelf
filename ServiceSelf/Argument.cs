namespace ServiceSelf
{
    /// <summary>
    /// 启动参数
    /// </summary>
    public class Argument
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
    }
}
