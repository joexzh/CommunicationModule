using System;
using System.Collections.Generic;

using System.Text;
using System.Data;
using System.Data.SqlClient;
using CommunicationModule.Entity;
using ZHD.SYS.CommonUtility.DatabaseLib;

namespace CommunicationModule
{
    public class StationInfoDBOperate
    {
        private DataInfoSaver m_dbSaver;
        private DBStyle m_DBStyle;

        /// <summary>
        /// 连接数据库
        /// </summary>
        /// <param name="DatabaseType"></param>
        /// <param name="strDataSource"></param>
        /// <param name="strDatabase"></param>
        /// <param name="strUser"></param>
        /// <param name="strPassword"></param>
        public void LinkDatabase(int DatabaseType, string strDataSource, string strDatabase, 
            string strUser, string strPassword)
        {
            DBConfigInfo dbConfigInfo = new DBConfigInfo();
            dbConfigInfo.DbServer = strDataSource;
            dbConfigInfo.DbName = strDatabase;
            dbConfigInfo.DbUser = strUser;
            dbConfigInfo.DbPassword = strPassword;
            dbConfigInfo.DbStyle = (DBStyle)DatabaseType;
            m_DBStyle = (DBStyle)DatabaseType;      //记录数据库类型add2014.8.21
            m_dbSaver = new DataInfoSaver(dbConfigInfo);
        }

        /// <summary>
        /// 打开数据库
        /// </summary>
        /// <returns></returns>
        public int OpenDatabase()
        {
            return m_dbSaver.Connect();
        }

        /// <summary>
        /// 关闭数据库
        /// </summary>
        /// <returns></returns>
        public int Close()
        {
            return m_dbSaver.Disconnect();
        }

        /// <summary>
        /// 获取通讯站点信息
        /// </summary>
        /// <param name="strStationInfoTable">表名</param>
        /// <param name="listStationInfo">站点列表</param>
        /// <returns></returns>
        public int GetStationInfo(string strStationInfoTable, 
            out List<StationInfo> listStationInfo)
        {
            listStationInfo = null;
            listStationInfo = new List<StationInfo>();
            DataTable StationTable = new DataTable();

            string strExecuteCmd = string.Format("select * from [{0}]", 
                strStationInfoTable);

            int nReturn = m_dbSaver.ExecuteSql(strExecuteCmd, out StationTable);

            if (null == StationTable || 0 > nReturn || 0 >= StationTable.Rows.Count)
            {
                FileOperator.ExceptionLog(Properties.Resources.strReadTableFailure);
                return nReturn;
            }
            
            int nRows = StationTable.Rows.Count;
            for (int i = 0; i < nRows; i++)
            {
                StationInfo myStation = new StationInfo();
                myStation.pro_CommunicationType = (CommunicationType)(int.Parse(StationTable.Rows[i][1].ToString()));

                if (StationTable.Rows[i][2] != DBNull.Value)
                {
                    myStation.pro_strIP = StationTable.Rows[i][2].ToString();
                }
                if (StationTable.Rows[i][3] != DBNull.Value)
                {
                    myStation.pro_nPort = int.Parse(StationTable.Rows[i][3].ToString());
                }
                if (StationTable.Rows[i][4] != DBNull.Value)
                {
                    myStation.pro_nBaudRate = int.Parse(StationTable.Rows[i][4].ToString());
                }
                if (StationTable.Rows[i][5] != DBNull.Value)
                {
                    myStation.pro_Protocal = (ProtocalType)int.Parse(StationTable.Rows[i][5].ToString());
                }
                if (StationTable.Rows[i][6] != DBNull.Value)
                {
                    int nOnLine = int.Parse(StationTable.Rows[i][6].ToString());
                    if (0 < nOnLine)
                    {
                        myStation.pro_IsOnline = true;
                    }
                }
                if (StationTable.Rows[i][7] != DBNull.Value)
                {
                    int nStop = int.Parse(StationTable.Rows[i][7].ToString());
                    if (0 < nStop)
                    {
                        myStation.pro_isStop = true;
                    }
                }
                if (StationTable.Rows[i][8] != DBNull.Value)
                {
                    myStation.pro_nMainPort = int.Parse(StationTable.Rows[i][8].ToString());
                }
                if (StationTable.Rows[i][9] != DBNull.Value)
                {
                    myStation.pro_MainPortCommType = (CommunicationType)(int.Parse(StationTable.Rows[i][9].ToString()));
                }
                listStationInfo.Add(myStation);
            }
            return 0;
        }

