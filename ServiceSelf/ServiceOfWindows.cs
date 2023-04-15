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
            using var hSCManager = AdvApi32.OpenSCManager(null, null, AdvApi32.ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if (hSCManager.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            var hService = AdvApi32.OpenService(hSCManager, this.Name, AdvApi32.ServiceAccess.SERVICE_ALL_ACCESS);
            if (hService.IsInvalid == true)
            {
                filePath = Path.GetFullPath(filePath);
                arguments ??= Enumerable.Empty<Argument>();
                arguments = string.IsNullOrEmpty(workingDirectory)
                    ? arguments.Append(new Argument(workingDirArgName, Path.GetDirectoryName(filePath)))
                    : arguments.Append(new Argument(workingDirArgName, Path.GetFullPath(workingDirectory)));

                hService = AdvApi32.CreateService(
                    hSCManager,
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

                if (hService.IsInvalid == true)
                {
                    throw new Win32Exception();
                }

                if (string.IsNullOrEmpty(description) == false)
                {
                    var desc = new SERVICE_DESCRIPTION { lpDescription = description };
                    var pDesc = Marshal.AllocHGlobal(Marshal.SizeOf(desc));
                    Marshal.StructureToPtr(desc, pDesc, false);
                    AdvApi32.ChangeServiceConfig2(hService, AdvApi32.ServiceInfoLevel.SERVICE_CONFIG_DESCRIPTION, pDesc);
                    Marshal.FreeHGlobal(pDesc);
                }
            }

            using (hService)
            {
                var status = new AdvApi32.SERVICE_STATUS();
                if (AdvApi32.QueryServiceStatus(hService, ref status) == false)
                {
                    throw new Win32Exception();
                }

                if (status.dwCurrentState != AdvApi32.ServiceState.SERVICE_RUNNING)
                {
                    if (AdvApi32.StartService(hService, 0, null) == false)
                    {
                        throw new Win32Exception();
                    }
                }
            }
        }

        /// <summary>
        /// 停止并删除服务
        /// </summary>  
        [SupportedOSPlatform("windows")]
        public override void StopDelete()
        {
            using var hSCManager = AdvApi32.OpenSCManager(null, null, AdvApi32.ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if (hSCManager.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            using var hService = AdvApi32.OpenService(hSCManager, this.Name, AdvApi32.ServiceAccess.SERVICE_ALL_ACCESS);
            if (hService.IsInvalid == true)
            {
                return;
            }

            var status = new AdvApi32.SERVICE_STATUS();
            if (AdvApi32.QueryServiceStatus(hService, ref status) == true)
            {
                if (status.dwCurrentState != AdvApi32.ServiceState.SERVICE_STOP_PENDING &&
                    status.dwCurrentState != AdvApi32.ServiceState.SERVICE_STOPPED)
                {
                    AdvApi32.ControlService(hService, AdvApi32.ServiceControl.SERVICE_CONTROL_STOP, ref status);
                }
            }

            if (AdvApi32.DeleteService(hService) == false)
            {
                throw new Win32Exception();
            }
        }
    }
}
