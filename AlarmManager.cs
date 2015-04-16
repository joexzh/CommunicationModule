/*---------------------------------------------------------------- 
// Copyright (C) 1999-2013 广州中海达卫星导航技术股份有限公司 
// 版权所有。       
// 文件功能描述:  报警管理
// 
// 创建标识:     张永, 2013/4/7 11:58:40 
// 
// 修改标识：   修改人，修改时间
// 修改描述：   原某功能 改为某功能
//----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using ZHD.SYS.CommonUtility.AlarmLib;
using System.Threading;
using System.Data;
using CommunicationModule.Entity;

namespace CommunicationModule
{
    /// <summary>
    /// 报警管理
    /// </summary>
    public class AlarmManager
    {
        #region 字段

        private SMSSender m_smsSender = new SMSSender();//短信报警实例
        private Timer m_DisconnectedAlarmTimer;   //add2014.8.22添加掉线报警定时器
        private CommAlarmConfig m_CommConfig;
        private StationInfoDBOperate m_DBOperate;

        #endregion

        #region 属性

        #endregion

        #region 委托与事件

        #endregion

        #region 公有函数
        /// <summary>
        /// 构造函数change2014.8.22
        /// </summary>
        /// <param name="DBOperate">工程库信息</param>
        /// <param name="SendInfo">数据超警戒值报警配置</param>
        /// <param name="CommConfig">掉线报警配置</param>
        public bool StartAlarm(StationInfoDBOperate DBOperate)
        {
            if (null == DBOperate)
            {
                FileOperator.ExceptionLog(CommunicationModule.Properties.Resources.strNullParameterError);
                return false;
            }

            DBOperate.GetCommAlarmConfig(out m_CommConfig);

            //add2014.8.22一分钟调用一次该回调检测
            if (m_CommConfig.isDisconnectedAlarmEnable)
            {
                m_DBOperate = DBOperate;
                m_DisconnectedAlarmTimer = new Timer(new TimerCallback(DisconnectedAlarmTimer_Tick), null, 0, 60000);
                return true;
            }
            else
            {
                return false;
            }
       }

        public void StopAlarm()
        {
            if (null != m_DisconnectedAlarmTimer)
            {
                m_DisconnectedAlarmTimer.Dispose();
            }
            if (null != m_DBOperate)
            {
                m_DBOperate.ResetCommunicationLog();
            }
        }
        #endregion

        #region 私有方法

        /// <summary>
        /// 报警
        /// </summary>
        /// <param name="alarmInfo">报警信息</param>
        /// <param name="drContactInfo">联系人信息</param>
        private void SendAlarm(CommLogInfo alarmInfo)
        {
            if (null == m_DBOperate || null == m_CommConfig)
            {
                FileOperator.ExceptionLog(CommunicationModule.Properties.Resources.strNullParameterError);
                return;
            }

            string strAlarmContent = "";//报警内容
            int retSMS = 0;

            List<string> PhoneList;
            m_DBOperate.GetLinkManList(out PhoneList);

            if(null == PhoneList || 0 >= PhoneList.Count)
            {
                FileOperator.ExceptionLog(CommunicationModule.Properties.Resources.strPhoneListError);
                return ;
            }

            if (alarmInfo.isConnected)
            {
                strAlarmContent = string.Format(
                    CommunicationModule.Properties.Resources.strConnectedAlarmContent,
                    alarmInfo.nLinkPort,
                    alarmInfo.strDeviceID,
                    alarmInfo.ConnectedTime);
            }
            else
            {
                strAlarmContent = string.Format(
                    CommunicationModule.Properties.Resources.strDisconnectedAlarmContent,
                    alarmInfo.nLinkPort,
                    alarmInfo.strDeviceID,
                    alarmInfo.DisconnectedTime);
            }

            int nCount = PhoneList.Count;
            for (int i = 0; i < nCount; i++)
            {
                retSMS = m_smsSender.SendSMS(
                                SMSProvider.Wavecom,
                                int.Parse(m_CommConfig.strComPort.Substring(3)),
                                m_CommConfig.nBaudRate,
                                strAlarmContent,
                                PhoneList[i]);
            }
        }

        /// <summary>
        /// 掉线报警定时器回调add2014.8.22
        /// </summary>
        /// <param name="sender"></param>
        private void DisconnectedAlarmTimer_Tick(Object sender)
        {
            if (null == m_DBOperate || null == m_CommConfig)
            {
                FileOperator.ExceptionLog(CommunicationModule.Properties.Resources.strNullParameterError);
                return;
            }

            List<CommLogInfo> LogInfoList;
            m_DBOperate.GetCommunicationLog(out LogInfoList);

            if (null == LogInfoList || 0 >= LogInfoList.Count)
            {
                FileOperator.ExceptionLog(CommunicationModule.Properties.Resources.strPhoneListError);
                return;
            }

            int nCount = LogInfoList.Count;
            for (int i = 0; i < nCount; i++)
            {
                if (LogInfoList[i].isConnected)
                {
                    if (null == LogInfoList[i].DisconnectedTime)
                    {
                        continue;
                    }
                    else
                    {
                        //如果连接时间与断开时间间隔大于指定报警间隔，且当前时间与连接时间差距小于一分钟则报警
                        TimeSpan SubTime1 = ((DateTime)LogInfoList[i].ConnectedTime).Subtract(
                            (DateTime)LogInfoList[i].DisconnectedTime);

                        TimeSpan SubTime2 = DateTime.Now.Subtract((DateTime)LogInfoList[i].ConnectedTime);

                        if (SubTime1.TotalMinutes >= m_CommConfig.nAlarmInterval
                            && 1 >= SubTime2.TotalMinutes)
                        {
                            SendAlarm(LogInfoList[i]);
                        }
                    }
                }
                else
                {
                    //如果客户当前断开，则计算当前断开时间，判定其断开时间是否大于指定时间间隔，决定是否报警
                    TimeSpan SubTime = DateTime.Now.Subtract((DateTime)LogInfoList[i].DisconnectedTime);

                    if (SubTime.TotalMinutes >= m_CommConfig.nAlarmInterval
                        && SubTime.TotalMinutes <= m_CommConfig.nAlarmInterval + 1)
                    {
                        SendAlarm(LogInfoList[i]);
                    }
                }
            }
        }
        #endregion
    }
}
