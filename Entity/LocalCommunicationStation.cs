using System;
using System.Collections.Generic;

using System.Text;
using ZHD.SYS.CommonUtility.CommunicationLib;

namespace CommunicationModule.Entity
{
    /// <summary>
    /// 管理本地各个模块通讯
    /// </summary>
    class LocalCommunicationStation
    {
        private string m_strModuleType;
        private CommunicationPointInfoBase m_nwPointInfo;

        /// <summary>
        /// 获取或设置模块类型
        /// </summary>
        public string pro_strModuleType
        {
            get { return m_strModuleType; }
            set { m_strModuleType = value; }
        }

        /// <summary>
        /// 获取或设置网络通讯站点信息
        /// </summary>
        public CommunicationPointInfoBase pro_nwPointInfo
        {
            get { return m_nwPointInfo; }
            set { m_nwPointInfo = value; }
        }
    }
}
