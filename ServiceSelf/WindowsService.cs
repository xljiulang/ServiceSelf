using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Services;
using static Windows.Win32.PInvoke;

namespace ServiceSelf
{
    [SupportedOSPlatform("windows")]
    sealed class WindowsService : Service
    {
        private const string WorkingDirArgName = "WD";
        private const int SC_MANAGER_ALL_ACCESS = 0xF003F;
        private const int SERVICE_ALL_ACCESS = 0xF01FF;
        private const int SERVICE_CONTROL_STOP = 0x00000001;

        public WindowsService(string name)
           : base(name)
        {
        }

        /// <summary>
        /// 应用工作目录
        /// </summary> 
        /// <returns></returns>
        public static bool UseWorkingDirectory()
        {
            var directory = Path.GetDirectoryName(Environment.ProcessPath);
            if (string.IsNullOrEmpty(directory))
            {
                return false;
            }

            Environment.CurrentDirectory = directory;
            return true;
        }

        public override void CreateStart(string filePath, ServiceOptions options)
        {
            using var managerHandle = OpenSCManager(null, default(string), SC_MANAGER_ALL_ACCESS);
            if (managerHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            filePath = Path.GetFullPath(filePath);
            using var oldServiceHandle = OpenService(managerHandle, this.Name, SERVICE_ALL_ACCESS);

            if (oldServiceHandle.IsInvalid)
            {
                using var newServiceHandle = this.CreateService(managerHandle, filePath, options);
                StartService(newServiceHandle);
            }
            else
            {
                var oldFilePath = QueryServiceFilePath(oldServiceHandle);
                if (oldFilePath.Length > 0 && oldFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) == false)
                {
                    throw new InvalidOperationException("系统已存在同名但不同路径的服务");
                }
                StartService(oldServiceHandle);
            }
        }

