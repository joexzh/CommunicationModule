using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CommunicationModule
{
    public partial class ConfigForm : Form
    {
        public DatabaseConfigInfo DBConfigInfo = new DatabaseConfigInfo();
        private SetFileRW m_Config;
        public bool isOKButton = false;

        public ConfigForm()
        {
            InitializeComponent();
            m_Config = new SetFileRW();
            m_Config.GetConfigMsg(DBConfigInfo);

            if (0 <= DBConfigInfo.DbStyle)
            {
                this.comboBoxDatabaseType.Text = this.comboBoxDatabaseType.Items[DBConfigInfo.DbStyle].ToString();
            }
            if (null != DBConfigInfo.DbServer)
            {
                this.textBoxIP.Text = DBConfigInfo.DbServer;
            }
            this.textBoxDatabase.Text = DBConfigInfo.DbMgrName;
            this.textBoxUser.Text = DBConfigInfo.DbUser;
            this.textBoxPsw.Text = DBConfigInfo.DbPassword;

            if (0 < DBConfigInfo.nHeartIntervalSeconds)
            {
                this.textBoxHeartInterval.Text = DBConfigInfo.nHeartIntervalSeconds.ToString();
            }
            if (0 < DBConfigInfo.nStopIntervalSeconds)
            {
                this.textBoxStopInterval.Text = DBConfigInfo.nStopIntervalSeconds.ToString();
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            isOKButton = true;
            DBConfigInfo.DbStyle = this.comboBoxDatabaseType.SelectedIndex;
            DBConfigInfo.DbServer = this.textBoxIP.Text;
            DBConfigInfo.DbMgrName = this.textBoxDatabase.Text;
            DBConfigInfo.DbUser = this.textBoxUser.Text;
            DBConfigInfo.DbPassword = this.textBoxPsw.Text;

            if ("" == DBConfigInfo.DbServer || "" == DBConfigInfo.DbMgrName
                || "" == DBConfigInfo.DbUser || "" == DBConfigInfo.DbPassword)
            {
                MessageBox.Show("请提供完整的信息", "提示!",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                return ;
            }
            string strHeartInterval = this.textBoxHeartInterval.Text;
            string strStopInterval = this.textBoxStopInterval.Text;
            DBConfigInfo.nHeartIntervalSeconds = int.Parse(strHeartInterval == "" ? "0" : strHeartInterval);
            DBConfigInfo.nStopIntervalSeconds = int.Parse(strStopInterval == "" ? "0" : strStopInterval);
            m_Config.SetConfigMsg(DBConfigInfo);
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            isOKButton = false;
            this.Close();
        }
    }
}
