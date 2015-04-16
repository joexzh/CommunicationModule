using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using CommunicationModule.Entity;
using ZHD.SYS.CommonUtility.CommunicationLib;
using System.Timers;
using System.IO;
using System.Threading;

namespace CommunicationModule
{
    class CommunicationBase
    {
        #region 字段
        public delegate void LinkStatusHandler(ShowInfo ReturnInfo);    //定义事件委托，接收数据
        public event LinkStatusHandler StatusEventHandler;              //接收数据事件
        private List<CommunicationBaseStation> m_listCommStation;       //通讯服务器信息，包含主副端口
        private List<ShowInfo> m_listShow;                              //返回给界面显示的连接、断开或接收数据信息等
        private string m_strTableName = "STATIONINFO";                  //通讯服务器的专用配置表名
        private int m_UDPStopInterval = 15;                             //默认间隔15s未收到数据则删除该客户信息
        private int m_UDPHeartInterval = 5000;                          //心跳发送间隔5s，仅针对带协议udp传输
        private List<TimerDisposeInfo> m_listTimerInfo;                 //存放udp最新活动时间，用于判断是否该断开该"连接"
        private const int m_nUDPHeadLength = 15;                        //udp上层自定义协议头长度
        private List<DataInfo> m_listDataInfo;                          //接收到的数据的存放列表
        private Thread m_SendMsgThread;                                 //专用于转发数据或命令的线程

        private List<CommunicationPointInfoBase> m_listUDPSrcPoint;     //用于记录设备的点，给它们发心跳
        private System.Timers.Timer m_UDPHeartTimer;                    //定时发送心跳和断开udp“连接”的定时器，共用

        //udp协议的各类协议信息及应答信息
        private const string m_strUDPHead = "$$";                       //自定义udp协议必须携带的头
        private const string m_strUDPSrcLogin = "LO";                   //设备端登陆
        private const string m_strUDPDstLogin = "LI";                   //数据接收端登陆
        private const string m_strUDPData = "DO";                       //设备端发送的数据包
        private const string m_strUDPDstHeart = "HI";                   //数据接收端心跳包头
        private const string m_strUDPSrcHeart = "HO";                   //设备端心跳包头
        private const string m_strUDPAnswer = "$$H00000000000";         //心跳回应包内容
        CommunicationDataBase m_AnswerData;                             //udp心跳回应包
        private SetFileRW m_LogFile;                                    //写各个端口连接、断开信息的日志
        
        private StationInfoDBOperate m_DBOperate;                       //读数据库得到配置信息
        private List<StationInfo> m_listStationInfo;                    //存放从配置文件中读取的站点信息表
        private AlarmManager m_Alarm;                                   //掉线报警类对象add2014.8.25
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        public CommunicationBase()
        {
            m_listShow = new List<ShowInfo>();
            m_listCommStation = new List<CommunicationBaseStation>();
            m_listDataInfo = new List<DataInfo>();
            m_LogFile = new SetFileRW();
            m_Alarm = new AlarmManager();                       //add2014.8.25
  //          m_listStationInfo = new List<StationInfo>();
            m_SendMsgThread = new Thread(new ThreadStart(SendDataThread));
            m_SendMsgThread.Start();
        }
        
        /// <summary>
        /// 停止转发数据的线程
        /// </summary>
        public void Close()
        {
            m_SendMsgThread.Abort();
            m_SendMsgThread.Join();
        }

        /// <summary>
        /// 全部启动
        /// </summary>
        /// <param name="DBConfigInfo">数据库连接信息</param>
        /// <returns>true:全部启动成功
        /// false:启动失败</returns>
        public bool StartAll(StationInfoDBOperate DBOperate, int nHeartInterval, int nStopInterval)
        {
            //由上层提供心跳时间及udp停止时间
            if (0 < nHeartInterval)
            {
                m_UDPHeartInterval = nHeartInterval * 1000;
            }
            if (0 < nStopInterval)
            {
                m_UDPStopInterval = nStopInterval;
            }

            m_DBOperate = DBOperate;
            if (null == m_DBOperate)
            {
                return false;
            }

            if (null != m_listStationInfo)
            {
                m_listStationInfo.Clear();
                m_listStationInfo = null;
            }

            //获取通讯表的所有信息
            int nReturn = m_DBOperate.GetStationInfo(m_strTableName, out m_listStationInfo);
            if (0 > nReturn)
            {
                FileOperator.ExceptionLog(Properties.Resources.strTableNotExistError);
                return false;
            }

            //若通讯表中有记录，则启动相应服务
            if (0 < m_listStationInfo.Count)
            {
                int nStationNum = m_listStationInfo.Count;
                for (int i = 0; i < nStationNum; i++)
                {
                    //保证端口在1至65536间时启动服务add2014.8.4
                    if (0 < m_listStationInfo[i].pro_nPort && 65536 > m_listStationInfo[i].pro_nPort)
                    {
                        StartOne(m_listStationInfo[i]);
                    }
                }
            }

            //设置各个服务器启动后的状态
            m_DBOperate.SetStationInfo(m_strTableName, m_listStationInfo);

            //add2014.8.25掉线报警启动
            if (null != m_Alarm)
            {
                m_Alarm.StartAlarm(m_DBOperate);
            }

            return true;
        }

        /// <summary>
        /// 全部停止
        /// </summary>
        public void StopAll()
        {
            //停止掉线报警检测
            if (null != m_Alarm)
            {
                m_Alarm.StopAlarm();
            }
            if (null != m_DBOperate)
            {
                m_DBOperate.ResetStationInfo(m_strTableName);
            }

            //若当前有服务器启动，则一次遍历，关闭所有服务并清空所有连接
            if (0 < m_listCommStation.Count)
            {
                int nStationNum = m_listCommStation.Count;
                for (int i = 0; i < nStationNum; i++)
                {
                    //停止主端口服务
                    if (null != m_listCommStation[i].pro_MainCommStation)
                    {
                        m_listCommStation[i].pro_MainCommStation.Close();
                        m_listCommStation[i].pro_listMainPointInfo.Clear();

                        if (null != StatusEventHandler)
                        {
                            //add2014.5.26回应给上层说明当前服务已停止
                            ShowInfo Show = new ShowInfo();
                            Show.pro_nLocalPort = m_listCommStation[i].pro_nMainPort;
                            Show.pro_isStop = true;
                            StatusEventHandler(Show);
                        }
                        else
                        {
                            FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
                        }
                    }

                    //停止副端口服务
                    if (null != m_listCommStation[i].pro_AssistCommStation)
                    {
                        m_listCommStation[i].pro_AssistCommStation.Close();
                        m_listCommStation[i].pro_listAssistPointInfo.Clear();

                        if (null != StatusEventHandler)
                        {
                            //add2014.5.26
                            ShowInfo ShowAssist = new ShowInfo();
                            ShowAssist.pro_nLocalPort = m_listCommStation[i].pro_nAssistPort;
                            ShowAssist.pro_isStop = true;
                            StatusEventHandler(ShowAssist);
                        }
                        else
                        {
                            FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
                        }
                    }
                }

                //清空显示表及通讯服务表
                m_listShow.Clear();
                m_listCommStation.Clear();
            }
        }

        /// <summary>
        /// 客户连接
        /// </summary>
        /// <param name="serverPointInfo">tcp客户信息</param>
        /// <param name="errorArgs">错误信息</param>
        private void TCPServerObj_evClientAccept(CommunicationPointInfoBase serverPointInfo, EventArgs errorArgs)
        {
            if (0 >= m_listCommStation.Count)
            {
                //此种情况基本不可能出现
                FileOperator.ExceptionLog(Properties.Resources.strListCommStationEmpty);
                return;
            }
 
            int nPort = ((NetworkPointInfo)serverPointInfo).pro_RemotePort;

            //写该服务端口的连接日志
            m_LogFile.WriteLogFile(nPort.ToString() + ".txt", Properties.Resources.strConnected,
               ((NetworkPointInfo)serverPointInfo).pro_LocalIP.ToString(), ((NetworkPointInfo)serverPointInfo).pro_LocalPort);

            int nListNum = m_listCommStation.Count;
            bool isNoneProtocal = false;

            #region 对无协议端口的连接进行处理
            for (int i = 0; i < nListNum; i++)
            {
                if (nPort == m_listCommStation[i].pro_nMainPort && 
                    m_listCommStation[i].pro_MainCommType == CommunicationType.TCPServer)
                {
                    if (ProtocalType.None == m_listCommStation[i].pro_MainProtocal)
                    {
                        isNoneProtocal = true;
                        StationPointInfo SPInfo = new StationPointInfo();
                        SPInfo.pro_StationPoint = serverPointInfo;
                        m_listCommStation[i].pro_listMainPointInfo.Add(SPInfo);
                    }
                    break;
                }

                if (nPort == m_listCommStation[i].pro_nAssistPort &&
                    m_listCommStation[i].pro_AssistCommType == CommunicationType.TCPServer)
                {
                    if (ProtocalType.None == m_listCommStation[i].pro_AssistProtocal)
                    {
                        isNoneProtocal = true;
                        StationPointInfo SPInfo = new StationPointInfo();
                        SPInfo.pro_StationPoint = serverPointInfo;
                        m_listCommStation[i].pro_listAssistPointInfo.Add(SPInfo);
                    }
                    break;
                }
            }
            #endregion

            if (isNoneProtocal)
            {
                #region 不采取协议时返回给显示界面//add2014.5.26
                if (null != StatusEventHandler)
                {
                    if (0 >= m_listShow.Count)
                    {
                        FileOperator.ExceptionLog(Properties.Resources.strListShowEmpty);
                        return;
                    }

                    int nCount = m_listShow.Count;
                    for (int i = 0; i < nCount; i++)
                    {
                        if (((NetworkPointInfo)serverPointInfo).pro_RemotePort == m_listShow[i].pro_nLocalPort)
                        {
                            //change2014.5.27应对同时启动多个客户端的情况
                            m_listShow[i].pro_nClientNum += 1;

                            ShowInfo preShow = new ShowInfo();
                            preShow.pro_isConnected = true;
                            preShow.pro_strDeviceID = m_listShow[i].pro_strDeviceID;
                            preShow.pro_nClientNum = m_listShow[i].pro_nClientNum;
                            preShow.pro_ConnectedTime = DateTime.Now;
                            preShow.pro_nLocalPort = ((NetworkPointInfo)serverPointInfo).pro_RemotePort;
                            preShow.pro_strIP = string.Format("{0}:{1}",
                                ((NetworkPointInfo)serverPointInfo).pro_LocalIP, ((NetworkPointInfo)serverPointInfo).pro_LocalPort);
                            StatusEventHandler(preShow);
                            break;
                        }
                    }
                }
                else
                {
                    FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
                }
                #endregion
            }
        }

