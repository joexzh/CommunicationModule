using System;
using System.Collections.Generic;

using System.Windows.Forms;
using ZHD.SYS.CommonUtility.CommunicationLib;

namespace CommunicationModule
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //处理未捕捉的异常
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            //处理UI线程
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            //处理非UI线程
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
 
            Application.Run(new MainForm());
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ee = e.ExceptionObject as Exception;
            if (ee != null)
            {
                FileOperator.ExceptionLog("[Program.CurrentDomain_UnhandledException]" + ee.Message + Environment.NewLine + ee.Source + Environment.NewLine + ee.StackTrace);
            }
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            FileOperator.ExceptionLog("[Program.Application_ThreadException]" + e.Exception.Message + Environment.NewLine + e.Exception.Source + Environment.NewLine + e.Exception.StackTrace);
        }
    }
}
