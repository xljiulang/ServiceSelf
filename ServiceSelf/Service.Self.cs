using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ServiceSelf
{
    partial class Service
    {
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
            var serviceOptions = new ServiceOptions { Arguments = serviceArguments };
            return UseServiceSelf(args, serviceName, serviceOptions);
        }

        /// <summary>
        /// 为程序应用ServiceSelf
        /// 返回true表示可以正常进入程序逻辑
        /// </summary> 
        /// <param name="args">启动参数</param>
        /// <param name="serviceName">服务名</param>
        /// <param name="serviceOptions">服务选项</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public static bool UseServiceSelf(string[] args, string? serviceName, ServiceOptions? serviceOptions)
        {
            // windows服务模式时需要将工作目录参数设置到环境变量
            if (WindowsServiceHelpers.IsWindowsService())
            {
                return WindowsService.UseWorkingDirectory(args);
            }

            // systemd服务模式时不再检查任何参数
            if (SystemdHelpers.IsSystemdService())
            {
                return true;
            }

            // 具有可交互的模式时，比如桌面程序、控制台等
            if (Enum.TryParse<Command>(args.FirstOrDefault(), true, out var command) &&
                Enum.IsDefined(typeof(Command), command))
            {
                var arguments = args.Skip(1);
                UseCommand(command, arguments, serviceName, serviceOptions);
                return false;
            }

            // 没有command子命令时
            return true;
        }


        /// <summary>
        /// 应用服务命令
        /// </summary>
        /// <param name="command"></param> 
        /// <param name="arguments">剩余参数</param>
        /// <param name="name">服务名</param>
        /// <param name="options">服务选项</param> 
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="PlatformNotSupportedException"></exception>
        private static void UseCommand(Command command, IEnumerable<string> arguments, string? name, ServiceOptions? options)
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
                name = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : Path.GetFileName(filePath);
            }

            var service = Create(name);
            if (command == Command.Start)
            {
                service.CreateStart(filePath, options ?? new ServiceOptions());
            }
            else if (command == Command.Stop)
            {
                service.StopDelete();
            }
            else if (command == Command.Logs)
            {
                var filter = Argument.GetValueOrDefault(arguments, "filter");
                service.ListenLogs(filter, log => log.WriteTo(Console.Out));
            }
        }
    }
}
