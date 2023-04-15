namespace ServiceSelf
{
    /// <summary>
    /// Install章节
    /// </summary>
    public sealed class SystemdInstallSection : SystemdSection
    {
        /// <summary>
        /// Install章节
        /// </summary>
        public SystemdInstallSection()
            : base("[Install]")
        {
        }

        /// <summary>
        /// 将单元链接到提供特定功能的.target单元
        /// </summary>
        public string? WantedBy
        {
            get => Get(nameof(WantedBy));
            set => Set(nameof(WantedBy), value);
        }

        /// <summary>
        /// 在启动时强制依赖于单个或多个其他单元
        /// </summary>
        public string? RequiredBy
        {
            get => Get(nameof(RequiredBy));
            set => Set(nameof(RequiredBy), value);
        }
    }
}
