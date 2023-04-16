using System.Text;

namespace ServiceSelf
{
    /// <summary>
    /// linux独有的服务选项
    /// </summary>
    public sealed class LinuxServiceOptions
    {
        /// <summary>
        /// 获取Unit章节
        /// </summary>
        public SystemdUnitSection Unit { get; private set; } = new SystemdUnitSection();

        /// <summary>
        /// 获取Service章节
        /// </summary>
        public SystemdServiceSection Service { get; private set; } = new SystemdServiceSection();

        /// <summary>
        /// 获取Install章节
        /// </summary>
        public SystemdInstallSection Install { get; private set; } = new SystemdInstallSection();

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public LinuxServiceOptions Clone()
        {
            return new LinuxServiceOptions
            {
                Unit = new SystemdUnitSection(this.Unit),
                Service = new SystemdServiceSection(this.Service),
                Install = new SystemdInstallSection(this.Install),
            };
        }

        /// <summary>
        /// 转换为文本
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return new StringBuilder()
                .AppendLine(this.Unit.ToString())
                .AppendLine(this.Service.ToString())
                .AppendLine(this.Install.ToString())
                .ToString();
        }
    }
}
