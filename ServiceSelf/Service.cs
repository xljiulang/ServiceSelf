using System;
using System.IO;
using System.Linq;

namespace ServiceSelf
{
    /// <summary>
    /// 服务
    /// </summary>
    public abstract class Service
    {
        /// <summary>
        /// 服务名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 文件完整路径
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 工作目录
        /// </summary>
        public string? WorkingDirectory { get; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// 服务
        /// </summary>
        /// <param name="name"></param>
        /// <param name="filePath"></param>
        /// <param name="workingDirectory"></param>
        /// <param name="description"></param>
        public Service(string name, string filePath, string? workingDirectory, string? description)
        {
            this.Name = name;
            this.FilePath = Path.GetFullPath(filePath);

            this.WorkingDirectory = string.IsNullOrEmpty(workingDirectory)
                ? Path.GetDirectoryName(this.FilePath)
                : Path.GetFullPath(workingDirectory);

            this.Description = description;
        }

        /// <summary>
        /// 安装并启动服务
        /// </summary>
        public abstract void InstallStart();

        /// <summary>
        /// 停止并删除服务
        /// </summary>
        public abstract void StopDelete();

        /// <summary>
        /// 为程序应用ServiceSelf
        /// 返回true表示可以正常进入程序逻辑
        /// </summary> 
        /// <param name="args">启动参数</param>
        /// <param name="serviceName">服务名，null则为文件名</param>
        /// <returns></returns>
        public static bool UseServiceSelf(string[] args, string? serviceName = null)
        {
            if (UseCommand(args, serviceName))
            {
                return false;
            }

            if (OperatingSystem.IsWindows())
            {
                var workingDirArg = args.FirstOrDefault(item => item.StartsWith("WD="));
                if (string.IsNullOrEmpty(workingDirArg) == false)
                {
                    var workingDir = workingDirArg[3..];
                    if (Directory.Exists(workingDir))
                    {
                        Environment.CurrentDirectory = workingDir;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 应用服务命令
        /// </summary>
        /// <param name="args"></param>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        private static bool UseCommand(string[] args, string? serviceName)
        {
            if (Enum.TryParse<Command>(args.FirstOrDefault(), true, out var cmd) == false)
            {
                return false;
            }

            var filePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }
            if (string.IsNullOrEmpty(serviceName))
            {
                serviceName = Path.GetFileNameWithoutExtension(filePath);
            }

            var service = Create(serviceName, filePath);
            if (cmd == Command.Start)
            {
                service.InstallStart();
            }
            else if (cmd == Command.Stop)
            {
                service.StopDelete();
            }
            return true;
        }


        /// <summary>
        /// 创建服务对象
        /// </summary>
        /// <param name="name">服务名称</param>
        /// <param name="filePath">进程文件路径</param>
        /// <param name="workingDirectory">工作目录(win平台需要在服务程序里自行接收和处理WD=参数)</param>
        /// <param name="description">服务描述</param>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public static Service Create(string name, string filePath, string? workingDirectory = null, string? description = null)
        {
            if (OperatingSystem.IsWindows())
            {
                return new ServiceOfWindows(name, filePath, workingDirectory, description);
            }

            if (OperatingSystem.IsLinux())
            {
                return new ServiceOfLinux(name, filePath, workingDirectory, description);
            }

            throw new PlatformNotSupportedException();
        }
    }
}
