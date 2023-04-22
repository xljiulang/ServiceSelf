using System;
using System.Runtime.InteropServices;

namespace ServiceSelf
{
    /// <summary>
    /// 服务
    /// </summary>
    public abstract partial class Service
    {
        /// <summary>
        /// 获取服务名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 服务
        /// </summary>
        /// <param name="name">服务名</param> 
        public Service(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// 创建并启动服务
        /// </summary>
        /// <param name="filePath">文件完整路径</param>
        /// <param name="options">服务选项</param> 
        public abstract void CreateStart(string filePath, ServiceOptions options);

        /// <summary>
        /// 停止并删除服务
        /// </summary>
        public abstract void StopDelete();

        /// <summary>
        /// 监听服务进程的LoggingEventSource的日志
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <param name="callback">日志回调</param>
        /// <returns></returns>
        public bool ListenLogs(string? filter, Action<LogItem> callback)
        {
            if (this.TryGetProcessId(out var processId))
            {
                NamedPipeLoggerServer.ListenLogs(processId, filter, callback);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 尝试获取服务的进程id
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        protected abstract bool TryGetProcessId(out int processId);


        /// <summary>
        /// 创建服务对象
        /// </summary>
        /// <param name="name">服务名称</param>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public static Service Create(string name)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsService(name);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxService(name);
            }

            throw new PlatformNotSupportedException();
        }
    }
}
