using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using CommunicationModule.Entity;
using System.Timers;

namespace CommunicationModule
{
    public partial class MainForm : Form
    {
        private SetFileRW m_Config;
        private DatabaseConfigInfo m_DBConfigInfo = new DatabaseConfigInfo();

        private bool m_isStartSuccess = false;
        private Thread m_mtCommThread;
        private ConfigForm m_ConfigForm;
        private LocalCommunication m_LocalServer;
        private List<ShowInfo> m_listStation = new List<ShowInfo>();
        private System.Windows.Forms.Timer m_ShowTimer = new System.Windows.Forms.Timer();

        private void LinkStatus(ShowInfo ReturnInfo)
        {
            #region 原来的处理
            //bool isExist = false;
            //if (0 < m_listStation.Count)
            //{
            //    int nListLen = m_listStation.Count;
            //    for (int i = 0; i < nListLen; i++)
            //    {
            //        if (ReturnInfo.pro_nLocalPort == m_listStation[i].pro_nLocalPort)
            //        {
            //            isExist = true;
            //            if (ReturnInfo.pro_isConnected)
            //            {
            //                m_listStation[i].pro_nClientNum += 1;
            //            }
            //            else
            //            {
            //                m_listStation[i].pro_nClientNum -= 1;
            //            }                            
            //            m_listStation[i].pro_isUpdate = true;
            //        }
            //    }
            //}

            //if (!isExist)
            //{
            #endregion

           // object lockThis = new object();
            lock (m_listStation)
            {
                m_listStation.Add(ReturnInfo);
            }
        }

        public MainForm()
        {
            InitializeComponent();

            m_Config = new SetFileRW();
            m_Config.GetConfigMsg(m_DBConfigInfo);

            m_mtCommThread = new Thread(new ParameterizedThreadStart(MTCommRun));
            m_LocalServer = new LocalCommunication();
            m_LocalServer.StatusEventHandler += new LocalCommunication.LinkStatusHandler(LinkStatus);
            m_mtCommThread.Start(m_LocalServer);

            m_ShowTimer.Tick += new EventHandler(ShowTimer_Tick);
            m_ShowTimer.Interval = 500;
            m_ShowTimer.Start();
        }

        private void MTCommRun(object CommServer)
        {
            while (true)
            {
                //是否点击配置窗口的确定按钮
                if (null != m_ConfigForm && m_ConfigForm.IsDisposed && m_ConfigForm.isOKButton)
                {
                    if ("" == m_ConfigForm.DBConfigInfo.DbMgrName || "" == m_ConfigForm.DBConfigInfo.DbUser
                        || "" == m_ConfigForm.DBConfigInfo.DbPassword)
                    {
                        //    m_Config.WriteErrorLogFile("数据库配置信息填写不完整");
                        Thread.Sleep(500);
                        continue;
                    }
                    else
                    {
                        m_DBConfigInfo = m_ConfigForm.DBConfigInfo;
                        m_ConfigForm = null;
                        if (m_isStartSuccess)
                        {
                            ((LocalCommunication)CommServer).StopService();
                            m_isStartSuccess = false;
                        }
                    }
                }

                //读配置文件值是否正确
                if (null == m_DBConfigInfo.DbServer || null == m_DBConfigInfo.DbUser
                    || null == m_DBConfigInfo.DbPassword || null == m_DBConfigInfo.DbMgrName
                    || 0 > m_DBConfigInfo.DbStyle)
                {
                    Thread.Sleep(500);
                    continue;
                }

                //启动本地和对外的服务器
                if (null != CommServer && !m_isStartSuccess)
                {
                    m_isStartSuccess = ((LocalCommunication)CommServer).StartService(m_DBConfigInfo);
                }

                Thread.Sleep(1000);
            }
        }

