using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZHD.SYS.CommonUtility.DatabaseLib;

namespace CommunicationModule
{
    public class DatabaseConfigInfo
    {
        /// <summary>
        /// 数据库服务器，默认为127.0.0.1
        /// </summary>
        public string DbServer { set; get; }

        /// <summary>
        /// 工程管理数据库名称，默认为ZHDMTProjmgr
        /// </summary>
        public string DbMgrName { set; get; }

        /// <summary>
        /// 工程数据库名称，默认为ZHDMTProj
        /// </summary>
        public string DbName { set; get; }

        /// <summary>
        /// 数据库管理员用户名，默认为sa
        /// </summary>
        public string DbUser { set; get; }

        /// <summary>
        /// 数据库管理员用户名，默认为123456
        /// </summary>
        public string DbPassword { set; get; }

        /// <summary>
        /// 数据库类型，默认为SQL SERVER
        /// </summary>
        public int DbStyle { set; get; }

        /// <summary>
        /// udp带协议心跳间隔
        /// </summary>
        public int nHeartIntervalSeconds { set; get; }

        /// <summary>
        /// udp停止间隔
        /// </summary>
        public int nStopIntervalSeconds { set; get; }
    }
}
