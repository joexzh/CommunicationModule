/*---------------------------------------------------------------- 
// Copyright (C) 1999-2012 广州市中海达卫星导航技术股份有限公司 
// 版权所有。       
// 文件功能描述: 定义传感器类型信息
// 
// 创建标识:     肖绍志，2012/12/1 16:52:17
// 
// 修改标识：   修改人 ，修改时间
// 修改描述：   原某功能 改为某功能
//----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationModule.Entity
{
    /// <summary>
    /// 传感器监测项目类型
    /// </summary>
    public enum ProtocalType
    {
        /// <summary>
        /// 无协议
        /// </summary>
        None,
        /// <summary>
        /// Ntrip协议
        /// </summary>
        Ntrip,
        /// <summary>
        /// vnet udp协议
        /// </summary>
        VNetUdp
    }
}
