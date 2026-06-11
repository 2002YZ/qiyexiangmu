using PanelHandingSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;


namespace Log
{

    //日志记录类
    public static class LogRecordVision
    {

        public static string SystemLogPath = "D:\\History\\SystemLog\\";
        public static string SystemLogFilePath = "";
        //日志文件相关
        private static FileStream SystemLogFilestream;
        private static StreamWriter SystemLogStreamwriter;
        public static  LogDelegate refreshLog;
        public static string LogHistory = "";
        static object History_locker = new object();

        public delegate void LogDelegate(string Message);

        public static RichTextBox richTextBox = null;

        public static int LogRows = 0;
        //最大行数设置，超过这个行数会新建日志
        public static int MaxRows = 6000;

        static LogRecordVision()
        {
            //检查路径是否存在


            try
            {
                if (!Directory.Exists(SystemLogPath))//如果不存在就创建文件夹
                {
                    Directory.CreateDirectory(SystemLogPath);//创建该文件夹
                }    
            }
            catch
            {


            }



             refreshLog = new LogDelegate(RrefreshLogFunc);
             NewLogFile();




        }


        public static void NewLogFile()
        {


            //创建日志文件
            string data = System.DateTime.Now.ToString();
            string[] data1 = data.Split(' ');
            data = data1[0];
            string SystemTime = data.Replace('/', '_');
            SystemLogFilePath = SystemLogPath + SystemTime + "SystemLogVision.txt";
            if (File.Exists(SystemLogFilePath))
            {
                string name = System.DateTime.Now.ToString();
                name = name.Replace('/', '_');
                name = name.Replace(' ', '_');
                name = name.Replace(':', '_');
                SystemLogFilePath = SystemLogPath + name + "SystemLogVision.txt";
                SystemLogFilestream = new FileStream(SystemLogFilePath, FileMode.Create);
                SystemLogStreamwriter = new StreamWriter(SystemLogFilestream);
            }
            else
            {
                SystemLogFilestream = new FileStream(SystemLogFilePath, FileMode.Create);
                SystemLogStreamwriter = new StreamWriter(SystemLogFilestream);
            }


        }




        //控件注册
        public static void RegistTextbox(RichTextBox tb)
        {

         
            richTextBox = tb;
        }


        private static void RrefreshLogFunc(string SystemStatus)
        {
            string time = System.DateTime.Now.ToString("MM/dd HH:mm:ss.fff");
            string msg = SystemStatus.TrimEnd('\r', '\n');
            msg = msg.Replace("\r", "").Replace("\n", "");
            msg = msg.Replace("HALCON", "").Replace("Halcon","");
            string message = time + " : " + msg ;

            if (true || SystemStatus != LogHistory)
            {

           

                LogHistory = SystemStatus;
                lock (History_locker)
                {
                    //写入系统日志文件
                
                    if (LogRows > MaxRows)
                    {
                        NewLogFile();
                        LogRows = 0;
                    }

                    SystemLogStreamwriter.WriteLine(message);
                    //清空缓冲区
                    SystemLogStreamwriter.Flush();
                    AddMessageToTextbox(message);
                    Thread.Sleep(5);

                    LogRows++;
                }
            }  
        }

        public static void AddMessageToTextbox(string str)
        {
            Form1.frm1.AddLog2(str);
        }


      




        public static void addLog( string message)
        {
           // refreshLog.BeginInvoke( message, null, null);
            refreshLog.Invoke(message);

        }











    }
}
