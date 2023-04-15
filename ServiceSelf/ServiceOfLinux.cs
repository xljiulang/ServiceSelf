using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace ServiceSelf
{
    [SupportedOSPlatform("linux")]
    sealed class ServiceOfLinux : Service
    {
        [DllImport("libc", SetLastError = true)]
        private static extern uint geteuid();

        public ServiceOfLinux(string name)
            : base(name)
        {
        }

        public override void CreateStart(string filePath, IEnumerable<Argument>? arguments, string? workingDirectory, string? description)
        {
            CheckRoot();

            filePath = Path.GetFullPath(filePath);
            workingDirectory = string.IsNullOrEmpty(workingDirectory)
                ? Path.GetDirectoryName(filePath)
                : Path.GetFullPath(workingDirectory);

            var execStart = arguments == null
                ? filePath
                : $"{filePath} {string.Join(' ', arguments)}";

            var serviceBuilder = new StringBuilder()
                .AppendLine("[Unit]")
                .AppendLine($"Description={description}")
                .AppendLine()
                .AppendLine("[Service]")
                .AppendLine("Type=notify")
                .AppendLine($"ExecStart={execStart}")
                .AppendLine($"WorkingDirectory={workingDirectory}")
                .AppendLine()
                .AppendLine("[Install]")
                .AppendLine("WantedBy=multi-user.target");

            var serviceFilePath = $"/etc/systemd/system/{this.Name}.service";
            File.WriteAllText(serviceFilePath, serviceBuilder.ToString());

            Shell("chcon", $"--type=bin_t {filePath}", false); // SELinux
            SystemCtl("daemon-reload");
            SystemCtl($"start {this.Name}.service");
            SystemCtl($"enable {this.Name}.service", false);
        }

        public override void StopDelete()
        {
            CheckRoot();

            var serviceFilePath = $"/etc/systemd/system/{this.Name}.service";
            if (File.Exists(serviceFilePath) == false)
            {
                return;
            }

            SystemCtl($"stop {this.Name}.service");
            SystemCtl($"disable {this.Name}.service", false);
            SystemCtl("daemon-reload");

            File.Delete(serviceFilePath);
        }

        private static void SystemCtl(string arguments, bool showError = true)
        {
            Shell("systemctl", arguments, showError);
        }

        private static void Shell(string fileName, string arguments, bool showError = true)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = !showError
            };
            Process.Start(startInfo)?.WaitForExit();
        }

        private static void CheckRoot()
        {
            if (geteuid() != 0)
            {
                throw new UnauthorizedAccessException("无法操作服务：没有root权限");
            }
        }
    }
}