        private unsafe SafeHandle CreateService(SafeHandle managerHandle, string filePath, ServiceOptions options)
        {
            var arguments = options.Arguments ?? Enumerable.Empty<Argument>();
            var serviceHandle = PInvoke.CreateService(
                managerHandle,
                this.Name,
                options.Windows.DisplayName,
                SERVICE_ALL_ACCESS,
                ENUM_SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS,
                SERVICE_START_TYPE.SERVICE_AUTO_START,
                SERVICE_ERROR.SERVICE_ERROR_NORMAL,
                $@"""{filePath}"" {string.Join(' ', arguments)}",
                lpLoadOrderGroup: null,
                lpdwTagId: null,
                lpDependencies: options.Windows.Dependencies,
                lpServiceStartName: options.Windows.ServiceStartName,
                lpPassword: options.Windows.Password);

            if (serviceHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            if (string.IsNullOrEmpty(options.Description) == false)
            {
                fixed (char* description = options.Description)
                {
                    var desc = new SERVICE_DESCRIPTIONW { lpDescription = new PWSTR(description) };
                    ChangeServiceConfig2W(serviceHandle, SERVICE_CONFIG.SERVICE_CONFIG_DESCRIPTION, &desc);
                }
            }


            var action = new SC_ACTION
            {
                Type = (SC_ACTION_TYPE)options.Windows.FailureActionType,
            };
            var failureAction = new SERVICE_FAILURE_ACTIONSW
            {
                cActions = 1,
                lpsaActions = &action,
                dwResetPeriod = (uint)TimeSpan.FromDays(1d).TotalSeconds
            };

            if (ChangeServiceConfig2W(serviceHandle, SERVICE_CONFIG.SERVICE_CONFIG_FAILURE_ACTIONS, &failureAction) == false)
            {
                throw new Win32Exception();
            }

            return serviceHandle;
        }

        private static unsafe ReadOnlySpan<char> QueryServiceFilePath(SafeHandle serviceHandle)
        {
            const int ERROR_INSUFFICIENT_BUFFER = 122;
            if (QueryServiceConfig(serviceHandle, null, 0, out var bytesNeeded) == false)
            {
                if (Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new Win32Exception();
                }
            }

            var buffer = stackalloc byte[(int)bytesNeeded];
            if (QueryServiceConfig(serviceHandle, (QUERY_SERVICE_CONFIGW*)buffer, bytesNeeded, out _) == false)
            {
                throw new Win32Exception();
            }

            var serviceConfig = *(QUERY_SERVICE_CONFIGW*)buffer;
            var binaryPathName = serviceConfig.lpBinaryPathName.AsSpan();
            if (binaryPathName.IsEmpty)
            {
                return ReadOnlySpan<char>.Empty;
            }

            if (binaryPathName[0] == '"')
            {
                binaryPathName = binaryPathName[1..];
                var index = binaryPathName.IndexOf('"');
                return index < 0 ? binaryPathName : binaryPathName[..index];
            }
            else
            {
                var index = binaryPathName.IndexOf(' ');
                return index < 0 ? binaryPathName : binaryPathName[..index];
            }

        }

        private unsafe static void StartService(SafeHandle serviceHandle)
        {
            if (QueryServiceStatus(serviceHandle, out var status) == false)
            {
                throw new Win32Exception();
            }

            if (status.dwCurrentState == SERVICE_STATUS_CURRENT_STATE.SERVICE_RUNNING ||
                status.dwCurrentState == SERVICE_STATUS_CURRENT_STATE.SERVICE_START_PENDING)
            {
                return;
            }

            if (PInvoke.StartService(serviceHandle, ReadOnlySpan<PCWSTR>.Empty) == false)
            {
                throw new Win32Exception();
            }
        }

        /// <summary>
        /// 停止并删除服务
        /// </summary>   
        public override void StopDelete()
        {
            using var managerHandle = OpenSCManager(null, default(string), SC_MANAGER_ALL_ACCESS);
            if (managerHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            using var serviceHandle = OpenService(managerHandle, this.Name, SERVICE_ALL_ACCESS);
            if (serviceHandle.IsInvalid == true)
            {
                return;
            }

            StopService(serviceHandle, TimeSpan.FromSeconds(30d));
            if (DeleteService(serviceHandle) == false)
            {
                throw new Win32Exception();
            }
        }

        private static unsafe void StopService(SafeHandle serviceHandle, TimeSpan maxWaitTime)
        {
            if (QueryServiceStatus(serviceHandle, out var status) == false)
            {
                throw new Win32Exception();
            }

            if (status.dwCurrentState == SERVICE_STATUS_CURRENT_STATE.SERVICE_STOPPED)
            {
                return;
            }

            if (status.dwCurrentState != SERVICE_STATUS_CURRENT_STATE.SERVICE_STOP_PENDING)
            {
                var failureAction = new SERVICE_FAILURE_ACTIONSW();
                if (ChangeServiceConfig2W(serviceHandle, SERVICE_CONFIG.SERVICE_CONFIG_FAILURE_ACTIONS, &failureAction) == false)
                {
                    throw new Win32Exception();
                }

                if (ControlService(serviceHandle, SERVICE_CONTROL_STOP, out status) == false)
                {
                    throw new Win32Exception();
                }

                // 这里不需要恢复SERVICE_CONFIG_FAILURE_ACTIONS，因为下面我们要删除服务
            }

            var stopwatch = Stopwatch.StartNew();
            var statusQueryDelay = TimeSpan.FromMilliseconds(100d);
            while (stopwatch.Elapsed < maxWaitTime)
            {
                if (status.dwCurrentState == SERVICE_STATUS_CURRENT_STATE.SERVICE_STOPPED)
                {
                    return;
                }

                Thread.Sleep(statusQueryDelay);
                if (QueryServiceStatus(serviceHandle, out status) == false)
                {
                    throw new Win32Exception();
                }
            }

            throw new TimeoutException($"等待服务停止超过了{maxWaitTime.TotalSeconds}秒");
        }

        /// <summary>
        /// 尝试获取服务的进程id
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns> 
        protected unsafe override bool TryGetProcessId(out int processId)
        {
            processId = 0;
            using var managerHandle = OpenSCManager(null, default(string), SC_MANAGER_ALL_ACCESS);
            if (managerHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            using var serviceHandle = OpenService(managerHandle, this.Name, SERVICE_ALL_ACCESS);
            if (serviceHandle.IsInvalid == true)
            {
                return false;
            }

            var status = new SERVICE_STATUS_PROCESS();
            var buffer = new Span<byte>(&status, sizeof(SERVICE_STATUS_PROCESS));
            if (QueryServiceStatusEx(serviceHandle, SC_STATUS_TYPE.SC_STATUS_PROCESS_INFO, buffer, out _) == false)
            {
                return false;
            }

            processId = (int)status.dwProcessId;
            return processId > 0;
        }
    }
}
