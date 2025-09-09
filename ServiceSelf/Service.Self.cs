using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ServiceSelf
{
    partial class Service
    {
        [DllImport("kernel32")]
        [SupportedOSPlatform("windows")]
        private static extern bool AllocConsole();

        [DllImport("libc")]
        [SupportedOSPlatform("linux")]
        private static extern int openpty(out int master, out int slave, IntPtr name, IntPtr termios, IntPtr winsize);

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
        /// <para>start  安装并启动服务</para>
        /// <para>stop 停止并删除服务</para>
        /// <para>logs 控制台输出服务的日志</para>
        /// <para>logs filter="key words" 控制台输出过滤的服务日志</para>
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
            if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
            {
                return WindowsService.UseWorkingDirectory(args);
            }

            // systemd服务模式时不再检查任何参数
            if (OperatingSystem.IsLinux() && SystemdHelpers.IsSystemdService())
            {
                return true;
            }

            // 具有可交互的模式时，比如桌面程序、控制台等
            if (Command.TryParse(args.FirstOrDefault(), out var command))
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
            var filePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(filePath))
            {
                throw new FileNotFoundException("无法获取当前进程的启动文件路径");
            }

            if (string.IsNullOrEmpty(name))
            {
                name = OperatingSystem.IsWindows()
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : Path.GetFileName(filePath);
            }

            var service = Create(name);
            if (command.Equals(Command.Start))
            {
                service.CreateStart(filePath, options ?? new ServiceOptions());
            }
            else if (command.Equals(Command.Stop))
            {
                service.StopDelete();
            }
            else if (command.Equals(Command.Logs))
            {
                var writer = GetConsoleWriter();
                var filter = Argument.GetValueOrDefault(arguments, "filter");
                service.ListenLogs(filter, log => log.WriteTo(writer));
            }
        }

        /// <summary>
        /// 获取控制台输出
        /// </summary>
        /// <returns></returns>
        private static TextWriter GetConsoleWriter()
        {
            using (var stream = Console.OpenStandardOutput())
            {
                if (stream != Stream.Null)
                {
                    return Console.Out;
                }
            }

            if (OperatingSystem.IsWindows())
            {
                AllocConsole();
            }
            else if (OperatingSystem.IsLinux())
            {
                _ = openpty(out var _, out var _, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }

            var outputStream = Console.OpenStandardOutput();
            var streamWriter = new StreamWriter(outputStream, Console.OutputEncoding, 256, leaveOpen: true)
            {
                AutoFlush = true
            };

            // Synchronized确保线程安全
            return TextWriter.Synchronized(streamWriter);
        }
    }
}
