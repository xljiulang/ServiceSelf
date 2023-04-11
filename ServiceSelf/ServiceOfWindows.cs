using PInvoke;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ServiceSelf
{
    [SupportedOSPlatform("windows")]
    sealed class ServiceOfWindows : Service
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_DESCRIPTION
        {
            public string lpDescription;
        }

        public ServiceOfWindows(string name, string filePath, string? workingDirectory, string? description)
           : base(name, filePath, workingDirectory, description)
        {
        }

        /// <summary>
        /// 安装并启动服务
        /// </summary> 
        public override void InstallStart()
        {
            using var hSCManager = AdvApi32.OpenSCManager(null, null, AdvApi32.ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if (hSCManager.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            var hService = AdvApi32.OpenService(hSCManager, this.Name, AdvApi32.ServiceAccess.SERVICE_ALL_ACCESS);
            if (hService.IsInvalid == true)
            {
                hService = AdvApi32.CreateService(
                    hSCManager,
                    this.Name,
                    this.Name,
                    AdvApi32.ServiceAccess.SERVICE_ALL_ACCESS,
                    AdvApi32.ServiceType.SERVICE_WIN32_OWN_PROCESS,
                    AdvApi32.ServiceStartType.SERVICE_AUTO_START,
                    AdvApi32.ServiceErrorControl.SERVICE_ERROR_NORMAL,
                    $@"""{this.FilePath}"" ""WD={this.WorkingDirectory}""",
                    lpLoadOrderGroup: null,
                    lpdwTagId: 0,
                    lpDependencies: null,
                    lpServiceStartName: null,
                    lpPassword: null);
            }

            if (hService.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            if (string.IsNullOrEmpty(this.Description) == false)
            {
                var desc = new SERVICE_DESCRIPTION { lpDescription = this.Description };
                var pDesc = Marshal.AllocHGlobal(Marshal.SizeOf(desc));
                Marshal.StructureToPtr(desc, pDesc, false);
                AdvApi32.ChangeServiceConfig2(hService, AdvApi32.ServiceInfoLevel.SERVICE_CONFIG_DESCRIPTION, pDesc);
                Marshal.FreeHGlobal(pDesc);
            }

            using (hService)
            {
                if (AdvApi32.StartService(hService, 0, null) == false)
                {
                    throw new Win32Exception();
                }
            }
        }

        /// <summary>
        /// 停止并删除服务
        /// </summary>  
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
