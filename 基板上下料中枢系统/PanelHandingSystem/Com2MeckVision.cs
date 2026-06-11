using EasyModbus;
using Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace PanelHandingSystem
{


    /// <summary>
    /// 
    /// 与梅卡vision通讯，tcp 通讯做客户端
    /// 两个功能，
    /// 1.下料前检查，检查飞巴是否有掉板
    /// 2.上料前，如果陪镀板上料区域有更新，进行陪镀板检查，判断陪镀板尺寸是否在许可范围内
    /// 
    /// </summary>
    public static class Com2Vision
    {

        private static TcpClient _tcpClient;
        private static NetworkStream _stream;
        public static bool _isConnected;
        public static string _ip;
        public static int _port;

        //超时时间设置
        public static int MechVisiontimeout = 8000;//单位ms

        public static bool MechVisionError = false;



        public static bool Listeningrunning = true;



        public static void Connect()
        {

            try
            {

                _ip = ParaSet.recipe.MechVisionIPAddr;
                _port = ParaSet.recipe.MechVisionIPPort;

                _tcpClient = new TcpClient();
                _tcpClient.Connect(_ip, _port);

                if (_tcpClient.Connected)
                {
                    _isConnected = true;
                    _stream = _tcpClient.GetStream();
                    LogRecordVision.addLog("连接VISION软件," + _ip + ":" + _port.ToString());
                    //Thread receiveThread = new Thread(new ThreadStart(ReceiveData));
                    //receiveThread.Start();
                }
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("连接VISION软件出错:" + ex.Message);
            }
        }

        /// <summary>
        /// 发送板子信息给3D
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="thickness"></param>
        /// <returns></returns>
        public static bool SendPanelSizeTo3DVision(double width, double height, double thickness)
        {
            try
            {
                if (!_isConnected)
                {
                    LogRecordVision.addLog("3D未连接，无法发送尺寸数据");
                    return false;
                }
                string message = $"发送板子信息{width:F2},{height:F2},{thickness:F2}";

                Send(message);
                LogRecordVision.addLog($"向3D视觉发送板子尺寸: 宽度={width:F2}, 高度={height:F2}, 厚度={thickness:F2}");
                return true;
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("发送板子尺寸到3D视觉失败: " + ex.Message);
                return false;
            }

        }


        /// <summary>3D相机进行定位拍照，接收VISION返回的定位结果
        /// 1. 上位机向3D 发送  p15,  P16   p15触发工程15拍照，发送"p,16"，触发工程16拍照
        /// 2.Vsion 返回 1，ok, x,y，z,rx,ry,rz  后面三个数字表示机器人角度， 1，ng 逗号前的第一个数字代表工程状态，如果不为1 ，则表示工程处理出错  ,ok 或 ng 表示检测 OK 或 NG  
        /// 返回信息举例 1,ok,43.456,44.562，35.213，56.562，59.256，41.652  ,vision需发回浮点型数据 ， 单位mm
        /// </summary>
        /// 

        public static bool Start3DCaptureForPosition(int robotNo, out double x, out double y, out double z,
                                             out double rx, out double ry, out double rz)
        {
            x = y = z = rx = ry = rz = 0;

            while (isCapturing)
            {
                // 如果相机正在拍照流程，需等待
                Thread.Sleep(200);
            }

            isCapturing = true;
            bool Res = false;

            // 触发工程：p15 或 p16
            int projectNo = 14 + robotNo; 
            string ComStr = $"p,{projectNo}";

            LogRecordVision.addLog($"触发3D相机进行定位拍照, 工程:{ComStr}");
            Send(ComStr);

            DateTime startTime = DateTime.Now;
            bool GetRes = false;
            byte[] buffer = new byte[1024];

            while (!GetRes)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        LogRecordVision.addLog($"Received Form VISION: " + message);

                        string strRecieve = message;
                        try
                        {
                            string[] StrReturn = strRecieve.Split(',');

                            if (int.Parse(StrReturn[0]) != 1)
                            {
                                MechVisionError = true;
                                LogRecordVision.addLog($"3D相机工程处理出错");
                                return false;
                            }
                            else if (StrReturn[1].Contains("ok"))
                            {
                                // x,y,z,rx,ry,rz
                                x = double.Parse(StrReturn[2]);
                                y = double.Parse(StrReturn[3]);
                                z = double.Parse(StrReturn[4]);
                                rx = double.Parse(StrReturn[5]);
                                ry = double.Parse(StrReturn[6]);
                                rz = double.Parse(StrReturn[7]);

                                LogRecord.addLog($"解析到3D定位结果: x={x:F3}, y={y:F3}, z={z:F3}, rx={rx:F3}, ry={ry:F3}, rz={rz:F3}");
                                Res = true;
                            }
                            else
                            {
                                Res = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogRecordVision.addLog($"从Vision返回的结果处理出错" + ex.Message);
                        }
                        GetRes = true;
                    }
                }
                catch (Exception ex)
                {
                    LogRecordVision.addLog($"从Vision获取结果出错" + ex.Message);
                }

                TimeSpan duration = DateTime.Now - startTime;
                if (duration.TotalMilliseconds > MechVisiontimeout)
                {
                    LogRecordVision.addLog($"3D拍照超时");
                    break;
                }
                Thread.Sleep(200);
            }

            isCapturing = false;
            return Res;
        }














        //----------------
        private static void ReceiveData()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (Listeningrunning)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        LogRecordVision.addLog("Received Form VISION: " + message);

                        string strRecieve = message;

                        //处理接收到的信息
                        try
                        {

                            string[] Res = strRecieve.Split(',');

                          

                        }
                        catch (Exception ex)
                        {
                            LogRecordVision.addLog("从Vision接收数据处理出错"+ex.Message);
                        }



                    }
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("Error receiving data: " + ex.Message);

            }
            finally
            {
                _stream.Close();
                _tcpClient.Close();
            }
        }



        public static void Send(string message)
        {
            try
            {
                if (_isConnected)
                {
                    byte[] msg = Encoding.ASCII.GetBytes(message + "\r\n");
                    _stream.Write(msg, 0, msg.Length);
                    LogRecordVision.addLog("向VISION发送信息:" + message);
                }
                else
                {
                    LogRecordVision.addLog("VISION尚未建立连接");
                }

            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("VISION通讯失败" + ex.Message);

            }

        }





        //3D相机进行陪镀板尺寸确认检查
        //
        /// <summary>
        /// 3D相机拍照触发，下料前检查
        /// 3D相机触发流程如下
        /// 1.上位机向VISION 发送 p,3   p,4    p5    p6   分别触发工程3  4   5   6，
        /// 这四个工程 分别是两个机器人末端相机在陪镀板区域的拍照流程，共四个拍照位
        /// P3是1号机器人的1号拍照，P4是1号机器人的2号拍照位
        /// P5是2号机器人的1号拍照位，P6是2号机器人的2号拍照位
        /// 2.Vision 返回 1，ok, w,h,t    ,后面三个数字表示长宽厚， 1，ng 逗号前的第一个数字代表工程状态，如果不为1 ，则表示工程处理出错  ,ok 或 ng 表示检测 OK 或 NG  
        /// 返回信息举例 1,ok,43,44,35  ,vision需发回整型数据 ， 单位mm
        /// </summary>
        /// <param name="CamNo"></param>
        /// 输出算法算出来的陪镀板宽度和高度
        /// <returns></returns>
        /// 


        public static bool Start3DCapturePeiDuCheck(int CamNo, int Postison, out int PeiduWidth, out int PeiduHeight)
        {

            while (isCapturing)
            {

                //如果相机正在拍照流程，需等待
                Thread.Sleep(200);

            }

            isCapturing = true;
            bool Res = false;
            PeiduWidth = -1;
            PeiduHeight = -1;


            LogRecordVision.addLog($"触发3D相机{CamNo}进行一次上料前陪镀板3D检查拍照,拍照位置:{Postison}");
            //触发工程1
            string ComStr = $"p,{(CamNo-1)*2 + 2 + Postison}"; // p,3 和 p,4分别触发工程3和工程4
            Send(ComStr);
            //相机开始拍照，并等待一定时间，返回检查结果

            DateTime startTime = DateTime.Now;

            bool GetRes = false;
            byte[] buffer = new byte[1024];
            while (!GetRes)
            {
                try
                {
                    //从Vision 接收信息

                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        LogRecordVision.addLog($" 相机{CamNo},Received Form VISION: " + message);
                        LogRecord.addLog($" 相机{CamNo},Received Form VISION: " + message);
                        string strRecieve = message;
                        //处理接收到的信息
                        try
                        {

                            //TODO：修改以下内容
                            string[] StrReturn = strRecieve.Split(',');

                            if (int.Parse(StrReturn[0]) != 1)
                            {
                                MechVisionError = true;
                                LogRecordVision.addLog($"3D相机{CamNo},Vison工程处理出错");
                                //出错当NG处理，返回false
                                return false;
                            }
                            else if (StrReturn[1].Contains("ok"))
                            {

                                //分解陪镀板长宽
                                int w = int.Parse(StrReturn[2]);
                                int h = int.Parse(StrReturn[3]);
                                PeiduWidth = w;
                                PeiduHeight = h;

                                LogRecord.addLog($"解析到陪镀板宽度:{w},陪镀板高度:{h}");

                                //3D拍照结果OK
                                Res = true;

                            }
                            else
                            {
                                //3D拍照结果NG
                                Res = false;
                            }


                        }
                        catch (Exception ex)
                        {

                            LogRecordVision.addLog($"3D相机{CamNo},从Vision返回的结果处理出错" + ex.Message);
                        }

                        GetRes = true;
                    }

                }
                catch (Exception ex)
                {
                    LogRecordVision.addLog($"3D相机{CamNo},从Vision获取结果出错" + ex.Message);
                }

                TimeSpan duration = DateTime.Now - startTime; // 计算耗时
                if (duration.TotalMilliseconds > MechVisiontimeout)
                {
                    //等待超时，日志提示，但是还是继续等待
                    LogRecordVision.addLog($"3D相机{CamNo},拍照超时");

                    break;
                }
                Thread.Sleep(200);
            }


            isCapturing = false;
            return Res;
        }





        //
        /// <summary>
        /// 3D相机拍照触发，下料前检查
        /// 3D相机触发流程如下
        /// 1.上位机向VISION 发送 p,1   和  p,2分别触发工程1和工程2，工程1 和工程2分贝是两个机器人末端相机拍照流程
        /// 2.Vision 返回 1，ok   或 1，ng 逗号前的第一个数字代表工程状态，如果不为1 ，则表示工程处理出错  ,ok 或 ng 表示检测 OK 或 NG
        /// 3.20260310修改 ，触发 P11,P12  ,P13,P14  分别代表
        /// 1号机器人前飞巴拍照  P11
        /// 1号机器人后飞巴拍照  P12
        /// 2号机器人前飞巴拍照  P13
        /// 2号机器人后飞巴拍照  P14
        /// </summary>
        /// <param name="CamNo"></param>
        /// <returns></returns>
        public static bool isCapturing = false;
        public static bool Start3DCaptureXiaLiao( int CamNo, int RowNo)
        {

            while (isCapturing)
            {

                //如果相机正在拍照流程，需等待
                Thread.Sleep(200);

            }

            isCapturing = true;
            bool Res = false;
   

   
            //触发工程

            string ComStr = $"p,1{((CamNo-1)*2)+ RowNo  }"; // p,1 和 p,2分别触发工程1和工程2
            LogRecordVision.addLog($"触发3D相机{CamNo}进行一次下料前3D检查拍照:{ComStr}");
            Send(ComStr);
            //相机开始拍照，并等待一定时间，返回检查结果
           
            DateTime startTime = DateTime.Now;

            bool GetRes = false;
            byte[] buffer = new byte[1024];
            while (!GetRes)
            {    
                try
                {
                    //从Vision 接收信息

                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        LogRecordVision.addLog($" 相机{CamNo},Received Form VISION: " + message);

                        string strRecieve = message;
                        //处理接收到的信息
                        try
                        {
                            string[] StrReturn = strRecieve.Split(',');

                            if (int.Parse(StrReturn[0]) != 1)
                            {
                                MechVisionError = true;
                                LogRecordVision.addLog($"3D相机{CamNo},Vison工程处理出错");
                                //出错当NG处理，返回false
                                return false;
                            }
                            else if (StrReturn[1].Contains("ok"))
                            {
                                //3D拍照结果OK
                                Res = true;

                            }
                            else
                            {
                                //3D拍照结果NG
                                Res = false;
                            }
                          

                        }
                        catch (Exception ex)
                        {

                            LogRecordVision.addLog($"3D相机{CamNo},从Vision返回的结果处理出错" + ex.Message);
                        }

                        GetRes = true;
                    }
                  
                }
                catch(Exception ex)
                {
                    LogRecordVision.addLog($"3D相机{CamNo},从Vision获取结果出错" +ex.Message);
                }

                TimeSpan duration = DateTime.Now - startTime; // 计算耗时
                if (duration.TotalMilliseconds > MechVisiontimeout)
                {
                    //等待超时，日志提示，但是还是继续等待
                    LogRecordVision.addLog($"3D相机{CamNo},拍照超时");
                 
                    break;
                }
                Thread.Sleep(200);
            }
            isCapturing = false;
            return Res;

        }


        //3D相机进行飞巴定位拍照
        //
        /// <summary>
        /// 3D相机拍照触发，
        /// 3D相机触发流程如下
        /// 1.上位机向VISION 发送 p,7   p,8     分别触发工程 7  8，
        /// 这2个工程 分别是两个机器人末端相机在在飞巴定位拍照位拍照并返回结果的程序，
        /// P7是1号机器人的飞巴定位拍照位
        /// P8是2号机器人的飞巴定位拍照位
        /// 2.Vision 返回 1，ok, dx,0,0    ,后面三个数字中的第一个表示偏移值， 1，ng 逗号前的第一个数字代表工程状态，如果不为1 ，则表示工程处理出错  ,ok 或 ng 表示检测 OK 或 NG  
        /// 返回信息举例 1,ok,-2.5,0,0  ,vision需发回浮点型数据 ， 单位mm，表示偏移-2.5mm
        /// </summary>
        /// <param name="CamNo"></param>
        /// 
        /// <returns></returns>
        /// 


        public static bool Start3DCaptureFeiBaPosition(int CamNo, out float PositionDX )
        {
            //3D相机飞巴定位拍照
            while (isCapturing)
            {
                //如果相机正在拍照流程，需等待
                LogRecordVision.addLog("等待上一次3D处理结束");
                Thread.Sleep(500);
            }
            isCapturing = true;
            bool Res = false;

            PositionDX = -999;

            LogRecordVision.addLog($"触发3D相机{CamNo}进行下料前3D飞巴定位拍照");
            //触发工程1
            string ComStr = $"p,{CamNo+6}"; // p,7 和 p,8分别触发工程1和工程2，这里用P7,P8
            LogRecordVision.addLog($"触发3D相机{ComStr}");
            Send(ComStr);
            //相机开始拍照，并等待一定时间，返回检查结果

            DateTime startTime = DateTime.Now;

            bool GetRes = false;
            byte[] buffer = new byte[1024];
            while (!GetRes)
            {
                try
                {
                    //从Vision 接收信息

                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        LogRecordVision.addLog($" 相机{CamNo},Received Form VISION: " + message);

                        string strRecieve = message;
                        //处理接收到的信息
                        try
                        {
                            string[] StrReturn = strRecieve.Split(',');

                            if (int.Parse(StrReturn[0]) != 1)
                            {
                                MechVisionError = true;
                                LogRecordVision.addLog($"3D相机{CamNo},Vison工程处理出错");
                                //出错当NG处理，返回false
                                return false;
                            }
                            else if (StrReturn[1].Contains("ok"))
                            {
                                //3D拍照结果OK
                                //分解陪镀板长宽
                                float dx = float.Parse(StrReturn[2]);

                                PositionDX = dx;
                              
                                LogRecord.addLog($"解析到飞巴偏移值:{dx}");
                                //3D拍照结果OK
                                Res = true;
                            }
                            else
                            {
                                //3D拍照结果NG
                                Res = false;
                            }


                        }
                        catch (Exception ex)
                        {

                            LogRecordVision.addLog($"3D相机{CamNo},从Vision返回的结果处理出错" + ex.Message);
                        }

                        GetRes = true;
                    }

                }
                catch (Exception ex)
                {
                    LogRecordVision.addLog($"3D相机{CamNo},从Vision获取结果出错" + ex.Message);
                }

                TimeSpan duration = DateTime.Now - startTime; // 计算耗时
                if (duration.TotalMilliseconds > MechVisiontimeout)
                {
                    //等待超时，日志提示，但是还是继续等待
                    LogRecordVision.addLog($"3D相机{CamNo},拍照超时");

                    break;
                }
                Thread.Sleep(200);
            }
            isCapturing = false;
            return Res;

        }





    }
}
