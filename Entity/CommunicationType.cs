/*---------------------------------------------------------------- 
// Copyright (C) 1999-2012 广州市中海达卫星导航技术股份有限公司 
// 版权所有。       
// 文件功能描述:  定义通讯类型信息枚举
// 
// 创建标识:     肖绍志，2012/12/1 16:55:53
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
     /// 通讯类型
     /// </summary>
     public enum CommunicationType
     {
          /// <summary>
          /// TCP服务端
          /// </summary>
          TCPServer,
          /// <summary>
          /// TCP客户端
          /// </summary>
          TCPClient,
          /// <summary>
          /// UDP服务端
          /// </summary>
          UDPServer,
          /// <summary>
          /// UDP客户端
          /// </summary>
          UDPClient,
          /// <summary>
          /// 串口
          /// </summary>
          SerialPort,
          /// <summary>
          /// 无通讯类型，应对主副端口情形,add2014.6.6
          /// </summary>
          None
     };
}
