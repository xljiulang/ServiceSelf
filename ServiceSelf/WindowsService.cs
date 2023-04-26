using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using static ServiceSelf.AdvApi32;

namespace ServiceSelf
{
    sealed class WindowsService : Service
    {
        private const string workingDirArgName = "WD";
         

        [SupportedOSPlatform("windows")]
        public WindowsService(string name)
           : base(name)
        {
        }

        /// <summary>
        /// 应用工作目录
        /// </summary>
        /// <param name="args">启动参数</param>
        /// <returns></returns>
        public static bool UseWorkingDirectory(string[] args)
        {
            var prefix = $"{workingDirArgName}=";
            var workingDirArgument = args.FirstOrDefault(item => item.StartsWith(prefix));
            if (string.IsNullOrEmpty(workingDirArgument) == false)
            {
                Environment.CurrentDirectory = workingDirArgument[prefix.Length..];
                return true;
            }
            return false;
        }

        [SupportedOSPlatform("windows")]
        public override void CreateStart(string filePath, ServiceOptions options)
        {
            using var managerHandle = OpenSCManager(null, null, ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if (managerHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            filePath = Path.GetFullPath(filePath);
            using var oldServiceHandle = OpenService(managerHandle, this.Name, ServiceAccess.SERVICE_ALL_ACCESS);

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

        private unsafe SafeServiceHandle CreateService(SafeServiceHandle managerHandle, string filePath, ServiceOptions options)
        {
            var arguments = options.Arguments ?? Enumerable.Empty<Argument>();
            arguments = string.IsNullOrEmpty(options.WorkingDirectory)
                ? arguments.Append(new Argument(workingDirArgName, Path.GetDirectoryName(filePath)))
                : arguments.Append(new Argument(workingDirArgName, Path.GetFullPath(options.WorkingDirectory)));

            var serviceHandle = AdvApi32.CreateService(
                managerHandle,
                this.Name,
                options.Windows.DisplayName,
                ServiceAccess.SERVICE_ALL_ACCESS,
                ServiceType.SERVICE_WIN32_OWN_PROCESS,
                ServiceStartType.SERVICE_AUTO_START,
                ServiceErrorControl.SERVICE_ERROR_NORMAL,
                $@"""{filePath}"" {string.Join(' ', arguments)}",
                lpLoadOrderGroup: null,
                lpdwTagId: 0,
                lpDependencies: options.Windows.Dependencies,
                lpServiceStartName: options.Windows.ServiceStartName,
                lpPassword: options.Windows.Password);

            if (serviceHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            if (string.IsNullOrEmpty(options.Description) == false)
            {
                var desc = new ServiceDescription { lpDescription = options.Description };
                var pDesc = Marshal.AllocHGlobal(Marshal.SizeOf(desc));
                Marshal.StructureToPtr(desc, pDesc, false);
                ChangeServiceConfig2(serviceHandle, ServiceInfoLevel.SERVICE_CONFIG_DESCRIPTION, pDesc.ToPointer());
                Marshal.FreeHGlobal(pDesc);
            }


            var action = new SC_ACTION
            {
                Type = (SC_ACTION_TYPE)options.Windows.FailureActionType,
            };
            var failureAction = new SERVICE_FAILURE_ACTIONS
            {
                cActions = 1,
                lpsaActions = &action,
                dwResetPeriod = (int)TimeSpan.FromDays(1d).TotalSeconds
            };

            if (ChangeServiceConfig2(serviceHandle, ServiceInfoLevel.SERVICE_CONFIG_FAILURE_ACTIONS, &failureAction) == false)
            {
                throw new Win32Exception();
            }

            return serviceHandle;
        }

        private static ReadOnlySpan<char> QueryServiceFilePath(SafeServiceHandle serviceHandle)
        {
            const int ERROR_INSUFFICIENT_BUFFER = 122;
            if (QueryServiceConfig(serviceHandle, IntPtr.Zero, 0, out var bytesNeeded) == false)
            {
                if (Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new Win32Exception();
                }
            }

            var buffer = Marshal.AllocHGlobal(bytesNeeded);
            try
            {
                if (QueryServiceConfig(serviceHandle, buffer, bytesNeeded, out _) == false)
                {
                    throw new Win32Exception();
                }

                var serviceConfig = Marshal.PtrToStructure<QUERY_SERVICE_CONFIG>(buffer);
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
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static void StartService(SafeServiceHandle serviceHandle)
        {
            var status = new SERVICE_STATUS();
            if (QueryServiceStatus(serviceHandle, ref status) == false)
            {
                throw new Win32Exception();
            }

            if (status.dwCurrentState != ServiceState.SERVICE_RUNNING)
            {
                if (AdvApi32.StartService(serviceHandle, 0, null) == false)
                {
                    throw new Win32Exception();
                }
            }
        }

        /// <summary>
        /// 停止并删除服务
        /// </summary>  
        [SupportedOSPlatform("windows")]
        public override void StopDelete()
        {
            using var managerHandle = OpenSCManager(null, null, ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if (managerHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            using var serviceHandle = OpenService(managerHandle, this.Name, ServiceAccess.SERVICE_ALL_ACCESS);
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

        private static unsafe void StopService(SafeServiceHandle serviceHandle, TimeSpan maxWaitTime)
        {
            var status = new SERVICE_STATUS();
            if (QueryServiceStatus(serviceHandle, ref status) == false)
            {
                throw new Win32Exception();
            }

            if (status.dwCurrentState == ServiceState.SERVICE_STOPPED)
            {
                return;
            }

            if (status.dwCurrentState != ServiceState.SERVICE_STOP_PENDING)
            {
                var failureAction = new SERVICE_FAILURE_ACTIONS();
                if (ChangeServiceConfig2(serviceHandle, ServiceInfoLevel.SERVICE_CONFIG_FAILURE_ACTIONS, &failureAction) == false)
                {
                    throw new Win32Exception();
                }

                if (ControlService(serviceHandle, ServiceControl.SERVICE_CONTROL_STOP, ref status) == false)
                {
                    throw new Win32Exception();
                }

                // 这里不需要恢复SERVICE_CONFIG_FAILURE_ACTIONS，因为下面我们要删除服务
            }

            var stopwatch = Stopwatch.StartNew();
            var statusQueryDelay = TimeSpan.FromMilliseconds(100d);
            while (stopwatch.Elapsed < maxWaitTime)
            {
                if (status.dwCurrentState == ServiceState.SERVICE_STOPPED)
                {
                    return;
                }

                Thread.Sleep(statusQueryDelay);
                if (QueryServiceStatus(serviceHandle, ref status) == false)
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
            using var managerHandle = OpenSCManager(null, null, ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if (managerHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            using var serviceHandle = OpenService(managerHandle, this.Name, ServiceAccess.SERVICE_ALL_ACCESS);
            if (serviceHandle.IsInvalid == true)
            {
                return false;
            }

            var status = new SERVICE_STATUS_PROCESS();
            if (QueryServiceStatusEx(serviceHandle, SC_STATUS_TYPE.SC_STATUS_PROCESS_INFO, &status, sizeof(SERVICE_STATUS_PROCESS), out _) == false)
            {
                return false;
            }

            processId = (int)status.dwProcessId;
            return processId > 0;
        }
    }
}
