namespace CommunicationModule
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.MenuItem = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.Config = new System.Windows.Forms.ToolStripMenuItem();
            this.HideDlg = new System.Windows.Forms.ToolStripMenuItem();
            this.ShowDlg = new System.Windows.Forms.ToolStripMenuItem();
            this.Exit = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewLink = new System.Windows.Forms.ListView();
            this.ColumnPort = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnCommType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnProtocal = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnUser = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnTotal = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnDevID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnClientIP = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnLinkTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnRemark = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listViewMsg = new System.Windows.Forms.ListView();
            this.columnDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnObject = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnMessage = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MenuConfig = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notifyIcon1.BalloonTipText = "程序已隐藏运行";
            this.notifyIcon1.ContextMenuStrip = this.MenuItem;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "通讯中转服务器";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // MenuItem
            // 
            this.MenuItem.ImeMode = System.Windows.Forms.ImeMode.On;
            this.MenuItem.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Config,
            this.HideDlg,
            this.ShowDlg,
            this.Exit});
            this.MenuItem.Name = "MenuItem";
            this.MenuItem.Size = new System.Drawing.Size(101, 92);
            // 
            // Config
            // 
            this.Config.Name = "Config";
            this.Config.Size = new System.Drawing.Size(100, 22);
            this.Config.Text = "配置";
            this.Config.Click += new System.EventHandler(this.Config_Click);
            // 
            // HideDlg
            // 
            this.HideDlg.Name = "HideDlg";
            this.HideDlg.Size = new System.Drawing.Size(100, 22);
            this.HideDlg.Text = "隐藏";
            this.HideDlg.Click += new System.EventHandler(this.Hide_Click);
            // 
            // ShowDlg
            // 
            this.ShowDlg.Name = "ShowDlg";
            this.ShowDlg.Size = new System.Drawing.Size(100, 22);
            this.ShowDlg.Text = "显示";
            this.ShowDlg.Click += new System.EventHandler(this.Show_Click);
            // 
            // Exit
            // 
            this.Exit.Name = "Exit";
            this.Exit.Size = new System.Drawing.Size(100, 22);
            this.Exit.Text = "退出";
            this.Exit.Click += new System.EventHandler(this.Exit_Click);
            // 
            // listViewLink
            // 
            this.listViewLink.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColumnPort,
            this.columnCommType,
            this.columnProtocal,
            this.columnUser,
            this.columnTotal,
            this.columnDevID,
            this.columnClientIP,
            this.columnLinkTime,
            this.columnRemark});
            this.listViewLink.FullRowSelect = true;
            this.listViewLink.Location = new System.Drawing.Point(12, 36);
            this.listViewLink.Name = "listViewLink";
            this.listViewLink.Size = new System.Drawing.Size(875, 303);
            this.listViewLink.TabIndex = 1;
            this.listViewLink.UseCompatibleStateImageBehavior = false;
            this.listViewLink.View = System.Windows.Forms.View.Details;
            // 
            // ColumnPort
            // 
            this.ColumnPort.Text = "本地端口";
            this.ColumnPort.Width = 70;
            // 
            // columnCommType
            // 
            this.columnCommType.Text = "通讯类型";
            this.columnCommType.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnCommType.Width = 80;
            // 
            // columnProtocal
            // 
            this.columnProtocal.Text = "是否带协议";
            this.columnProtocal.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnProtocal.Width = 80;
            // 
            // columnUser
            // 
            this.columnUser.Text = "客户数量";
            this.columnUser.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnUser.Width = 70;
            // 
            // columnTotal
            // 
            this.columnTotal.Text = "接收数据量";
            this.columnTotal.Width = 80;
            // 
            // columnDevID
            // 
            this.columnDevID.Text = "设备ID";
            this.columnDevID.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnDevID.Width = 80;
            // 
            // columnClientIP
            // 
            this.columnClientIP.Text = "客户IP";
            this.columnClientIP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnClientIP.Width = 130;
            // 
            // columnLinkTime
            // 
            this.columnLinkTime.Text = "数据源连接时间";
            this.columnLinkTime.Width = 120;
            // 
            // columnRemark
            // 
            this.columnRemark.Text = "备注";
            this.columnRemark.Width = 280;
            // 
            // listViewMsg
            // 
            this.listViewMsg.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnDate,
            this.columnObject,
            this.columnMessage});
            this.listViewMsg.FullRowSelect = true;
            this.listViewMsg.Location = new System.Drawing.Point(12, 345);
            this.listViewMsg.Name = "listViewMsg";
            this.listViewMsg.Size = new System.Drawing.Size(875, 182);
            this.listViewMsg.TabIndex = 1;
            this.listViewMsg.UseCompatibleStateImageBehavior = false;
            this.listViewMsg.View = System.Windows.Forms.View.Details;
            // 
            // columnDate
            // 
            this.columnDate.Text = "日期";
            this.columnDate.Width = 120;
            // 
            // columnObject
            // 
            this.columnObject.Text = "设备号";
            this.columnObject.Width = 120;
            // 
            // columnMessage
            // 
            this.columnMessage.Text = "消息";
            this.columnMessage.Width = 600;
            // 
            // menuStrip1
            // 
            this.menuStrip1.AutoSize = false;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuConfig});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(896, 25);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "菜单";
            // 
            // MenuConfig
            // 
            this.MenuConfig.Name = "MenuConfig";
            this.MenuConfig.Size = new System.Drawing.Size(44, 21);
            this.MenuConfig.Text = "配置";
            this.MenuConfig.Click += new System.EventHandler(this.MenuConfig_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(896, 539);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.listViewMsg);
            this.Controls.Add(this.listViewLink);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "通讯中转服务器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.MenuItem.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip MenuItem;
        private System.Windows.Forms.ToolStripMenuItem HideDlg;
        private System.Windows.Forms.ToolStripMenuItem ShowDlg;
        private System.Windows.Forms.ToolStripMenuItem Exit;
        private System.Windows.Forms.ListView listViewLink;
        private System.Windows.Forms.ColumnHeader ColumnPort;
        private System.Windows.Forms.ListView listViewMsg;
        private System.Windows.Forms.ColumnHeader columnDate;
        private System.Windows.Forms.ColumnHeader columnDevID;
        private System.Windows.Forms.ColumnHeader columnClientIP;
        private System.Windows.Forms.ColumnHeader columnUser;
        private System.Windows.Forms.ColumnHeader columnTotal;
        private System.Windows.Forms.ColumnHeader columnLinkTime;
        private System.Windows.Forms.ColumnHeader columnObject;
        private System.Windows.Forms.ColumnHeader columnMessage;
        private System.Windows.Forms.ColumnHeader columnRemark;
        private System.Windows.Forms.ColumnHeader columnProtocal;
        private System.Windows.Forms.ToolStripMenuItem Config;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem MenuConfig;
        private System.Windows.Forms.ColumnHeader columnCommType;
    }
}

