using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
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

        [DllImport("libc", CharSet = CharSet.Ansi)]
        private static extern int system(string command);

        [DllImport("libc")]
        private static extern StreamSafeHandle popen(string command, string mode = "r");

        [DllImport("libc")]
        private static extern IntPtr fgets(IntPtr buffer, int size, IntPtr stream);

        public LinuxService(string name)
            : base(name)
        {
            if (geteuid() != 0)
            {
                throw new UnauthorizedAccessException("无法操作服务：没有root权限");
            }
        }

        public override void CreateStart(string filePath, ServiceOptions options)
        {
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
            _ = system($"chcon --type=bin_t {filePath}");
            _ = system($"systemctl daemon-reload");
            _ = system($"systemctl start {unitName}");
            _ = system($"systemctl enable {unitName}");
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
            var unitName = $"{this.Name}.service";
            var unitFilePath = $"/etc/systemd/system/{unitName}";
            if (File.Exists(unitFilePath) == false)
            {
                return;
            }

            _ = system($"systemctl stop {unitName}");
            _ = system($"systemctl disable {unitName}");
            _ = system($"systemctl daemon-reload");

            File.Delete(unitFilePath);
        }


        /// <summary>
        /// 尝试查询服务的进程id
        /// </summary> 
        /// <param name="processId"></param>
        /// <returns></returns>
        protected override bool TryGetProcessId(out int processId)
        {
            processId = 0;
            var command = $"systemctl show -p MainPID {this.Name}.service";
            using var stream = popen(command);
            if (stream.IsInvalid)
            {
                return false;
            }

            const int SIZE = 4096;
            var buffer = Marshal.AllocHGlobal(SIZE);
            var builder = new StringBuilder();

            while (fgets(buffer, SIZE, stream.DangerousGetHandle()) != IntPtr.Zero)
            {
                var line = Marshal.PtrToStringAnsi(buffer);
                builder.Append(line);
            }

            Marshal.FreeHGlobal(buffer);
            var output = builder.ToString();

            var match = Regex.Match(output, @"\d+");
            return match.Success &&
                int.TryParse(match.Value, out processId) &&
                processId > 0 &&
                kill(processId, 0) == 0;
        }


        private class StreamSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            [DllImport("libc")]
            private static extern int pclose(IntPtr stream);

            private StreamSafeHandle()
                : base(ownsHandle: true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return pclose(this.handle) == 0;
            }
        }
    }
}
