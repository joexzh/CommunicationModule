using System;
using System.Collections.Generic;

using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Windows.Forms;
using CommunicationModule.Entity;
using System.Net.Sockets;

namespace CommunicationModule
{
    public class SetFileRW
    {
        private static SetFileRW m_FileRW = null;

        /// <summary>
        /// 获取配置文件所在目录地址
        /// </summary>
        /// <returns></returns>
        public static string GetConfigDirPath()
        {
            string curdir = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.ToString();
            curdir = curdir.Substring("file:///".Length);
            curdir = Path.GetDirectoryName(curdir);
            curdir = curdir + "\\";
            curdir = curdir + "Config\\";

            if (!Directory.Exists(curdir))
            {
                Directory.CreateDirectory(curdir);
            }

            return curdir;
        }

        /// <summary>
        /// 获取日志文件所在目录地址
        /// </summary>
        /// <returns></returns>
        private static string GetLogFileDirPath()
        {
            string curdir = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.ToString();
            curdir = curdir.Substring("file:///".Length);
            curdir = Path.GetDirectoryName(curdir);
            curdir = curdir + "\\";
            curdir = curdir + "LogFile\\";

            if (!Directory.Exists(curdir))
            {
                Directory.CreateDirectory(curdir);
            }

            return curdir;
        }

        /// <summary>
        /// 获取类对象
        /// </summary>
        /// <returns></returns>
        public static SetFileRW GetFileOperator()
        {
            if (m_FileRW == null)
            {
                m_FileRW = new SetFileRW();
            }
            return m_FileRW;
        }

        /// <summary>
        /// 若文件不存在时则创建日志文件
        /// </summary>
        /// <param name="strFileName">文件名称</param>
        /// <returns></returns>
        private bool CreateFile(string strFileName)
        {
            lock (GetFileOperator())
            {
                try
                {
                    if (!File.Exists(strFileName))
                    {
                        FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                        fs.Close();
                        return true;
                    }
                }
                catch
                {
                }
            }
            return false;
        }

