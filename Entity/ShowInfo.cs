using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommunicationModule.Entity
{
    public class ShowInfo
    {
        private bool m_isDataSource;    //add2014.8.21
        private int m_nLocalPort;
        private ProtocalType m_Protocal;
        private int m_nClientNum;
        private long m_nTotalRecvBytes;
        private string m_strDevID;
        private string m_strIP;
        private DateTime m_ConnectedTime;
        private DateTime m_DisConnectedTime;
        private string m_strRemark;        //备注
        private bool m_isConnected;
        private bool m_isStop;
        private CommunicationType m_CommType;

        /// <summary>
        /// 是否为数据源add2014.8.21
        /// </summary>
        public bool pro_isDataSource
        {
            get { return m_isDataSource; }
            set { m_isDataSource = value; }
        }
        /// <summary>
        /// 获取或设置本地端口号
        /// </summary>
        public int pro_nLocalPort
        {
            get { return m_nLocalPort; }
            set { m_nLocalPort = value; }
        }

        /// <summary>
        /// 获取或设置是否走协议
        /// </summary>
        public ProtocalType pro_Protocal
        {
            get { return m_Protocal; }
            set { m_Protocal = value; }
        }

        /// <summary>
        /// 获取或设置客户端连接数
        /// </summary>
        public int pro_nClientNum
        {
            get { return m_nClientNum; }
            set { m_nClientNum = value; }
        }

        /// <summary>
        /// 获取或设置接收到的总数据量
        /// </summary>
        public long pro_nTotalRecvBytes
        {
            get { return m_nTotalRecvBytes; }
            set { m_nTotalRecvBytes = value; }
        }

        /// <summary>
        /// 获取或设置设备ID
        /// </summary>
        public string pro_strDeviceID
        {
            get { return m_strDevID; }
            set { m_strDevID = value; }
        }

        /// <summary>
        /// 获取或设置客户端IP
        /// </summary>
        public string pro_strIP
        {
            get { return m_strIP; }
            set { m_strIP = value; }
        }

        /// <summary>
        /// 获取或设置客户连接时间
        /// </summary>
        public DateTime pro_ConnectedTime
        {
            get { return m_ConnectedTime; }
            set { m_ConnectedTime = value; }
        }

        /// <summary>
        /// 获取或设置客户断开时间
        /// </summary>
        public DateTime pro_DisConnectedTime
        {
            get { return m_DisConnectedTime; }
            set { m_DisConnectedTime = value; }
        }

        /// <summary>
        /// 获取或设置该项的更新状态
        /// </summary>
        public string pro_strRemark
        {
            get { return m_strRemark; }
            set { m_strRemark = value; }
        }

        /// <summary>
        /// 获取或设置连接状态
        /// </summary>
        public bool pro_isConnected
        {
            get { return m_isConnected; }
            set { m_isConnected = value; }
        }

        /// <summary>
        /// 获取或设置停止状态
        /// </summary>
        public bool pro_isStop
        {
            get { return m_isStop; }
            set { m_isStop = value; }
        }

        /// <summary>
        /// 获取或设置通讯类型
        /// </summary>
        public CommunicationType pro_CommType
        {
            get { return m_CommType; }
            set { m_CommType = value; }
        }
    }
}
