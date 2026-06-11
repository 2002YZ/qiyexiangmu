using HalconDotNet;
using Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PanelHandingSystem
{


    //与VisionMaster通讯
    public static class Com2VisionMaster
    {


        //通讯流程
        //上位机向visionmaster 发送  "1"  触发拍照
        //VisionMaster返回完成信号 "2" ,表示拍照成功, 同时返回坐标值，表示拍照成功 "x,y,r"  举例 "201.3,456.2,3.5"
        //返回信息举例 "2, 201.3, 456.2, 3.5"  第一位2 表示拍照成功,后面三个数字表示 "x,y,r" 




        public static Socket socket_2D = null;
        public static string IpAddr_2D = "127.0.0.5";//模拟调试用虚拟地址_2D检测
        public static int Port_2D = 7930;//2D端口
        public static int VisonMasterTimeout = 5000;//2D端口


        public static void Connect()
        {
            try
            {
                IpAddr_2D = ParaSet.recipe.HikVMIPAddr;
                Port_2D = ParaSet.recipe.HikVMIPPort;

                if (socket_2D != null)
                {
                    try { socket_2D.Close(); } catch { }
                }

                socket_2D = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//2D定位实例化


                //   string IpAddr_2D_local = "127.0.0.1";//模拟调试用虚拟地址
                //string IpAddr_2D_local = "192.168.31.221";//模拟调试用虚拟地址
                //IPAddress ip_local_2D = IPAddress.Parse(IpAddr_2D_local);
                IPAddress ip = IPAddress.Parse(IpAddr_2D);
                //Random random_port = new Random();
                //IPEndPoint point11 = new IPEndPoint(ip_local_2D, Convert.ToInt32(random_port.Next(100)));
                IPEndPoint point22 = new IPEndPoint(ip, Convert.ToInt32(Port_2D));


                socket_2D.ReceiveTimeout = VisonMasterTimeout;
                socket_2D.SendTimeout = VisonMasterTimeout;

                //socket_2D.Bind(point11);
                socket_2D.Connect(point22);
                //var tmp = socket_2D.LocalEndPoint;



                LogRecordVision.addLog("VisionMaster连接成功!");
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("连接VisonMaster出错:" + ex.Message);
            }



           


        }

        public static void Send2VisionMaster(string information)
        {
            if (socket_2D == null)
            {
                LogRecordVision.addLog("[操作错误]: 尚未连接视觉系统！请先点击主界面的'连接VisionMaster'按钮。");
                return; 
            }

           
            if (!socket_2D.Connected)
            {
                LogRecordVision.addLog("[通信错误]: 视觉系统连接已断开，无法发送指令！");
                return;
            }

            byte[] buffer2 = new byte[2048];
            string strSend2 = information;
            //string strSend = strRes  + strX + strY + strT + "\r\n";
            buffer2 = Encoding.Default.GetBytes(strSend2);
            try
            {
                socket_2D.Send(buffer2);

            }
            catch
            {
                LogRecordVision.addLog("发信息给2D失败，请检查网络后重启软件");
            }

        }



        public static void ClearSocketBuffer(Socket socket)
        {


            if (socket == null || !socket.Connected)
                return;

            // 保存原来的超时设置
            int originalReceiveTimeout = socket.ReceiveTimeout;

            try
            {
                // 设置短暂的超时时间
                socket.ReceiveTimeout = 100; // 100ms

                byte[] buffer = new byte[1024];
                int bytesRead;

                // 循环读取直到没有数据
                do
                {
                    bytesRead = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                }
                while (bytesRead > 0);
            }
            catch (SocketException ex)
            {
                // 超时异常 (10060) 表示缓冲区已空
                if (ex.SocketErrorCode != SocketError.TimedOut)
                {
                    throw;
                }
            }
            finally
            {
                // 恢复原来的超时设置
                socket.ReceiveTimeout = originalReceiveTimeout;
            }
        }





        public static bool isCapturing = false;

        //通讯流程上位机向visionmaster 发送  "1"  触发工程1拍照，发送"2"，触发工程2拍照
        //VisionMaster返回完成信号 "2" ,表示拍照成功, 同时返回坐标值，表示拍照成功 "x,y,r"  举例 "201.3,456.2,3.5"
        //返回信息举例 "2, 201.3, 456.2, 3.5"  第一位2 表示1号相机拍照成功,后面三个数字表示 "x,y,r" 
        //返回信息举例 "3, 201.3, 456.2, 3.5"  第一位3 表示2号相机拍照成功,后面三个数字表示 "x,y,r" 
        //


        public static bool StartCapture(out double x,out double y,out double r ,int CamNo)
        {
            while(isCapturing)
            {

                //如果相机正在拍照流程，需等待
                Thread.Sleep(200);

            }


            isCapturing = true;


            ClearSocketBuffer(socket_2D);


            x = -999;
            y = -999;
            r = -999;

            if (socket_2D == null)
            {
                LogRecordVision.addLog($"[调用错误]: {CamNo}号相机触发失败，尚未连接视觉系统(socket is null)！");
                isCapturing = true;
                return false; // 直接退出，防止进入下面的死循环
            }

            if (!socket_2D.Connected)
            {
                LogRecordVision.addLog($"[通信错误]: {CamNo}号相机触发失败，视觉系统连接已断开！");
                isCapturing = true;
                return false;
            }

            //开始一次拍照，并等待反馈结果,默认没有动平衡点,有动平衡点返回true
            bool Res = false;          

            string TriggerNo = CamNo.ToString();

            LogRecordVision.addLog($"{TriggerNo}号相机2D定位拍照触发");
            //发送1,触发拍照1，发送2，触发拍照2
            Send2VisionMaster(TriggerNo);

            //等待VisonMaster返回结果
            DateTime startTime = DateTime.Now;

            bool GetRes = false;
            while (!GetRes)
            {

                try
                {
                    if (socket_2D.Available > 0)
                    {

                        byte[] buffer = new byte[1280];
                        //  清空缓冲区
                        Array.Clear(buffer, 0, buffer.Length);

                        int res = socket_2D.Receive(buffer);

                        if (res > 0)  //读取到数据
                        {
                            //  string strRecieve = Encoding.Unicode.GetString(buffer);
                            string strRecieve = System.Text.Encoding.ASCII.GetString(buffer,0,res);

                            //以逗号分割返回数据
                            string[] StrRes = strRecieve.Split(',');
                            strRecieve = strRecieve.Replace("\0", "");
                            LogRecordVision.addLog("收到VisionMaster发来信息:" + strRecieve);



                            if (StrRes[0] == (CamNo + 1).ToString())
                            {
                                //VM返回OK 
                                Res = true;

                            
                                string xStr = StrRes[1].Trim();
                                string yStr = StrRes[2].Trim();
                                string rStr = StrRes[3].Trim();

                                x = double.Parse(xStr);
                                y = double.Parse(yStr);
                                r = double.Parse(rStr);

                            }
                            else
                            {

                                Res = false;
                            }



                            GetRes = true;
                        }
                    }
                }
                catch (Exception ex)
                {

                    LogRecordVision.addLog("VisionMaster接收信息处理出错"+ex.Message);

                }


                TimeSpan duration = DateTime.Now - startTime; // 计算耗时
                if (duration.TotalMilliseconds > VisonMasterTimeout)
                {
                    //等待超时，日志提示，但是还是继续等待
                    LogRecordVision.addLog("VisionMaster拍照超时");

                    break;
                }
                Thread.Sleep(50);
            }

            isCapturing = false;
            return Res;
        }




    }
}
