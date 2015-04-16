using System;
using System.Collections.Generic;

using System.Text;
using ZHD.SYS.CommonUtility.CommunicationLib;

namespace CommunicationModule.Entity
{
    public class StationInfo
    {
        #region 字段 | fields
        private CommunicationType m_communitionType;
        private string m_strIP;
        private int m_nPort;
        private int m_nBaudRate;
        private ProtocalType m_Protocal;
        private string m_strDeviceID;
        private bool m_isOnline;
        private bool m_isStop;
        private int m_nMainPort;
        private CommunicationType m_MainPortCommType;
        #endregion

        #region 属性 | properties

        /// <summary>
        /// 获取或设置通讯类型
        /// </summary>
        internal CommunicationType pro_CommunicationType
        {
            get { return m_communitionType; }
            set { m_communitionType = value; }
        }

        /// <summary>
        /// 获取或设置IP地址，格式为点分十进制，如"127.0.0.1"
        /// </summary>
        public string pro_strIP
        {
            get { return m_strIP; }
            set { m_strIP = value; }
        }

        /// <summary>
        /// 获取或设置站点通讯端口
        /// </summary>
        public int pro_nPort
        {
            get { return m_nPort; }
            set { m_nPort = value; }
        }

        /// <summary>
        /// 获取或设置波特率
        /// </summary>
        internal int pro_nBaudRate
        {
            get { return m_nBaudRate; }
            set { m_nBaudRate = value; }
        }

        /// <summary>
        /// 获取或设置通讯协议
        /// </summary>
        public ProtocalType pro_Protocal
        {
            get { return m_Protocal; }
            set { m_Protocal = value; }
        }

        /// <summary>
        /// 获取或设置设备ID
        /// </summary>
        public string pro_strDeviceID
        {
            get { return m_strDeviceID; }
            set { m_strDeviceID = value; }
        }

        /// <summary>
        /// 获取或设置站点是否处于连线状态
        /// </summary>
        public bool pro_IsOnline
        {
            get { return m_isOnline; }
            set { m_isOnline = value; }
        }

        /// <summary>
        /// 该站点是否停止
        /// </summary>
        public bool pro_isStop
        {
            get { return m_isStop; }
            set { m_isStop = value; }
        }

        /// <summary>
        /// 获取或设置通讯协议
        /// </summary>
        public int pro_nMainPort
        {
            get { return m_nMainPort; }
            set { m_nMainPort = value; }
        }

        /// <summary>
        /// 获取或设置主端口的通讯类型
        /// </summary>
        internal CommunicationType pro_MainPortCommType
        {
            get { return m_MainPortCommType; }
            set { m_MainPortCommType = value; }
        }
 
        #endregion
    }
}
