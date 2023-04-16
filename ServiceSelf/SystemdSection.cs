using System.Collections.Generic;
using System.Text;

namespace ServiceSelf
{
    /// <summary>
    /// 选项章节
    /// </summary>
    public class SystemdSection
    {
        private readonly string name;
        private readonly Dictionary<string, string?> nodes = new Dictionary<string, string?>();

        /// <summary>
        /// 获取或设置值
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public string? this[string key]
        {
            get => this.Get(key);
            set => this.Set(key, value);
        }

        /// <summary>
        /// 选项章节
        /// </summary>
        /// <param name="name">章节名称</param>
        public SystemdSection(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected string? Get(string key)
        {
            this.nodes.TryGetValue(key, out var value);
            return value;
        }

        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected void Set(string key, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                this.nodes.Remove(key);
            }
            else
            {
                this.nodes[key] = value;
            }
        }

        /// <summary>
        /// 转换为文本
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var builder = new StringBuilder().AppendLine(this.name);
            foreach (var kv in this.nodes)
            {
                builder.AppendLine($"{kv.Key}={kv.Value}");
            }
            return builder.ToString();
        }


        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator()
        {
            return this.nodes.GetEnumerator();
        }
    }
}
