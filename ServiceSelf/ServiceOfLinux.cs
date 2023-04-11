using System;
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

        public ServiceOfLinux(string name, string filePath, string? workingDirectory, string? description)
            : base(name, filePath, workingDirectory, description)
        {
        }

        public override void InstallStart()
        {
            CheckRoot();

            var serviceBuilder = new StringBuilder()
                .AppendLine("[Unit]")
                .AppendLine($"Description={this.Description}")
                .AppendLine()
                .AppendLine("[Service]")
                .AppendLine("Type=notify")
                .AppendLine($"User={Environment.UserName}")
                .AppendLine($"ExecStart={this.FilePath}")
                .AppendLine($"WorkingDirectory={this.WorkingDirectory}")
                .AppendLine()
                .AppendLine("[Install]")
                .AppendLine("WantedBy=multi-user.target");

            var serviceFilePath = $"/etc/systemd/system/{this.Name}.service";
            File.WriteAllText(serviceFilePath, serviceBuilder.ToString());

            Process.Start("chcon", $"--type=bin_t {this.FilePath}").WaitForExit(); // SELinux
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
