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
                .AppendLine($"User={Environment.UserName}")
                .AppendLine($"ExecStart={execStart}")
                .AppendLine($"WorkingDirectory={workingDirectory}")
                .AppendLine()
                .AppendLine("[Install]")
                .AppendLine("WantedBy=multi-user.target");

            var serviceFilePath = $"/etc/systemd/system/{this.Name}.service";
            File.WriteAllText(serviceFilePath, serviceBuilder.ToString());

            Process.Start("chcon", $"--type=bin_t {filePath}").WaitForExit(); // SELinux
            Process.Start("systemctl", "daemon-reload").WaitForExit();
            Process.Start("systemctl", $"start {this.Name}.service").WaitForExit();
            Process.Start("systemctl", $"enable {this.Name}.service").WaitForExit();
        }

        public override void StopDelete()
        {
            CheckRoot();

            var serviceFilePath = $"/etc/systemd/system/{this.Name}.service";
            Process.Start("systemctl", $"stop {this.Name}.service").WaitForExit();
            Process.Start("systemctl", $"disable {this.Name}.service").WaitForExit();

            if (File.Exists(serviceFilePath))
            {
                File.Delete(serviceFilePath);
            }
            Process.Start("systemctl", "daemon-reload").WaitForExit();
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
