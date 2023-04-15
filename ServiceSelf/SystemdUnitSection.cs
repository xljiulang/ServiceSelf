namespace ServiceSelf
{
    /// <summary>
    /// Unit章节
    /// </summary>
    public sealed class SystemdUnitSection : SystemdSection
    {
        /// <summary>
        /// Unit章节
        /// </summary>
        public SystemdUnitSection()
            : base("[Unit]")
        {
        }

        /// <summary>
        /// 单元启动后必须启动的其他单元列表
        /// </summary>
        public string? After
        {
            get => Get(nameof(After));
            set => Set(nameof(After), value);
        }

        /// <summary>
        /// 单元所需的其他单元列表
        /// </summary>
        public string? Requires
        {
            get => Get(nameof(Requires));
            set => Set(nameof(Requires), value);
        }

        /// <summary>
        /// 单元启动前必须启动的其他单元列表
        /// </summary>
        public string? Before
        {
            get => Get(nameof(Before));
            set => Set(nameof(Before), value);
        }
        /// <summary>
        /// 单元希望启动但不是必需的其他单元列表
        /// </summary>
        public string? Wants
        {
            get => Get(nameof(Wants));
            set => Set(nameof(Wants), value);
        }
    }
}
