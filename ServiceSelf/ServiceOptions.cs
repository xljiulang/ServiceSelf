using System.Collections.Generic;

namespace ServiceSelf
{
    /// <summary>
    /// 服务选项
    /// </summary>
    public sealed class ServiceOptions
    {
        /// <summary>
        /// 启动参数
        /// </summary>
        public IEnumerable<Argument>? Arguments { get; set; }

        /// <summary>
        /// 工作目录
        /// </summary>
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// 描述服务
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 获取linux选项
        /// </summary>
        public LinuxServiceOptions Linux { get;  } = new LinuxServiceOptions();

        /// <summary>
        /// 获取windows选项
        /// </summary>
        public WindowsServiceOptions Windows { get;  } = new WindowsServiceOptions();
    }
}
