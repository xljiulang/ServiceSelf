namespace ServiceSelf
{
    /// <summary>
    /// Service章节
    /// </summary>
    public sealed class SystemdServiceSection : SystemdSection
    {
        /// <summary>
        /// Service章节
        /// </summary>
        public SystemdServiceSection()
            : base("[Service]")
        {
        }

        /// <summary>
        /// 为服务定义环境变量
        /// </summary>
        public string? Environment
        {
            get => Get(nameof(Environment));
            set => Set(nameof(Environment), value);
        }

        /// <summary>
        /// 服务进程运行的用户
        /// </summary>
        public string? User
        {
            get => Get(nameof(User));
            set => Set(nameof(User), value);
        }

        /// <summary>
        /// 服务进程运行的用户组
        /// </summary>
        public string? Group
        {
            get => Get(nameof(Group));
            set => Set(nameof(Group), value);
        }

        /// <summary>
        /// 重启选项
        /// <para>no 不自动重启服务，如果服务崩溃或退出，则不会自动重启</para>
        /// <para>on-success 只有在服务以退出状态0成功完成时才自动重启</para>
        /// <para>on-failure 只有在服务以退出状态非零失败时才自动重启</para>
        /// <para>on-abnormal 只有在服务异常退出时才自动重启，例如信号9终止进程</para>
        /// <para>on-abort 只有在服务被显式停止时才自动重启</para>
        /// <para>on-watchdog 只有当WatchdogSec（看门狗）指定的时间内未收到服务发送的“喂狗”信号时，才会自动重启服</para>
        /// <para>always 无论退出状态如何，都自动重启服务</para>
        /// </summary>
        public string? Restart
        {
            get => Get(nameof(Restart));
            set => Set(nameof(Restart), value);
        }

        /// <summary>
        /// 尝试重新启动服务之前需要等待多长时间
        /// </summary>
        public string? RestartSec
        {
            get => Get(nameof(RestartSec));
            set => Set(nameof(RestartSec), value);
        }

        /// <summary>
        /// 启动服务进程的最长时间
        /// </summary>
        public string? TimeoutStartSec
        {
            get => Get(nameof(TimeoutStartSec));
            set => Set(nameof(TimeoutStartSec), value);
        }

        /// <summary>
        /// 停止服务进程的最长时间
        /// </summary>
        public string? TimeoutStopSec
        {
            get => Get(nameof(TimeoutStopSec));
            set => Set(nameof(TimeoutStopSec), value);
        }
    }
}