        /// <summary>
        /// 写端口断开连接时间日志
        /// </summary>
        /// <param name="strFileName">文件名</param>
        /// <param name="strLogMsg">日志信息</param>
        /// <param name="strIP">客户IP</param>
        /// <param name="nPort">客户端口</param>
        public void WriteLogFile(string strFileName, string strLogMsg, string strIP, int nPort)
        {
            string strWholeFileName = GetLogFileDirPath() + strFileName;
            CreateFile(strWholeFileName);

            string strWholeLogMsg = string.Format("{0}\t{1}:{2}\t{3}\n", strLogMsg, strIP, nPort, DateTime.Now);

            lock (GetFileOperator())
            {
                try
                {
                    using (StreamWriter sw1 = new StreamWriter(strWholeFileName, true))
                    {
                        sw1.WriteLine(strWholeLogMsg);
                        sw1.Close();
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 写错误日志
        /// </summary>
        /// <param name="strErrorMsg">日志信息</param>
        public void WriteErrorLogFile(string strErrorMsg)
        {
            if (null == strErrorMsg || "" == strErrorMsg)
            {
                return;
            }

            string strFileName = System.DateTime.Now.Date.Year.ToString("D4") + System.DateTime.Now.Date.Month.ToString("D2") +
                                            System.DateTime.Now.Date.Day.ToString("D2") + "ErrorLog.txt";
            string strCurDir = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase.ToString();
            strCurDir = strCurDir.Substring("file:///".Length);
            strCurDir = Path.GetDirectoryName(strCurDir);
            strCurDir = strCurDir + "\\ErrorLog";

            if (!Directory.Exists(strCurDir))
            {
                Directory.CreateDirectory(strCurDir);
            }

            strCurDir = strCurDir + "\\" + strFileName;
            CreateFile(strCurDir);

            lock (GetFileOperator())
            {
                try
                {
                    StreamWriter writeFile = new StreamWriter(strCurDir, true);
                    writeFile.WriteLine(DateTime.Now.ToString() + ":" + strErrorMsg + "\n");
                    writeFile.Close();
                }
                catch
                {
                }
            }

        }

        private static string strConfigName = GetConfigDirPath() + "ConfigDatabase.ini";
       
        /// <summary>
        /// 获取数据库配置信息
        /// </summary>
        /// <param name="DBConfigInfo">数据库及通讯等配置信息</param>
        public void GetConfigMsg(DatabaseConfigInfo DBConfigInfo)
        {
            CreateFile(strConfigName);

            lock (GetFileOperator())
            {
                try
                {
                    using (StreamReader sr1 = new StreamReader(strConfigName))
                    {
                        string strDBStyle = sr1.ReadLine();
                        if (null != strDBStyle)
                        {
                            DBConfigInfo.DbStyle = int.Parse(strDBStyle);
                        }
                        DBConfigInfo.DbServer = sr1.ReadLine();
                        DBConfigInfo.DbMgrName = sr1.ReadLine();
                        DBConfigInfo.DbUser = sr1.ReadLine();
                        DBConfigInfo.DbPassword = sr1.ReadLine();

                        string strHeartSeconds = sr1.ReadLine();
                        if (null != strHeartSeconds && "" != strHeartSeconds)
                        {
                            DBConfigInfo.nHeartIntervalSeconds = int.Parse(strHeartSeconds);
                        }
                        else
                        {
                            DBConfigInfo.nHeartIntervalSeconds = 0;
                        }

                        string strStopSeconds = sr1.ReadLine();
                        if (null != strStopSeconds && "" != strStopSeconds)
                        {
                            DBConfigInfo.nStopIntervalSeconds = int.Parse(strStopSeconds);
                        }
                        else
                        {
                            DBConfigInfo.nStopIntervalSeconds = 0;
                        }

                        sr1.Close();
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 设置数据库配置信息
        /// </summary>
        /// <param name="DBConfigInfo">数据库及通讯等配置信息</param>
        public void SetConfigMsg(DatabaseConfigInfo DBConfigInfo)
        {
            lock (GetFileOperator())
            {
                try
                {
                    CreateFile(strConfigName);
                    using (StreamWriter sw1 = new StreamWriter(strConfigName, false))
                    {
                        sw1.WriteLine(DBConfigInfo.DbStyle);
                        sw1.WriteLine(DBConfigInfo.DbServer);
                        sw1.WriteLine(DBConfigInfo.DbMgrName);
                        sw1.WriteLine(DBConfigInfo.DbUser);
                        sw1.WriteLine(DBConfigInfo.DbPassword);
                        sw1.WriteLine(DBConfigInfo.nHeartIntervalSeconds);
                        sw1.WriteLine(DBConfigInfo.nStopIntervalSeconds);
                        sw1.Close();
                    }
                }
                catch 
                {
                }
            }
        }


        /// <summary>
        /// 设置的通讯类型列表
        /// </summary>        
        private string strFileName = GetConfigDirPath() + "communication.ini";//设置文件名
        private string fileVer = "1.0";//文件版本

        public static List<string> groupTitles = new List<string>();

        public SetFileRW()
        {

        }

        /// <summary>
        /// 存储站点信息，已弃用
        /// </summary>
        /// <param name="listStationInfo"></param>
        /// <returns></returns>
        public bool SaveSetPar(List<StationInfo> listStationInfo)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(strFileName, false))
                {
                    sw.WriteLine(fileVer);
                    sw.WriteLine(listStationInfo.Count);
                    for (int i = 0; i < listStationInfo.Count; i++)
                    {
                        sw.WriteLine(listStationInfo[i].pro_nBaudRate);
                        sw.WriteLine(listStationInfo[i].pro_strIP);
                        sw.WriteLine(listStationInfo[i].pro_nPort);
                        sw.WriteLine(listStationInfo[i].pro_CommunicationType.ToString());
                    }
                }
            }
            catch
            {
            }

            return true;
        }

        /// <summary>
        /// 获取站点信息，已弃用
        /// </summary>
        /// <param name="listStationInfo"></param>
        /// <returns></returns>
        public bool GetSetPar(out List<StationInfo> listStationInfo)
        {
            listStationInfo = null;
            listStationInfo = new List<StationInfo>();
            groupTitles.Clear();

            if (!File.Exists(strFileName))
            {
                return false;
            }

            try
            {
                using (StreamReader sr = new StreamReader(strFileName))
                {
                    if (sr.ReadLine() == fileVer)
                    {
                        int count = Convert.ToInt32(sr.ReadLine());

                        for (int i = 0; i < count; i++)
                        {
                            StationInfo SInfo = new StationInfo();
                            SInfo.pro_nBaudRate = Convert.ToInt32(sr.ReadLine());
                            SInfo.pro_strIP = sr.ReadLine();
                            SInfo.pro_nPort = Convert.ToInt32(sr.ReadLine());
                            SInfo.pro_CommunicationType = (CommunicationType)Enum.Parse(typeof(CommunicationType), sr.ReadLine());
                            listStationInfo.Add(SInfo);
                        }
                    }
                }
            }
            catch
            {
         //       MessageBox.Show(Resources.ReadFile_Fail);//"尝试读取配置文件失败"
            }

            return true;
        }
    }
}