        /// <summary>
        /// 客户断开
        /// </summary>
        /// <param name="serverPointInfo">tcp客户信息</param>
        /// <param name="errorArgs">错误信息</param>
        private void TCPServerObj_evLinkDisconnect(CommunicationPointInfoBase serverPointInfo, EventArgs errorArgs)
        {
            int nPort = ((NetworkPointInfo)serverPointInfo).pro_RemotePort;
            m_LogFile.WriteLogFile(nPort.ToString() + ".txt", "\t" + Properties.Resources.strDisconnected,
                ((NetworkPointInfo)serverPointInfo).pro_LocalIP.ToString(), ((NetworkPointInfo)serverPointInfo).pro_LocalPort);

            int nListNum = m_listCommStation.Count;
            bool isExist = false;

            #region 对客户的断开进行处理
            for (int i = 0; i < nListNum; i++)
            {
                //先查找主端口，若相等，则在主端口的远程站点中查找相应点删除
                if (nPort == m_listCommStation[i].pro_nMainPort &&
                    m_listCommStation[i].pro_MainCommType == CommunicationType.TCPServer)
                {
                    int nListCommNum = m_listCommStation[i].pro_listMainPointInfo.Count;
                    for (int j = 0; j < nListCommNum; j++)
                    {
                        //change2014.10.14，判断ip和端口，确保准确找到对应对话框
                        if (((NetworkPointInfo)serverPointInfo).pro_LocalIP.ToString() ==
                            ((NetworkPointInfo)m_listCommStation[i].pro_listMainPointInfo[j].pro_StationPoint).pro_LocalIP.ToString()
                             && ((NetworkPointInfo)serverPointInfo).pro_LocalPort ==
                            ((NetworkPointInfo)m_listCommStation[i].pro_listMainPointInfo[j].pro_StationPoint).pro_LocalPort)
                        {
                            isExist = true;
                            m_listCommStation[i].pro_listMainPointInfo.RemoveAt(j);
                            break;
                        }
                    }
                    break;
                }

                //查找副端口，与主端口中的处理类似
                if (nPort == m_listCommStation[i].pro_nAssistPort &&
                    m_listCommStation[i].pro_AssistCommType == CommunicationType.TCPServer)
                {
                    int nListCommNum = m_listCommStation[i].pro_listAssistPointInfo.Count;
                    for (int j = 0; j < nListCommNum; j++)
                    {
                        //change2014.10.14，判断ip和端口，确保准确找到对应对话框
                        if (((NetworkPointInfo)serverPointInfo).pro_LocalIP.ToString() ==
                            ((NetworkPointInfo)m_listCommStation[i].pro_listAssistPointInfo[j].pro_StationPoint).pro_LocalIP.ToString()
                             && ((NetworkPointInfo)serverPointInfo).pro_LocalPort ==
                            ((NetworkPointInfo)m_listCommStation[i].pro_listAssistPointInfo[j].pro_StationPoint).pro_LocalPort)
                        {
                            isExist = true;
                            m_listCommStation[i].pro_listAssistPointInfo.RemoveAt(j);
                            break;
                        }
                    }
                    break;
                }
            }
            #endregion

            #region//连接确认回调,在未分离设备号前返回，有助于区分解析模块和设备模块
            if (isExist && null != StatusEventHandler)
            {
                //add2014.5.26  
                if (0 >= m_listShow.Count)
                {
                    FileOperator.ExceptionLog(Properties.Resources.strListShowEmpty);
                    return;
                }

                int nCount = m_listShow.Count;
                for (int i = 0; i < nCount; i++)
                {
                    if (((NetworkPointInfo)serverPointInfo).pro_RemotePort == m_listShow[i].pro_nLocalPort
                        && 0 < m_listShow[i].pro_nClientNum)
                    {
                        //断开
                        m_listShow[i].pro_nClientNum -= 1;
                        ShowInfo preShow = new ShowInfo();
                        preShow.pro_nLocalPort = m_listShow[i].pro_nLocalPort;
                        preShow.pro_nTotalRecvBytes = 0;
                        preShow.pro_isConnected = false;
                        preShow.pro_DisConnectedTime = DateTime.Now;
                        preShow.pro_nClientNum = m_listShow[i].pro_nClientNum;
                        preShow.pro_strDeviceID = m_listShow[i].pro_strDeviceID;
                        preShow.pro_strIP = string.Format("{0}:{1}",
                            ((NetworkPointInfo)serverPointInfo).pro_LocalIP, ((NetworkPointInfo)serverPointInfo).pro_LocalPort);
                        StatusEventHandler(preShow);

                        //记录通讯日志
                        if (null != m_DBOperate)
                        {
                            m_DBOperate.UpdateCommunicationLog(preShow);
                        }
                        break;
                    }
                }
            }
            //else
            //{
            //    FileOperator.ExceptionLog("StatusEventHandler句柄为空，无法将底层信息返回给界面显示");
            //}
            #endregion
        }

