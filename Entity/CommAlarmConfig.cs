using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommunicationModule.Entity
{
    /// <summary>
    /// 掉线报警配置
    /// </summary>
    public class CommAlarmConfig
    {
        /// <summary>
        /// 是否启用掉线报警
        /// </summary>
        public bool isDisconnectedAlarmEnable;
        /// <summary>
        /// 设定多长时间间隔启动报警
        /// </summary>
        public int nAlarmInterval;
        /// <summary>
        /// 波特率
        /// </summary>
        public int nBaudRate;
        /// <summary>
        /// 串口号
        /// </summary>
        public string strComPort;
    }
}
