using Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PanelHandingSystem
{

    //扫码工作站
    public class BarcodeScan
    {

        //socket TCP扫码枪
        public  int SaoMaTimeOut = 6000;
        private  TcpClient tcpClient;
        public  bool isConnected=false;
        
        public static bool ManualTrigger = false;
        
        public int BarScanNo = -1;//扫码枪编号 1，2  没有0  
        public string IPAddr = "";
        public int Port = -1;
        
        public string TriggerCommad="start"; //扫码触发命令

        private  NetworkStream stream;

        public  void  Connect(string _IPAddr, int _port, int _BarScanNo)
        {

            try
            {

                IPAddr = _IPAddr;
                Port = _port;
                BarScanNo = _BarScanNo;


                LogRecordVision.addLog($"{BarScanNo}号连接扫码枪:{IPAddr}:{Port}");
                tcpClient = new TcpClient();              
                tcpClient.Connect(IPAddr, Port);


                if (tcpClient.Connected)
                {
                    isConnected = true;
                    stream = tcpClient.GetStream();
                    stream.ReadTimeout = SaoMaTimeOut;
                    LogRecordVision.addLog($"{BarScanNo}号扫码枪连接成功," + IPAddr + ":" + Port.ToString());
                }
            }
            catch(Exception ex)
            {

                LogRecordVision.addLog($"{BarScanNo}号连接扫码枪出错:" + ex.Message);
            }
            //连接扫码枪

           

        }


        public  void Send(string message)
        {
            try
            {
                if (isConnected)
                {
                    byte[] msg = Encoding.ASCII.GetBytes(message);
                    stream.Write(msg, 0, msg.Length);

                    LogRecordVision.addLog($"{BarScanNo}号,向扫码枪发送信息:" + message);
                }
                else
                {
                    LogRecordVision.addLog($"{BarScanNo}号扫码枪尚未建立连接");
                }

            }
            catch (Exception ex)
            {
                LogRecordVision.addLog($"{BarScanNo}号扫码枪通讯失败" + ex.Message);

            }

        }

        //等待时间较长，需在线程运行


        public static int offlineZhuBanCount1 = 0;
        public static int offlineZhuBanCount2 = 4;
        /// <summary>
        /// 启动扫码
        /// </summary>
        /// <param name="useSoftTrigger">false手持模式</param>
        /// <returns></returns>
        public  string StarBarcode(bool useSoftTrigger = false)
        {
            //扫码枪开始扫码，并等待一定时间，返回扫码结果
            string Res = "";
         

            LogRecordVision.addLog($"{BarScanNo}号开始扫码");
          
            if (WorkFlowControl.isOfflineDebug && BarScanNo > 2 && BarScanNo <5 && !ManualTrigger)
            {

                //3号和4号扫码枪扫码
                if (BarScanNo == 3 || BarScanNo == 4)
                {
                    Res = "00" + (BarScanNo-2).ToString();
                    LogRecordVision.addLog($"{BarScanNo}号扫码枪扫飞巴码,当前为离线调试模式,飞巴码返回{Res}");
                }
                return Res;
            }
             

            if(WorkFlowControl.panelNoCode && (BarScanNo <3  || BarScanNo > 4)  && WorkFlowControl.isOfflineDebug && !ManualTrigger)
            {
                //1 号和2号扫码枪扫码 ，5号和6号扫码枪扫码
                if(BarScanNo == 1)
                {
                    //扫码枪扫主板码,切当前设定为主板无码 "自动按  0001,0002,0003 返回扫码信息"
                    Res = "000" + (offlineZhuBanCount1+1).ToString();
                    LogRecordVision.addLog($"{BarScanNo}号扫码枪扫主板码,当前设定为主板无码,将自动生成码,主板码返回{Res}");
                    offlineZhuBanCount1++;
                }

                if(BarScanNo == 2)
                {
                    //扫码枪扫主板码,2号扫码枪切当前设定为主板无码 "自动按  0004,0005,0006 返回扫码信息"
                    Res = "000" + (offlineZhuBanCount2+1).ToString();
                    LogRecordVision.addLog($"{BarScanNo}号扫码枪扫主板码,当前设定为主板无码,将自动生成码,主板码返回{Res}");
                    offlineZhuBanCount2++;
                }

                if (BarScanNo == 5)
                {
                    //扫码枪扫主板码,2号扫码枪切当前设定为主板无码 "自动按  0004,0005,0006 返回扫码信息"
                    Res = "000" + (offlineZhuBanCount1 + 1).ToString();
                    LogRecordVision.addLog($"{BarScanNo}号扫码枪扫主板码,当前设定为主板无码,将自动生成码,主板码返回{Res}");
                    offlineZhuBanCount1++;
                }

                if (BarScanNo == 6)
                {
                    //扫码枪扫主板码,2号扫码枪切当前设定为主板无码 "自动按  0004,0005,0006 返回扫码信息"
                    Res = "000" + (offlineZhuBanCount2 + 1).ToString();
                    LogRecordVision.addLog($"{BarScanNo}号扫码枪扫主板码,当前设定为主板无码,将自动生成码,主板码返回{Res}");
                    offlineZhuBanCount2++;
                }

                return Res;

            }


            DateTime startTime = DateTime.Now;
            //向扫码枪发送启动扫码信号
            Send("start");
            bool GetBarCode = false;
            byte[] buffer = new byte[1024];

            while (!GetBarCode)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        LogRecordVision.addLog($"{BarScanNo}号收到扫码枪信号: " + message);

                        string strRes = message;

                        if (strRes == "NoRead")
                        {
                            LogRecordVision.addLog($"{BarScanNo}号没有扫到条码");
                            Res = strRes;
                        }            
                        else
                            Res = strRes;
                      
                        GetBarCode = true;
                        break;

                    }  
                    Thread.Sleep(200);
 
                }
                catch (Exception ex)
                {
                    LogRecordVision.addLog($"{BarScanNo}号扫码出错或超时：" +ex.Message);
                }

                TimeSpan duration = DateTime.Now - startTime; // 计算耗时
                if (duration.TotalMilliseconds > SaoMaTimeOut)
                {
                    //等待超时，日志提示，但是还是继续等待
                    LogRecordVision.addLog($"{BarScanNo}号扫码超时未返回");
                    break;
                }
                Thread.Sleep(200);


            }

            return Res;
        }

    }
}
