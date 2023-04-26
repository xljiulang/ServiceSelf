using System;
using System.Runtime.InteropServices;

namespace ServiceSelf
{
    /// <summary>
    /// advApi32
    /// </summary>
    static unsafe class AdvApi32
    {
        private const string AdvApi32Lib = "advapi32.dll";
        private const uint STANDARD_RIGHTS_REQUIRED = 0xF0000;

        [Flags]
        public enum ServiceManagerAccess
        {
            SC_MANAGER_CONNECT = 0x0001,
            SC_MANAGER_CREATE_SERVICE = 0x0002,
            SC_MANAGER_ENUMERATE_SERVICE = 0x0004,
            SC_MANAGER_LOCK = 0x0008,
            SC_MANAGER_QUERY_LOCK_STATUS = 0x0010,
            SC_MANAGER_MODIFY_BOOT_CONFIG = 0x0020,
            SC_MANAGER_ALL_ACCESS = 0xF003F
        }

        [Flags]
        public enum ServiceAccess : uint
        {
            SERVICE_QUERY_CONFIG = 0x0001,
            SERVICE_CHANGE_CONFIG = 0x0002,
            SERVICE_QUERY_STATUS = 0x0004,
            SERVICE_ENUMERATE_DEPENDENTS = 0x0008,
            SERVICE_START = 0x0010,
            SERVICE_STOP = 0x0020,
            SERVICE_PAUSE_CONTINUE = 0x0040,
            SERVICE_INTERROGATE = 0x0080,
            SERVICE_USER_DEFINED_CONTROL = 0x0100,
            SERVICE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SERVICE_QUERY_CONFIG | SERVICE_CHANGE_CONFIG | SERVICE_QUERY_STATUS | SERVICE_ENUMERATE_DEPENDENTS | SERVICE_START | SERVICE_STOP | SERVICE_PAUSE_CONTINUE | SERVICE_INTERROGATE | SERVICE_USER_DEFINED_CONTROL),
        }

        public enum SC_STATUS_TYPE
        {
            SC_STATUS_PROCESS_INFO = 0,
        }

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [Flags]
        public enum ServiceType : uint
        {
            SERVICE_KERNEL_DRIVER = 0x00000001,
            SERVICE_FILE_SYSTEM_DRIVER = 0x00000002,
            SERVICE_DRIVER = SERVICE_KERNEL_DRIVER | SERVICE_FILE_SYSTEM_DRIVER,
            SERVICE_ADAPTER = 0x00000004,
            SERVICE_RECOGNIZER_DRIVER = 0x00000008,
            SERVICE_WIN32_OWN_PROCESS = 0x00000010,
            SERVICE_WIN32_SHARE_PROCESS = 0x00000020,
            SERVICE_WIN32 = SERVICE_WIN32_OWN_PROCESS | SERVICE_WIN32_SHARE_PROCESS,
            SERVICE_INTERACTIVE_PROCESS = 0x00000100,
            SERVICE_NO_CHANGE = 0xFFFFFFFF,
        }

        public enum ServiceStartType : uint
        {
            SERVICE_BOOT_START = 0x00000000,
            SERVICE_SYSTEM_START = 0x00000001,
            SERVICE_AUTO_START = 0x00000002,
            SERVICE_DEMAND_START = 0x00000003,
            SERVICE_DISABLED = 0x00000004,
            SERVICE_NO_CHANGE = 0xFFFFFFFF,
        }

        public enum ServiceErrorControl : uint
        {
            SERVICE_ERROR_IGNORE = 0x00000000,
            SERVICE_ERROR_NORMAL = 0x00000001,
            SERVICE_ERROR_SEVERE = 0x00000002,
            SERVICE_ERROR_CRITICAL = 0x00000003,
            SERVICE_NO_CHANGE = 0xFFFFFFFF,
        }

        public enum ServiceInfoLevel
        {
            SERVICE_CONFIG_DESCRIPTION = 1,
            SERVICE_CONFIG_FAILURE_ACTIONS = 2,
            SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3,
            SERVICE_CONFIG_FAILURE_ACTIONS_FLAG = 4,
            SERVICE_CONFIG_SERVICE_SID_INFO = 5,
            SERVICE_CONFIG_REQUIRED_PRIVILEGES_INFO = 6,
            SERVICE_CONFIG_PRESHUTDOWN_INFO = 7,
            SERVICE_CONFIG_TRIGGER_INFO = 8,
            SERVICE_CONFIG_PREFERRED_NODE = 9,
            SERVICE_CONFIG_LAUNCH_PROTECTED = 12,
        }

