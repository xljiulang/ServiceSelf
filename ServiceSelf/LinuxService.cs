﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace ServiceSelf
{
    [SupportedOSPlatform("linux")]
    sealed class LinuxService : Service
    {
        [DllImport("libc", SetLastError = true)]
        private static extern uint geteuid();

        [DllImport("libc")]
        private static extern int kill(int pid, int sig);

        public LinuxService(string name)
            : base(name)
        {
        }

        public override void CreateStart(string filePath, ServiceOptions options)
        {
            CheckRoot();

            filePath = Path.GetFullPath(filePath);

            var unitName = $"{this.Name}.service";
            var unitFilePath = $"/etc/systemd/system/{unitName}";
            var oldFilePath = QueryServiceFilePath(unitFilePath);

            if (oldFilePath.Length > 0 && oldFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new InvalidOperationException("系统已存在同名但不同路径的服务");
            }

            var linuxOptions = CreateLinuxOptions(filePath, options);
            using (var fileStream = File.OpenWrite(unitFilePath))
            {
                using var wirter = new StreamWriter(fileStream);
                linuxOptions.WriteTo(wirter);
            }

            // SELinux
            Shell("chcon", $"--type=bin_t {filePath}", showError: false);

            SystemControl("daemon-reload", showError: true);
            SystemControl($"start {unitName}", showError: true);
            SystemControl($"enable {unitName}", showError: false);
        }

        private static ReadOnlySpan<char> QueryServiceFilePath(string unitFilePath)
        {
            if (File.Exists(unitFilePath) == false)
            {
                return ReadOnlySpan<char>.Empty;
            }

            var execStartPrefix = "ExecStart=".AsSpan();
            var wantedByPrefix = "WantedBy=".AsSpan();

            using var stream = File.OpenRead(unitFilePath);
            var reader = new StreamReader(stream);

            var filePath = ReadOnlySpan<char>.Empty;
            var wantedBy = ReadOnlySpan<char>.Empty;
            while (reader.EndOfStream == false)
            {
                var line = reader.ReadLine().AsSpan();
                if (line.StartsWith(execStartPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    line = line[execStartPrefix.Length..];
                    var index = line.IndexOf(' ');
                    filePath = index < 0 ? line : line[..index];
                }
                else if (line.StartsWith(wantedByPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    wantedBy = line[wantedByPrefix.Length..].Trim();
                }

                if (filePath.Length > 0 && wantedBy.Length > 0)
                {
                    break;
                }
            }

            if (filePath.IsEmpty || wantedBy.IsEmpty)
            {
                return ReadOnlySpan<char>.Empty;
            }

            var wants = $"{wantedBy.ToString()}.wants";
            var unitFileName = Path.GetFileName(unitFilePath);
            var unitFileDir = Path.GetDirectoryName(unitFilePath);
            var unitLink = Path.Combine(unitFileDir!, wants, unitFileName);
            return File.Exists(unitLink) ? filePath : ReadOnlySpan<char>.Empty;
        }


        private static LinuxServiceOptions CreateLinuxOptions(string filePath, ServiceOptions options)
        {
            var execStart = filePath;
            if (options.Arguments != null)
            {
                var args = options.Arguments.ToArray(); // 防止多次迭代
                if (args.Length > 0)
                {
                    execStart = $"{filePath} {string.Join<Argument>(' ', args)}";
                }
            }


            var linuxOptions = options.Linux.Clone();
            linuxOptions.Unit["Description"] = options.Description;
            linuxOptions.Service["ExecStart"] = execStart;

            if (string.IsNullOrEmpty(linuxOptions.Service.WorkingDirectory))
            {
                linuxOptions.Service.WorkingDirectory = Path.GetDirectoryName(filePath);
            }

            if (string.IsNullOrEmpty(linuxOptions.Service.Type))
            {
                linuxOptions.Service.Type = "notify";
            }

            if (string.IsNullOrEmpty(linuxOptions.Install.WantedBy))
            {
                linuxOptions.Install.WantedBy = "multi-user.target";
            }

            return linuxOptions;
        }


        public override void StopDelete()
        {
            CheckRoot();

            var unitName = $"{this.Name}.service";
            var unitFilePath = $"/etc/systemd/system/{unitName}";
            if (File.Exists(unitFilePath) == false)
            {
                return;
            }

            SystemControl($"stop {unitName}", showError: true);
            SystemControl($"disable {unitName}", showError: false);
            SystemControl("daemon-reload", showError: true);

            File.Delete(unitFilePath);
        }


        /// <summary>
        /// 尝试查询服务的进程id
        /// </summary> 
        /// <param name="processId"></param>
        /// <returns></returns>
        protected override bool TryGetProcessId(out int processId)
        {
            CheckRoot();

            processId = 0;
            var output = SystemControl($"show -p MainPID {this.Name}.service", false);
            if (output == null)
            {
                return false;
            }

            var match = Regex.Match(output, @"\d+");
            return match.Success &&
                int.TryParse(match.Value, out processId) &&
                processId > 0 &&
                kill(processId, 0) == 0;
        }

        private static string? SystemControl(string arguments, bool showError)
        {
            return Shell("systemctl", arguments, showError);
        }

        private static string? Shell(string fileName, string arguments, bool showError)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = !showError
            };
            var process = Process.Start(startInfo);
            if (process == null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
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
