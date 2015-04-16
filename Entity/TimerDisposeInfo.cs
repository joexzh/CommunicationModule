using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommunicationModule.Entity
{
    /// <summary>
    /// 记录最新数据的信息
    /// </summary>
    class TimerDisposeInfo
    {
        private string m_strRemoteIP;
        private int m_nRemotePort;
        private string m_strLocalIP;
        private int m_nLocalPort;
        private DateTime m_RecvTime;

        /// <summary>
        /// 服务器IP
        /// </summary>
        public string pro_strRemoteIP
        {
            get { return m_strRemoteIP; }
            set { m_strRemoteIP = value; }
        }

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int pro_nRemotePort
        {
            get { return m_nRemotePort; }
            set { m_nRemotePort = value; }
        }

        /// <summary>
        /// 客户IP
        /// </summary>
        public string pro_strLocalIP
        {
            get { return m_strLocalIP; }
            set { m_strLocalIP = value; }
        }

        /// <summary>
        /// 客户端口
        /// </summary>
        public int pro_nLocalPort
        {
            get { return m_nLocalPort; }
            set { m_nLocalPort = value; }
        }

        //收到最新数据的时间
        public DateTime pro_RecvTime
        {
            get { return m_RecvTime; }
            set { m_RecvTime = value; }
        }
    }
}
