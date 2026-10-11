using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VinXiangQi
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 第十四轮崩溃加固：全局兜底——UI 线程异常记入 fatal.log 并继续运行；
            // 后台线程（引擎输出/着法回调）异常已在各自源头隔离并记入 err.log。
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => LogFatal(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogFatal(e.ExceptionObject as Exception);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Mainform());
        }

        // 全局异常落盘（fatal.log）；写日志本身失败不得再次抛出
        static void LogFatal(Exception ex)
        {
            try { System.IO.File.AppendAllText("fatal.log", DateTime.Now + "\r\n" + ex + "\r\n\r\n"); } catch { }
        }
    }
}
