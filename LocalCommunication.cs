using System;
using System.Collections.Generic;

using System.Text;
using System.Net;
using System.Net.Sockets;
using ZHD.SYS.CommonUtility.CommunicationLib;
using CommunicationModule.Entity;

namespace CommunicationModule
{
    class LocalCommunication
    {
        //定义事件委托，接收数据
        public delegate void LinkStatusHandler(ShowInfo ReturnInfo);
        public event LinkStatusHandler StatusEventHandler;  //接收数据事件

        #region  非公开
        private DatabaseConfigInfo m_DBConfigInfo;                  //数据库及本地通讯等信息
        private TCPServerClass m_TCPServerObj = null;               //本地通讯类
        private CommunicationBase m_CommBase = null;                //通讯管理类对象
        private const int m_nHeaderLength = 20;                     //各模块间通信是要求传输的数据头长度
        private List<LocalCommunicationStation> m_listModuleStation;//记录各个模块的本地通讯信息
        private int m_nModuleNum = 0;                               //记录已连接模块个数
        private StationInfoDBOperate m_DBOperate;                       //读数据库得到配置信息
        private string m_strLocalIP = "";
        private int m_nLocalPort = -1;
        private bool m_isDatabaseOpen = false;
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        public LocalCommunication()
        {
            m_DBOperate = new StationInfoDBOperate();
            m_DBConfigInfo = new DatabaseConfigInfo();
            m_listModuleStation = new List<LocalCommunicationStation>();
            m_CommBase = new CommunicationBase();
            m_CommBase.StatusEventHandler += new CommunicationBase.LinkStatusHandler(LinkStatus);
        }

        /// <summary>
        /// 关闭所有通讯
        /// </summary>
        public void Close()
        {
            m_CommBase.StatusEventHandler -= new CommunicationBase.LinkStatusHandler(LinkStatus);
            m_CommBase.Close();
        }

        /// <summary>
        /// 为底层回调提供中转，将信息返回给界面层
        /// </summary>
        /// <param name="ReturnInfo">回调信息</param>
        private void LinkStatus(ShowInfo ReturnInfo)
        {
            if (null != StatusEventHandler)
            {
                StatusEventHandler(ReturnInfo);
            }
            else
            {
                FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
            }
        }

        /// <summary>
        /// 各模块连接请求
        /// </summary>
        /// <param name="serverPointInfo">客户信息</param>
        /// <param name="errorArgs">错误信息</param>
        private void m_TCPServerObj_evClientAccept(CommunicationPointInfoBase serverPointInfo, EventArgs errorArgs)
        {
            m_nModuleNum += 1;

            //客户端刚连接时将客户信息回调给上层
            if (null != StatusEventHandler)
            {
                //add2014.5.26  
                ShowInfo Show = new ShowInfo();
                Show.pro_isConnected = true;
                Show.pro_nClientNum = m_nModuleNum;
                Show.pro_ConnectedTime = DateTime.Now;
                Show.pro_nLocalPort = ((NetworkPointInfo)serverPointInfo).pro_RemotePort;
                Show.pro_strIP = string.Format("{0}:{1}",
                    ((NetworkPointInfo)serverPointInfo).pro_LocalIP, ((NetworkPointInfo)serverPointInfo).pro_LocalPort);
                StatusEventHandler(Show);
            }
            else
            {
                FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
            }
        }

        /// <summary>
        /// 各模块断开信息
        /// </summary>
        /// <param name="sender">客户信息</param>
        /// <param name="args">错误信息</param>
        private void m_TCPServerObj_evLinkDisconnect(CommunicationPointInfoBase sender, EventArgs args)
        {
            if (0 >= m_nModuleNum)
            {
                return;
            }
            m_nModuleNum -= 1;

            int nCount = m_listModuleStation.Count;
            for (int i = 0; i < nCount; i++)
            {
                //change2014.10.14必须细致到ip端口才能准确找到客户端，之前显示问题也许就是这里导致的
                if (((NetworkPointInfo)sender).pro_LocalIP.ToString() ==
                    ((NetworkPointInfo)m_listModuleStation[i].pro_nwPointInfo).pro_LocalIP.ToString()
                        && ((NetworkPointInfo)sender).pro_LocalPort ==
                    ((NetworkPointInfo)m_listModuleStation[i].pro_nwPointInfo).pro_LocalPort)
                {
                    m_listModuleStation.RemoveAt(i);
                    break;
                }
            }

            //客户端连接断开时通知上层修改状态
            if (null != StatusEventHandler)
            {
                //add2014.5.26
                ShowInfo Show = new ShowInfo();
                Show.pro_nTotalRecvBytes = 0;
                Show.pro_isConnected = false;
                Show.pro_nClientNum = m_nModuleNum;
                Show.pro_DisConnectedTime = DateTime.Now;
                Show.pro_nLocalPort = ((NetworkPointInfo)sender).pro_RemotePort;
                Show.pro_strIP = string.Format("{0}:{1}",
                    ((NetworkPointInfo)sender).pro_LocalIP, ((NetworkPointInfo)sender).pro_LocalPort);
                StatusEventHandler(Show);
            }
            else
            {
                FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
            }
        }