        //界面显示操作
        private void ShowTimer_Tick(Object myObject, EventArgs myEventArgs)
        {
            if (0 >= m_listStation.Count)
            {
                return;
            }

            #region  加锁（恢复change2014.8.21,因目前已有实例因多线程操作导致程序崩溃)
            lock (m_listStation)
            {
                int nListLen = m_listStation.Count;
                for (int i = 0; i < nListLen; i++)
                {
                    #region 若为停止，则删除相应项

                    if (m_listStation[i].pro_isStop)
                    {
                        bool isRemove = false;
                        foreach (ListViewItem eachItem in listViewLink.Items)
                        {
                            //找到相应服务器索引，删除服务端口及其下的客户端连接
                            if (eachItem.Text == m_listStation[i].pro_nLocalPort.ToString())
                            {
                                isRemove = true;
                                listViewLink.Items.Remove(eachItem);
                            }
                            else if (isRemove && (null == eachItem.Text || "" == eachItem.Text))
                            {
                                listViewLink.Items.Remove(eachItem);
                            }
                            else
                            {
                                isRemove = false;
                            }
                        }

                        continue;
                    }
                    
                    #endregion

                    //add2014.5.27
                    if (0 == m_listStation[i].pro_nTotalRecvBytes)
                    {
                        //IP为null时说明是服务器启动
                        if (null == m_listStation[i].pro_strIP)
                        {
                            ListViewItem StationItem = new ListViewItem();
                            StationItem.Text = m_listStation[i].pro_nLocalPort.ToString();
                            StationItem.SubItems.Add(m_listStation[i].pro_CommType.ToString());
                            StationItem.SubItems.Add(m_listStation[i].pro_Protocal.ToString());
                           
                            StationItem.SubItems.Add(m_listStation[i].pro_nClientNum.ToString());
                            StationItem.SubItems.Add(m_listStation[i].pro_nTotalRecvBytes.ToString());
                            StationItem.SubItems.Add(m_listStation[i].pro_strDeviceID);
                            StationItem.SubItems.Add(m_listStation[i].pro_strIP);
                            StationItem.SubItems.Add(m_listStation[i].pro_ConnectedTime.ToString());
                       //     StationItem.SubItems.Add(m_listStation[i].pro_DisConnectedTime.ToString());
                            StationItem.SubItems.Add(m_listStation[i].pro_strRemark);
                            this.listViewLink.Items.Add(StationItem);
                        }
                        else if(m_listStation[i].pro_isConnected)
                        {
                            int nParentIndex = -1;

                            //pro_isConnected为true则说明有客户端连接上了
                            foreach (ListViewItem eachItem in listViewLink.Items)
                            {
                                //找到相应服务器索引，并修改连接数
                                if (eachItem.Text == m_listStation[i].pro_nLocalPort.ToString())
                                {
                                    nParentIndex = eachItem.Index;
                                    eachItem.SubItems[3].Text = m_listStation[i].pro_nClientNum.ToString();
                                //    eachItem.SubItems[3].Text = m_listStation[i].pro_nTotalRecvBytes.ToString();
                                    break;
                                }
                            }

                            if (0 <= nParentIndex)
                            {
                                ListViewItem StationItem = new ListViewItem();
                                StationItem.Text = "";
                                StationItem.SubItems.Add("");
                                StationItem.SubItems.Add("");
                                StationItem.SubItems.Add("");
                                StationItem.SubItems.Add("0");
                                StationItem.SubItems.Add(m_listStation[i].pro_strDeviceID);
                                StationItem.SubItems.Add(m_listStation[i].pro_strIP);
                                StationItem.SubItems.Add(m_listStation[i].pro_ConnectedTime.ToString());
                          //      StationItem.SubItems.Add(m_listStation[i].pro_DisConnectedTime.ToString());
                         //       StationItem.SubItems.Add(m_listStation[i].pro_strRemark);
                                this.listViewLink.Items.Insert(nParentIndex + 1, StationItem);   
                            }

                            //显示客户端的连接信息
                            ListViewItem MsgItem = new ListViewItem();
                            MsgItem.Text = m_listStation[i].pro_ConnectedTime.ToString();
                            MsgItem.SubItems.Add(m_listStation[i].pro_strDeviceID);
                            MsgItem.SubItems.Add(m_listStation[i].pro_strIP + "已连接!");
                            this.listViewMsg.Items.Add(MsgItem);
                        }
                        else
                        {
                            foreach (ListViewItem eachItem in listViewLink.Items)
                            {
                                //找到相应服务器索引，并修改连接数change2014.10.14,加trim
                                if (eachItem.Text.Trim() == m_listStation[i].pro_nLocalPort.ToString().Trim())
                                {
                                    eachItem.SubItems[3].Text = m_listStation[i].pro_nClientNum.ToString();
                                    if (0 == m_listStation[i].pro_nClientNum)
                                    {
                                        eachItem.SubItems[4].Text = "0";
                                    }
                                }
                                if (eachItem.SubItems[6].Text.Trim() == m_listStation[i].pro_strIP.Trim())
                                {
                                    listViewLink.Items.Remove(eachItem);
                                    break;
                                }
                            }

                            //显示客户端的断开信息
                            ListViewItem MsgItem = new ListViewItem();
                            MsgItem.Text = m_listStation[i].pro_DisConnectedTime.ToString();
                            MsgItem.SubItems.Add(m_listStation[i].pro_strDeviceID);
                            MsgItem.SubItems.Add(m_listStation[i].pro_strIP + "已断开!");
                            this.listViewMsg.Items.Add(MsgItem);
                        }
                    }
                    else
                    {
                        foreach (ListViewItem eachItem in listViewLink.Items)
                        {
                            //找到相应服务器索引，并修改连接数
                            if (eachItem.Text == m_listStation[i].pro_nLocalPort.ToString())
                            {
                                string strRecvBytes = eachItem.SubItems[4].Text;
                                long nRecvBytes = long.Parse(strRecvBytes) + m_listStation[i].pro_nTotalRecvBytes;
                                eachItem.SubItems[4].Text = nRecvBytes.ToString();
                            }
                            if (eachItem.SubItems[6].Text == m_listStation[i].pro_strIP)
                            {
                                string strRecvBytes = eachItem.SubItems[4].Text;
                                long nRecvBytes = long.Parse(strRecvBytes) + m_listStation[i].pro_nTotalRecvBytes;
                                eachItem.SubItems[4].Text = nRecvBytes.ToString();
                                break;
                            }
                        }
                    }
                }

                m_listStation.Clear();
            }
            #endregion
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                e.Cancel = true;
                this.WindowState = System.Windows.Forms.FormWindowState.Minimized;

                this.Hide();
                this.notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(3000);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                this.notifyIcon1.Visible = true;
            }
            else if (this.WindowState == FormWindowState.Minimized)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            }
        }

        private void Hide_Click(object sender, EventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
                this.Hide();
                this.notifyIcon1.Visible = true;
            }
        }

        private void Show_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要退出服务器？", "提示!",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != System.Windows.Forms.DialogResult.Cancel)
            {
                if (this.WindowState != FormWindowState.Minimized)
                {
                    this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
                    this.Hide();
                    this.notifyIcon1.Visible = true;
                }

                //停止所有服务器,先停,再解除事件
                m_mtCommThread.Abort();
                m_mtCommThread.Join();
                m_LocalServer.StopService();
                m_LocalServer.StatusEventHandler -= new LocalCommunication.LinkStatusHandler(LinkStatus);
                m_LocalServer.Close();
                this.Close();
            }
        }

        private void Config_Click(object sender, EventArgs e)
        {
            if (null == m_ConfigForm || m_ConfigForm.IsDisposed)
            {
                m_ConfigForm = new ConfigForm();
                m_ConfigForm.Show();
            }
            
        }

        private void MenuConfig_Click(object sender, EventArgs e)
        {
            if (null == m_ConfigForm || m_ConfigForm.IsDisposed)
            {
                m_ConfigForm = new ConfigForm();
                m_ConfigForm.Show();
            }
        }
    }
}
