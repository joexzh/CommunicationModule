using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommunicationModule.Entity
{
    /// <summary>
    /// 通讯日志信息
    /// </summary>
    public class CommLogInfo
    {
        /// <summary>
        /// 连接端口
        /// </summary>
        public int nLinkPort;
        /// <summary>
        /// 设备ID
        /// </summary>
        public string strDeviceID;
        /// <summary>
        /// 是否为连接状态
        /// </summary>
        public bool isConnected;
        /// <summary>
        /// 是否为数据源
        /// </summary>
        public bool isDataSource;
        /// <summary>
        /// 最新连接客户IP信息
        /// </summary>
        public string strCommIP;
        /// <summary>
        /// 最近一次连接时间
        /// </summary>
        public DateTime ? ConnectedTime = null;
        /// <summary>
        /// 最近一次断开时间
        /// </summary>
        public DateTime ? DisconnectedTime = null;
    }
}
