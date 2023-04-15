using PInvoke;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ServiceSelf
{
    sealed class ServiceOfWindows : Service
    {
        private const string workingDirArgName = "WD";

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SERVICE_DESCRIPTION
        {
            public string lpDescription;
        }

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

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool QueryServiceConfig(
            AdvApi32.SafeServiceHandle serviceHandle,
            IntPtr buffer,
            int bufferSize,
            out int bytesNeeded);


        [SupportedOSPlatform("windows")]
        public ServiceOfWindows(string name)
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
        public override void CreateStart(string filePath, IEnumerable<Argument>? arguments, string? workingDirectory, string? description)
        {
            using var managerHandle = AdvApi32.OpenSCManager(null, null, AdvApi32.ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if (managerHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            filePath = Path.GetFullPath(filePath);
            using var oldServiceHandle = AdvApi32.OpenService(managerHandle, this.Name, AdvApi32.ServiceAccess.SERVICE_ALL_ACCESS);

            if (oldServiceHandle.IsInvalid)
            {
                using var newServiceHandle = this.CreateService(managerHandle, filePath, arguments, workingDirectory, description);
                StartService(newServiceHandle);
            }
            else
            {
                var oldFilePath = QueryServiceFilePath(oldServiceHandle);
                if (string.Equals(filePath, oldFilePath, StringComparison.OrdinalIgnoreCase) == false)
                {
                    throw new InvalidOperationException("系统已存在同名但不同路径的服务");
                }
                StartService(oldServiceHandle);
            }
        }

        private AdvApi32.SafeServiceHandle CreateService(AdvApi32.SafeServiceHandle managerHandle, string filePath, IEnumerable<Argument>? arguments, string? workingDirectory, string? description)
        {
            arguments ??= Enumerable.Empty<Argument>();
            arguments = string.IsNullOrEmpty(workingDirectory)
                ? arguments.Append(new Argument(workingDirArgName, Path.GetDirectoryName(filePath)))
                : arguments.Append(new Argument(workingDirArgName, Path.GetFullPath(workingDirectory)));

            var serviceHandle = AdvApi32.CreateService(
                managerHandle,
                this.Name,
                this.Name,
                AdvApi32.ServiceAccess.SERVICE_ALL_ACCESS,
                AdvApi32.ServiceType.SERVICE_WIN32_OWN_PROCESS,
                AdvApi32.ServiceStartType.SERVICE_AUTO_START,
                AdvApi32.ServiceErrorControl.SERVICE_ERROR_NORMAL,
                $@"""{filePath}"" {string.Join(' ', arguments)}",
                lpLoadOrderGroup: null,
                lpdwTagId: 0,
                lpDependencies: null,
                lpServiceStartName: null,
                lpPassword: null);

            if (serviceHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            if (string.IsNullOrEmpty(description) == false)
            {
                var desc = new SERVICE_DESCRIPTION { lpDescription = description };
                var pDesc = Marshal.AllocHGlobal(Marshal.SizeOf(desc));
                Marshal.StructureToPtr(desc, pDesc, false);
                AdvApi32.ChangeServiceConfig2(serviceHandle, AdvApi32.ServiceInfoLevel.SERVICE_CONFIG_DESCRIPTION, pDesc);
                Marshal.FreeHGlobal(pDesc);
            }

            return serviceHandle;
        }

        private static string QueryServiceFilePath(AdvApi32.SafeServiceHandle serviceHandle)
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

        private static void StartService(AdvApi32.SafeServiceHandle serviceHandle)
        {
            var status = new AdvApi32.SERVICE_STATUS();
            if (AdvApi32.QueryServiceStatus(serviceHandle, ref status) == false)
            {
                throw new Win32Exception();
            }

            if (status.dwCurrentState != AdvApi32.ServiceState.SERVICE_RUNNING)
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
            using var managerHandle = AdvApi32.OpenSCManager(null, null, AdvApi32.ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if (managerHandle.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            using var serviceHandle = AdvApi32.OpenService(managerHandle, this.Name, AdvApi32.ServiceAccess.SERVICE_ALL_ACCESS);
            if (serviceHandle.IsInvalid == true)
            {
                return;
            }

            var status = new AdvApi32.SERVICE_STATUS();
            if (AdvApi32.QueryServiceStatus(serviceHandle, ref status) == true)
            {
                if (status.dwCurrentState != AdvApi32.ServiceState.SERVICE_STOP_PENDING &&
                    status.dwCurrentState != AdvApi32.ServiceState.SERVICE_STOPPED)
                {
                    AdvApi32.ControlService(serviceHandle, AdvApi32.ServiceControl.SERVICE_CONTROL_STOP, ref status);
                }
            }

            if (AdvApi32.DeleteService(serviceHandle) == false)
            {
                throw new Win32Exception();
            }
        }
    }
}
