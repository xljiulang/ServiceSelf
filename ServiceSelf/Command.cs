namespace ServiceSelf
{
    /// <summary>
    /// 服务命令
    /// </summary>
    enum Command
    {
        /// <summary>
        /// 停止并删除
        /// </summary>
        Stop = 0,

        /// <summary>
        /// 安装并启动
        /// </summary>
        Start = 1,

        /// <summary>
        /// 查看日志
        /// </summary>
        Logs = 2
    }
}
