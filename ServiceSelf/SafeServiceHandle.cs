using System;
using System.Runtime.InteropServices;

namespace ServiceSelf
{
    /// <summary>
    /// 表示安全服务句柄
    /// </summary>
    sealed class SafeServiceHandle : SafeHandle
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        public SafeServiceHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        /// <inheritdoc />
        public override bool IsInvalid => this.handle == IntPtr.Zero;

        /// <inheritdoc />
        protected override bool ReleaseHandle()
        {
            return CloseServiceHandle(this.handle);
        }
    }
}