        /// <summary>
        /// 带协议客户连接
        /// </summary>
        /// <param name="serverPointInfo">tcp客户信息</param>
        /// <param name="errorArgs">错误信息</param>
        private void TCPServerObj_evClientLinkProtocolConfirm(CommunicationPointInfoBase serverPointInfo, EventArgs errorArgs)
        {
            //获取ntrip协议中的设备号
            NtripCasterProtocol gnssProtocol = serverPointInfo.pro_CommunicationProtocol as NtripCasterProtocol;
            string strDevID = "";
            if (null == gnssProtocol)
            {
                FileOperator.ExceptionLog(Properties.Resources.strProtocalObjectError);
                return;
            }

            strDevID = gnssProtocol.pro_UserName;
            if ("" == strDevID)
            {
                FileOperator.ExceptionLog(Properties.Resources.strDeviceIDError);
                return;
            }

            //用$分离字符串strDevID，若为设备以外的模块，则会加上标识该模块的字符串
            char[] colonSplitChar = new char[] { '$' };
            string[] userDetailInfo = strDevID.Split(colonSplitChar, StringSplitOptions.RemoveEmptyEntries);

            #region 返回给显示界面,连接确认回调,在未分离设备号前返回，有助于区分解析模块和设备模块
            if (null != StatusEventHandler)
            {
                if (0 >= m_listShow.Count)
                {
                    FileOperator.ExceptionLog(Properties.Resources.strListShowEmpty);
                    return;
                }

                int nCount = m_listShow.Count;
                for (int i = 0; i < nCount; i++)
                {
                    if (((NetworkPointInfo)serverPointInfo).pro_RemotePort == m_listShow[i].pro_nLocalPort)
                    {
                        //连接，change2014.5.27应对同时启动多个客户端的情况
                        m_listShow[i].pro_nClientNum += 1;

                        ShowInfo preShow = new ShowInfo();
                        preShow.pro_nTotalRecvBytes = 0;
                        preShow.pro_isConnected = true;
                        preShow.pro_Protocal = m_listShow[i].pro_Protocal;
                        preShow.pro_strDeviceID = strDevID;
                        preShow.pro_nClientNum = m_listShow[i].pro_nClientNum;
                        preShow.pro_ConnectedTime = DateTime.Now;
                        preShow.pro_nLocalPort = ((NetworkPointInfo)serverPointInfo).pro_RemotePort;
                        preShow.pro_strIP = string.Format("{0}:{1}",
                            ((NetworkPointInfo)serverPointInfo).pro_LocalIP, ((NetworkPointInfo)serverPointInfo).pro_LocalPort);
                        StatusEventHandler(preShow);

                        //add2014.8.21添加是否是数据源标识
                        if (1 == userDetailInfo.Length)
                        {
                            preShow.pro_isDataSource = true;
                        }
                        else if (2 == userDetailInfo.Length)
                        {
                            preShow.pro_isDataSource = false;
                        }

                        //记录通讯日志
                        if (null != m_DBOperate)
                        {
                            m_DBOperate.UpdateCommunicationLog(preShow);
                        }
                        break;
                    }
                }
            }
            else
            {
                FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
            }
            #endregion

            bool isSrcPoint = false;

            if (1 == userDetailInfo.Length)
            {
                isSrcPoint = false;
            }
            else if (2 == userDetailInfo.Length)
            {
                //获取数据的设备，在此称为源点
                isSrcPoint = true;
                strDevID = userDetailInfo[1];
            }

            if (0 >= m_listCommStation.Count)
            {
                FileOperator.ExceptionLog(Properties.Resources.strListCommStationEmpty);
                return;
            }

            int nPort = ((NetworkPointInfo)serverPointInfo).pro_RemotePort;

            m_LogFile.WriteLogFile(nPort.ToString() + ".txt", Properties.Resources.strConnected,
               ((NetworkPointInfo)serverPointInfo).pro_LocalIP.ToString(), ((NetworkPointInfo)serverPointInfo).pro_LocalPort);

            int nListNum = m_listCommStation.Count;

            #region 对带协议端口的连接进行处理
            for (int i = 0; i < nListNum; i++)
            {
                #region 主端口处理设备断电问题，只能暂时解决，需再想方案 add2014.6.17
                if (nPort == m_listCommStation[i].pro_nMainPort &&
                    m_listCommStation[i].pro_MainCommType == CommunicationType.TCPServer)
                {
                    int nMainListNum = m_listCommStation[i].pro_listMainPointInfo.Count;
                    if (0 < nMainListNum && !isSrcPoint)
                    {
                        for (int j = 0; j < nMainListNum; j++)
                        {
                            if (isSrcPoint == m_listCommStation[i].pro_listMainPointInfo[j].pro_isSrc
                                && strDevID == m_listCommStation[i].pro_listMainPointInfo[j].pro_strDeviceID)
                            {
                                #region 连接确认回调,在未分离设备号前返回，有助于区分解析模块和设备模块
                                if (null != StatusEventHandler)
                                {
                                    int nCount = m_listShow.Count;
                                    for (int k = 0; k < nCount; k++)
                                    {
                                        if (((NetworkPointInfo)serverPointInfo).pro_RemotePort == m_listShow[k].pro_nLocalPort
                                            && 0 < m_listShow[i].pro_nClientNum)
                                        {
                                            //断开
                                            m_listShow[i].pro_nClientNum -= 1;
                                            ShowInfo preShow = new ShowInfo();
                                            preShow.pro_nLocalPort = m_listShow[k].pro_nLocalPort;
                                            preShow.pro_nTotalRecvBytes = 0;
                                            preShow.pro_isConnected = false;
                                            preShow.pro_DisConnectedTime = DateTime.Now;
                                            preShow.pro_nClientNum = m_listShow[k].pro_nClientNum;
                                            preShow.pro_strDeviceID = strDevID;
                                            preShow.pro_strIP = string.Format("{0}:{1}",
                                                ((NetworkPointInfo)m_listCommStation[i].pro_listMainPointInfo[j].pro_StationPoint).pro_LocalIP,
                                                ((NetworkPointInfo)m_listCommStation[i].pro_listMainPointInfo[j].pro_StationPoint).pro_LocalPort);
                                            StatusEventHandler(preShow);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
                                }
                                #endregion

                                m_listCommStation[i].pro_listMainPointInfo.RemoveAt(j);
                                string strErrorLog = string.Format(Properties.Resources.strStationDisposeException, strDevID);
                                FileOperator.ExceptionLog(strErrorLog);
                                break;
                            }
                        }
                    }
                    StationPointInfo SPInfo = new StationPointInfo();
                    SPInfo.pro_isSrc = isSrcPoint;
                    SPInfo.pro_strDeviceID = strDevID;
                    SPInfo.pro_StationPoint = serverPointInfo;
                    m_listCommStation[i].pro_listMainPointInfo.Add(SPInfo);
                    break;
                }
                #endregion

                #region 副端口处理设备断电问题 add2014.6.17
                if (nPort == m_listCommStation[i].pro_nAssistPort &&
                    m_listCommStation[i].pro_AssistCommType == CommunicationType.TCPServer)
                {
                    int nAssistListNum = m_listCommStation[i].pro_listAssistPointInfo.Count;
                    if (0 < nAssistListNum && !isSrcPoint)
                    {
                        for (int j = 0; j < nAssistListNum; j++)
                        {
                            if (isSrcPoint == m_listCommStation[i].pro_listAssistPointInfo[j].pro_isSrc
                                && strDevID == m_listCommStation[i].pro_listAssistPointInfo[j].pro_strDeviceID)
                            {
                                #region 连接确认回调,在未分离设备号前返回，有助于区分解析模块和设备模块
                                if (null != StatusEventHandler)
                                {
                                    int nCount = m_listShow.Count;
                                    for (int k = 0; k < nCount; k++)
                                    {
                                        if (((NetworkPointInfo)serverPointInfo).pro_RemotePort == m_listShow[k].pro_nLocalPort
                                            && 0 < m_listShow[i].pro_nClientNum)
                                        {
                                            //断开
                                            m_listShow[i].pro_nClientNum -= 1;
                                            ShowInfo preShow = new ShowInfo();
                                            preShow.pro_nLocalPort = m_listShow[k].pro_nLocalPort;
                                            preShow.pro_nTotalRecvBytes = 0;
                                            preShow.pro_isConnected = false;
                                            preShow.pro_DisConnectedTime = DateTime.Now;
                                            preShow.pro_nClientNum = m_listShow[k].pro_nClientNum;
                                            preShow.pro_strDeviceID = strDevID;
                                            preShow.pro_strIP = string.Format("{0}:{1}",
                                                ((NetworkPointInfo)m_listCommStation[i].pro_listAssistPointInfo[j].pro_StationPoint).pro_LocalIP,
                                                ((NetworkPointInfo)m_listCommStation[i].pro_listAssistPointInfo[j].pro_StationPoint).pro_LocalPort);
                                            StatusEventHandler(preShow);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
                                }
                                #endregion

                                m_listCommStation[i].pro_listAssistPointInfo.RemoveAt(j);
                                string strErrorLog = string.Format(Properties.Resources.strStationDisposeException, strDevID);
                                FileOperator.ExceptionLog(strErrorLog);
                                break;
                            }
                        }
                    }
                    StationPointInfo SPInfo = new StationPointInfo();
                    SPInfo.pro_isSrc = isSrcPoint;
                    SPInfo.pro_strDeviceID = strDevID;
                    SPInfo.pro_StationPoint = serverPointInfo;
                    m_listCommStation[i].pro_listAssistPointInfo.Add(SPInfo);
                    break;
                }
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// TCP数据接收及转发
        /// </summary>
        /// <param name="sender">tcp客户信息</param>
        /// <param name="data">数据信息</param>
        private void TCPDataReceived(CommunicationPointInfoBase sender, CommunicationDataBase data)
        {
            if (0 >= m_listCommStation.Count)
            {
                FileOperator.ExceptionLog(Properties.Resources.strListCommStationEmpty);
                return;
            }

            #region 接收数据时返回给显示界面//add2014.5.26
            
            //change2014.6.3
            if (null != StatusEventHandler)
            {
                ShowInfo preShow = new ShowInfo();
                preShow.pro_nTotalRecvBytes = data.pro_DataSize;
                preShow.pro_nLocalPort = ((NetworkPointInfo)sender).pro_RemotePort;
                preShow.pro_strIP = string.Format("{0}:{1}",
                    ((NetworkPointInfo)sender).pro_LocalIP, ((NetworkPointInfo)sender).pro_LocalPort);
                StatusEventHandler(preShow);
            }
            else
            {
                FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
            }
            #endregion

            #region  将数据信息加入堆栈add2014.6.26
            DataInfo DataInfoUnit = new DataInfo();
            DataInfoUnit.DataMsg = data;
            DataInfoUnit.nLocalPort = ((NetworkPointInfo)sender).pro_LocalPort;
            DataInfoUnit.strLocalIP = ((NetworkPointInfo)sender).pro_LocalIP.ToString();
            DataInfoUnit.nRemotePort = ((NetworkPointInfo)sender).pro_RemotePort;
            DataInfoUnit.strRemoteIP = ((NetworkPointInfo)sender).pro_RemoteIP.ToString();
            m_listDataInfo.Add(DataInfoUnit);
            #endregion

        }

        /// <summary>
        /// UDP数据接收及转发
        /// </summary>
        /// <param name="sender">udp客户信息</param>
        /// <param name="data">数据信息</param>
        private void UDPDataReceived(CommunicationPointInfoBase sender, CommunicationDataBase data)
        {
            if (0 >= m_listCommStation.Count)
            {
                FileOperator.ExceptionLog(Properties.Resources.strListCommStationEmpty);
                return;
            }

            int nLocalPort = ((NetworkPointInfo)sender).pro_LocalPort;
            string strLocalIP = ((NetworkPointInfo)sender).pro_LocalIP.ToString();
            int nRemotePort = ((NetworkPointInfo)sender).pro_RemotePort;
            string strRemoteIP = ((NetworkPointInfo)sender).pro_RemoteIP.ToString();

            UpdateActiveTime(nLocalPort, strLocalIP, nRemotePort, strRemoteIP);

            int nListNum = m_listCommStation.Count;
            string strMsg = System.Text.Encoding.Default.GetString(data.pro_Data);
            bool isProtocal = false;

            if (m_nUDPHeadLength-1 <= strMsg.Length)
            {
                string strHead = strMsg.Substring(0, 2);

                if (m_strUDPHead == strHead)
                {
                    #region 可能带协议，还需再判断
                    string strType = strMsg.Substring(2, 2).TrimEnd('\0');

                    if (m_strUDPDstLogin == strType)
                    {
                        m_LogFile.WriteLogFile(nRemotePort.ToString() + ".txt", 
                            Properties.Resources.strUDPDstConnected, strLocalIP, nLocalPort);

                        //当该信息为数据接收端登陆信息时，给该客户连接的端口添加远程点
                        string strDevID = strMsg.Substring(4, 10).TrimEnd('\0');
                        AddUDPLinker(false, strDevID, sender);

                        #region udp客户发送登陆命令时返回连接上信息给显示界面//add2014.5.26
                        if (null != StatusEventHandler)
                        {
                            if (0 >= m_listShow.Count)
                            {
                                FileOperator.ExceptionLog(Properties.Resources.strListShowEmpty);
                                return;
                            }

                            int nCount = m_listShow.Count;
                            for (int i = 0; i < nCount; i++)
                            {
                                if (((NetworkPointInfo)sender).pro_RemotePort == m_listShow[i].pro_nLocalPort)
                                {
                                    //change2014.5.27应对同时启动多个客户端的情况
                                    m_listShow[i].pro_nClientNum += 1;

                                    ShowInfo preShow = new ShowInfo();
                                    preShow.pro_isConnected = true;
                                    preShow.pro_isDataSource = false;
                                    preShow.pro_Protocal = m_listShow[i].pro_Protocal;
                                    preShow.pro_strDeviceID = strDevID;
                                    preShow.pro_nClientNum = m_listShow[i].pro_nClientNum;
                                    preShow.pro_ConnectedTime = DateTime.Now;
                                    preShow.pro_nLocalPort = nRemotePort;
                                    preShow.pro_strIP = string.Format("{0}:{1}", strLocalIP, nLocalPort);
                                    StatusEventHandler(preShow);

                                    //记录通讯日志
                                    if (null != m_DBOperate)
                                    {
                                        m_DBOperate.UpdateCommunicationLog(preShow);
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
                        }
                        #endregion

                        return;
                    }
                    else if (m_strUDPSrcLogin == strType)
                    {
                        m_LogFile.WriteLogFile(nRemotePort.ToString() + ".txt",
                            Properties.Resources.strUDPSrcConnected, strLocalIP, nLocalPort);

                        //当该信息为设备登陆信息时，给该客户连接的端口添加远程点
                        string strDevID = strMsg.Substring(4, 10).TrimEnd('\0');
                        AddUDPLinker(true, strDevID, sender);
                        m_listUDPSrcPoint.Add(sender);      //加入发送心跳列表

                        #region udp客户发送登陆命令时返回连接上信息给显示界面//add2014.5.26
                        if (null != StatusEventHandler)
                        {
                            if (0 >= m_listShow.Count)
                            {
                                return;
                            }

                            int nCount = m_listShow.Count;
                            for (int i = 0; i < nCount; i++)
                            {
                                if (((NetworkPointInfo)sender).pro_RemotePort == m_listShow[i].pro_nLocalPort)
                                {
                                    //change2014.5.27应对同时启动多个客户端的情况
                                    m_listShow[i].pro_nClientNum += 1;

                                    ShowInfo preShow = new ShowInfo();
                                    preShow.pro_isConnected = true;
                                    preShow.pro_isDataSource = true;
                                    preShow.pro_Protocal = m_listShow[i].pro_Protocal;
                                    preShow.pro_strDeviceID = strDevID;
                                    preShow.pro_nClientNum = m_listShow[i].pro_nClientNum;
                                    preShow.pro_ConnectedTime = DateTime.Now;
                                    preShow.pro_nLocalPort = nRemotePort;
                                    preShow.pro_strIP = string.Format("{0}:{1}", strLocalIP, nLocalPort);
                                    StatusEventHandler(preShow);

                                    //记录通讯日志
                                    if (null != m_DBOperate)
                                    {
                                        m_DBOperate.UpdateCommunicationLog(preShow);
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
                        }
                        #endregion

                        return;
                    }
                    else if (m_strUDPSrcHeart == strType)
                    {
                        return;
                    }
                    else if (m_strUDPDstHeart == strType)
                    {
                        #region //对udp心跳的应答处理
                        for (int i = 0; i < nListNum; i++)
                        {
                            //当该客户连接的端口为主端口时，对带协议与否分类处理
                            if ((m_listCommStation[i].pro_MainCommType == CommunicationType.UDPServer ||
                                m_listCommStation[i].pro_MainCommType == CommunicationType.UDPClient) &&
                                nRemotePort == m_listCommStation[i].pro_nMainPort)
                            {
                                m_listCommStation[i].pro_MainCommStation.Send(m_AnswerData, sender);
                                break;
                            }
                            else if ((m_listCommStation[i].pro_AssistCommType == CommunicationType.UDPServer ||
                                m_listCommStation[i].pro_AssistCommType == CommunicationType.UDPClient) &&
                                nRemotePort == m_listCommStation[i].pro_nAssistPort)
                            {
                                m_listCommStation[i].pro_AssistCommStation.Send(m_AnswerData, sender);
                                break;
                            }
                        }

                        //之前因为少了return，导致udp变成走无协议的，结束时一直不正确！！要注意！！
                        return;
                        #endregion
                    }
                    else if (m_strUDPData == strType)
                    {
                        //add2014.6.4去掉udp协议头
                        isProtocal = true;
                        byte[] byteData = new byte[data.pro_DataSize - m_nUDPHeadLength];
                        Array.ConstrainedCopy(data.pro_Data, m_nUDPHeadLength, byteData, 0, byteData.Length);
                        data.pro_Data = byteData;
                        data.pro_DataSize = byteData.Length;

                        #region  将数据信息加入堆栈add2014.6.26
                        DataInfo DataInfoProtocal = new DataInfo();
                        DataInfoProtocal.DataMsg = data;
                        DataInfoProtocal.nLocalPort = nLocalPort;
                        DataInfoProtocal.strLocalIP = strLocalIP;
                        DataInfoProtocal.nRemotePort = nRemotePort;
                        DataInfoProtocal.strRemoteIP = strRemoteIP;
                        m_listDataInfo.Add(DataInfoProtocal);
                        #endregion

                        #region 接收数据时返回给显示界面//add2014.5.26
                        //change2014.6.3
                        if (null != StatusEventHandler)
                        {
                            ShowInfo preShow = new ShowInfo();
                            preShow.pro_nTotalRecvBytes = data.pro_DataSize;
                            preShow.pro_nLocalPort = ((NetworkPointInfo)sender).pro_RemotePort;
                            preShow.pro_strIP = string.Format("{0}:{1}",
                                ((NetworkPointInfo)sender).pro_LocalIP, ((NetworkPointInfo)sender).pro_LocalPort);
                            StatusEventHandler(preShow);
                        }
                        else
                        {
                            FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
                        }
                        #endregion

                        return;
                    }

                    #endregion
                }
            }

            if (!isProtocal)
            {
                #region  将数据信息加入堆栈add2014.6.26
                DataInfo DataInfoNoneProtocal = new DataInfo();
                DataInfoNoneProtocal.DataMsg = data;
                DataInfoNoneProtocal.nLocalPort = nLocalPort;
                DataInfoNoneProtocal.strLocalIP = strLocalIP;
                DataInfoNoneProtocal.nRemotePort = nRemotePort;
                DataInfoNoneProtocal.strRemoteIP = strRemoteIP;
                m_listDataInfo.Add(DataInfoNoneProtocal);
                #endregion

                #region 接收数据时返回给显示界面//add2014.5.26
                //change2014.6.3
                if (null != StatusEventHandler)
                {
                    ShowInfo preShow = new ShowInfo();
                    preShow.pro_nTotalRecvBytes = data.pro_DataSize;
                    preShow.pro_nLocalPort = nRemotePort;
                    preShow.pro_strIP = string.Format("{0}:{1}", strLocalIP, nLocalPort);
                    StatusEventHandler(preShow);
                }
                #endregion
            }
            
        }

        /// <summary>
        /// 添加udp客户信息
        /// </summary>
        /// <param name="isSource">是否为数据源</param>
        /// <param name="strDeviceID">设备ID</param>
        /// <param name="sender">udp客户信息</param>
        private void AddUDPLinker(bool isSource, string strDeviceID, CommunicationPointInfoBase sender)
        {
            int nPort = ((NetworkPointInfo)sender).pro_RemotePort;
            int nListNum = m_listCommStation.Count;

            for (int i = 0; i < nListNum; i++)
            {
                if ((m_listCommStation[i].pro_MainCommType == CommunicationType.UDPServer ||
                    m_listCommStation[i].pro_MainCommType == CommunicationType.UDPClient) &&
                    nPort == m_listCommStation[i].pro_nMainPort)
                {
                    //如果连接已存在则不予处理，否则加入新连接
                    if (!isLinkerExist(m_listCommStation[i].pro_listMainPointInfo, sender))
                    {
                        StationPointInfo SPInfo = new StationPointInfo();
                        SPInfo.pro_isSrc = isSource;
                        SPInfo.pro_StationPoint = sender;
                        SPInfo.pro_strDeviceID = strDeviceID;
                        m_listCommStation[i].pro_listMainPointInfo.Add(SPInfo);
                    }
                    break;
                }
                else if ((m_listCommStation[i].pro_AssistCommType == CommunicationType.UDPServer ||
                    m_listCommStation[i].pro_AssistCommType == CommunicationType.UDPClient) &&
                    nPort == m_listCommStation[i].pro_nAssistPort)
                {
                    if (!isLinkerExist(m_listCommStation[i].pro_listAssistPointInfo, sender))
                    {
                        StationPointInfo SPInfo = new StationPointInfo();
                        SPInfo.pro_isSrc = isSource;
                        SPInfo.pro_StationPoint = sender;
                        SPInfo.pro_strDeviceID = strDeviceID;
                        m_listCommStation[i].pro_listAssistPointInfo.Add(SPInfo);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 检测udp连接是否已存在
        /// </summary>
        /// <param name="listSPInfo">已存在的客户信息</param>
        /// <param name="sender">当前客户信息</param>
        /// <returns></returns>
        private bool isLinkerExist(List<StationPointInfo> listSPInfo, CommunicationPointInfoBase sender)
        {
            if (0 >= listSPInfo.Count)
            {
                FileOperator.ExceptionLog(Properties.Resources.strListUDPLinkerEmpty);
                return false;
            }

            bool isExist = false;
            int nCount = listSPInfo.Count;

            for (int i = 0; i < nCount; i++)
            {
                if (((NetworkPointInfo)listSPInfo[i].pro_StationPoint).pro_LocalIP.ToString() == ((NetworkPointInfo)sender).pro_LocalIP.ToString()
                    && ((NetworkPointInfo)listSPInfo[i].pro_StationPoint).pro_LocalPort == ((NetworkPointInfo)sender).pro_LocalPort)
                {
                    isExist = true;
                }
            }

            return isExist;
        }

        /// <summary>
        /// 串口数据接收及转发
        /// </summary>
        /// <param name="sender">串口信息</param>
        /// <param name="data">数据信息</param>
        private void COMDataReceived(CommunicationPointInfoBase sender, CommunicationDataBase data)
        {
           // int nPort = ((NetworkPointInfo)sender).pro_RemotePort;
        }

        /// <summary>
        /// 不带协议端口转发数据
        /// </summary>
        /// <param name="isMainPort">是否为主端口</param>
        /// <param name="CommStation">服务器信息</param>
        /// <param name="DataInfoUnit">要发送数据信息</param>
        private void NoneProtocalSend(bool isMainPort, CommunicationBaseStation CommStation,
            DataInfo DataInfoUnit)
        {
            bool isPointExist = false;
            bool isNoneProtocalUDP;     //用于判断是否为无协议udp传输

            if (isMainPort)
            {
                #region //主端口客户发来的数据，若不带协议，则除自身以外对所有连接主副端口的客户直接群发
                //add2014.6.4针对无协议udp断开的处理
                isNoneProtocalUDP = false;
                if (ProtocalType.None == CommStation.pro_MainProtocal &&
                    (CommStation.pro_MainCommType == CommunicationType.UDPClient ||
                    CommStation.pro_MainCommType == CommunicationType.UDPServer))
                {
                    isNoneProtocalUDP = true;
                }

                int nMainListNum = CommStation.pro_listMainPointInfo.Count;
                for (int j = 0; j < nMainListNum; j++)
                {
                    if (((NetworkPointInfo)CommStation.pro_listMainPointInfo[j].pro_StationPoint).pro_LocalIP.ToString() == DataInfoUnit.strLocalIP
                        && ((NetworkPointInfo)CommStation.pro_listMainPointInfo[j].pro_StationPoint).pro_LocalPort == DataInfoUnit.nLocalPort)
                    {
                        isPointExist = true;
                        continue;
                    }
                    CommStation.pro_MainCommStation.Send(DataInfoUnit.DataMsg,
                        CommStation.pro_listMainPointInfo[j].pro_StationPoint);

                    if (isNoneProtocalUDP)
                    {
                        //若为无协议udp传输，则更新活动时间
                        UpdateActiveTime(((NetworkPointInfo)CommStation.pro_listMainPointInfo[j].pro_StationPoint).pro_LocalPort,
                            ((NetworkPointInfo)CommStation.pro_listMainPointInfo[j].pro_StationPoint).pro_LocalIP.ToString(),
                            ((NetworkPointInfo)CommStation.pro_listMainPointInfo[j].pro_StationPoint).pro_RemotePort,
                            ((NetworkPointInfo)CommStation.pro_listMainPointInfo[j].pro_StationPoint).pro_RemoteIP.ToString());
                    }
                }

                //只针对无协议副端口的连接客户群发
                if (ProtocalType.None == CommStation.pro_AssistProtocal &&
                    0 < CommStation.pro_nAssistPort)
                {
                    isNoneProtocalUDP = false;
                    if (CommStation.pro_AssistCommType == CommunicationType.UDPClient ||
                        CommStation.pro_AssistCommType == CommunicationType.UDPServer)
                    {
                        isNoneProtocalUDP = true;
                    }

                    int nAssistListNum = CommStation.pro_listAssistPointInfo.Count;
                    for (int k = 0; k < nAssistListNum; k++)
                    {
                        CommStation.pro_AssistCommStation.Send(DataInfoUnit.DataMsg,
                            CommStation.pro_listAssistPointInfo[k].pro_StationPoint);

                        if (isNoneProtocalUDP)
                        {
                            //若为无协议udp传输，则更新活动时间
                            UpdateActiveTime(((NetworkPointInfo)CommStation.pro_listAssistPointInfo[k].pro_StationPoint).pro_LocalPort,
                                ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[k].pro_StationPoint).pro_LocalIP.ToString(),
                                ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[k].pro_StationPoint).pro_RemotePort,
                                ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[k].pro_StationPoint).pro_RemoteIP.ToString());
                        }
                    }
                }
                #endregion
            }
            else
            {
                #region//接收到的是副端口发来的数据，若不带协议，则除自身以外对所有连接主副端口的客户直接群发
                isNoneProtocalUDP = false;
                if (ProtocalType.None == CommStation.pro_AssistProtocal &&
                    (CommStation.pro_AssistCommType == CommunicationType.UDPClient ||
                    CommStation.pro_AssistCommType == CommunicationType.UDPServer))
                {
                    isNoneProtocalUDP = true;
                }

                int nAssistListNum = CommStation.pro_listAssistPointInfo.Count;
                for (int j = 0; j < nAssistListNum; j++)
                {
                    if (((NetworkPointInfo)CommStation.pro_listAssistPointInfo[j].pro_StationPoint).pro_LocalIP.ToString() == DataInfoUnit.strLocalIP
                        && ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[j].pro_StationPoint).pro_LocalPort == DataInfoUnit.nLocalPort)
                    {
                        isPointExist = true;
                        continue;
                    }
                    CommStation.pro_AssistCommStation.Send(DataInfoUnit.DataMsg,
                        CommStation.pro_listAssistPointInfo[j].pro_StationPoint);

                    if (isNoneProtocalUDP)
                    {
                        //若为无协议udp传输，则更新活动时间
                        UpdateActiveTime(((NetworkPointInfo)CommStation.pro_listAssistPointInfo[j].pro_StationPoint).pro_LocalPort,
                            ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[j].pro_StationPoint).pro_LocalIP.ToString(),
                            ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[j].pro_StationPoint).pro_RemotePort,
                            ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[j].pro_StationPoint).pro_RemoteIP.ToString());
                    }
                }

                //只针对无协议主端口的连接客户群发
                if (ProtocalType.None == CommStation.pro_MainProtocal &&
                    0 < CommStation.pro_nMainPort)
                {
                    isNoneProtocalUDP = false;
                    if (CommStation.pro_MainCommType == CommunicationType.UDPClient ||
                        CommStation.pro_MainCommType == CommunicationType.UDPServer)
                    {
                        isNoneProtocalUDP = true;
                    }

                    int nMainListNum = CommStation.pro_listMainPointInfo.Count;
                    for (int k = 0; k < nMainListNum; k++)
                    {
                        CommStation.pro_MainCommStation.Send(DataInfoUnit.DataMsg,
                            CommStation.pro_listMainPointInfo[k].pro_StationPoint);

                        if (isNoneProtocalUDP)
                        {
                            //若为无协议udp传输，则更新活动时间
                            UpdateActiveTime(((NetworkPointInfo)CommStation.pro_listMainPointInfo[k].pro_StationPoint).pro_LocalPort,
                                ((NetworkPointInfo)CommStation.pro_listMainPointInfo[k].pro_StationPoint).pro_LocalIP.ToString(),
                                ((NetworkPointInfo)CommStation.pro_listMainPointInfo[k].pro_StationPoint).pro_RemotePort,
                                ((NetworkPointInfo)CommStation.pro_listMainPointInfo[k].pro_StationPoint).pro_RemoteIP.ToString());
                        }
                    }
                }
                #endregion
            }

            if (!isPointExist)
            {
                //该点不存在，若为udp通讯则对其加点
                StationPointInfo SPInfo = new StationPointInfo();
                SPInfo.pro_StationPoint = new NetworkPointInfo();
                ((NetworkPointInfo)SPInfo.pro_StationPoint).pro_RemotePort = DataInfoUnit.nRemotePort;
                ((NetworkPointInfo)SPInfo.pro_StationPoint).pro_LocalIP = IPAddress.Parse(DataInfoUnit.strLocalIP);
                ((NetworkPointInfo)SPInfo.pro_StationPoint).pro_LocalPort = DataInfoUnit.nLocalPort;
                ((NetworkPointInfo)SPInfo.pro_StationPoint).pro_RemoteIP = IPAddress.Parse(DataInfoUnit.strRemoteIP);

                if (isMainPort && (CommStation.pro_MainCommType == CommunicationType.UDPClient
                    || CommStation.pro_MainCommType == CommunicationType.UDPServer))
                {
                    CommStation.pro_listMainPointInfo.Add(SPInfo);
                }
                else if(!isMainPort && (CommStation.pro_AssistCommType == CommunicationType.UDPClient
                    || CommStation.pro_AssistCommType == CommunicationType.UDPServer))
                {
                    CommStation.pro_listAssistPointInfo.Add(SPInfo);
                }

                #region udp客户发送登陆命令时返回连接上信息给显示界面//add2014.5.26
                if (null != StatusEventHandler)
                {
                    if (0 >= m_listShow.Count)
                    {
                        FileOperator.ExceptionLog(Properties.Resources.strListShowEmpty);
                        return;
                    }

                    int nCount = m_listShow.Count;
                    for (int i = 0; i < nCount; i++)
                    {
                        if (DataInfoUnit.nRemotePort == m_listShow[i].pro_nLocalPort)
                        {
                            m_LogFile.WriteLogFile(DataInfoUnit.nRemotePort.ToString() + ".txt", 
                                Properties.Resources.strConnected,
                                DataInfoUnit.strLocalIP, DataInfoUnit.nLocalPort);

                            //change2014.5.27应对同时启动多个客户端的情况
                            m_listShow[i].pro_nClientNum += 1;

                            ShowInfo preShow = new ShowInfo();
                            preShow.pro_isConnected = true;
                            preShow.pro_Protocal = m_listShow[i].pro_Protocal;
                            preShow.pro_nClientNum = m_listShow[i].pro_nClientNum;
                            preShow.pro_ConnectedTime = DateTime.Now;
                            preShow.pro_nLocalPort = DataInfoUnit.nRemotePort;
                            preShow.pro_strIP = string.Format("{0}:{1}",
                                DataInfoUnit.strLocalIP, DataInfoUnit.nLocalPort);
                            StatusEventHandler(preShow);
                            break;
                        }
                    }
                }
                else
                {
                    FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
                }
                #endregion
            }
        }

        /// <summary>
        /// 带协议转发数据
        /// </summary>
        /// <param name="isMainPort">是否为主端口</param>
        /// <param name="CommStation">服务器信息</param>
        /// <param name="DataInfoUnit">要发送数据信息</param>
        private void ProtocalSend(bool isMainPort, CommunicationBaseStation CommStation,
            DataInfo DataInfoUnit)
        {
            bool isNoneProtocalUDP;

            if (isMainPort)
            {
                #region//收到主端口客户发的数据，带协议，则先找出该客户负责的设备号及该设备属于源或目的方
                bool isSource = false;
                string strDevID = "";
                int nMainListNum = CommStation.pro_listMainPointInfo.Count;
     
                for (int j = 0; j < nMainListNum; j++)
                {
                    if (((NetworkPointInfo)CommStation.pro_listMainPointInfo[j].pro_StationPoint).pro_LocalIP.ToString() == DataInfoUnit.strLocalIP
                        && ((NetworkPointInfo)CommStation.pro_listMainPointInfo[j].pro_StationPoint).pro_LocalPort == DataInfoUnit.nLocalPort)
                    {
                        isSource = CommStation.pro_listMainPointInfo[j].pro_isSrc;
                        strDevID = CommStation.pro_listMainPointInfo[j].pro_strDeviceID;
                        break;
                    }
                }

                if ("" == strDevID)
                {
                    FileOperator.ExceptionLog(Properties.Resources.strDeviceIDError);
                    return;
                }

                #region 给主端口连接的客户发数据，根据设备号找出要发送的远程点发送
                for (int k = 0; k < nMainListNum; k++)
                {
                    if (CommStation.pro_listMainPointInfo[k].pro_isSrc != isSource &&
                        CommStation.pro_listMainPointInfo[k].pro_strDeviceID == strDevID)
                    {
                        CommStation.pro_MainCommStation.Send(DataInfoUnit.DataMsg,
                            CommStation.pro_listMainPointInfo[k].pro_StationPoint);
                    }
                }
                #endregion

                #region 给副端口连接的客户发数据,若无协议则群发，有则精确发送
                if (ProtocalType.None == CommStation.pro_AssistProtocal &&
                    0 < CommStation.pro_nAssistPort)
                {
                    isNoneProtocalUDP = false;
                    if (CommStation.pro_AssistCommType == CommunicationType.UDPClient ||
                        CommStation.pro_AssistCommType == CommunicationType.UDPServer)
                    {
                        isNoneProtocalUDP = true;
                    }

                    int nAssistListNum = CommStation.pro_listAssistPointInfo.Count;
                    for (int k = 0; k < nAssistListNum; k++)
                    {
                        CommStation.pro_AssistCommStation.Send(DataInfoUnit.DataMsg,
                            CommStation.pro_listAssistPointInfo[k].pro_StationPoint);

                        if (isNoneProtocalUDP)
                        {
                            //若为无协议udp传输，则更新活动时间
                            UpdateActiveTime(((NetworkPointInfo)CommStation.pro_listAssistPointInfo[k].pro_StationPoint).pro_LocalPort,
                                ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[k].pro_StationPoint).pro_LocalIP.ToString(),
                                ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[k].pro_StationPoint).pro_RemotePort,
                                ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[k].pro_StationPoint).pro_RemoteIP.ToString());
                        }
                    }
                }
                else if (ProtocalType.None != CommStation.pro_AssistProtocal &&
                    0 < CommStation.pro_nAssistPort)
                {
                    int nAssistListNum = CommStation.pro_listAssistPointInfo.Count;
                    for (int k = 0; k < nAssistListNum; k++)
                    {
                        if (CommStation.pro_listAssistPointInfo[k].pro_strDeviceID == strDevID)
                        {
                            CommStation.pro_AssistCommStation.Send(DataInfoUnit.DataMsg,
                                CommStation.pro_listAssistPointInfo[k].pro_StationPoint);
                        }
                    }
                }
                #endregion
                #endregion
            }
            else
            {
                #region  收到副端口客户发来的数据,找到副端口客户的设备号
                bool isSource = false;
                string strDevID = "";
                int nAssistListNum = CommStation.pro_listAssistPointInfo.Count;

                for (int j = 0; j < nAssistListNum; j++)
                {
                    if (((NetworkPointInfo)CommStation.pro_listAssistPointInfo[j].pro_StationPoint).pro_LocalIP.ToString() == DataInfoUnit.strLocalIP
                        && ((NetworkPointInfo)CommStation.pro_listAssistPointInfo[j].pro_StationPoint).pro_LocalPort == DataInfoUnit.nLocalPort)
                    {
                        isSource = CommStation.pro_listAssistPointInfo[j].pro_isSrc;
                        strDevID = CommStation.pro_listAssistPointInfo[j].pro_strDeviceID;
                        break;
                    }
                }

                if ("" == strDevID)
                {
                    return;
                }

                #region//给副端口连接的客户发数据，根据设备号找出要发送的远程点发送
                for (int k = 0; k < nAssistListNum; k++)
                {
                    if (CommStation.pro_listAssistPointInfo[k].pro_isSrc != isSource &&
                        CommStation.pro_listAssistPointInfo[k].pro_strDeviceID == strDevID)
                    {
                        CommStation.pro_AssistCommStation.Send(DataInfoUnit.DataMsg,
                            CommStation.pro_listAssistPointInfo[k].pro_StationPoint);
                    }
                }
                #endregion

                //若主端口无协议，对无协议主端口的连接客户群发
                if (ProtocalType.None == CommStation.pro_MainProtocal &&
                    0 < CommStation.pro_nMainPort)
                {
                    isNoneProtocalUDP = false;
                    if (CommStation.pro_MainCommType == CommunicationType.UDPClient ||
                        CommStation.pro_MainCommType == CommunicationType.UDPServer)
                    {
                        isNoneProtocalUDP = true;
                    }

                    int nMainListNum = CommStation.pro_listMainPointInfo.Count;
                    for (int k = 0; k < nMainListNum; k++)
                    {
                        CommStation.pro_MainCommStation.Send(DataInfoUnit.DataMsg,
                            CommStation.pro_listMainPointInfo[k].pro_StationPoint);

                        if (isNoneProtocalUDP)
                        {
                            //若为无协议udp传输，则更新活动时间
                            UpdateActiveTime(((NetworkPointInfo)CommStation.pro_listMainPointInfo[k].pro_StationPoint).pro_LocalPort,
                                ((NetworkPointInfo)CommStation.pro_listMainPointInfo[k].pro_StationPoint).pro_LocalIP.ToString(),
                                ((NetworkPointInfo)CommStation.pro_listMainPointInfo[k].pro_StationPoint).pro_RemotePort,
                                ((NetworkPointInfo)CommStation.pro_listMainPointInfo[k].pro_StationPoint).pro_RemoteIP.ToString());
                        }
                    }
                }
                else if (ProtocalType.None != CommStation.pro_MainProtocal &&
                    0 < CommStation.pro_nMainPort)
                {
                    //主端口带协议，根据设备号精确转发
                    int nMainListNum = CommStation.pro_listMainPointInfo.Count;
                    for (int k = 0; k < nMainListNum; k++)
                    {
                        if (strDevID == CommStation.pro_listMainPointInfo[k].pro_strDeviceID)
                        {
                            CommStation.pro_MainCommStation.Send(DataInfoUnit.DataMsg,
                                CommStation.pro_listMainPointInfo[k].pro_StationPoint);
                        }
                    }
                }
                #endregion
            }
        }

        /// <summary>
        /// 更新udp活动时间，用于管理无效用户
        /// </summary>
        /// <param name="nLocalPort"></param>
        /// <param name="strLocalIP"></param>
        /// <param name="nRemotePort"></param>
        /// <param name="strRemoteIP"></param>
        private void UpdateActiveTime(int nLocalPort, string strLocalIP, int nRemotePort, string strRemoteIP)
        {
            #region 更新客户端的接收时间
            bool isClientExist = false;
            int nCountList = m_listTimerInfo.Count;
            for (int i = 0; i < nCountList; i++)
            {
                if (m_listTimerInfo[i].pro_nLocalPort == nLocalPort &&
                    m_listTimerInfo[i].pro_strLocalIP == strLocalIP &&
                    m_listTimerInfo[i].pro_nRemotePort == nRemotePort &&
                    m_listTimerInfo[i].pro_strRemoteIP == strRemoteIP)
                {
                    isClientExist = true;
                    m_listTimerInfo[i].pro_RecvTime = DateTime.Now;
                    break;
                }
            }

            if (!isClientExist)
            {
                TimerDisposeInfo ClientInfo = new TimerDisposeInfo();
                ClientInfo.pro_nLocalPort = nLocalPort;
                ClientInfo.pro_nRemotePort = nRemotePort;
                ClientInfo.pro_strLocalIP = strLocalIP;
                ClientInfo.pro_strRemoteIP = strRemoteIP;
                ClientInfo.pro_RecvTime = DateTime.Now;
                m_listTimerInfo.Add(ClientInfo);
            }

            #endregion
        }

        /// <summary>
        /// 查找客户是否存在，若存在则删除
        /// </summary>
        /// <param name="nLocalPort"></param>
        /// <param name="strLocalIP"></param>
        /// <param name="nRemotePort"></param>
        /// <param name="strRemoteIP"></param>
        /// <returns></returns>
        private bool DisposeUDPClient(int nLocalPort, string strLocalIP, int nRemotePort, string strRemoteIP)
        {
            bool isRemove = false;
            int nListNum = m_listCommStation.Count;

            for (int i = 0; i < nListNum; i++)
            {
                if ((m_listCommStation[i].pro_MainCommType == CommunicationType.UDPServer ||
                    m_listCommStation[i].pro_MainCommType == CommunicationType.UDPClient) &&
                    nRemotePort == m_listCommStation[i].pro_nMainPort)
                {
                    isRemove = RemoveClientInfo(m_listCommStation[i].pro_listMainPointInfo, nLocalPort, strLocalIP);
                    break;
                }
                else if ((m_listCommStation[i].pro_AssistCommType == CommunicationType.UDPServer ||
                    m_listCommStation[i].pro_AssistCommType == CommunicationType.UDPClient) &&
                    nRemotePort == m_listCommStation[i].pro_nAssistPort)
                {
                    isRemove = RemoveClientInfo(m_listCommStation[i].pro_listAssistPointInfo, nLocalPort, strLocalIP);
                    break;
                }
            }

            return isRemove;
        }

        /// <summary>
        /// 删除udp客户的信息
        /// </summary>
        /// <param name="listSPInfo">已有客户列表</param>
        /// <param name="nLocalPort">客户端口</param>
        /// <param name="strLocalIP">客户IP</param>
        /// <returns></returns>
        private bool RemoveClientInfo(List<StationPointInfo> listSPInfo, int nLocalPort, string strLocalIP)
        {
            bool isRemove = false;
            int nListNum = listSPInfo.Count;

            for (int i = 0; i < nListNum; i++)
            {
                if (((NetworkPointInfo)listSPInfo[i].pro_StationPoint).pro_LocalIP.ToString() == strLocalIP
                    && ((NetworkPointInfo)listSPInfo[i].pro_StationPoint).pro_LocalPort == nLocalPort)
                {
                    isRemove = true;
                    listSPInfo.RemoveAt(i);
                    break;
                }
            }

            return isRemove;
        }

        /// <summary>
        /// 定时发送udp带协议心跳包
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void m_UDPHeartTimer_Elapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            #region 发送心跳
            lock (m_listUDPSrcPoint)
             {
                 int nCount = m_listUDPSrcPoint.Count;
                 if (0 < nCount)
                 {

                     for (int i = 0; i < nCount; i++)
                     {
                         int nPort = ((NetworkPointInfo)m_listUDPSrcPoint[i]).pro_RemotePort;

                         int nListNum = m_listCommStation.Count;
                         for (int j = 0; j < nListNum; j++)
                         {
                             if ((m_listCommStation[j].pro_MainCommType == CommunicationType.UDPServer ||
                                 m_listCommStation[j].pro_MainCommType == CommunicationType.UDPClient) &&
                                 nPort == m_listCommStation[j].pro_nMainPort)
                             {
                                 m_listCommStation[j].pro_MainCommStation.Send(m_AnswerData, m_listUDPSrcPoint[i]);
                             }
                             else if ((m_listCommStation[j].pro_AssistCommType == CommunicationType.UDPServer ||
                                      m_listCommStation[j].pro_AssistCommType == CommunicationType.UDPClient) &&
                                      nPort == m_listCommStation[j].pro_nAssistPort)
                             {
                                 m_listCommStation[j].pro_AssistCommStation.Send(m_AnswerData, m_listUDPSrcPoint[i]);
                             }
                         }
                     }
                 }
             }
            #endregion

            #region 停止服务
            int nCountList = m_listTimerInfo.Count;
            if (0 >= nCountList)
            {
                return;
            }

            lock (m_listUDPSrcPoint)
            {
                for (int i = 0; i < nCountList; i++)
                {
                    TimeSpan TimeInterval = DateTime.Now - m_listTimerInfo[i].pro_RecvTime;
                    if (TimeInterval.TotalSeconds > m_UDPStopInterval)
                    {
                        bool isRemove = DisposeUDPClient(m_listTimerInfo[i].pro_nLocalPort,
                            m_listTimerInfo[i].pro_strLocalIP,
                            m_listTimerInfo[i].pro_nRemotePort,
                            m_listTimerInfo[i].pro_strRemoteIP);

                        if (isRemove)
                        {
                            m_LogFile.WriteLogFile(m_listTimerInfo[i].pro_nRemotePort.ToString() + ".txt",
                                "\t" + Properties.Resources.strDisconnected, m_listTimerInfo[i].pro_strLocalIP, m_listTimerInfo[i].pro_nLocalPort);

                            #region udp断开确认回调
                            if (null != StatusEventHandler && 0 < m_listShow.Count)
                            {
                                int nShowNum = m_listShow.Count;
                                for (int j = 0; j < nShowNum; j++)
                                {
                                    if (m_listTimerInfo[i].pro_nRemotePort == m_listShow[j].pro_nLocalPort
                                        && 0 < m_listShow[j].pro_nClientNum)
                                    {
                                        m_listShow[j].pro_nClientNum -= 1;
                                        ShowInfo preShow = new ShowInfo();
                                        preShow.pro_nLocalPort = m_listShow[j].pro_nLocalPort;
                                        preShow.pro_strDeviceID = m_listShow[j].pro_strDeviceID;
                                        preShow.pro_nTotalRecvBytes = 0;
                                        preShow.pro_isConnected = false;
                                        preShow.pro_DisConnectedTime = DateTime.Now;
                                        preShow.pro_nClientNum = m_listShow[j].pro_nClientNum;
                                        preShow.pro_strIP = string.Format("{0}:{1}",
                                            m_listTimerInfo[i].pro_strLocalIP,
                                            m_listTimerInfo[i].pro_nLocalPort);
                                        StatusEventHandler(preShow);

                                        //记录通讯日志，只记录带协议通讯
                                        if (null != m_DBOperate)
                                        {
                                            m_DBOperate.UpdateCommunicationLog(preShow);
                                        }
                                        break;
                                    }
                                }
                            }
                            #endregion

                            int nHeartPointCount = m_listUDPSrcPoint.Count;
                            for (int j = 0; j < nHeartPointCount; j++)
                            {
                                if (((NetworkPointInfo)m_listUDPSrcPoint[j]).pro_LocalIP.ToString() == m_listTimerInfo[i].pro_strLocalIP
                                    && ((NetworkPointInfo)m_listUDPSrcPoint[j]).pro_LocalPort == m_listTimerInfo[i].pro_nLocalPort)
                                {
                                    m_listUDPSrcPoint.RemoveAt(j);
                                    break;
                                }
                            }

                            m_listTimerInfo.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 启动单个服务器
        /// </summary>
        /// <param name="Station">服务器站点信息</param>
        private void StartOne(StationInfo Station)
        {
            if (!Station.pro_IsOnline && !Station.pro_isStop)
            {
                #region 若站点未启动及停止位为false，则建立通讯
                switch (Station.pro_CommunicationType)
                {
                    case CommunicationModule.Entity.CommunicationType.TCPServer:
                        StartTCPServer(Station);
                        break;
                    case CommunicationModule.Entity.CommunicationType.TCPClient:
                        #region 暂不处理
                        //NetworkPointInfo TCPClientPoint = new NetworkPointInfo();
                        //TCPClientPoint.pro_LocalIP = IPAddress.Parse(Station.pro_IP);
                        //TCPClientPoint.pro_LocalPort = Station.pro_Port;
                        //TCPClientClass TCPClientObj = new TCPClientClass(TCPClientPoint);
                        ////    TCPClientObj.evConnectServerSuccess += new dConnectServerSuccessEventHandler(Connected);
                        //TCPClientObj.evConnectServerFail += new dConnectServerFailEventHandler(Connected);
                        //TCPClientObj.evDataReceived += new dDataReceivedEventHandler(DataReceived);

                        //CommStation TCPClientStationObj = new CommStation();
                        //TCPClientStationObj.pro_strDeviceID = Station.pro_strDeviceID;
                        //TCPClientStationObj.pro_CommStation = TCPClientObj;
                        //TCPClientStationObj.pro_nwPointInfo = TCPClientPoint;
                        //m_listCommStation.Add(TCPClientStationObj);
                        //Station.pro_IsOnline = TCPClientObj.Start();
                        #endregion

                        break;
                    case CommunicationModule.Entity.CommunicationType.UDPServer:
                    case CommunicationModule.Entity.CommunicationType.UDPClient:
                        StartUDPServer(Station);
                        
                        //用于定时清除无效的udp客户信息add2014.6.3
                        m_listTimerInfo = new List<TimerDisposeInfo>();
                        if (null == m_UDPHeartTimer)
                        {
                            m_AnswerData = new CommunicationDataBase();
                            m_AnswerData.pro_Data = System.Text.Encoding.ASCII.GetBytes(m_strUDPAnswer);
                            m_listUDPSrcPoint = new List<CommunicationPointInfoBase>();
                            m_UDPHeartTimer = new System.Timers.Timer();
                            m_UDPHeartTimer.Elapsed += new ElapsedEventHandler(m_UDPHeartTimer_Elapsed);
                            m_UDPHeartTimer.Interval = m_UDPHeartInterval;
                            m_UDPHeartTimer.Enabled = true;
                            m_UDPHeartTimer.Start();
                        }
                        break;
                    case CommunicationModule.Entity.CommunicationType.SerialPort:
                        StartSerialPort(Station);
                        break;
                    default:
                        break;
                }

                #region 启动状态返回

                if (Station.pro_IsOnline)
                {
                    if (null != StatusEventHandler)
                    {
                        //add2014.5.26
                        ShowInfo Show = new ShowInfo();
                        Show.pro_nLocalPort = Station.pro_nPort;
                        Show.pro_CommType = Station.pro_CommunicationType;
                        if (ProtocalType.None == Station.pro_Protocal)
                        {
                            Show.pro_strRemark = Properties.Resources.strRemark;
                        }
                        else
                        {
                            Show.pro_Protocal = Station.pro_Protocal;
                        }
                        m_listShow.Add(Show);
                        StatusEventHandler(Show);
                    }
                    else
                    {
                        FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
                    }
                }
                #endregion

                #endregion
            }
        }

        /// <summary>
        /// 建立tcp服务器
        /// </summary>
        /// <param name="Station"></param>
        private void StartTCPServer(StationInfo Station)
        {
            #region 建立tcp通讯
            TCPServerClass TCPServerObj = null;
            if (Station.pro_Protocal == ProtocalType.None)
            {
                //不带协议的配置
                NetworkPointInfo TCPServerPoint = new NetworkPointInfo();
                TCPServerPoint.pro_LocalIP = IPAddress.Parse(Station.pro_strIP);
                TCPServerPoint.pro_LocalPort = Station.pro_nPort;
                TCPServerObj = new TCPServerClass(TCPServerPoint);

                TCPServerObj.evClientAccept += new dClientAcceptEventHandler(TCPServerObj_evClientAccept);
                TCPServerObj.evDataReceived += new dDataReceivedEventHandler(TCPDataReceived);
                TCPServerObj.evLinkDisconnect += new dLinkDisconnectEventHandler(TCPServerObj_evLinkDisconnect);
            }
            else if (Station.pro_Protocal == ProtocalType.Ntrip)
            {
                //带ntrip协议的配置
                TCPServerObj = new TCPServerClass();
                TCPServerObj.pro_CommunicationProtocol = new NtripCasterProtocol();
                TCPServerObj.pro_ServerIP = IPAddress.Parse(Station.pro_strIP);
                TCPServerObj.pro_Port = Station.pro_nPort;
                TCPServerObj.evClientAccept += new dClientAcceptEventHandler(TCPServerObj_evClientAccept);
                TCPServerObj.evLinkDisconnect += new dLinkDisconnectEventHandler(TCPServerObj_evLinkDisconnect);
                TCPServerObj.evClientLinkProtocolConfirm += new dProtocolConfirmEventHandler(TCPServerObj_evClientLinkProtocolConfirm);
                TCPServerObj.evDataReceived += new dDataReceivedEventHandler(TCPDataReceived);
             }

            Station.pro_IsOnline = TCPServerObj.Start();

            if (null == TCPServerObj || !Station.pro_IsOnline)
            {
                return;
            }

            bool isExist = false;
            
            if (0 < m_listCommStation.Count)
            {
                int nListNum = m_listCommStation.Count;
                for (int i = 0; i < nListNum; i++)
                {
                    if (0 >= Station.pro_nMainPort && 
                        m_listCommStation[i].pro_nMainPort == Station.pro_nPort && 
                        m_listCommStation[i].pro_MainCommType == Station.pro_CommunicationType)
                    {
                        //该端口为主端口，而且该端口的副端口已存在，则直接写入主端口信息
                        isExist = true;
                        m_listCommStation[i].pro_MainProtocal = Station.pro_Protocal;
                        m_listCommStation[i].pro_MainCommStation = TCPServerObj;
                        m_listCommStation[i].pro_listMainPointInfo = new List<StationPointInfo>();
                        break;
                    }
                    else if (0 < Station.pro_nMainPort &&
                        m_listCommStation[i].pro_nMainPort == Station.pro_nMainPort &&
                        m_listCommStation[i].pro_MainCommType == Station.pro_MainPortCommType)
                    {
                        //该端口为副端口，且主端口已存在，直接写入副端口信息
                        isExist = true;
                        m_listCommStation[i].pro_AssistCommType = Station.pro_CommunicationType;
                        m_listCommStation[i].pro_AssistProtocal = Station.pro_Protocal;
                        m_listCommStation[i].pro_nAssistPort = Station.pro_nPort;
                        m_listCommStation[i].pro_AssistCommStation = TCPServerObj;
                        m_listCommStation[i].pro_listAssistPointInfo = new List<StationPointInfo>();
                        break;
                    }
                }
            }

            if (!isExist)
            {
                //该端口在表中没有记录，则创建新项
                CommunicationBaseStation TCPServerStationObj = new CommunicationBaseStation();
                if (0 >= Station.pro_nMainPort)
                {
                    //主端口
                    TCPServerStationObj.pro_MainCommType = Station.pro_CommunicationType;
                    TCPServerStationObj.pro_MainProtocal = Station.pro_Protocal;
                    TCPServerStationObj.pro_nMainPort = Station.pro_nPort;
                    TCPServerStationObj.pro_MainCommStation = TCPServerObj;
                    TCPServerStationObj.pro_listMainPointInfo = new List<StationPointInfo>();
                }
                else
                {
                    //副端口
                    TCPServerStationObj.pro_nMainPort = Station.pro_nMainPort;
                    TCPServerStationObj.pro_MainCommType = Station.pro_MainPortCommType;
                    TCPServerStationObj.pro_AssistCommType = Station.pro_CommunicationType;
                    TCPServerStationObj.pro_AssistProtocal = Station.pro_Protocal;
                    TCPServerStationObj.pro_nAssistPort = Station.pro_nPort;
                    TCPServerStationObj.pro_AssistCommStation = TCPServerObj;
                    TCPServerStationObj.pro_listAssistPointInfo = new List<StationPointInfo>();
                }
                m_listCommStation.Add(TCPServerStationObj);
            }

            #endregion
        }

        /// <summary>
        /// 建立udp服务器
        /// </summary>
        /// <param name="Station"></param>
        private void StartUDPServer(StationInfo Station)
        {
            #region 建立udp通讯
            NetworkPointInfo UDPServerPoint = new NetworkPointInfo();
            UDPServerPoint.pro_LocalIP = IPAddress.Parse(Station.pro_strIP);
            UDPServerPoint.pro_LocalPort = Station.pro_nPort;
            UDPClass UDPObj = new UDPClass(UDPServerPoint);
            UDPObj.evDataReceived += new dDataReceivedEventHandler(UDPDataReceived);

            Station.pro_IsOnline = UDPObj.Start();
            if (!Station.pro_IsOnline)
            {
                return;
            }

            bool isExist = false;
            
            if (0 < m_listCommStation.Count)
            {
                int nListNum = m_listCommStation.Count;
                for (int i = 0; i < nListNum; i++)
                {
                    if (0 >= Station.pro_nMainPort && 
                        m_listCommStation[i].pro_nMainPort == Station.pro_nPort && 
                        m_listCommStation[i].pro_MainCommType == Station.pro_CommunicationType)
                    {
                        //该端口为主端口，而且该端口的副端口已存在，则直接写入主端口信息
                        isExist = true;
                        m_listCommStation[i].pro_MainProtocal = Station.pro_Protocal;
                        m_listCommStation[i].pro_MainCommStation = UDPObj;
                        m_listCommStation[i].pro_listMainPointInfo = new List<StationPointInfo>();
                        break;
                    }
                    else if (0 < Station.pro_nMainPort &&
                        m_listCommStation[i].pro_nMainPort == Station.pro_nMainPort &&
                        m_listCommStation[i].pro_MainCommType == Station.pro_MainPortCommType)
                    {
                        //该端口为副端口，且主端口已存在，直接写入副端口信息
                        isExist = true;
                        m_listCommStation[i].pro_AssistCommType = Station.pro_CommunicationType;
                        m_listCommStation[i].pro_AssistProtocal = Station.pro_Protocal;
                        m_listCommStation[i].pro_nAssistPort = Station.pro_nPort;
                        m_listCommStation[i].pro_AssistCommStation = UDPObj;
                        m_listCommStation[i].pro_listAssistPointInfo = new List<StationPointInfo>();
                        break;
                    }
                }
            }

            if (!isExist)
            {
                CommunicationBaseStation UDPServerStationObj = new CommunicationBaseStation();
                if (0 >= Station.pro_nMainPort)
                {
                    //主端口
                    UDPServerStationObj.pro_MainCommType = Station.pro_CommunicationType;
                    UDPServerStationObj.pro_MainProtocal = Station.pro_Protocal;
                    UDPServerStationObj.pro_nMainPort = Station.pro_nPort;
                    UDPServerStationObj.pro_MainCommStation = UDPObj;
                    UDPServerStationObj.pro_listMainPointInfo = new List<StationPointInfo>();
                }
                else
                {
                    //副端口
                    UDPServerStationObj.pro_MainCommType = Station.pro_MainPortCommType;
                    UDPServerStationObj.pro_nMainPort = Station.pro_nMainPort;
                    UDPServerStationObj.pro_AssistCommType = Station.pro_CommunicationType;
                    UDPServerStationObj.pro_AssistProtocal = Station.pro_Protocal;
                    UDPServerStationObj.pro_nAssistPort = Station.pro_nPort;
                    UDPServerStationObj.pro_AssistCommStation = UDPObj;
                    UDPServerStationObj.pro_listAssistPointInfo = new List<StationPointInfo>();
                }

                m_listCommStation.Add(UDPServerStationObj);
            }

            #endregion
        }

        /// <summary>
        /// 建立串口通讯
        /// </summary>
        /// <param name="Station"></param>
        private void StartSerialPort(StationInfo Station)
        {
            #region 建立串口通讯
            SerialPortService serialPort = new SerialPortService();
            serialPort.pro_SerialPortPoint.pro_ComBaudRate = Station.pro_nBaudRate;
            serialPort.pro_SerialPortPoint.pro_PortName = "COM" + Station.pro_nPort.ToString();
            serialPort.evDataReceived += new dDataReceivedEventHandler(COMDataReceived);

            Station.pro_IsOnline = serialPort.Start();
            if (!Station.pro_IsOnline)
            {
                FileOperator.ExceptionLog(Properties.Resources.strCOMStartError);
                return;
            }
            bool isExist = false;
            
            if (0 < m_listCommStation.Count)
            {
                int nListNum = m_listCommStation.Count;
                for (int i = 0; i < nListNum; i++)
                {
                    if (0 >= Station.pro_nMainPort && 
                        m_listCommStation[i].pro_nMainPort == Station.pro_nPort && 
                        m_listCommStation[i].pro_MainCommType == Station.pro_CommunicationType)
                    {
                        //该端口为主端口，而且该端口的副端口已存在，则直接写入主端口信息
                        isExist = true;
                        m_listCommStation[i].pro_MainProtocal = Station.pro_Protocal;
                        m_listCommStation[i].pro_MainCommStation = serialPort;
                        m_listCommStation[i].pro_listMainPointInfo = new List<StationPointInfo>();
                        break;
                    }
                    else if (0 < Station.pro_nMainPort &&
                        m_listCommStation[i].pro_nMainPort == Station.pro_nMainPort &&
                        m_listCommStation[i].pro_MainCommType == Station.pro_MainPortCommType)
                    {
                        //该端口为副端口，且主端口已存在，直接写入副端口信息
                        isExist = true;
                        m_listCommStation[i].pro_AssistCommType = Station.pro_CommunicationType;
                        m_listCommStation[i].pro_AssistProtocal = Station.pro_Protocal;
                        m_listCommStation[i].pro_nAssistPort = Station.pro_nPort;
                        m_listCommStation[i].pro_AssistCommStation = serialPort;
                        m_listCommStation[i].pro_listAssistPointInfo = new List<StationPointInfo>();
                        break;
                    }
                }
            }

            if (!isExist)
            {
                CommunicationBaseStation serialPortStationObj = new CommunicationBaseStation();
                if (0 >= Station.pro_nMainPort)
                {
                    //该端口没有主端口，说明其本身就是主端口
                    serialPortStationObj.pro_MainCommType = Station.pro_CommunicationType;
                    serialPortStationObj.pro_MainProtocal = Station.pro_Protocal;
                    serialPortStationObj.pro_nMainPort = Station.pro_nPort;
                    serialPortStationObj.pro_MainCommStation = serialPort;
                    serialPortStationObj.pro_listMainPointInfo = new List<StationPointInfo>();
                }
                else
                {
                    //该端口为副端口
                    serialPortStationObj.pro_MainCommType = Station.pro_MainPortCommType;
                    serialPortStationObj.pro_nMainPort = Station.pro_nMainPort;
                    serialPortStationObj.pro_AssistCommType = Station.pro_CommunicationType;
                    serialPortStationObj.pro_AssistProtocal = Station.pro_Protocal;
                    serialPortStationObj.pro_nAssistPort = Station.pro_nPort;
                    serialPortStationObj.pro_AssistCommStation = serialPort;
                    serialPortStationObj.pro_listAssistPointInfo = new List<StationPointInfo>();
                }

                m_listCommStation.Add(serialPortStationObj);
            }
           
            #endregion
        }

        /// <summary>
        /// add2014.6.26转发数据线程
        /// </summary>
        private void SendDataThread()
        {
            while (true)
            {
                if (0 >= m_listDataInfo.Count)
                {
                    Thread.Sleep(100);
                    continue;
                }

                //加锁，预防多线程同时操作表问题add2014.8.20
                lock (m_listDataInfo)
                {
                    int nListNum = m_listDataInfo.Count;
                    for (int k = 0; k < nListNum; k++)
                    {
                        int nStationListNum = m_listCommStation.Count;
                        for (int i = 0; i < nStationListNum; i++)
                        {
                            if (m_listDataInfo[k].nRemotePort == m_listCommStation[i].pro_nMainPort)
                            {
                                //当该客户连接的端口为主端口时，对带协议与否分类处理
                                if (ProtocalType.None == m_listCommStation[i].pro_MainProtocal)
                                {
                                    NoneProtocalSend(true, m_listCommStation[i], m_listDataInfo[k]);
                                }
                                else
                                {
                                    ProtocalSend(true, m_listCommStation[i], m_listDataInfo[k]);
                                }
                            }
                            else if (m_listDataInfo[k].nRemotePort == m_listCommStation[i].pro_nAssistPort)
                            {
                                //当该客户连接的端口为副端口时，对带协议与否分类处理
                                if (ProtocalType.None == m_listCommStation[i].pro_AssistProtocal)
                                {
                                    NoneProtocalSend(false, m_listCommStation[i], m_listDataInfo[k]);
                                }
                                else
                                {
                                    ProtocalSend(false, m_listCommStation[i], m_listDataInfo[k]);
                                }
                            }
                        }
                    }

                    m_listDataInfo.Clear();
                }
            }
        }
    }
}
