using PInvoke;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static PInvoke.AdvApi32;

namespace ServiceSelf
{
    sealed class WindowsService : Service
    {
        private const string workingDirArgName = "WD";

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct QUERY_SERVICE_CONFIG
        {
            public int dwServiceType;
            public int dwStartType;
            public int dwErrorControl;
            public string lpBinaryPathName;
            public string lpLoadOrderGroup;
            public int dwTagId;
            public string lpDependencies;
            public string lpServiceStartName;
            public string lpDisplayName;
        }

        [DllImport(nameof(AdvApi32), CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool QueryServiceConfig(SafeServiceHandle serviceHandle, IntPtr buffer, int bufferSize, out int bytesNeeded);


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
                if (filePath.Equals(oldFilePath, StringComparison.OrdinalIgnoreCase) == false)
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
                options.OSWindows.DisplayName,
                ServiceAccess.SERVICE_ALL_ACCESS,
                ServiceType.SERVICE_WIN32_OWN_PROCESS,
                ServiceStartType.SERVICE_AUTO_START,
                ServiceErrorControl.SERVICE_ERROR_NORMAL,
                $@"""{filePath}"" {string.Join(' ', arguments)}",
                lpLoadOrderGroup: null,
                lpdwTagId: 0,
                lpDependencies: options.OSWindows.Dependencies,
                lpServiceStartName: options.OSWindows.ServiceStartName,
                lpPassword: options.OSWindows.Password);

            if (serviceHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            if (string.IsNullOrEmpty(options.Description) == false)
            {
                var desc = new ServiceDescription { lpDescription = options.Description };
                var pDesc = Marshal.AllocHGlobal(Marshal.SizeOf(desc));
                Marshal.StructureToPtr(desc, pDesc, false);
                ChangeServiceConfig2(serviceHandle, ServiceInfoLevel.SERVICE_CONFIG_DESCRIPTION, pDesc);
                Marshal.FreeHGlobal(pDesc);
            }


            var action = new SC_ACTION
            {
                Type = (SC_ACTION_TYPE)options.OSWindows.FailureActionType,
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

        private static string QueryServiceFilePath(SafeServiceHandle serviceHandle)
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
                    throw new FieldAccessException("系统已存在同名服务，且无法获取其文件路径");
                }

                if (binaryPathName[0] == '"')
                {
                    binaryPathName = binaryPathName[1..];
                    var index = binaryPathName.IndexOf('"');
                    return index < 0 ? binaryPathName.ToString() : binaryPathName[..index].ToString();
                }
                else
                {
                    var index = binaryPathName.IndexOf(' ');
                    return index < 0 ? binaryPathName.ToString() : binaryPathName[..index].ToString();
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

            var status = new SERVICE_STATUS();
            if (QueryServiceStatus(serviceHandle, ref status) == true)
            {
                if (status.dwCurrentState != ServiceState.SERVICE_STOP_PENDING &&
                    status.dwCurrentState != ServiceState.SERVICE_STOPPED)
                {
                    ControlService(serviceHandle, ServiceControl.SERVICE_CONTROL_STOP, ref status);
                }
            }

            if (DeleteService(serviceHandle) == false)
            {
                throw new Win32Exception();
            }
        }
    }
}
