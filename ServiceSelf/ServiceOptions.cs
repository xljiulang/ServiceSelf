﻿using System.Collections.Generic;

namespace ServiceSelf
{
    /// <summary>
    /// 服务选项
    /// </summary>
    public sealed class ServiceOptions
    {  /// <summary>
       /// 服务描述
       /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 启动参数
        /// </summary>
        public IEnumerable<Argument>? Arguments { get; set; }      

        /// <summary>
        /// 获取仅适用于linux的选项
        /// </summary>
        public LinuxServiceOptions Linux { get; } = new LinuxServiceOptions();

        /// <summary>
        /// 获取仅适用于windows的选项
        /// </summary>
        public WindowsServiceOptions Windows { get; } = new WindowsServiceOptions();
    }
}
