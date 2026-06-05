using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HeCheck
{
    static class Program
    {
        private static System.Threading.Mutex mutex;//互斥量
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createNew = false;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mutex = new System.Threading.Mutex(true, "ScanCheck", out createNew);

            if (createNew && mutex.WaitOne(0, false))
            {
                Application.Run(new FormMain());
            }
            else
            {
                MessageBox.Show("已经有一个程序正在运行！");
                System.Environment.Exit(1);
            }
        }
    }
}