        public enum ServiceControl
        {
            SERVICE_CONTROL_STOP = 0x00000001,
            SERVICE_CONTROL_PAUSE = 0x00000002,
            SERVICE_CONTROL_CONTINUE = 0x00000003,
            SERVICE_CONTROL_INTERROGATE = 0x00000004,
            SERVICE_CONTROL_SHUTDOWN = 0x00000005,
            SERVICE_CONTROL_PARAMCHANGE = 0x00000006,
            SERVICE_CONTROL_DEVICEEVENT = 0x0000000B,
            SERVICE_CONTROL_HARDWAREPROFILECHANGE = 0x0000000C,
            SERVICE_CONTROL_POWEREVENT = 0x0000000D,
            SERVICE_CONTROL_SESSIONCHANGE = 0x0000000E,
            SERVICE_CONTROL_PRESHUTDOWN = 0x0000000F,
            SERVICE_CONTROL_TIMECHANGE = 0x00000010,
            SERVICE_CONTROL_TRIGGEREVENT = 0x00000020,
            SERVICE_CONTROL_USERMODEREBOOT = 0x00000040,
        }

        public enum SC_ACTION_TYPE
        {
            SC_ACTION_NONE,
            SC_ACTION_RESTART,
            SC_ACTION_REBOOT,
            SC_ACTION_RUN_COMMAND,
            SC_ACTION_OWN_RESTART
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct QUERY_SERVICE_CONFIG
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

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS_PROCESS
        {
            public ServiceType dwServiceType;
            public ServiceState dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
            public uint dwProcessId;
            public uint dwServiceFlags;
        }


        public struct SERVICE_STATUS
        {
            public ServiceType dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ServiceDescription
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpDescription;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SC_ACTION
        {
            public SC_ACTION_TYPE Type;
            public uint Delay;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_FAILURE_ACTIONS
        {
            public int dwResetPeriod;
            public unsafe char* lpRebootMsg;
            public unsafe char* lpCommand;
            public int cActions;
            public unsafe SC_ACTION* lpsaActions;
        }



        [DllImport(AdvApi32Lib, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeServiceHandle OpenSCManager(
            string? lpMachineName,
            string? lpDatabaseName,
            ServiceManagerAccess dwDesiredAccess);

        [DllImport(AdvApi32Lib, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeServiceHandle OpenService(
            SafeServiceHandle hSCManager,
            string lpServiceName,
            ServiceAccess dwDesiredAccess);

        [DllImport(AdvApi32Lib, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeServiceHandle CreateService(
            SafeServiceHandle hSCManager,
            string lpServiceName,
            string? lpDisplayName,
            ServiceAccess dwDesiredAccess,
            ServiceType dwServiceType,
            ServiceStartType dwStartType,
            ServiceErrorControl dwErrorControl,
            string lpBinaryPathName,
            string? lpLoadOrderGroup,
            int lpdwTagId,
            string? lpDependencies,
            string? lpServiceStartName,
            string? lpPassword);

        [DllImport(AdvApi32Lib, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool StartService(
            SafeServiceHandle hService,
            int dwNumServiceArgs,
            string? lpServiceArgVectors);

        [DllImport(AdvApi32Lib, SetLastError = true)]
        public static extern bool DeleteService(
            SafeServiceHandle hService);

        [DllImport(AdvApi32Lib, SetLastError = true)]
        public static unsafe extern bool QueryServiceStatusEx(
            SafeServiceHandle hService,
            SC_STATUS_TYPE InfoLevel,
            void* lpBuffer,
            int cbBufSize,
            out int pcbBytesNeeded);

        [DllImport(AdvApi32Lib, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool QueryServiceConfig(
            SafeServiceHandle serviceHandle,
            IntPtr buffer,
            int bufferSize,
            out int bytesNeeded);

        [DllImport(AdvApi32Lib, SetLastError = true)]
        public static extern bool QueryServiceStatus(
            SafeServiceHandle hService,
            ref SERVICE_STATUS dwServiceStatus);


        [DllImport(AdvApi32Lib, SetLastError = true, CharSet = CharSet.Unicode)]
        public static unsafe extern bool ChangeServiceConfig2(
            SafeServiceHandle hService,
            ServiceInfoLevel dwInfoLevel,
            void* lpInfo);

        [DllImport(AdvApi32Lib, SetLastError = true)]
        public static extern bool ControlService(
            SafeServiceHandle hService,
            ServiceControl dwControl,
            ref SERVICE_STATUS lpServiceStatus);

    }
}
