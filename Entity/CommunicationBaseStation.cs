using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZHD.SYS.CommonUtility.CommunicationLib;

namespace CommunicationModule.Entity
{
    public class StationPointInfo
    { 
        private bool m_isSrc;
        private string m_strDeviceID;
        private CommunicationPointInfoBase m_StationPoint;

        /// <summary>
        /// 获取或设置是否为数据源
        /// </summary>
        public bool pro_isSrc
        {
            get { return m_isSrc; }
            set { m_isSrc = value; }
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
        /// 获取或设置通讯站点
        /// </summary>
        public CommunicationPointInfoBase pro_StationPoint
        {
            get { return m_StationPoint; }
            set { m_StationPoint = value; }
        }
    }

    public class CommunicationBaseStation
    {
        private CommunicationType m_MainCommType;
        private ProtocalType m_MainProtocal;
        private int m_nMainPort;     
        private CommunicationStationBase m_MainCommStation;
        private List<StationPointInfo> m_listMainPointInfo;

        private CommunicationType m_AssistCommType;
        private ProtocalType m_AssistProtocal;
        private int m_nAssistPort;
        private CommunicationStationBase m_AssistCommStation;
        private List<StationPointInfo> m_listAssistPointInfo;


        /// <summary>
        /// 获取或设置主端口通讯类型
        /// </summary>
        public CommunicationType pro_MainCommType
        {
            get { return m_MainCommType; }
            set { m_MainCommType = value; }
        }

        /// <summary>
        /// 获取或设置使用协议
        /// </summary>
        public ProtocalType pro_MainProtocal
        {
            get { return m_MainProtocal; }
            set { m_MainProtocal = value; }
        }

        /// <summary>
        /// 获取或设置主端口
        /// </summary>
        public int pro_nMainPort
        {
            get { return m_nMainPort; }
            set { m_nMainPort = value; }
        }

        /// <summary>
        /// 获取或设置主端口通讯服务
        /// </summary>
        public CommunicationStationBase pro_MainCommStation
        {
            get { return m_MainCommStation; }
            set { m_MainCommStation = value; }
        }

        /// <summary>
        /// 获取或设置主端口站点表
        /// </summary>
        public List<StationPointInfo> pro_listMainPointInfo
        {
            get { return m_listMainPointInfo; }
            set { m_listMainPointInfo = value; }
        }

        /// <summary>
        /// 获取或设置副端口通讯类型
        /// </summary>
        public CommunicationType pro_AssistCommType
        {
            get { return m_AssistCommType; }
            set { m_AssistCommType = value; }
        }

        /// <summary>
        /// 获取或设置使用协议
        /// </summary>
        public ProtocalType pro_AssistProtocal
        {
            get { return m_AssistProtocal; }
            set { m_AssistProtocal = value; }
        }

        /// <summary>
        /// 获取或设置副端口
        /// </summary>
        public int pro_nAssistPort
        {
            get { return m_nAssistPort; }
            set { m_nAssistPort = value; }
        }

        /// <summary>
        /// 获取或设置副端口服务
        /// </summary>
        public CommunicationStationBase pro_AssistCommStation
        {
            get { return m_AssistCommStation; }
            set { m_AssistCommStation = value; }
        }

        /// <summary>
        /// 获取或设置副端口站点表
        /// </summary>
        public List<StationPointInfo> pro_listAssistPointInfo
        {
            get { return m_listAssistPointInfo; }
            set { m_listAssistPointInfo = value; }
        }
    }
}