        /// <summary>
        /// 数据接收
        /// </summary>
        /// <param name="sender">客户信息</param>
        /// <param name="data">收到的数据信息</param>
        private void DataReceived(CommunicationPointInfoBase sender, CommunicationDataBase data)
        {
            //返回接收到的数据长度
            if (null != StatusEventHandler)
            {
                //add2014.5.26
                ShowInfo Show = new ShowInfo();
                Show.pro_nTotalRecvBytes = data.pro_DataSize;
                Show.pro_nLocalPort = ((NetworkPointInfo)sender).pro_RemotePort;
                Show.pro_strIP = string.Format("{0}:{1}",
                    ((NetworkPointInfo)sender).pro_LocalIP, ((NetworkPointInfo)sender).pro_LocalPort);
                StatusEventHandler(Show);
            }
            else
            {
                FileOperator.ExceptionLog(Properties.Resources.strStatusEventHandlerError);
            }
           
            string strMsg = System.Text.Encoding.Default.GetString(data.pro_Data).TrimEnd();

            //用$分离字符串strDevID，若为设备以外的模块，则会加上标识该模块的字符串
            char[] colonSplitChar = new char[] { '$' };
            string[] userDetailInfo = strMsg.Split(colonSplitChar, StringSplitOptions.RemoveEmptyEntries);

            if(2 != userDetailInfo.Length)
            {
                return ;
            }

            //当它为注册信息时，先查找当前列表是否有该用户，没有再添加
            if ("register" == userDetailInfo[0].ToLower())
            {
                bool isExist = false;
                int nCount = m_listModuleStation.Count;

                for (int i = 0; i < nCount; i++)
                {
                    if (userDetailInfo[1].ToLower() == m_listModuleStation[i].pro_strModuleType)
                    {
                        isExist = true;
                        m_listModuleStation[i].pro_nwPointInfo = sender;
                        break;
                    }
                }

                if (!isExist)
                {
                    LocalCommunicationStation ResultStation = new LocalCommunicationStation();
                    ResultStation.pro_nwPointInfo = sender;
                    ResultStation.pro_strModuleType = userDetailInfo[1].ToLower();
                    m_listModuleStation.Add(ResultStation);
                }

                CommunicationDataBase cmData = new CommunicationDataBase();
                cmData.pro_Data = System.Text.Encoding.ASCII.GetBytes(userDetailInfo[1].ToLower() + "$register");
                m_TCPServerObj.Send(cmData, sender);
            }
            else
            {
                if ("communication" == userDetailInfo[0].ToLower())
                {
                    m_CommBase.StopAll();
                    m_CommBase.StartAll(m_DBOperate, m_DBConfigInfo.nHeartIntervalSeconds, 
                        m_DBConfigInfo.nStopIntervalSeconds);

                    CommunicationDataBase cmData = new CommunicationDataBase();
                    cmData.pro_Data = System.Text.Encoding.ASCII.GetBytes("config$communication");
                    m_TCPServerObj.Send(cmData,sender);
                }
                else
                {
                    #region//改动信息，直接将信息转发
                    //LocalCommunicationStation SendStation = m_listModuleStation.Find(
                    //                      delegate(LocalCommunicationStation Station)
                    //                      {
                    //                          return Station.pro_strModuleType == userDetailInfo[0].ToLower();
                    //                      }
                    //                  );
                    //if (null != SendStation)
                    //{
                    //    m_TCPServerObj.Send(data, SendStation.pro_nwPointInfo);
                    //}
                    #endregion

                    int nCount = m_listModuleStation.Count;
                    for (int i = 0; i < nCount; i++)
                    {
                        if (userDetailInfo[0].ToLower() == m_listModuleStation[i].pro_strModuleType)
                        {
                            try
                            {
                                m_TCPServerObj.Send(data, m_listModuleStation[i].pro_nwPointInfo);
                            }
                            catch(Exception error)
                            {
                                FileOperator.ExceptionLog(error.Message);
                                m_listModuleStation.RemoveAt(i);
                                i--;
                                nCount--;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="DBConfigInfo">数据库及本地通讯等信息</param>
        /// <returns></returns>
        public bool StartService(DatabaseConfigInfo DBConfigInfo)
        {
            //连接并打开数据库
            m_DBOperate.LinkDatabase(DBConfigInfo.DbStyle, DBConfigInfo.DbServer,
                DBConfigInfo.DbMgrName, DBConfigInfo.DbUser, DBConfigInfo.DbPassword);
            int nReturn = m_DBOperate.OpenDatabase();

            if (0 > nReturn)
            {
                FileOperator.ExceptionLog(Properties.Resources.strDatabaseConnectError);
                return false;
            }

            m_isDatabaseOpen = true;
            m_DBConfigInfo = DBConfigInfo;

            if (null != m_CommBase)
            {
                //change2014.6.26
                bool isCommStart = m_CommBase.StartAll(m_DBOperate, 
                    DBConfigInfo.nHeartIntervalSeconds, DBConfigInfo.nStopIntervalSeconds);
                if (!isCommStart)
                {
                    return isCommStart;
                }
            }

            m_DBOperate.GetLocalCommInfo(out m_strLocalIP, out m_nLocalPort);

            if ("" == m_strLocalIP || 0 >= m_nLocalPort || 65536 < m_nLocalPort) //add2014.8.4添加65536限制
            {
                return true;
            }

            NetworkPointInfo TCPServerPoint = new NetworkPointInfo();
            TCPServerPoint.pro_LocalIP = IPAddress.Parse(m_strLocalIP);
            TCPServerPoint.pro_LocalPort = m_nLocalPort;        //固定，专用于各个模块的通讯服务器
            m_TCPServerObj = new TCPServerClass(TCPServerPoint);

            //添加各类回调
            m_TCPServerObj.evClientAccept += new dClientAcceptEventHandler(m_TCPServerObj_evClientAccept);
            m_TCPServerObj.evDataReceived += new dDataReceivedEventHandler(DataReceived);
            m_TCPServerObj.evLinkDisconnect += new dLinkDisconnectEventHandler(m_TCPServerObj_evLinkDisconnect);

            bool isStart = m_TCPServerObj.Start();
            if (isStart && null != StatusEventHandler)
            {
                //add2014.6.4返回本地通讯信息
                ShowInfo Show = new ShowInfo();
                Show.pro_CommType = CommunicationType.TCPServer;
                Show.pro_nLocalPort = m_nLocalPort;
                StatusEventHandler(Show);
            }

            return true;
    
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <returns></returns>
        public bool StopService()
        {
            if (null != m_CommBase)
            {
                m_CommBase.StopAll();
            }

            if (m_isDatabaseOpen && null != m_DBOperate)
            {
                m_DBOperate.Close();
            }

            bool isStop = false;
            if (null != m_TCPServerObj)
            {
                isStop = m_TCPServerObj.Close();
                m_TCPServerObj.evLinkDisconnect -= new dLinkDisconnectEventHandler(m_TCPServerObj_evLinkDisconnect);
                m_TCPServerObj.evClientAccept -= new dClientAcceptEventHandler(m_TCPServerObj_evClientAccept);
                m_TCPServerObj.evDataReceived -= new dDataReceivedEventHandler(DataReceived);
            }

            if (null != StatusEventHandler)
            {
                //add2014.6.4返回本地通讯信息
                ShowInfo Show = new ShowInfo();
                Show.pro_nTotalRecvBytes = 0;
                Show.pro_isStop = true;
                Show.pro_nLocalPort = m_nLocalPort;
                StatusEventHandler(Show);
            }

            return isStop;
        }

    }
}
