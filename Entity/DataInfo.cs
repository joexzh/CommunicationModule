using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZHD.SYS.CommonUtility.CommunicationLib;

namespace CommunicationModule.Entity
{
    /// <summary>
    /// 用于存放接收的数据信息，加入队列，用另一线程读取并转发
    /// </summary>
    public class DataInfo
    {
        /// <summary>
        /// 客户端IP
        /// </summary>
        public string strLocalIP;
        /// <summary>
        /// 客户端端口
        /// </summary>
        public int nLocalPort;
        /// <summary>
        /// 服务器IP
        /// </summary>
        public string strRemoteIP;
        /// <summary>
        /// 服务器端口
        /// </summary>
        public int nRemotePort;
        /// <summary>
        /// 收到的数据
        /// </summary>
        public CommunicationDataBase DataMsg;
    }
}