        /// <summary>
        /// 设置通讯站点信息
        /// </summary>
        /// <param name="strStationInfoTable">表名</param>
        /// <param name="listStationInfo">站点列表</param>
        /// <returns></returns>
        public int SetStationInfo(string strStationInfoTable,
            List<StationInfo> listStationInfo)
        {
            int nReturn = -1;
            if (0 > listStationInfo.Count)
            {
                FileOperator.ExceptionLog(Properties.Resources.strListStationEmpty);
                return nReturn;
            }

            int nListNum = listStationInfo.Count;
            for (int i = 0; i < nListNum; i++)
            {
                string strExecuteCmd;
                if (listStationInfo[i].pro_IsOnline)
                {
                    strExecuteCmd = string.Format("update [{0}] set [ISONLINE] = 1 where [PORT] = {1}",
                        strStationInfoTable, listStationInfo[i].pro_nPort);
                }
                else
                {
                    strExecuteCmd = string.Format("update [{0}] set [ISONLINE] = 0 where [PORT] = {1}",
                        strStationInfoTable, listStationInfo[i].pro_nPort);
                }

                m_dbSaver.ExecuteSql(strExecuteCmd, out nReturn);
            }
            return nReturn;
        }

        /// <summary>
        /// 重置通讯站点信息
        /// </summary>
        /// <param name="strStationInfoTable">表名</param>
        /// <returns></returns>
        public int ResetStationInfo(string strStationInfoTable)
        {
            int nReturn = -1;
            string strExecuteCmd = string.Format("update [{0}] set [ISONLINE] = 0",
                strStationInfoTable);
            m_dbSaver.ExecuteSql(strExecuteCmd, out nReturn);
            return nReturn;
        }

        /// <summary>
        /// 更新通讯日志表，若记录不存在，则插入记录add2014.8.22
        /// </summary>
        /// <param name="StationCommLog"></param>
        /// <returns></returns>
        public int UpdateCommunicationLog(ShowInfo StationCommLog)
        {
            int nReturn = -1;
            int nDataSource = -1;

            if (StationCommLog.pro_isDataSource)
            {
                nDataSource = 1;
            }
            else
            {
                nDataSource = 0;
            }

            if (StationCommLog.pro_isConnected)
            {
                #region 连接信息处理
                string strExcuteCmd = string.Format(@"select * from COMMUNICATIONLOG where
                    LINKPORT = {0} and DEVICEID = '{1}' and ISDATASOURCE = {2}",
                   StationCommLog.pro_nLocalPort, StationCommLog.pro_strDeviceID,
                   nDataSource);

                DataTable CommDT = new DataTable();
                nReturn = m_dbSaver.ExecuteSql(strExcuteCmd, out CommDT);

                if (0 == nReturn && null != CommDT && 0 < CommDT.Rows.Count)
                {
                    strExcuteCmd = string.Format(@"update COMMUNICATIONLOG set
                    LINKSTATUS = 1, CONNECTEDTIME = '{0}', COMMIP = '{4}' where 
                    LINKPORT = {1} and DEVICEID = '{2}' and ISDATASOURCE = {3}",
                            StationCommLog.pro_ConnectedTime,
                            StationCommLog.pro_nLocalPort,
                            StationCommLog.pro_strDeviceID,
                            nDataSource, StationCommLog.pro_strIP);
                    m_dbSaver.ExecuteSql(strExcuteCmd, out nReturn);
                }
                else
                {
                    #region 设备连接信息
                    switch (m_DBStyle)
                    {
                        case DBStyle.OracleStyle:
                            strExcuteCmd = string.Format(@"insert into COMMUNICATIONLOG(
                            AID, LINKPORT, DEVICEID, LINKSTATUS, ISDATASOURCE, CONNECTEDTIME, 
                             COMMIP) values(AID.nextval, {0}, '{1}', 1, {2}, '{3}', '{4}')",
                                StationCommLog.pro_nLocalPort,
                                StationCommLog.pro_strDeviceID,
                                nDataSource,
                                StationCommLog.pro_ConnectedTime,
                                StationCommLog.pro_strIP);
                            break;
                        default:
                            strExcuteCmd = string.Format(@"insert into COMMUNICATIONLOG(
                            LINKPORT, DEVICEID, LINKSTATUS, ISDATASOURCE, CONNECTEDTIME, COMMIP) 
                            values({0}, '{1}', 1, {2}, '{3}', '{4}')",
                                StationCommLog.pro_nLocalPort,
                                StationCommLog.pro_strDeviceID,
                                nDataSource,
                                StationCommLog.pro_ConnectedTime,
                                StationCommLog.pro_strIP);
                            break;
                    }
                    m_dbSaver.ExecuteSql(strExcuteCmd, out nReturn);
                    #endregion
                }
                #endregion
            }
            else
            {
                #region 断开信息处理
                string strExcuteCmd = string.Format(@"select * from COMMUNICATIONLOG where
                    COMMIP = '{0}'", StationCommLog.pro_strIP);

                DataTable CommDT = new DataTable();
                nReturn = m_dbSaver.ExecuteSql(strExcuteCmd, out CommDT);

                if (0 == nReturn && null != CommDT && 0 < CommDT.Rows.Count)
                {
                    strExcuteCmd = string.Format(@"update COMMUNICATIONLOG set
                    LINKSTATUS = 0, DISCONNECTEDTIME = '{0}' where 
                    COMMIP = '{1}'",
                            StationCommLog.pro_DisConnectedTime,
                            StationCommLog.pro_strIP);
                    m_dbSaver.ExecuteSql(strExcuteCmd, out nReturn);
                }
                #endregion
            }

            return nReturn;
        }

