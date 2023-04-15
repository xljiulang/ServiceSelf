using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ServiceSelf
{
    /// <summary>
    /// 服务
    /// </summary>
    public abstract class Service
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
        /// <param name="arguments">服务启动参数</param>
        /// <param name="workingDirectory">服务工作目录</param>
        /// <param name="description">服务描述</param>
        public abstract void CreateStart(string filePath, IEnumerable<Argument>? arguments = null, string? workingDirectory = null, string? description = null);

        /// <summary>
        /// 停止并删除服务
        /// </summary>
        public abstract void StopDelete();

        /// <summary>
        /// 为程序应用ServiceSelf
        /// 返回true表示可以正常进入程序逻辑
        /// </summary> 
        /// <param name="args">启动参数</param>
        /// <param name="serviceName">服务名</param>
        /// <param name="serviceArguments">服务启动参数</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public static bool UseServiceSelf(string[] args, string? serviceName = null, IEnumerable<Argument>? serviceArguments = null)
        {
            if (Enum.TryParse<Command>(args.FirstOrDefault(), true, out var command))
            {
                UseCommand(command, serviceName, serviceArguments);
                return false;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ServiceOfWindows.UseWorkingDirectory(args);
            }

            return true;
        }

        /// <summary>
        /// 应用服务命令
        /// </summary>
        /// <param name="command"></param> 
        /// <param name="name">服务名</param>
        /// <param name="arguments">服务启动参数</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="PlatformNotSupportedException"></exception>
        private static void UseCommand(Command command, string? name, IEnumerable<Argument>? arguments)
        {
#if NET6_0_OR_GREATER
            var filePath = Environment.ProcessPath;
#else
            var filePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
#endif
            if (string.IsNullOrEmpty(filePath))
            {
                throw new FileNotFoundException("无法获取当前进程的启动文件路径");
            }

            if (string.IsNullOrEmpty(name))
            {
                name = Path.GetFileNameWithoutExtension(filePath);
            }

            var service = Create(name);
            if (command == Command.Start)
            {
                service.CreateStart(filePath, arguments);
            }
            else if (command == Command.Stop)
            {
                service.StopDelete();
            }
        }


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
                return new ServiceOfWindows(name);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new ServiceOfLinux(name);
            }

            throw new PlatformNotSupportedException();
        }
    }
}