        /// <summary>
        /// 重置通讯日志
        /// </summary>
        /// <returns></returns>
        public int ResetCommunicationLog()
        {
            int nReturn = -1;
            string strExcuteCmd = string.Format(@"update COMMUNICATIONLOG set
                    LINKSTATUS = 0, DISCONNECTEDTIME = '{0}'", DateTime.Now);
            m_dbSaver.ExecuteSql(strExcuteCmd, out nReturn);

            return nReturn;
        }

        /// <summary>
        /// 获取通讯日志信息
        /// </summary>
        /// <param name="CommList">输出通讯日志表</param>
        /// <returns></returns>
        public int GetCommunicationLog(out List<CommLogInfo> CommList)
        {
            if (null == m_dbSaver)
            {
                CommList = null;
                return -1;
            }

            CommList = null;
            CommList = new List<CommLogInfo>();
            DataTable StationTable = new DataTable();

            string strExecuteCmd = string.Format(@"select * from [COMMUNICATIONLOG]");

            int nReturn = m_dbSaver.ExecuteSql(strExecuteCmd, out StationTable);
            if (null == StationTable || 0 > nReturn || 0 >= StationTable.Rows.Count)
            {
                FileOperator.ExceptionLog(Properties.Resources.strReadTableFailure);
                return nReturn;
            }

            int nCount = StationTable.Rows.Count;
            for (int i = 0; i < nCount; i++)
            {
                #region 通讯日志信息
                CommLogInfo LogInfo = new CommLogInfo();
                if (StationTable.Rows[i]["LINKPORT"] != DBNull.Value)
                {
                    LogInfo.nLinkPort = int.Parse(StationTable.Rows[i]["LINKPORT"].ToString());
                }
                if (StationTable.Rows[i]["DEVICEID"] != DBNull.Value)
                {
                    LogInfo.strDeviceID = StationTable.Rows[i]["DEVICEID"].ToString();
                }
                if (StationTable.Rows[i]["LINKSTATUS"] != DBNull.Value)
                {
                    int nLinkStatus = int.Parse(StationTable.Rows[i]["LINKSTATUS"].ToString());
                    if (0 < nLinkStatus)
                    {
                        LogInfo.isConnected = true;
                    }
                    else
                    {
                        LogInfo.isConnected = false;
                    }
                }
                if (StationTable.Rows[i]["ISDATASOURCE"] != DBNull.Value)
                {
                    int nDataSource = int.Parse(StationTable.Rows[i]["ISDATASOURCE"].ToString());
                    if (0 < nDataSource)
                    {
                        LogInfo.isDataSource = true;
                    }
                    else
                    {
                        LogInfo.isDataSource = false;
                    }
                }
                if (StationTable.Rows[i]["COMMIP"] != DBNull.Value)
                {
                    LogInfo.strCommIP = StationTable.Rows[i]["COMMIP"].ToString();
                }
                if (StationTable.Rows[i]["CONNECTEDTIME"] != DBNull.Value)
                {
                    LogInfo.ConnectedTime = DateTime.Parse(StationTable.Rows[i]["CONNECTEDTIME"].ToString());
                }
                if (StationTable.Rows[i]["DISCONNECTEDTIME"] != DBNull.Value)
                {
                    LogInfo.DisconnectedTime = DateTime.Parse(StationTable.Rows[i]["DISCONNECTEDTIME"].ToString());
                }
                #endregion

                CommList.Add(LogInfo);
            }
            
            return 0;
        }

        /// <summary>
        /// 读取掉线配置信息add2014.8.22
        /// </summary>
        /// <param name="AlarmInfo"></param>
        /// <returns></returns>
        public int GetCommAlarmConfig(out CommAlarmConfig AlarmInfo)
        {
            if (null == m_dbSaver)
            {
                AlarmInfo = null;
                return -1;
            }
            AlarmInfo = null;
            AlarmInfo = new CommAlarmConfig();
            DataTable StationTable = new DataTable();

            string strExecuteCmd = string.Format(@"select * from [COMMALARMCONFIG]");

            int nReturn = m_dbSaver.ExecuteSql(strExecuteCmd, out StationTable);
            if (null == StationTable || 0 > nReturn || 0 >= StationTable.Rows.Count)
            {
                FileOperator.ExceptionLog(Properties.Resources.strReadTableFailure);
                return nReturn;
            }


            #region 掉线配置信息

            if (StationTable.Rows[0]["ALARMINTERVAL"] != DBNull.Value)
            {
                AlarmInfo.nAlarmInterval = int.Parse(StationTable.Rows[0]["ALARMINTERVAL"].ToString());
            }
            if (StationTable.Rows[0]["ALARMENABLE"] != DBNull.Value)
            {
                int nAlarmEnable = int.Parse(StationTable.Rows[0]["ALARMENABLE"].ToString());
                if (0 < nAlarmEnable)
                {
                    AlarmInfo.isDisconnectedAlarmEnable = true;
                }
                else
                {
                    AlarmInfo.isDisconnectedAlarmEnable = false;
                }
            }
            if (StationTable.Rows[0]["COMPORT"] != DBNull.Value)
            {
                AlarmInfo.strComPort = StationTable.Rows[0]["COMPORT"].ToString();
            }
            if (StationTable.Rows[0]["BAUDRATE"] != DBNull.Value)
            {
                AlarmInfo.nBaudRate = int.Parse(StationTable.Rows[0]["BAUDRATE"].ToString());
            }
            #endregion

            return 0;
        }

        /// <summary>
        /// 获取联系人手机号
        /// </summary>
        /// <param name="LinkManList"></param>
        /// <returns></returns>
        public int GetLinkManList(out List<string> LinkManList)
        {
            if (null == m_dbSaver)
            {
                LinkManList = null;
                return -1;
            }
            LinkManList = null;
            LinkManList = new List<string>();
            DataTable StationTable = new DataTable();

            string strExecuteCmd = string.Format(@"select APHONE from [COMMLINKMAN]");

            int nReturn = m_dbSaver.ExecuteSql(strExecuteCmd, out StationTable);
            if (null == StationTable || 0 > nReturn || 0 >= StationTable.Rows.Count)
            {
                FileOperator.ExceptionLog(Properties.Resources.strReadTableFailure);
                return nReturn;
            }


            #region 掉线配置信息
            int nCount = StationTable.Rows.Count;
            for (int i = 0; i < nCount; i++)
            {
                if (StationTable.Rows[i]["APHONE"] != DBNull.Value)
                {
                    string strPhone = StationTable.Rows[0]["APHONE"].ToString();
                    LinkManList.Add(strPhone);
                }
            }
            
            #endregion

            return 0;
        }

        /// <summary>
        /// 获取本地通讯信息
        /// </summary>
        /// <param name="strIP"></param>
        /// <param name="nPort"></param>
        /// <returns></returns>
        public int GetLocalCommInfo(out string strIP, out int nPort)
        {
            strIP = "";
            nPort = -1;
            if (null == m_dbSaver)
            {
                return -1;
            }

            DataTable StationTable = new DataTable();
            string strExecuteCmd = string.Format(@"select * from [COMMUNICATION]");

            int nReturn = m_dbSaver.ExecuteSql(strExecuteCmd, out StationTable);
            if (null == StationTable || 0 > nReturn || 0 >= StationTable.Rows.Count)
            {
                FileOperator.ExceptionLog(Properties.Resources.strReadTableFailure);
                return nReturn;
            }

            if (StationTable.Rows[0]["IPADDRESS"] != DBNull.Value)
            {
                strIP = StationTable.Rows[0]["IPADDRESS"].ToString();
            }
            if (StationTable.Rows[0]["PORT"] != DBNull.Value)
            {
                nPort = int.Parse(StationTable.Rows[0]["PORT"].ToString());
            }

            return nReturn;
        }
    }
}
