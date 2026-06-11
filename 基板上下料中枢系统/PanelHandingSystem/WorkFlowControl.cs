

using FolderKeeper;
using HalconDotNet;
using Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static PanelHandingSystem.Com2Mes;
using static PanelHandingSystem.ParaSet;
namespace PanelHandingSystem
{

    //核心流程控制类
    public static class WorkFlowControl
    {
        public static HTuple WindowHandle;

        /// <summary>
        /// MES 数据采集
        /// </summary>
        private static Thread _mesDataReadingThread;
        private static bool _mesDataThreadRunning = false;
        private static readonly object _mesDataLock = new object();
        public static Dictionary<int, object> MesDataValues = new Dictionary<int, object>();  //存储实际值


        public delegate bool ReadHoldingRegistersDelegate(int startAddr, int length, out int[] values);
        public static ReadHoldingRegistersDelegate ReadHoldingRegisters;





        /// <summary>
        /// //是否离线调试，离线调试具备以下特征
        /// 1.前后飞巴码分别为001,和002，对应飞巴文件001和002,需提前修改文件内容
        /// 2.按顺序生成0001,0002,0003,0004,0005,0006 - 0012 序号的板码
        /// 3.加载离线测试工单文件
        /// </summary>
        public static bool isOfflineDebug = false;
        //离线时的飞巴文件名
        public static string offlineFeiBaFile1 = "";
        public static string offlineFeiBaFile2 = "";

        //下料调试时，需要区分两个机器人
        public static bool TestRobot1 = false;
        public static bool TestRobot2 = false;


        public static bool CancelXiaLiaoInspect = false;
        public static bool panelNoCode = false;//主板无码状态，生成模拟码

        //6个扫码枪
        public static BarcodeScan bacodeScan1 = new BarcodeScan();
        public static BarcodeScan bacodeScan2 = new BarcodeScan();


        public static BarcodeScan bacodeScan3 = new BarcodeScan();
        public static BarcodeScan bacodeScan4 = new BarcodeScan();


        public static BarcodeScan bacodeScan5 = new BarcodeScan();
        public static BarcodeScan bacodeScan6 = new BarcodeScan();



        //一个PLC汇总
        public static Com2PLC com2PLC = new Com2PLC();


        //两个机器人
        public static Com2RobotM robot1 = new Com2RobotM();
        public static Com2RobotM robot2 = new Com2RobotM();





        /// <summary>
        /// 状态计数
        /// </summary>
        /// <returns></returns>

        public static int CountShangBan = 0;//上板计数
        public static int CountXiaBan = 0;//下板计数
        public static int CountWorkOrder = 0;//工单计数


        //当前流程状态,每次进入新的状态时更新
        public static string Statues = "null";


        //工单列表
        public static Queue<WorkOrder> WorkOrderQueue = new Queue<WorkOrder>();

        //当前工单
        public static WorkOrder WorkOrderCurrent = new WorkOrder();


        //当前飞巴  前
        public static FeiBaWorker FeiBaCurrentQian = new FeiBaWorker();

        //当前飞巴  后
        public static FeiBaWorker FeiBaCurrentHou = new FeiBaWorker();


        //当前处理的板子信息  ,传送带1，扫码时入队列，上料拍照时出队列
        public static Queue<FeiBaPanelInfo> PanelInfoList1 = new Queue<FeiBaPanelInfo>();

        //当前处理的板子信息  ,传送带2，扫码时入队列，上料拍照时出队列
        public static Queue<FeiBaPanelInfo> PanelInfoList2 = new Queue<FeiBaPanelInfo>();

        public static string CurrentStatues = "null";


        //当前飞巴 排版信息

        public static double BaseAdj;
        public static LinkedList<double> PosXQueue;
        public static LinkedList<double> PosYQueue;
        public static Queue<double> PosPeiDuXQueue;
        public static Queue<double> PosPeiDuYQueue;




        public static string CurrentPanel = "null";
        public static int CountXialiao = 0;
        public static int CountShangliao = 0;
        public static int CountWorkOrderDone = 0;
        public static string CurrentWorkOrderDoneRatio = "null";//当前工单完成率字符串


        //飞巴文件路径
        public static string PathFeibaInfo = "FeiBaLog\\";

        //----扫码枪-------------
        public static Action<string> OnHandheldScanSuccess;



        public static void SendWorkOrder2PLC(WorkOrder wo)
        {

            LogRecord.addLog($"工单号{wo.lot_num}:计算工单排版，并向PLC写入工单主板数据");
            //向PLC写入工单数量与左右缓存数据

            int Res = WorkFlowControl.CalPaiBan(wo.pallet_width, wo.pallet_length, wo.pallet_thickness,
                                                               out double PeiduWidth, out double PeiduHeight, out double peiduT, out double BaseAdj, out LinkedList<double> PosXQueue, out LinkedList<double> PosYQueue,
                                                               out Queue<double> PosPeiDuXQueue, out Queue<double> PosPeiDuYQueue);


            //计算缓存数量
            int keyX1 = com2PLC.DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前工单主板数量")).Key;
            com2PLC.writeDataD(keyX1, wo.pnlqty);
            LogRecord.addLog($"工单{wo.lot_num}，主板数量:{wo.pnlqty},已写入PLC ");


            int CountFeibaPanel = PosXQueue.Count;

            //左右分配
            //修改成右边多，左边少
            int ShangLiaoCount1Temp = (CountFeibaPanel) / 2;

            //修改
            // int ShangLiaoCount2Temp = CountFeibaPanel - ShangLiaoCount1All;
            int ShangLiaoCount2Temp = CountFeibaPanel - ShangLiaoCount1Temp;

            //计算左边缓存数量

            int LoopCount = wo.pnlqty / CountFeibaPanel;

            //LoopCount要是偶数  
            LoopCount = (LoopCount % 2 != 0) ? LoopCount - 1 : LoopCount;

            int WeiBan = wo.pnlqty - CountFeibaPanel * LoopCount;

            int ZuoHuanCunCount = ShangLiaoCount1Temp * LoopCount;

            //修改
            int YouHuanCunCount = ShangLiaoCount2Temp * LoopCount;
            //int YouHuanCunCount = wo.pnlqty - ZuoHuanCunCount;

            int keyX2 = com2PLC.DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前工单缓存机1上料数量")).Key;
            com2PLC.writeDataD(keyX2, ZuoHuanCunCount);
            LogRecord.addLog($"工单{wo.lot_num}，左缓存总数量:{ZuoHuanCunCount},已写入PLC ");

            int keyX3 = com2PLC.DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前工单缓存机2上料数量")).Key;
            com2PLC.writeDataD(keyX3, YouHuanCunCount);
            LogRecord.addLog($"工单{wo.lot_num}，右缓存总数量:{YouHuanCunCount},已写入PLC ");


        }



        /// <summary>
        /// 检查主板中是否有边夹配镀板
        /// </summary>
        /// <param name="wo"></param>
        public static bool CheckPeiDuAnnle(double panelWidth)
        {

            if (panelWidth >= 419 && panelWidth <= 444)
            {
                LogRecord.addLog($"主板宽度是{panelWidth}，需要陪镀板增加夹点(尺寸440-444)");
                return true;
            }
            else if (panelWidth >= 445 && panelWidth <= 452)
            {
                LogRecord.addLog($"主板宽度是{panelWidth}，需要陪镀板增加夹点(尺寸445-452)");
                return true;
            }
            else if (panelWidth >= 479 && panelWidth <= 509)
            {
                LogRecord.addLog($"主板宽度是{panelWidth}，需要陪镀板增加夹点(尺寸501-509)");
                return true;
            }
            else if (panelWidth >= 510 && panelWidth <= 528)
            {
                LogRecord.addLog($"主板宽度是{panelWidth}，需要陪镀板增加夹点(尺寸512-528)");
                return true;
            }
            else if (panelWidth >= 559 && panelWidth <= 595)
            {
                LogRecord.addLog($"主板宽度是{panelWidth}，需要陪镀板增加夹点(尺寸590-595)");
                return true;
            }
            else if (panelWidth >= 596 && panelWidth <= 630)
            {
                LogRecord.addLog($"主板宽度是{panelWidth}，需要陪镀板增加夹点(尺寸616-630)");
                return true;
            }
            return false;
        }


        /// <summary>
        /// 检查当前工单是否需要
        /// </summary>
        /// <param name="wo"></param>
        public static bool CheckWorkOrderAngle(WorkOrder wo)
        {
            if (wo == null)
            {
                return false;
            }
            return CheckPeiDuAnnle(wo.pallet_width);
        }


        public static void EnqueueWorkOrderList(WorkOrder wo)
        {



            /* if (CheckWorkOrderAngle(wo))
             {
                 LogRecord.addLog($"检测到工单{wo.lot_num}主板宽度{wo.pallet_width},需要配镀板夹边,需人工处理");


                 com2PLC.writeCoil(85, true);
                 return;
             }*/

            //向工单队列加入工单数据
            LogRecord.addLog($"工单号{wo.lot_num}:进入工单队列");

            if(wo.BanDianOrTuDian ==0)
            {
                LogRecord.addLog($"当前工单为图电工单");
            }
            else
            {
                LogRecord.addLog($"当前工单为板电工单");
            }
           
            WorkOrderQueue.Enqueue(wo);
        }


        public static T GetElementAtIndex<T>(this LinkedList<T> list, int index)
        {
            if (index < 0 || index >= list.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            LinkedListNode<T> current;
            int size = list.Count;
            int distanceFromHead = index;
            int distanceFromTail = size - 1 - index;

            // 选择遍历方向（头部或尾部）
            if (distanceFromHead <= distanceFromTail)
            {
                current = list.First;
                for (int i = 0; i < distanceFromHead; i++)
                {
                    current = current.Next;
                }
            }
            else
            {
                current = list.Last;
                for (int i = 0; i < distanceFromTail; i++)
                {
                    current = current.Previous;
                }
            }
            return current.Value;
        }


        //根据扫码结果获取工单

        public static void RunHandheldScanProcess(string code)
        {

            //LogRecord.addLog("开始等待手持扫码...");
            //string barcode = bacodeScan1.StarBarcode(false);

            //if (barcode == "NoRead" || barcode == "Error" || barcode.StartsWith("Error_"))
            //{
            //    LogRecord.addLog("手持扫码失败或超时");

            //    return;
            //}
            //LogRecord.addLog($"手持扫码成功: {barcode}");


            var workOrder = Com2Mes.GetworkOrder(code);

            if (workOrder != null)
            {

                if (CheckWorkOrderAngle(workOrder))
                {
                    LogRecord.addLog($"工单{workOrder.lot_num}主板宽度{workOrder.pallet_width},需要配镀板夹边");

                    com2PLC.writeCoil(85, true);
                    return;
                }


                // WorkOrderCurrent = workOrder;
                LogRecord.addLog("进入工单队列");
                WorkFlowControl.EnqueueWorkOrderList(workOrder);

                //if (OnHandheldScanSuccess != null)
                //{
                //    OnHandheldScanSuccess(code);
                //}

                LogRecord.addLog($"工单 {workOrder.lot_num} 已加载");

                //进入工单队列




                LogRecord.addLog($"工单 {workOrder.lot_num} 已进入工单队列");
            }
            else
            {
                LogRecord.addLog($"条码 {code} 未能获取到有效工单");

            }
        }





        public class FeiBaPanelInfo
        {
            //飞巴上面的板子信息
            public string PanelNo = "null";

            //板子从左往右的序号
            public int ColNo = 0;
            //板子坐标
            public double posX = -1;
            public double posY = -1;


            //板子宽度
            public double PanelWidth = -1;
            //板子高度
            public double PanelHeight = -1;

            //板子厚度
            public double PanelThickness = -1;
            //板子重量
            public double PanelWeight = -1;

        }


        //飞巴上的板子信息
        public class FeiBaWorker
        {
            //飞巴编号，扫码获得
            public string FeiBaNo = "null";
            public int PanelCount = 0;
            //飞巴上面绑定的板子条码
            public Queue<FeiBaPanelInfo> PanelList = new Queue<FeiBaPanelInfo>();

            //主板信息
            //板子宽度
            public double PanelWidth = -1;
            //板子高度
            public double PanelHeight = -1;
            //板子厚度
            public double PanelThickness = -1;
            //板子重量
            public double PanelWeight = -1;

            //主板坐标队列,双向队列，改成数组，不要用队列
            public LinkedList<double> PosXQueue = new LinkedList<double>();
            public LinkedList<double> PosYQueue = new LinkedList<double>();


            //是否有陪镀板
            public bool PeiDuBanExist = false;

            //是否尾料
            public bool WeiLiao = false;

            //陪镀板1坐标
            public double PeiDuposX1 = -1;
            public double PeiDuposY1 = -1;

            //陪镀板2坐标
            public double PeiDuposX2 = -1;
            public double PeiDuposY2 = -1;

            //陪镀板重量和厚度,重量单位g,这个数据保留默认值
            public double PeiDuThickness = -1;
            public double PeiDuWeight = -1;
            //陪镀板高度
            public double PeiDuHeight = -1;
            public double PeiDuWidth = -1;
            //飞巴高度调整值
            public double FeiBaAdaptionValue = -1;

            //当前飞巴是前飞巴还是后飞巴
            public int Row = -1;

            //上料计数 ,左右两个方向的上料次数分别计数
            public int ShangLiaoCount1 = 0;
            public int ShangLiaoCount2 = 0;





            /// <summary>
            /// ////以下为方法
            /// </summary>
            /// <returns></returns>


            public void PrintFeiBaWorker()
            {
                //打印飞巴信息
                string FeiBaInfoStr = $"FeiBaNo:{FeiBaNo}" +
                                      $"--Row:{Row}" +
                                      $"--PanelCount:{PanelCount}" +
                                      $"--PanelWidth:{PanelWidth}" +
                                      $"--PeiDuWidth:{PeiDuWidth}" +
                                      $"--ShangLiaoCount1:{ShangLiaoCount1}" +
                                      $"--ShangLiaoCount2:{ShangLiaoCount2}";


                LogRecord.addLog(FeiBaInfoStr);


            }


            /// <summary>
            /// 根据 ColNo 对板子信息队列进行升序排序
            /// </summary>
            /// <param name="panelList">排序前的队列</param>
            /// <returns>排序后的新队列</returns>
            public static Queue<FeiBaPanelInfo> SortPanelQueue(Queue<FeiBaPanelInfo> panelList)
            {
                if (panelList == null || panelList.Count == 0)
                    return new Queue<FeiBaPanelInfo>();

                // 1. 使用 LINQ 进行排序
                // OrderBy 会根据 ColNo 进行升序排列
                var sortedItems = panelList.OrderBy(p => p.ColNo).ToList();

                // 2. 将排序后的列表重新包装成新的 Queue
                return new Queue<FeiBaPanelInfo>(sortedItems);
            }


            public static FeiBaWorker GenFeiBaWorkerTest(int row)
            {

                FeiBaWorker feibaWorker = new FeiBaWorker();

                DateTime startTime = DateTime.Now;
                string str = "feibanum001" + startTime.ToString("_MM_dd_HH_mm_ss_fff");
                //以当前系统时间生成编号
                feibaWorker.FeiBaNo = str;
                feibaWorker.PeiDuBanExist = true;
                feibaWorker.PeiDuposX1 = 213.5;
                feibaWorker.PeiDuposY1 = 421;

                feibaWorker.PeiDuposX2 = 234.2;
                feibaWorker.PeiDuposY2 = 342.6;

                feibaWorker.PeiDuThickness = 3;
                feibaWorker.PeiDuWeight = 2000;
                feibaWorker.PanelCount = 6;
                feibaWorker.ShangLiaoCount1 = 3;
                feibaWorker.ShangLiaoCount2 = 3;


                feibaWorker.Row = row;

                for (int i = 0; i < feibaWorker.PanelCount; i++)
                {
                    FeiBaPanelInfo Panelinfo = new FeiBaPanelInfo();
                    DateTime startTime1 = DateTime.Now;
                    string str1 = "lot_num" + startTime1.ToString("_MM_dd_HH_mm_ss_fff");

                    Panelinfo.PanelNo = str1;


                    Panelinfo.PanelThickness = 1;
                    Panelinfo.PanelWeight = 1000;
                    Panelinfo.posX = 20.5;
                    Panelinfo.posY = 40.5;

                    feibaWorker.PanelList.Enqueue(Panelinfo);

                    Thread.Sleep(4);
                }


                return feibaWorker;
            }


            public static void SavefeibaWorker(FeiBaWorker fbworker)
            {
                //保存飞巴文件到硬盘

                string jsonstr = Obj2Json<FeiBaWorker>(fbworker);

                DateTime startTime1 = DateTime.Now;

                //加个时间前缀，避免覆盖，需要的时候可以按修改时间排序
                string str = startTime1.ToString("MM-dd-HH-mm-ss-fff-");
                string strInfo = $"{fbworker.PanelWidth}_{fbworker.PanelHeight}_{fbworker.PanelThickness}";
                string path = PathFeibaInfo + str + strInfo + "_R" + fbworker.Row.ToString() + "_" + fbworker.FeiBaNo + ".json";
                SaveJsonFile(path, jsonstr);

                LogRecord.addLog($"飞巴文件已保存到{path}");


                if (fbworker.Row == 1)
                {
                    offlineFeiBaFile1 = path;
                }

                if (fbworker.Row == 2)
                {
                    offlineFeiBaFile2 = path;
                }
            }


            public static void GetLatestFeiBaFile()
            {

                //遍历文件夹，读取最近的两个飞巴文件，赋值全局飞巴变量


                try
                {
                    string FilePath = PathFeibaInfo;

                    (string First, string Second) = FolderSpaceKeeper.GetTwoMostRecentFiles(PathFeibaInfo);


                    string jsonContent1 = File.ReadAllText(First);
                    FeiBaWorker feibaWorker1 = Json2Obj<FeiBaWorker>(jsonContent1);


                    string jsonContent2 = File.ReadAllText(Second);
                    FeiBaWorker feibaWorker2 = Json2Obj<FeiBaWorker>(jsonContent2);

                    if (feibaWorker1.Row == 1)
                    {

                        FeiBaCurrentQian = feibaWorker1;
                        LogRecord.addLog($"最近前飞巴号码{feibaWorker1.FeiBaNo}");
                        LogRecord.addLog($"前飞巴文件名{First}");

                        FeiBaCurrentHou = feibaWorker2;
                        LogRecord.addLog($"最近后飞巴号码{feibaWorker2.FeiBaNo}");
                        LogRecord.addLog($"后飞巴文件名{Second}");

                    }
                    else
                    {
                        FeiBaCurrentQian = feibaWorker2;
                        LogRecord.addLog($"最近前飞巴号码{FeiBaCurrentQian.FeiBaNo}");
                        LogRecord.addLog($"前飞巴文件名{Second}");

                        FeiBaCurrentHou = feibaWorker1;
                        LogRecord.addLog($"最近后飞巴号码{feibaWorker1.FeiBaNo}");
                        LogRecord.addLog($"后飞巴文件名{First}");
                    }

                    FeiBaCurrentQian.PrintFeiBaWorker();
                    FeiBaCurrentHou.PrintFeiBaWorker();


                }
                catch (Exception ex)
                {
                    LogRecord.addLog($"打开最近修改的飞巴文件出错{ex.Message}");
                }





            }



            //public static FeiBaWorker GetFeiBaFile(string fileName)
            //{

            //    //根据文件名，载入飞巴信息
            //    FeiBaWorker feibaWorker = new FeiBaWorker();




            //    return feibaWorker;
            //}



        }

        /// <summary>
        /// 获取指定文件夹中包含指定字符串且修改时间最近的文件路径，用于获取当前飞巴上已经保存的绑定信息
        /// </summary>
        /// <param name="folderPath">目标文件夹路径</param>
        /// <param name="searchPattern">文件名包含的字符串</param>
        /// <returns>符合条件的最新文件路径，未找到则返回null</returns>
        public static string GetLatestFileWithPattern(string folderPath, string searchPattern)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("文件夹路径不能为空或空白字符");

            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"目录不存在: {folderPath}");

            if (string.IsNullOrEmpty(searchPattern))
                throw new ArgumentException("搜索字符串不能为空");

            DateTime latestTime = DateTime.MinValue;
            string latestFile = null;

            // 使用通配符构建搜索模式
            string searchExpression = $"*{searchPattern}*";

            try
            {
                // 遍历所有子目录中的文件
                foreach (string filePath in Directory.EnumerateFiles(folderPath, searchExpression, SearchOption.AllDirectories))
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        if (fileInfo.LastWriteTime > latestTime)
                        {
                            latestTime = fileInfo.LastWriteTime;
                            latestFile = fileInfo.FullName;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // 忽略无访问权限的文件
                        continue;
                    }
                    catch (IOException)
                    {
                        // 忽略无法访问的文件（如正在被占用的文件）
                        continue;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException($"无权限访问目录: {folderPath}");
            }
            catch (IOException)
            {
                throw new IOException($"无法访问目录: {folderPath}");
            }

            return latestFile;
        }




        public static string ToStringLinq(this LinkedList<double> list, string separator = ", ", string format = "G")
        {
            return string.Join(separator, list.Select(d => d.ToString(format)));
        }

        public static string ToStringLinq(this Queue<double> queue, string separator = ", ", string format = "F2")
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            // 将队列元素转换为指定格式的字符串数组
            var formattedElements = queue.Select(d => d.ToString(format));

            // 使用分隔符拼接字符串
            return string.Join(separator, formattedElements);
        }


        //流程初始化，连接所有设备
        public static int Init()
        {
            int Res = -1;

            try
            {
                LogRecord.addLog("开始连接所有外设");

                Task task = Task.Run(() =>
                {
                    ConnectPLC();

                    //通知机器人，和PLC,软件已经重启
                    com2PLC.writeCoil(90, true);
                });

                Task task1 = Task.Run(() =>
                {
                    ConnectRobot(1);
                    robot1.writeCoil(80, true);
                });

                Task task2 = Task.Run(() =>
                {
                    ConnectRobot(2);
                    robot2.writeCoil(80, true);
                    robot2.writeCoil(80, true);

                });

                Task task3 = Task.Run(() =>
                {
                    ConnectBarCodeScaner(1);
                });

                Task task4 = Task.Run(() =>
                {
                    ConnectBarCodeScaner(2);
                });

                Task task5 = Task.Run(() =>
                {
                    ConnectBarCodeScaner(3);

                });

                Task task6 = Task.Run(() =>
                {
                    ConnectBarCodeScaner(4);
                });

                Task task7 = Task.Run(() =>
                {
                    Com2VisionMaster.Connect();
                });

                Task task8 = Task.Run(() =>
                {
                    Com2Vision.Connect();
                });

                Task task9 = Task.Run(() =>
                {
                    Com2ForceControl.Connect();
                });


            }
            catch (Exception ex)
            {

                LogRecord.addLog("连接外设出错" + ex.Message);
            }




            return Res;
        }


        public static int ConnectPLC()
        {

            int Res = -1;

            Res = com2PLC.ConnectPLC();


            if (Res < 0)
            {
                LogRecord.addLog("PLC连接失败");
            }



            return Res;

        }



        //PLC信号监听线程








        public static int ConnectBarCodeScaner(int BarCodeNo)
        {

            int Res = -1;
            if (BarCodeNo == 1)
            {

                string IPAddr = ParaSet.recipe.BarCodeIPAddr1;
                int port = ParaSet.recipe.BarCodeIPPort1;
                bacodeScan1.Connect(IPAddr, port, BarCodeNo);
                Res = 1;
            }

            if (BarCodeNo == 2)
            {

                string IPAddr = ParaSet.recipe.BarCodeIPAddr2;
                int port = ParaSet.recipe.BarCodeIPPort2;
                bacodeScan2.Connect(IPAddr, port, BarCodeNo);
                Res = 1;
            }



            if (BarCodeNo == 3)
            {

                string IPAddr = ParaSet.recipe.BarCodeIPAddr3;
                int port = ParaSet.recipe.BarCodeIPPort3;
                bacodeScan3.Connect(IPAddr, port, BarCodeNo);
                Res = 1;
            }

            if (BarCodeNo == 4)
            {

                string IPAddr = ParaSet.recipe.BarCodeIPAddr4;
                int port = ParaSet.recipe.BarCodeIPPort4;
                bacodeScan4.Connect(IPAddr, port, BarCodeNo);
                Res = 1;
            }

            if (BarCodeNo == 5)
            {

                string IPAddr = ParaSet.recipe.BarCodeIPAddr5;
                int port = ParaSet.recipe.BarCodeIPPort5;
                bacodeScan5.Connect(IPAddr, port, BarCodeNo);
                Res = 1;
            }

            if (BarCodeNo == 6)
            {

                string IPAddr = ParaSet.recipe.BarCodeIPAddr6;
                int port = ParaSet.recipe.BarCodeIPPort6;
                bacodeScan6.Connect(IPAddr, port, BarCodeNo);
                Res = 1;
            }


            return Res;

        }


        public static string StartBarcode(int BarCodeNo)
        {

            string Res = "null";
            BarcodeScan.ManualTrigger = true;
            if (BarCodeNo == 1)
            {
                Res = bacodeScan1.StarBarcode();
            }

            if (BarCodeNo == 2)
            {
                Res = bacodeScan2.StarBarcode();
            }


            if (BarCodeNo == 3)
            {
                Res = bacodeScan3.StarBarcode();
            }



            if (BarCodeNo == 4)
            {
                Res = bacodeScan4.StarBarcode();
            }

            if (BarCodeNo == 5)
            {
                Res = bacodeScan5.StarBarcode();
            }

            if (BarCodeNo == 6)
            {
                Res = bacodeScan6.StarBarcode();
            }

            BarcodeScan.ManualTrigger = false;
            return Res;

        }



        public static int ConnectRobot(int RobotNo)
        {

            int Res = -1;
            if (RobotNo == 1)
            {

                string IPAddr = ParaSet.recipe.RobotIPAddr1;
                int port = ParaSet.recipe.RobotIPPort1;
                Res = robot1.Connect(IPAddr, port, RobotNo);

            }

            if (RobotNo == 2)
            {

                string IPAddr = ParaSet.recipe.RobotIPAddr2;
                int port = ParaSet.recipe.RobotIPPort2;
                Res = robot2.Connect(IPAddr, port, RobotNo);

            }
            return Res;

        }


        public static int ConnectForceControl(int ForceNo)
        {
            int Res = -1;
            if (ForceNo == 1)
            {
                //ToDo


                Res = 1;
            }

            if (ForceNo == 2)
            {

                //ToDo


                Res = 1;
            }
            return Res;

        }





        //计算排版
        public static int CalPaiBan(double PanelWidth, double PanelHeight, double PanelThickness,
            out double PeiDuWidth, out double PeiDuHeight, out double PeiduThickness,
            out double BaseAdj, out LinkedList<double> PosXQueue, out LinkedList<double> PosYQueue,
            out Queue<double> PosPeiDuXQueue, out Queue<double> PosPeiDuYQueue,
            int BanDianOrTuDian = 0)  //图电和板点指示，默认为0 ，0表示图电，1表示板点

        {

            int Res = -1;

            PosXQueue = new LinkedList<double>();
            PosYQueue = new LinkedList<double>();
            PosPeiDuXQueue = new Queue<double>();
            PosPeiDuYQueue = new Queue<double>();
            PeiDuWidth = -1;
            BaseAdj = -1;

            PeiDuHeight = -1;


            PeiduThickness = -1;


            try
            {

                HOperatorSet.ClearWindow(WindowHandle);


                //根据当前板子宽度，计算



                HTuple hv_PlatePosX, hv_PlatePosY, hv_PeiDuPosX, hv_PeiDuPosY;

                if (BanDianOrTuDian == 0)
                {


                    //计算当前陪镀板的宽度
                    TypeSetAlgorithm.CalBestPeiduWidthTu(ParaSet.recipe.TotalWidth, ParaSet.recipe.TotalHeight, PanelWidth, PanelHeight,
                                    ParaSet.recipe.FullThres, ParaSet.recipe.PanelInterval, out HTuple PeiDuWidth_out, out HTuple PeiDuHeight_out,
                                    out HTuple hv_PanelCount, out HTuple PeiDuExist);


                    TypeSetAlgorithm.CalBaseAdjustment(PanelWidth, PanelHeight, PanelThickness, out HTuple BaseAdjustVal);

                    PeiDuWidth = PeiDuWidth_out[0].D;
                    LogRecord.addLog($"陪镀板宽度计算结果:{PeiDuWidth}");


                    PeiDuHeight = PeiDuHeight_out[0].D;
                    LogRecord.addLog($"陪镀板高度计算结果:{PeiDuHeight}");


                    PeiduThickness = 2;
                    LogRecord.addLog($"陪镀板厚度度计算结果:{PeiduThickness}");

                    BaseAdj = BaseAdjustVal[0].D;
                    LogRecord.addLog($"基座调整值计算结果:{BaseAdj}");


                    //按图电计算排版
                    TypeSetAlgorithm.LayoutCalTu(ParaSet.recipe.TotalWidth, ParaSet.recipe.TotalHeight,
                                               PanelWidth, PanelHeight, hv_PanelCount, PeiDuWidth_out,
                                               ParaSet.recipe.FullThres, ParaSet.recipe.PanelInterval, WindowHandle, PeiDuHeight_out, BaseAdjustVal, PanelThickness, PeiDuExist,
                                               out  hv_PlatePosX, out  hv_PlatePosY, out  hv_PeiDuPosX, out  hv_PeiDuPosY);

                }
                else if (BanDianOrTuDian == 1)
                {

                    //计算当前陪镀板的宽度
                    TypeSetAlgorithm.CalBestPeiduWidthBan(ParaSet.recipe.TotalWidth, ParaSet.recipe.TotalHeight, PanelWidth, PanelHeight,
                                    ParaSet.recipe.FullThres, ParaSet.recipe.PanelInterval, out HTuple PeiDuWidth_out, out HTuple PeiDuHeight_out,
                                    out HTuple hv_PanelCount, out HTuple PeiDuExist);


                    TypeSetAlgorithm.CalBaseAdjustment(PanelWidth, PanelHeight, PanelThickness, out HTuple BaseAdjustVal);

                    PeiDuWidth = PeiDuWidth_out[0].D;
                    LogRecord.addLog($"陪镀板宽度计算结果:{PeiDuWidth}");


                    PeiDuHeight = PeiDuHeight_out[0].D;
                    LogRecord.addLog($"陪镀板高度计算结果:{PeiDuHeight}");


                    PeiduThickness = 2;
                    LogRecord.addLog($"陪镀板厚度度计算结果:{PeiduThickness}");

                    BaseAdj = BaseAdjustVal[0].D;
                    LogRecord.addLog($"基座调整值计算结果:{BaseAdj}");
                    //按板电计算排版
                    TypeSetAlgorithm.LayoutCalBan(ParaSet.recipe.TotalWidth, ParaSet.recipe.TotalHeight,
                                                  PanelWidth, PanelHeight, hv_PanelCount, PeiDuWidth_out,
                                                  ParaSet.recipe.FullThres, ParaSet.recipe.PanelInterval, WindowHandle, PeiDuHeight_out, BaseAdjustVal, PanelThickness, PeiDuExist,
                                                  out  hv_PlatePosX, out  hv_PlatePosY, out  hv_PeiDuPosX, out  hv_PeiDuPosY);

                }
                else
                {

                    LogRecord.addLog($"板电图电区分参数有无:{BanDianOrTuDian}");
                    return Res;
                }





                HOperatorSet.TupleLength(hv_PlatePosX, out HTuple hv_Length);
                int PanelCount = hv_Length[0].I;

                LogRecord.addLog($"排版包含{hv_Length}块Panel");

                string strX = "PosX:";
                string strY = "PosY:";

                string strPeiDuX = "PeiDuPosX:";
                string strPeiDuY = "PeiDuPosY:";

                HTuple end_val30 = hv_Length - 1;
                HTuple step_val30 = 1;
                for (int Index1 = 0; Index1 < hv_Length; Index1++)
                {
                    HTuple hv_x = hv_PlatePosX.TupleSelect(Index1);
                    float X = hv_x[0].F;
                    PosXQueue.AddLast(X);
                    strX = strX + X.ToString() + ",";

                    HTuple hv_y = hv_PlatePosY.TupleSelect(Index1);
                    float Y = hv_y[0].F;
                    PosYQueue.AddLast(Y);
                    strY = strY + Y.ToString() + ",";

                }


                HOperatorSet.TupleLength(hv_PeiDuPosX, out HTuple LengthPeiDu);
                if (LengthPeiDu == 2)
                {

                    for (int Index1 = 0; Index1 < LengthPeiDu; Index1++)
                    {
                        HTuple hv_x = hv_PeiDuPosX.TupleSelect(Index1);
                        double X = hv_x[0].D;
                        PosPeiDuXQueue.Enqueue(X);
                        strPeiDuX = strPeiDuX + X.ToString() + ",";

                        HTuple hv_y = hv_PeiDuPosY.TupleSelect(Index1);
                        double Y = hv_y[0].D;
                        PosPeiDuYQueue.Enqueue(Y);
                        strPeiDuY = strPeiDuY + Y.ToString() + ",";
                    }
                }
                else
                {
                    LogRecord.addLog("该排版没有陪镀板");
                }


                LogRecord.addLog(strX);
                LogRecord.addLog(strY);
                LogRecord.addLog(strPeiDuX);
                LogRecord.addLog(strPeiDuY);

                Res = 1;

            }
            catch (Exception ex)
            {
                LogRecord.addLog("排版计算出错" + ex.Message);

            }







            return Res;
        }


        public static int ShangLiaoCount1All = 0;
        public static int ShangLiaoCount2All = 0;

        public struct RobotPeiDuCheckParam
        {
            public int Position; //1表示1号位置,2表示2号位置,3表示都要检测
            public Com2RobotM RobootNow;

            public bool IsQuLiaoOrCheck;  //true取料 反之检查
            public int BinNumber; //取料仓号1，2


        }


        //正式上料前的流程
        internal static void RunShangLiaoFeiBaSaoMaAndBaseAdj()
        {
            LogRecord.addLog("进入上料飞巴扫码，陪镀板上料检查，基座调整流程,由M20触发");
            // 流程开始，主动上报设备状态为 "1:运行"
            _ = Com2Mes.SendStatusReport("1", "运行");

            try
            {


                ///////////////////////////////
                LogRecord.addLog("KeyPoint:进行上料飞巴扫码");
                string feibaCodeQian = bacodeScan3.StarBarcode();
                string feibaCodeHou = bacodeScan4.StarBarcode();


                FeiBaCurrentQian = new FeiBaWorker();
                FeiBaCurrentQian.FeiBaNo = feibaCodeQian;

                FeiBaCurrentHou = new FeiBaWorker();
                FeiBaCurrentHou.FeiBaNo = feibaCodeHou;

                LogRecord.addLog($"前飞巴编号{FeiBaCurrentQian.FeiBaNo}");
                LogRecord.addLog($"后飞巴编号{FeiBaCurrentHou.FeiBaNo}");


                //判断设备是否在手动状态
                bool[] IsMamual = com2PLC.readCoils(87, 1);
                if (IsMamual[0])
                {
                    LogRecord.addLog("当前设备处于手动模式,仅扫飞巴码后退出流程");

                    //直接保存飞巴文件，PanelList为空，在下料时，如果PanelList为空，说明这个飞巴是人工上料的，手动上料必须前后飞巴配对，不支持单个飞巴手动

                    //保存飞巴文件  
                    FeiBaWorker.SavefeibaWorker(FeiBaCurrentQian);
                    FeiBaWorker.SavefeibaWorker(FeiBaCurrentHou);


                    return;
                }





                //获取当前工单
                //获取工单队列中的第一个元素
                if (!(WorkOrderQueue.Count > 0))
                {
                    LogRecord.addLog("当前没有工单，请先获取工单后再启动上料");

                    //给PLC发送工单异常信号
                    com2PLC.writeCoil(84, true);
                    return;
                }

                WorkOrderCurrent = WorkOrderQueue.ElementAt(0);

                LogRecord.addLog($"当前工单号:{WorkOrderCurrent.lot_num}");
                //判断当前工单是否完成，如果完成，则调用工单队列中的下一个工单

                int PanelListCountAll1 = WorkOrderCurrent.CodeList.Count;
                int PanelListDone1 = WorkOrderCurrent.CodeListDone.Count;
                int PanelListRemaining1 = PanelListCountAll1 - PanelListDone1;




                if (PanelListRemaining1 <= 0)
                {
                    LogRecord.addLog("当前工单已完成，出队列，调用下一个");
                    WorkOrderQueue.Dequeue();
                    WorkOrderCurrent = WorkOrderQueue.ElementAt(0);


                    LogRecord.addLog($"新的工单号:{WorkOrderCurrent.lot_num}");
                    PrintWorkOrder(WorkOrderCurrent);
                    SendWorkOrder2PLC(WorkOrderCurrent);
                    //向MES发清线通知
                    _ = Com2Mes.SendClearLineComplete();

                    CountWorkOrderDone++;

                }

                if (PanelListDone1 == 0)
                {
                    LogRecord.addLog("当前工单为新工单，新工单启用");
                    LogRecord.addLog($"新的工单号:{WorkOrderCurrent.lot_num}");
                    PrintWorkOrder(WorkOrderCurrent);
                    SendWorkOrder2PLC(WorkOrderCurrent);


                }


                int PanelListCountAll = WorkOrderCurrent.CodeList.Count;
                int PanelListDone = WorkOrderCurrent.CodeListDone.Count;
                int PanelListRemaining = PanelListCountAll - PanelListDone;   //当前工单待处理的板子
                double Ratio = (double)PanelListDone / (double)PanelListCountAll;
                string RatioPercet = Ratio.ToString("P2");
                //通过完成率判断是否为尾料
                LogRecord.addLog($"KeyPoint:当前工单{WorkOrderCurrent.lot_num}，已上板数量{PanelListDone} ,工单主板总数{PanelListCountAll} ,上板完成率{PanelListDone}/{PanelListCountAll} ={RatioPercet} ");



                //进入陪镀板检查流程
                LogRecord.addLog("进入陪镀板更新状态检查流程");
                Thread threadRobot1 = new Thread(PeiDuBanCheck);
                threadRobot1.Start(robot1);


                Thread threadRobot2 = new Thread(PeiDuBanCheck);
                threadRobot2.Start(robot2);

                threadRobot1.Join();
                threadRobot2.Join();

                if (PeiDuCheckError[0] || PeiDuCheckError[1])
                {
                    LogRecord.addLog("机器人1上料前的陪镀板检查NG,将退出主流程，需复位");
                    return;

                }


                if (PeiDuCheckError[2] || PeiDuCheckError[3])
                {
                    LogRecord.addLog("机器人2上料前的陪镀板检查NG,将退出主流程，需复位");
                    return;

                }

                //以上程序都通过后，进入飞巴扫码

                LogRecord.addLog("机器人上料前的陪镀板检查OK");
                // com2PLC.writeCoil(51, true);



                //进行上料夹爪松开检查，改为传感器检查，PLC自动执行，主控上位机不再参与

                //WorkOrderCurrent.CodeList.Count

                int C1 = WorkOrderCurrent.CodeList.Count;
                List<string> distinctCodeList = WorkOrderCurrent.CodeList.Distinct().ToList();

                int C2 = distinctCodeList.Count;

                if (C2 < C1)
                {
                    LogRecord.addLog($"{WorkOrderCurrent}工单信息中的条码有重复的，已去重");
                    WorkOrderCurrent.CodeList = distinctCodeList;
                }


                //根据当前工单信息，计算排版，控制料台调整



                //尾料赋值
                //if(PanelListRemaining < FeiBaCurrentHou.PanelCount)
                //{
                //    //后飞巴尾料处理
                //    LogRecord.addLog($"后飞巴尾料处理赋值");
                //    FeiBaCurrentQian.WeiLiao = true;
                //    com2PLC.writeCoil(81, true);

                //}
                //else if (PanelListRemaining  < FeiBaCurrentHou.PanelCount+ FeiBaCurrentQian.PanelCount)
                //{
                //    //前飞巴尾料处理
                //    LogRecord.addLog($"前飞巴尾料处理赋值");
                //    FeiBaCurrentHou.WeiLiao = true;
                //    com2PLC.writeCoil(82, true);
                //}



                LogRecord.addLog($"计算当前工单排版");
                //计算排版,返回陪镀板宽度和基座调整值
                int Res = WorkFlowControl.CalPaiBan(WorkOrderCurrent.pallet_width, WorkOrderCurrent.pallet_length, WorkOrderCurrent.pallet_thickness,
                                                                        out double PeiduWidth, out double PeiduHeight, out double PeiduThickness, out double BaseAdj, out LinkedList<double> PosXQueue, out LinkedList<double> PosYQueue,
                                                                        out Queue<double> PosPeiDuXQueue, out Queue<double> PosPeiDuYQueue);





                //double[] array = PosXQueue.ToArray(); // 或使用 queue.CopyTo(array, 0)
                //LinkedList<double> PosXQueueL = new LinkedList<double>(array);

                //double[] array1 = PosYQueue.ToArray(); // 或使用 queue.CopyTo(array, 0)
                //LinkedList<double> PosYQueueL = new LinkedList<double>(array1);


                LogRecord.addLog($"KeyPoint:排版计算结果:陪镀板宽度-{PeiduWidth},基座调整值-{BaseAdj},主板数量-{PosXQueue.Count}");

                WorkOrderCurrent.peidu_width = (float)PeiduWidth;
                //if (PanelListRemaining <= 0)
                //{
                //    LogRecord.addLog($"KeyPoint:当前工单已经完成");
                //    //TODO  : Mes清线完成通知 ,也需要有个定时器，定制检查 WorkOrderCurrent.CodeListDone.Count 的个数，发送清线完成到MES

                //    LogRecord.addLog("工单完成，触发MES清线完成上报...");

                //  //  return;

                //}




                //判断是否为尾料

                //if (PanelListRemaining < PosXQueue.Count)
                //{
                //    LogRecord.addLog($"当前工单剩余主板数量不足覆盖一排飞巴，前飞巴为尾料处理");

                //    LogRecord.addLog($"前飞巴尾料处理赋值");
                //    FeiBaCurrentQian.WeiLiao = true;
                //    com2PLC.writeCoil(82, true);
                //}


                //只判断两个飞巴是否能装满，装不满就代表尾料
                if (PanelListRemaining < 2 * PosXQueue.Count)
                {
                    LogRecord.addLog($"当前工单剩余主板数量不足覆盖两排排飞巴，前=后飞巴为尾料处理");
                    //后飞巴尾料处理
                    LogRecord.addLog($"飞巴尾料处理赋值");
                    FeiBaCurrentHou.WeiLiao = true;
                    com2PLC.writeCoil(81, true);
                    LogRecord.addLog($"工单{WorkOrderCurrent.lot_num}已结束，剩余板子按尾料来处理");
                    com2PLC.writeCoil(77, true);

                    if (WorkOrderQueue.Count > 0 && WorkOrderQueue.Peek().lot_num == WorkOrderCurrent.lot_num)
                    {
                        WorkOrderQueue.Dequeue();
                        LogRecord.addLog($"尾料工单 {WorkOrderCurrent.lot_num} 已从队列中移除。");
                    }

                    //获取下一个工单
                    WorkOrder nextWorkOrder = null;
                    if (WorkOrderQueue.Count > 0)
                    {
                        nextWorkOrder = WorkOrderQueue.Peek(); // 
                    }
                    if (nextWorkOrder != null)
                    {
                        WorkOrderCurrent = nextWorkOrder;
                        PanelListRemaining = WorkOrderCurrent.CodeList.Count - WorkOrderCurrent.CodeListDone.Count;

                        LogRecord.addLog($"已切换到新工单：{WorkOrderCurrent.lot_num}，主板总数：{WorkOrderCurrent.CodeList.Count}，剩余：{PanelListRemaining}");

                        SendWorkOrder2PLC(WorkOrderCurrent);
                        com2PLC.writeCoil(84, false);  //复位工单异常信号
                        return;
                    }



                }




                LogRecord.addLog($"KeyPoint:下料飞巴基座调整{BaseAdj},{BaseAdj}");

                //向PLC写入基座调整值，进行基座调整，前后飞巴默认一致
                com2PLC.FeibaBaseAdjust(BaseAdj, BaseAdj, 1);

                int keyX = Com2RobotM.DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料料台底座调整距离")).Key;

                //告诉机器人基座调整距离，可能要在y方向做偏移
                robot1.writeDataD(keyX, BaseAdj);
                robot2.writeDataD(keyX, BaseAdj);

                LogRecord.addLog("等待基座调整完成");
                //com2PLC.WaitForSingal(35);
                LogRecord.addLog("KeyPoint:基座调整完成");
                //延时等待
                Thread.Sleep(1000);

                //单个飞巴上料循环
                //前飞巴
                int CountFeibaPanel = PosXQueue.Count;
                LogRecord.addLog($"KeyPoint:当前飞巴主板上料个数:{CountFeibaPanel}");
                LogRecord.addLog($"KeyPoint:当前飞巴陪镀板宽度:{PeiduWidth}");




                //前飞巴数据赋值
                if (PosPeiDuXQueue.Count < 1)
                    FeiBaCurrentQian.PeiDuBanExist = false;
                else
                {
                    FeiBaCurrentQian.PeiDuposX1 = PosPeiDuXQueue.ElementAt(0);
                    FeiBaCurrentQian.PeiDuposX2 = PosPeiDuXQueue.ElementAt(1);
                    FeiBaCurrentQian.PeiDuposY1 = PosPeiDuYQueue.ElementAt(0);
                    FeiBaCurrentQian.PeiDuposY2 = PosPeiDuYQueue.ElementAt(1);
                    FeiBaCurrentQian.PeiDuBanExist = true;
                    FeiBaCurrentQian.PeiDuWidth = PeiduWidth;
                    FeiBaCurrentQian.PeiDuHeight = PeiduHeight;


                    //配镀板厚度，在排版输出
                    FeiBaCurrentQian.PeiDuThickness = PeiduThickness;
                    //告诉PLC是否有陪镀板
                    com2PLC.writeCoil(28, true);

                }

                FeiBaCurrentQian.Row = 1;
                FeiBaCurrentQian.PosXQueue = PosXQueue;
                FeiBaCurrentQian.PosYQueue = PosYQueue;
                FeiBaCurrentQian.PanelCount = PosXQueue.Count;
                FeiBaCurrentQian.PanelWidth = WorkOrderCurrent.pallet_width;
                FeiBaCurrentQian.PanelHeight = WorkOrderCurrent.pallet_length;
                FeiBaCurrentQian.PanelThickness = WorkOrderCurrent.pallet_thickness;
                FeiBaCurrentQian.PanelWeight = WorkOrderCurrent.pallet_weight;
                FeiBaCurrentQian.FeiBaAdaptionValue = BaseAdj;
                //后飞巴数据赋值
                if (PosPeiDuXQueue.Count < 1)
                    FeiBaCurrentHou.PeiDuBanExist = false;
                else
                {
                    FeiBaCurrentHou.PeiDuposX1 = PosPeiDuXQueue.ElementAt(0);
                    FeiBaCurrentHou.PeiDuposX2 = PosPeiDuXQueue.ElementAt(1);
                    FeiBaCurrentHou.PeiDuposY1 = PosPeiDuYQueue.ElementAt(0);
                    FeiBaCurrentHou.PeiDuposY2 = PosPeiDuYQueue.ElementAt(1);
                    FeiBaCurrentHou.PeiDuBanExist = true;

                    FeiBaCurrentHou.PeiDuWidth = PeiduWidth;
                    FeiBaCurrentHou.PeiDuHeight = PeiduHeight;
                    //配镀板厚度，在排版输出
                    FeiBaCurrentHou.PeiDuThickness = PeiduThickness;

                    //告诉PLC是否有陪镀板
                    com2PLC.writeCoil(28, true);

                }

                FeiBaCurrentHou.Row = 2;
                FeiBaCurrentHou.PosXQueue = PosXQueue;
                FeiBaCurrentHou.PosYQueue = PosYQueue;
                FeiBaCurrentHou.PanelCount = PosXQueue.Count;
                FeiBaCurrentHou.PanelWidth = WorkOrderCurrent.pallet_width;
                FeiBaCurrentHou.PanelHeight = WorkOrderCurrent.pallet_length;
                FeiBaCurrentHou.PanelThickness = WorkOrderCurrent.pallet_thickness;
                FeiBaCurrentHou.PanelWeight = WorkOrderCurrent.pallet_weight;
                FeiBaCurrentHou.FeiBaAdaptionValue = BaseAdj;


                //分配主板上料循环次数并写入机器人和PLC，循环总数是全局变量，控制左右机器人上料个数

                //右边少，左边多
                //ShangLiaoCount1All = (CountFeibaPanel +1 )/ 2;
                //ShangLiaoCount2All = CountFeibaPanel  - ShangLiaoCount1All;

                //右边多，左边少
                ShangLiaoCount1All = (CountFeibaPanel) / 2;
                ShangLiaoCount2All = CountFeibaPanel - ShangLiaoCount1All;



                //循环次数由PLC控制扫码次数来控制
                int key = com2PLC.DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("缓存1的上料循环次数")).Key;
                com2PLC.writeDataD(key, ShangLiaoCount1All);

                int key1 = com2PLC.DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("缓存2的上料循环次数")).Key;
                com2PLC.writeDataD(key1, ShangLiaoCount2All);

                int key2 = Com2RobotM.DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料循环次数")).Key;
                robot1.writeDataD(key2, ShangLiaoCount1All);
                robot2.writeDataD(key2, ShangLiaoCount2All);

                //通知PLC 传送带上料
                //1号缓存机允许上料
                com2PLC.writeCoil(53, true);
                //2号缓存机允许上料
                com2PLC.writeCoil(54, true);

                LogRecord.addLog($"KeyPoint:已经向PLC写入上料循环次数,当前飞巴主板左右个数:{ShangLiaoCount1All},{ShangLiaoCount2All}");

                LogRecord.addLog($"KeyPoint:上料流程启动完成,传送带可以送料");



            }
            catch (Exception ex)
            {

                // 1. 上报报警
                _ = Com2Mes.SendStatusReport("3", "故障");


                // 捕获异常，上报报警 + 设备故障状态
                string errorMsg = "上料流程异常: " + ex.Message;
                LogRecord.addLog(errorMsg);


                // 2. 上报状态为 "3:故障"
                _ = Com2Mes.SendStatusReport("3", "故障");


                LogRecord.addLog("上料飞巴扫码，陪镀板上料检查，基座调整流程出错" + ex.Message);
            }




        }

        private static void PeiDuBanCheck(object Object)
        {

            Com2RobotM _RobotNow = (Com2RobotM)Object;
            bool Res = true;

            //26:区域1的1号仓    1-1
            //27:区域1的2号仓    1-2
            //30:区域2的1号仓    2-1
            //31:区域2的2号仓    2-2
            //区域1:左机器人
            //区域2:右机器人



            Thread threadRobot1 = new Thread(CheckPeiDuBeforeShangLiao);
            Thread threadRobot2 = new Thread(CheckPeiDuBeforeShangLiao);


            bool RobotRunningPeiDuCheck_1 = false;
            bool RobotRunningPeiDuCheck_2 = false;

            if (_RobotNow.RobotNo == 1)
            {

                //状态位更新
                PeiDuCheckError[0] = false;
                PeiDuCheckError[1] = false;
                //1号机器人陪镀板检查
                LogRecord.addLog($"{_RobotNow.RobotNo}号机器人陪镀板尺寸确认");
                bool[] PeiDuRefresh_1_1 = com2PLC.readCoils(26, 1);//读26 和 27  1-1
                bool[] PeiDuRefresh_1_2 = com2PLC.readCoils(27, 1);//读26 和 27  1-1

                //1号机器人的两个区域
                bool[] PeiDuRefresh_1 = { PeiDuRefresh_1_1[0], PeiDuRefresh_1_2[0] };

                LogRecord.addLog($"KeyPoint:陪镀板区域更新状态,1号机器人，区域1:{PeiDuRefresh_1[0]},区域2:{PeiDuRefresh_1[1]}");


                RobotPeiDuCheckParam param = new RobotPeiDuCheckParam();
                param.Position = 0;
                param.RobootNow = robot1;

                //机器人动作需判断是检查位置1还是位置2，还是都要检查

                if (PeiDuRefresh_1[0] && PeiDuRefresh_1[1])
                {
                    param.Position = 3;
                    //1号区域1和区域2都检测    
                    threadRobot1.Start(param);

                }
                else if (PeiDuRefresh_1[0])
                {

                    param.Position = 1;
                    //1号机器人区域1检测
                    threadRobot1.Start(param);
                }
                else if (PeiDuRefresh_1[1])
                {
                    param.Position = 2;
                    //1号机器人 区域2检测
                    threadRobot1.Start(param);

                }

                //等待线程完成

                if (PeiDuRefresh_1[0] || PeiDuRefresh_1[1])
                {
                    RobotRunningPeiDuCheck_1 = true;
                }


            }

            if (_RobotNow.RobotNo == 2)
            {

                LogRecord.addLog($"{_RobotNow.RobotNo}号机器人陪镀板尺寸确认");

                PeiDuCheckError[2] = false;
                PeiDuCheckError[3] = false;

                bool[] PeiDuRefresh_2_1 = com2PLC.readCoils(30, 1);//读30  和31 2-1
                bool[] PeiDuRefresh_2_2 = com2PLC.readCoils(31, 1);//读31   2-2
                                                                   //2号机器人的两个区域
                bool[] PeiDuRefresh_2 = { PeiDuRefresh_2_1[0], PeiDuRefresh_2_2[0] };
                LogRecord.addLog($"KeyPoint:陪镀板区域更新状态,2号机器人，区域1:{PeiDuRefresh_2[0]},区域2:{PeiDuRefresh_2[1]}");


                RobotPeiDuCheckParam param = new RobotPeiDuCheckParam();
                param.Position = 0;
                param.RobootNow = robot2;

                //2号机器人
                if (PeiDuRefresh_2[0] && PeiDuRefresh_2[1])
                {
                    param.Position = 3;
                    //2号机器人区域1和区域2都检测
                    threadRobot2.Start(param);

                }
                else if (PeiDuRefresh_2[0])
                {

                    param.Position = 1;
                    //2号区域1检测
                    threadRobot2.Start(param);
                }
                else if (PeiDuRefresh_2[1])
                {
                    param.Position = 2;
                    //2号区域2检测
                    threadRobot2.Start(param);

                }

                if (PeiDuRefresh_2[0] || PeiDuRefresh_2[1])
                {

                    RobotRunningPeiDuCheck_2 = true;
                }


            }

            if (RobotRunningPeiDuCheck_1)
            {
                threadRobot1.Join();
            }

            if (RobotRunningPeiDuCheck_2)
            {
                threadRobot2.Join();
            }





            LogRecord.addLog("KeyPoint:机器人上料前的陪镀板检查环节结束");

            if (PeiDuCheckError[0] || PeiDuCheckError[1])
            {

                LogRecord.addLog("1机器人上料前的陪镀板检查NG，退出上料流程");
                com2PLC.writeCoil(32, true);
                com2PLC.writeCoil(80, true);
                //NG后退出
                Res = false;
            }

            if (PeiDuCheckError[2] || PeiDuCheckError[3])
            {

                LogRecord.addLog("2机器人上料前的陪镀板检查NG，退出上料流程");
                com2PLC.writeCoil(33, true);
                com2PLC.writeCoil(80, true);
                //NG后退出
                Res = false;

            }





            return;


        }






        //陪镀板检查过程函数
        static bool[] PeiDuCheckError = { false, false, false, false };
        //分别表示1-1,,1-2,2-1,2-2陪镀板拍照状态


        public static void CheckPeiDuBeforeShangLiao(Object obj)
        {
            RobotPeiDuCheckParam param = (RobotPeiDuCheckParam)obj;



            Com2RobotM RobootNow = param.RobootNow;
            int Position = param.Position;

            //Position == 1表示1号位检查， ==2 表示2号位检查，  == 3表示都要检查

            bool isQuLiaoOrCheck = param.IsQuLiaoOrCheck;   //检查
            int bin = param.BinNumber;

            int No = RobootNow.RobotNo;
            bool Res = true;


            //1号位置陪镀板检查

            if (Position == 1 || Position == 3)
            {

                LogRecord.addLog($"KeyPoint:{RobootNow.RobotNo}号机器人启动陪镀板3D尺寸检测,位置{Position}");
                RobootNow.writeCoil(44, true);   //告诉机器人去位置1取陪镀板

                bool ResW = RobootNow.WaitForSingal(45);
                LogRecord.addLog($"{RobootNow.RobotNo}号机器人{Position}号位置陪镀板检测拍照位到位");

                //PLC 寄存器复位 ，26 和27号，1号机器人1号位置，26
                //1号机器人1号位置，线圈26
                //2号机器人1号位置，线圈27
                // com2PLC.writeCoil(25 + RobootNow.RobotNo, false);

                //触发3D拍照
                Res = Com2Vision.Start3DCapturePeiDuCheck(RobootNow.RobotNo, 1, out int PeiduWidth, out int PeiduHeight);

                //复位26和30，，一号区域
                com2PLC.writeCoil(25 + RobootNow.RobotNo, false);
                com2PLC.writeCoil(29 + RobootNow.RobotNo, false);


                //复位26和30，，1_1,2_1号区域

                if (RobootNow.RobotNo == 1)
                {
                    com2PLC.writeCoil(26, false);
                }
                else if (RobootNow.RobotNo == 2)
                {
                    com2PLC.writeCoil(30, false);
                }


                if (Res)
                {

                    //3D相机拍照结果
                    LogRecord.addLog($"3D相机拍照结果,陪镀板宽度:{PeiduWidth},陪镀板高度:{PeiduHeight}");
                    LogRecord.addLog($"工单标注,陪镀板宽度:{WorkOrderCurrent.peidu_width},陪镀板高度:{WorkOrderCurrent.peidu_length}");


                    if (isOfflineDebug)
                    {
                        LogRecord.addLog("现在是离线模式 配镀板检测强制ok");
                        LogRecord.addLog($"{RobootNow.RobotNo}号机器人{Position}号位置陪镀板检测OK");
                        RobootNow.writeCoil(46, true);
                    }
                    else
                    {
                        //正常运行
                        //正负10mm容差,标准需调整
                        if (PeiduWidth < WorkOrderCurrent.peidu_width + 10 && PeiduWidth > WorkOrderCurrent.peidu_width - 10
                            && PeiduHeight < WorkOrderCurrent.peidu_length + 10 && PeiduHeight > WorkOrderCurrent.peidu_length - 10)
                        {

                            LogRecord.addLog($"{RobootNow.RobotNo}号机器人{Position}号位置陪镀板检测OK");
                            RobootNow.writeCoil(46, true);

                        }

                        else
                        {
                            string msg = $"{RobootNow.RobotNo}号机器人{Position}号位置陪镀板尺寸超差NG";
                            LogRecord.addLog(msg);
                            RobootNow.writeCoil(47, true);
                            // 检测数据NG上报
                            _ = Com2Mes.SendAlarmReport("ERR_PD_SIZE", msg, 2, 1);

                            int No1 = (RobootNow.RobotNo - 1) * 2 + Position - 1;
                            //NG时置位
                            PeiDuCheckError[No1] = true;
                        }
                    }
                }
                else
                {

                    string msg = $"{RobootNow.RobotNo}号机器人{Position}号位置陪镀板视觉拍照失败";
                    LogRecord.addLog(msg);
                    RobootNow.writeCoil(31 + RobootNow.RobotNo, true);

                    // 视觉故障上报
                    _ = Com2Mes.SendAlarmReport("ERR_PD_CAM", msg, 3, 1);

                    int No1 = (RobootNow.RobotNo - 1) * 2 + Position - 1;
                    //NG时置位
                    PeiDuCheckError[No1] = true;


                    //向plc发送3D拍照失败信号
                    if (RobootNow.RobotNo == 1)  //左边机器人---78
                    {
                        com2PLC.writeCoil(78, true);
                        LogRecord.addLog($"向PLC发送78号信号：当前机器人{RobootNow.RobotNo}3D拍照失败，等待人工处理");
                    }
                    else if (RobootNow.RobotNo == 2) //右边机器人----79
                    {
                        com2PLC.writeCoil(79, true);
                        LogRecord.addLog($"向PLC发送78号信号：当前机器人{RobootNow.RobotNo}3D拍照失败，等待人工处理");
                    }

                    LogRecord.addLog("已通知PLC，等待PLC处理后流程继续");
                }



            }



            //2号位置陪镀板检查
            if (Position == 2 || Position == 3)
            {
                //1号位置陪镀板检查
                LogRecord.addLog($"{RobootNow.RobotNo}号机器人启动陪镀板3D尺寸检测,位置{Position}");
                //启动2号位置陪镀板拍照
                RobootNow.writeCoil(48, true);  //告诉机器人去位置2取陪镀板

                bool ResW = RobootNow.WaitForSingal(45);
                LogRecord.addLog($"{RobootNow.RobotNo}号机器人{Position}号位置陪镀板检测拍照位到位");
                //PLC 寄存器复位 ，30 和31
                //1号机器人2号位置，线圈30
                //2号机器人2号位置，线圈31
                //  com2PLC.writeCoil(29 + RobootNow.RobotNo, false);


                //触发3D拍照
                Res = Com2Vision.Start3DCapturePeiDuCheck(RobootNow.RobotNo, 2, out int PeiduWidth, out int PeiduHeight);

                //复位27和31，，1_2,2_2号区域

                if (RobootNow.RobotNo == 1)
                {
                    com2PLC.writeCoil(27, false);
                }
                else if (RobootNow.RobotNo == 2)
                {
                    com2PLC.writeCoil(31, false);
                }



                if (Res)
                {

                    if (isOfflineDebug)
                    {
                        LogRecord.addLog("现在是离线模式 配镀板检测强制ok");
                        LogRecord.addLog($"{RobootNow.RobotNo}号机器人{Position}号位置陪镀板检测OK");
                        RobootNow.writeCoil(46, true);
                    }
                    else
                    {
                        //正负10mm容差
                        if (PeiduWidth < WorkOrderCurrent.peidu_width + 10 && PeiduWidth > WorkOrderCurrent.peidu_width - 10
                               && PeiduHeight < WorkOrderCurrent.peidu_length + 10 && PeiduHeight > WorkOrderCurrent.peidu_length - 10)
                        {
                            LogRecord.addLog($"{RobootNow.RobotNo}号机器人{Position}号位置陪镀板检测OK");
                            RobootNow.writeCoil(46, true);

                        }
                        else
                        {
                            string msg = $"{RobootNow.RobotNo}号机器人{Position}号位置陪镀板尺寸超差NG";
                            LogRecord.addLog(msg);
                            RobootNow.writeCoil(47, true);
                            // 【新增】检测数据NG上报
                            _ = Com2Mes.SendAlarmReport("ERR_PD_SIZE", msg, 2, 1);
                            //NG时置位

                            int No1 = (RobootNow.RobotNo - 1) * 2 + Position - 1;
                            //NG时置位
                            PeiDuCheckError[No1] = true;

                        }



                    }




                }
                else
                {

                    string msg = $"{RobootNow.RobotNo}号机器人{Position}号位置陪镀板视觉拍照失败";
                    LogRecord.addLog(msg);
                    RobootNow.writeCoil(47, true);

                    // 【新增】视觉故障上报
                    _ = Com2Mes.SendAlarmReport("ERR_PD_CAM", msg, 3, 1);

                    int No1 = (RobootNow.RobotNo - 1) * 2 + Position - 1;
                    //NG时置位
                    PeiDuCheckError[No1] = true;


                    // 向PLC发送拍照失败信号
                    if (RobootNow.RobotNo == 1)  // 左边机器人
                    {
                        com2PLC.writeCoil(78, true);  // 发送右边边拍照失败信号
                        LogRecord.addLog("向PLC发送78号信号：当前配镀板左边3D拍照失败，等待人工处理");
                    }
                    else if (RobootNow.RobotNo == 2)  // 右边机器人
                    {
                        com2PLC.writeCoil(79, true);
                        LogRecord.addLog("向PLC发送79号信号：当前配镀板右边3D拍照失败，等待人工处理");
                    }

                    LogRecord.addLog("已通知PLC，等待PLC处理后流程继续");


                }

            }

        }


        public static bool XiaLiaoRunning = false;
        //保存飞巴文件时采用  "MM_dd_HH_mm_ss_fff_" +  FeiBaNo 的形式，确保文件不会覆盖，
        //下料载入时，遍历文件夹，获取修改时间最新的，并且文件名包含飞巴号的文件
        //此函数以线程形式运行
        //下料流程

        internal static void RunXiaLiao()
        {
            LogRecord.addLog("进入下料流程");

            // 流程开始，上报状态 "运行"
            _ = Com2Mes.SendStatusReport("1", "运行");

            try
            {

                if (XiaLiaoRunning)
                {
                    LogRecord.addLog("检测到下料流程正在运行，将忽略本次触发");
                    return;
                }

                XiaLiaoRunning = true;
                string feibaCodeQian = "";
                string feibaCodeHou = "";

                string feibafilename1 = "";
                string feibafilename2 = "";
                if (!isOfflineDebug)
                {
                    LogRecord.addLog("在线飞巴码读码");
                    feibaCodeQian = bacodeScan3.StarBarcode();
                    feibaCodeHou = bacodeScan4.StarBarcode();


                    // 下料扫码结果判断 ---

                    bool scanSuccess = !string.IsNullOrEmpty(feibaCodeQian) &&
                                      !feibaCodeQian.EndsWith("ead") &&
                                      !string.IsNullOrEmpty(feibaCodeHou) &&
                                      !feibaCodeHou.EndsWith("ead");

                    if (scanSuccess)
                    {
                        LogRecord.addLog("前后飞巴扫码成功");
                        com2PLC.writeCoil(52, true);  // 下料飞巴扫码成功
                    }
                    else
                    {
                        LogRecord.addLog("飞巴扫码失败");
                        com2PLC.writeCoil(51, true);  // 扫码失败信号
                        XiaLiaoRunning = false;
                        return;
                    }


                    feibafilename1 = GetLatestFileWithPattern(PathFeibaInfo, feibaCodeQian);
                    feibafilename2 = GetLatestFileWithPattern(PathFeibaInfo, feibaCodeHou);

                }
                else
                {

                    LogRecord.addLog("当前为离线模式,将载入预先设定好的飞巴文件");

                    //离线模式--测试使用
                    LogRecord.addLog("模拟下料飞巴扫码成功");
                    com2PLC.writeCoil(52, true);   //下料飞巴扫码成功

                    feibafilename1 = offlineFeiBaFile1;
                    feibafilename2 = offlineFeiBaFile2;
                }


                LogRecord.addLog($"前飞巴文件名{feibafilename1}");
                LogRecord.addLog($"后飞巴文件名{feibafilename2}");

                if (!File.Exists(feibafilename1))
                {
                    LogRecord.addLog($"前飞巴文件不存在");
                    com2PLC.writeCoil(51, true);  //下料飞巴扫码异常
                    return;
                }

                if (!File.Exists(feibafilename2))
                {
                    LogRecord.addLog($"后飞巴文件不存在");
                    com2PLC.writeCoil(51, true);     //下料飞巴扫码异常
                    return;
                }


                //从飞巴记录文件夹中，调出飞巴文件
                //遍历文件夹，并获取包含飞巴号的的最新文件名


                LogRecord.addLog($"载入前飞巴信息文件:{feibafilename1}");
                string jsonContent2 = File.ReadAllText(feibafilename1);
                FeiBaCurrentQian = Json2Obj<FeiBaWorker>(jsonContent2);




                LogRecord.addLog($"载入后飞巴信息文件:{feibafilename2}");
                string jsonContent3 = File.ReadAllText(feibafilename2);
                FeiBaCurrentHou = Json2Obj<FeiBaWorker>(jsonContent3);


                //判断飞巴文件中的PanelList 是否为空，如果为空，说明这个飞巴是人工上料的，也需要人工下料

                if (FeiBaCurrentQian.PanelList.Count == 0 || FeiBaCurrentQian.PanelList.Count == 0)
                {

                    LogRecord.addLog($"当前飞巴判定为人工上料的飞巴，也需要人工下料");
                    com2PLC.writeCoil(88, true);
                    return;
                }



                //检查飞巴文件参数，简单防呆

                if (FeiBaCurrentQian.PanelList.Count != FeiBaCurrentQian.PanelCount)
                {


                    LogRecord.addLog($"{FeiBaCurrentQian.Row}号飞巴参数有误,飞巴码{FeiBaCurrentQian.FeiBaNo},主板队列和主板总数不一致,主板队列数{FeiBaCurrentQian.PanelList.Count},主板总数{FeiBaCurrentQian.PanelCount}");


                    return;
                }

                if (FeiBaCurrentHou.PanelList.Count != FeiBaCurrentHou.PanelCount)
                {


                    LogRecord.addLog($"{FeiBaCurrentHou.Row}号飞巴参数有误,飞巴码{FeiBaCurrentHou.FeiBaNo},主板队列和主板总数不一致,主板队列数{FeiBaCurrentHou.PanelList.Count},主板总数{FeiBaCurrentHou.PanelCount}");


                    return;

                }




                //下料飞巴基座调整，有可能不需要,plc自行忽略
                //LogRecord.addLog($"下料飞巴基座调整{FeiBaCurrentQian.FeiBaAdaptionValue},{FeiBaCurrentHou.FeiBaAdaptionValue}");
                //com2PLC.FeibaBaseAdjust(FeiBaCurrentQian.FeiBaAdaptionValue, FeiBaCurrentHou.FeiBaAdaptionValue, 2);



                //加入等待信号
                //   Thread.Sleep(1000);
                // LogRecord.addLog("等待基座调整完成,等待已取消");
                // //调试时不等待
                //// com2PLC.WaitForSingal(35);
                //   LogRecord.addLog("基座调整完成");
                //扫码完成启动下料流程
                LogRecord.addLog("下料飞巴扫码已完成,启动机器人下料流程");


                if (!CancelXiaLiaoInspect)
                {

                    //进行下料前的3D视觉检查，检查夹爪是否张开以及是否有掉板的异常情况发生，已改成传感器检测，有可能取消这个环节
                    LogRecord.addLog("进入机器人下料前的3D检查环节,前飞巴");
                    //错误标志位复位
                    XiaLiaoCheckError = false;
                    XiaLiaoCheckRowNo = 1;
                    Thread threadRobot1 = new Thread(Check3DBeforeXiaLiaoRobot);
                    Thread threadRobot2 = new Thread(Check3DBeforeXiaLiaoRobot);



                    if (TestRobot1)
                    {

                        LogRecord.addLog("进入1号机器人下料前3D视觉检测流程");
                        threadRobot1.Start(robot1);
                    }

                    if (TestRobot2)
                    {
                        LogRecord.addLog("进入2号机器人下料前3D视觉检测流程");
                        threadRobot2.Start(robot2);
                    }

                    if (TestRobot1)
                    {
                        threadRobot1.Join();
                        LogRecord.addLog("1号机器人下料前3D视觉检测流程完成");
                    }

                    if (TestRobot2)
                    {
                        threadRobot2.Join();
                        LogRecord.addLog("2号机器人下料前3D视觉检测流程完成");
                    }

                    LogRecord.addLog("3D防呆检查流程结束，进入3D相机飞巴定位流程");


                    if (XiaLiaoCheckError)
                    {
                        LogRecord.addLog("下料前3D防呆检查流出错，请检查后重启下料流程");
                        return;
                    }


                }
                else
                {

                    LogRecord.addLog("下料3D防呆检查已经取消");
                }


                XiaLiaoFeiBaPositionCount = 0;

                ///////开始执行下料//前飞巴////////////////////////////////////////////
                LogRecord.addLog("进入前飞巴下料");
                Thread threadFeiBaXiaLiao1 = new Thread(XiaLiaoProcessFunc);
                threadFeiBaXiaLiao1.Start(FeiBaCurrentQian);


                // 等待线程完成
                threadFeiBaXiaLiao1.Join();
                LogRecord.addLog("前飞巴下料完成");


                ////////////////////////////////////////////////////////////////////

                LogRecord.addLog("进入后飞巴下料前3D检查环节");


                if (!CancelXiaLiaoInspect)
                {

                    //进行下料前的3D视觉检查，检查夹爪是否张开以及是否有掉板的异常情况发生，已改成传感器检测，有可能取消这个环节
                    LogRecord.addLog("进入机器人下料前的3D检查环节,后飞巴");
                    //错误标志位复位
                    XiaLiaoCheckError = false;
                    XiaLiaoCheckRowNo = 2;
                    Thread threadRobot1 = new Thread(Check3DBeforeXiaLiaoRobot);
                    Thread threadRobot2 = new Thread(Check3DBeforeXiaLiaoRobot);



                    if (TestRobot1)
                    {

                        LogRecord.addLog("进入1号机器人下料前3D视觉检测流程");
                        threadRobot1.Start(robot1);
                    }

                    if (TestRobot2)
                    {
                        LogRecord.addLog("进入2号机器人下料前3D视觉检测流程");
                        threadRobot2.Start(robot2);
                    }

                    if (TestRobot1)
                    {
                        threadRobot1.Join();
                        LogRecord.addLog("1号机器人下料前3D视觉检测流程完成");
                    }

                    if (TestRobot2)
                    {
                        threadRobot2.Join();
                        LogRecord.addLog("2号机器人下料前3D视觉检测流程完成");
                    }

                    LogRecord.addLog("3D防呆检查流程结束，进入3D相机飞巴定位流程");


                    if (XiaLiaoCheckError)
                    {
                        LogRecord.addLog("下料前3D防呆检查流出错，请检查后重启下料流程");
                        return;
                    }


                }
                else
                {

                    LogRecord.addLog("下料3D防呆检查已经取消");
                }





                ////////////////////////////////////////////////////////



                LogRecord.addLog("进入后飞巴下料");
                Thread threadFeiBaXiaLiao2 = new Thread(XiaLiaoProcessFunc);
                threadFeiBaXiaLiao2.Start(FeiBaCurrentHou);

                // 等待线程完成
                threadFeiBaXiaLiao2.Join();
                LogRecord.addLog("后飞巴下料完成");


                com2PLC.writeCoil(3, true);    //plc通知飞巴可以离开




            }
            catch (Exception ex)
            {
                LogRecord.addLog("下料流程出错" + ex.Message);
                // 捕获异常，上报报警 + 设备故障状态
                _ = Com2Mes.SendAlarmReport("ERR_XL_SYS", "下料流程系统异常: " + ex.Message, 3, 1);
                _ = Com2Mes.SendStatusReport("3", "故障");
                com2PLC.writeCoil(51, true);   //下料飞巴扫码异常
            }

            XiaLiaoRunning = false;
        }


        public static bool XiaLiaoFeiBaPositionCheckError = false;
        public static int XiaLiaoFeiBaPositionCount = 0;


        private static void FeiBaPositionBeforeXiaLiao(object obj)
        {
            Com2RobotM RobootNow = (Com2RobotM)obj;


            int No = RobootNow.RobotNo;
            bool Res = true;

            try
            {

                //通知机器人启动下料流程,机器人到下料飞巴定位拍照位，依次触发3D相机拍照
                RobootNow.writeCoil(12, true);

                LogRecord.addLog($"KeyPoint:进入{No}号机器人下料飞巴定位环节,机器人前往拍照位");

                Thread.Sleep(500);

                //等待机器人到位信号13
                bool re = RobootNow.WaitForSingal(13);
                if (re)
                {
                    LogRecord.addLog($"KeyPoint:{RobootNow.RobotNo}号机器人:到达3D飞巴定位拍照位");
                    RobootNow.writeCoil(13, false);//复位

                    //触发3D拍照
                    int projectNo = 0;

                    if (XiaLiaoFeiBaPositionCount < 2)
                    {
                        projectNo = No;

                    }
                    else
                    {
                        projectNo = No + 2;
                    }

                    bool r = Com2Vision.Start3DCaptureFeiBaPosition(projectNo, out float dx);

                    if (r || isOfflineDebug)
                    {
                        //离线调试时不做判断
                        LogRecord.addLog($"KeyPoint:{RobootNow.RobotNo}号机器人:下料偏移值位{dx}");
                        //
                        //写入机器人下料偏移值
                        double dxx = (double)dx;
                        dxx = Math.Round(dxx, 2);//保留两位小数
                        RobootNow.SendXiaLiaoDxData(dxx);
                        RobootNow.writeCoil(14, true);
                    }
                    else
                    {
                        LogRecord.addLog($"{RobootNow.RobotNo}号机器人:3D飞巴定位拍照失败");
                        XiaLiaoFeiBaPositionCheckError = true;
                        RobootNow.writeCoil(15, true);
                    }

                    XiaLiaoFeiBaPositionCount++;
                }
                else
                {
                    LogRecord.addLog($"{RobootNow.RobotNo}号机器人:等待机器人到达3D飞巴定位拍照位超时");
                    XiaLiaoFeiBaPositionCheckError = true;
                    return;
                }



            }
            catch (Exception ex)
            {

                LogRecord.addLog($"{No}号机器人下料前3D飞巴定位流程出错");
                XiaLiaoFeiBaPositionCheckError = true;
                Res = false;

            }



        }





        private static void XiaLiaoProcessFunc(object obj)
        {
            //下料流程 ,当前飞巴，两个机器人同时工作的，如果需要单独测试一边，需要屏蔽部分程序
            //两边机器人的同步模式，同时下完一块主板后同时等待下一个指令，需要改成异步
            FeiBaWorker FeibaNow = (FeiBaWorker)obj;


            LogRecord.addLog($"进入下料前的飞巴3D定位流程,飞巴号{FeibaNow.Row}:{FeibaNow.FeiBaNo}");
            /////////////////////////////////////////////////////////////////////////

            // --------------------------机器人飞巴定位  
            XiaLiaoFeiBaPositionCheckError = false;

            Thread threadRobotFeibaPositionRobot1 = new Thread(FeiBaPositionBeforeXiaLiao);
            Thread threadRobotFeibaPositionRobot2 = new Thread(FeiBaPositionBeforeXiaLiao);




            if (TestRobot1)
            {

                LogRecord.addLog("进入1号机器人下料前3D飞巴定位流程");
                threadRobotFeibaPositionRobot1.Start(robot1);

            }

            if (TestRobot2)
            {
                LogRecord.addLog("进入2号机器人下料前3D飞巴定位流程");
                threadRobotFeibaPositionRobot2.Start(robot2);

            }

            if (TestRobot1)
            {

                threadRobotFeibaPositionRobot1.Join();
                LogRecord.addLog("1号机器人下料前3D视觉飞巴定位流程完成");
            }

            if (TestRobot2)
            {
                threadRobotFeibaPositionRobot2.Join();
                LogRecord.addLog("2号机器人下料前3D视觉飞巴定位流程完成");
            }


            ///////////////////////////////////////////////////////////////////////////
            LogRecord.addLog("下料前的3D检查与飞巴定位环节结束");


            com2PLC.writeCoil(2, true);//PLC下料前的拍照程序结束

            ///////////////////////////////////////////////////////////
            if (XiaLiaoCheckError || XiaLiaoFeiBaPositionCheckError)
            {
                LogRecord.addLog("机器人下料前飞巴定位环异常");

            }


            /////////////////////////////////////////////////////////////


            LogRecord.addLog($"KeyPoint:{FeibaNow.Row}号飞巴下料启动，飞巴号{FeibaNow.FeiBaNo}");
            //判断是否有陪镀板
            if (FeibaNow.PeiDuBanExist)
            {
                LogRecord.addLog("KeyPoint:存在陪镀板,将进行陪镀板下料,配镀板下料需等待两个机器人同时完成");

                //向机器人1写入左陪镀板信息
                double PeiDuX = FeibaNow.PeiDuposX1;
                double PeiDuY = FeibaNow.PeiDuposY1;
                double PeiDuW = FeibaNow.PeiDuWeight;
                double PeiDuH = FeibaNow.PeiDuHeight;
                double PeiDuT = FeibaNow.PeiDuThickness;

                double PanelW = FeibaNow.PanelWidth;
                double PanelH = FeibaNow.PanelHeight;


                robot1.SendPeiDuPos(FeibaNow.PeiDuposX1, FeibaNow.PeiDuposY1, FeibaNow.Row, FeibaNow.PeiDuWeight, FeibaNow.PeiDuHeight, FeibaNow.PeiDuThickness, FeibaNow.PanelWidth, FeibaNow.PanelHeight, 2);
                robot2.SendPeiDuPos(FeibaNow.PeiDuposX2, FeibaNow.PeiDuposY2, FeibaNow.Row, FeibaNow.PeiDuWeight, FeibaNow.PeiDuHeight, FeibaNow.PeiDuThickness, FeibaNow.PanelWidth, FeibaNow.PanelHeight, 2);


                LogRecord.addLog($"KeyPoint:机器人收到的板子宽度是{FeibaNow.PanelWidth},板子高度是{FeibaNow.PanelHeight}");

                //启动陪镀板下料信号，两台机器人同步动作
                robot1.writeCoil(5, true);
                robot2.writeCoil(5, true);

                //等待陪镀板下料完成信号
                bool PeiDuXiaLiaoDone = false;
                bool PeiDuXiaLiaoDone1 = false;
                bool PeiDuXiaLiaoDone2 = false;




                if (TestRobot1)
                {


                    bool PeiDuDone1 = robot1.WaitForSingal(7);
                    if (PeiDuDone1 && !PeiDuXiaLiaoDone1)
                    {
                        LogRecord.addLog("KeyPoint:机器人1陪镀板下料完成");
                        PeiDuXiaLiaoDone1 = true;
                    }
                    // Thread.Sleep(1000);

                }

                if (TestRobot2)
                {

                    bool PeiDuDone2 = robot2.WaitForSingal(7);
                    if (PeiDuDone2 && !PeiDuXiaLiaoDone2)
                    {
                        LogRecord.addLog("KeyPoint:机器人2陪镀板下料完成");
                        PeiDuXiaLiaoDone2 = true;
                    }
                    // Thread.Sleep(1000);
                }
                PeiDuXiaLiaoDone = true;
                LogRecord.addLog("陪镀板下料完成,将进行主板下料");
            }
            else
            {
                LogRecord.addLog("当前飞巴没有陪镀板，跳过陪镀板下料流程");
                robot1.writeCoil(6, true);
                robot2.writeCoil(6, true);

            }

            ////进入主板下料流程

            LogRecord.addLog($"KeyPoint:{FeibaNow.Row}号飞巴主板下料启动，飞巴号{FeibaNow.FeiBaNo}");
            // Thread.Sleep(1000);

            RunXiaLiaoZhuBanError = false;

            //两边机器人同步下料模式
            //Thread threadRobot1XiaLiaoZhuban = new Thread(RunXiaLiaoZhuBanProcess);
            //threadRobot1XiaLiaoZhuban.Start(FeibaNow);

            //已改成异步模式
            Thread threadRobot1XiaLiaoZhuban = new Thread(RunXiaLiaoZhuBanProcessAsynFunc);
            threadRobot1XiaLiaoZhuban.Start(FeibaNow);
            // 等待线程完成
            threadRobot1XiaLiaoZhuban.Join();
            LogRecord.addLog($"KeyPoint:机器人主板下料{FeibaNow.Row}号飞巴结束");

            ///////////////////////////////////////////////////////////
            if (RunXiaLiaoZhuBanError)
            {
                LogRecord.addLog("机器人主板下料流程出错,将退出下料流程");
                // 逻辑判断异常上报
                _ = Com2Mes.SendAlarmReport("ERR_XL_ROBOT", "机器人下料动作执行失败", 3, 1);
                _ = Com2Mes.SendStatusReport("3", "故障");
                return;
            }


        }



        public static T PopTail<T>(ref Queue<T> queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue), "队列不能为 null");
            }

            if (queue.Count == 0)
            {
                throw new InvalidOperationException("队列为空，无法获取尾部元素");
            }

            // 转换为列表
            List<T> list = new List<T>(queue);

            // 获取最后一个元素
            T tail = list[list.Count - 1];

            // 移除最后一个元素
            list.RemoveAt(list.Count - 1);

            // 清空原队列，重新入队
            queue.Clear();
            foreach (var item in list)
            {
                queue.Enqueue(item);
            }

            return tail;
        }


        private static void RunXiaLiaoZhuBanRobot(object obj)
        {
            //输入PanelList，依次对PanelList进行下料     
            XiaLiaoParam Param1 = (XiaLiaoParam)obj;
            Com2RobotM robotNow = Param1.RobotNow;
            Queue<FeiBaPanelInfo> PanelList = Param1.feibaInfoList;
            FeiBaWorker FeibaNow = Param1.FeibaNow;

            while (PanelList.Count > 0)
            {



                FeiBaPanelInfo panelinfo1 = new FeiBaPanelInfo();
                //给左右机器人分别分配任务
                if (robotNow.RobotNo == 1)
                {
                    //一号机器人 从队头取
                    panelinfo1 = PanelList.Dequeue();

                }
                else if (robotNow.RobotNo == 2)
                {

                    //二号机器人，从队尾取
                    panelinfo1 = PopTail(ref PanelList);

                }
                else
                {
                    LogRecord.addLog($"下料线程出错，输入机器人号为{robotNow.RobotNo}");
                    return;

                }

                double x1 = panelinfo1.posX;
                double y1 = panelinfo1.posY;
                int row = FeibaNow.Row;
                double w = panelinfo1.PanelWidth;
                double h = panelinfo1.PanelHeight;
                double t = panelinfo1.PanelThickness;
                int ColNo = panelinfo1.ColNo;

                LogRecord.addLog($"当前下料坐标{x1},{y1},{row},ColNo:{ColNo}:准备执行机器人{robotNow.RobotNo}下料");
                LogRecord.addLog($"向PLC写入飞巴板子信息宽{w},高{h},厚{t}");
                WorkFlowControl.com2PLC.SendPanelSizeOnFeiBa(w, h, t, 200);



                robotNow.SendXiaLiaoPos(x1, y1, row, w, h, t);
                Thread.Sleep(800);
                //启动下料
                robotNow.writeCoil(9, true);
                robotNow.writeCoil(9, true);

                LogRecord.addLog($"{robotNow.RobotNo}号机器人等待当前下料动作完成坐标{x1},{y1},{row},ColNo:{ColNo}");
                //等待当前下料完成
                bool Re = false;
                Re = robotNow.WaitForSingal(10);
                if (Re)
                {
                    LogRecord.addLog($"{robotNow.RobotNo}号机器人，当前下料坐标{x1},{y1},{row}:下料完成");
                    robotNow.StatuesXiaLiao = "null";
                    CountXialiao++;
                }
                else
                    LogRecord.addLog($"{robotNow.RobotNo}号机器人，当前下料坐标{x1},{y1},{row}:下料超时");

            }


        }



        public struct XiaLiaoParam
        {
            public Queue<FeiBaPanelInfo> feibaInfoList; //1表示1号位置,2表示2号位置,3表示都要检测
            public Com2RobotM RobotNow;
            public FeiBaWorker FeibaNow;
        }

        private static void RunXiaLiaoZhuBanProcessAsynFunc(object obj)
        {
            //主板下料两边机器人的异步模式
            FeiBaWorker FeibaNow = (FeiBaWorker)obj;
            bool Res = true;

            try
            {

                //飞巴下料,检查前飞巴参数
                if (FeibaNow.PanelList.Count == FeibaNow.PanelCount)
                {


                    LogRecord.addLog($"{FeibaNow.Row}号飞巴进入主板下料流程,触发8号寄存器");
                    //robot1.writeCoil(12, true);
                    //robot2.writeCoil(12, true);

                    robot2.writeCoil(8, true);
                    robot1.writeCoil(8, true);


                    //Thread.Sleep(500);



                    //计算左右机器人下料的队列，对FeibaNow.PanelList进行分割

                    int n = (FeibaNow.PanelList.Count) / 2;

                    // 分割逻辑 PanelList1取前n个元素   ，PanelList2取n以外的元素
                    var PanelList1 = new Queue<FeiBaPanelInfo>(FeibaNow.PanelList.Take(n));
                    var PanelList2 = new Queue<FeiBaPanelInfo>(FeibaNow.PanelList.Skip(n));

                    LogRecord.addLog($"左右分割个数{n}:{PanelList1.Count},{PanelList2.Count}");


                    XiaLiaoParam Param1 = new XiaLiaoParam();
                    Param1.RobotNow = robot1;
                    Param1.feibaInfoList = PanelList1;
                    Param1.FeibaNow = FeibaNow;

                    XiaLiaoParam Param2 = new XiaLiaoParam();
                    Param2.RobotNow = robot2;
                    Param2.feibaInfoList = PanelList2;
                    Param2.FeibaNow = FeibaNow;

                    Thread threadRobot1 = new Thread(RunXiaLiaoZhuBanRobot);
                    Thread threadRobot2 = new Thread(RunXiaLiaoZhuBanRobot);

                    if (TestRobot2)
                    {
                        threadRobot2.Start(Param2);
                    }



                    if (TestRobot1)
                    {
                        threadRobot1.Start(Param1);
                    }



                    //等待两个线程完成

                    if (TestRobot1)
                    {
                        threadRobot1.Join();
                        robot1.writeCoil(11, true);
                    }

                    if (TestRobot2)
                    {
                        threadRobot2.Join();
                        robot2.writeCoil(11, true);
                    }

                    LogRecord.addLog($"KeyPoint:{FeibaNow.Row}号飞巴主板下料结束");
                    //下料任务执行完毕
                    LogRecord.addLog($"飞巴{FeibaNow.FeiBaNo},下料完毕");
                    // com2PLC.writeCoil(3, true);



                }
                else
                {

                    LogRecord.addLog($"{FeibaNow.Row}号飞巴参数有误,飞巴码{FeibaNow.FeiBaNo},主板队列和主板总数不一致,主板队列数{FeibaNow.PanelList.Count},主板总数{FeibaNow.PanelCount}");
                    RunXiaLiaoZhuBanError = true;
                }



            }
            catch (Exception ex)
            {

                LogRecord.addLog($"机器人主板下料检查流程出错{ex.Message}");
                RunXiaLiaoZhuBanError = true;

            }




        }


        //下料前的3D检测流程，由机器人自动跑点位完成


        static bool RunXiaLiaoZhuBanError = false;
        internal static void RunXiaLiaoZhuBanProcess(object obj)
        {


            FeiBaWorker FeibaNow = (FeiBaWorker)obj;
            bool Res = true;

            try
            {

                //飞巴下料,检查前飞巴参数
                if (FeibaNow.PanelList.Count == FeibaNow.PanelCount)
                {



                    LogRecord.addLog($"{FeibaNow.Row}号飞巴进入主板下料流程,触发8号寄存器");
                    //robot1.writeCoil(12, true);
                    //robot2.writeCoil(12, true);

                    robot1.writeCoil(8, true);
                    robot2.writeCoil(8, true);

                    Thread.Sleep(500);

                    while (FeibaNow.PanelList.Count > 0)
                    {
                        //给左右机器人分别分配任务

                        FeiBaPanelInfo panelinfo1 = FeibaNow.PanelList.Dequeue();
                        double x1 = panelinfo1.posX;
                        double y1 = panelinfo1.posY;
                        int row = FeibaNow.Row;
                        double w = panelinfo1.PanelWidth;
                        double h = panelinfo1.PanelHeight;
                        double t = panelinfo1.PanelThickness;
                        int ColNo = panelinfo1.ColNo;



                        LogRecord.addLog($"向PLC写入飞巴板子信息宽{w},高{h},厚{t}");
                        WorkFlowControl.com2PLC.SendPanelSizeOnFeiBa(w, h, t, 200);

                        LogRecord.addLog($"当前下料坐标{x1},{y1},{row},ColNo:{ColNo}:准备执行机器人1下料");

                        robot1.SendXiaLiaoPos(x1, y1, row, w, h, t);
                        robot1.writeCoil(9, true);


                        bool Robot2Action = false;
                        if (FeibaNow.PanelList.Count > 0)
                        {


                            //机器人2从队尾出列
                            // FeiBaPanelInfo panelinfo2 = FeiBaCurrentQian.PanelList.Dequeue();
                            FeiBaPanelInfo panelinfo2 = PopTail(ref FeibaNow.PanelList);

                            double x11 = panelinfo2.posX;
                            double y11 = panelinfo2.posY;
                            int row1 = FeibaNow.Row;
                            double w1 = panelinfo2.PanelWidth;
                            double h1 = panelinfo2.PanelHeight;
                            double t1 = panelinfo2.PanelThickness;
                            int ColNo1 = panelinfo2.ColNo;

                            robot2.SendXiaLiaoPos(x11, y11, row1, w1, h1, t1);

                            LogRecord.addLog($"当前下料坐标{x11},{y11},{row1},ColNo:{ColNo1}:准备执行机器人2下料");
                            robot2.writeCoil(9, true);
                            Robot2Action = true;

                        }

                        bool Re = false;
                        if (TestRobot1)
                        {
                            //机器人1和2已开始执行动作，等待单次动作执行结束
                            Re = robot1.WaitForSingal(10);
                            if (Re)
                                LogRecord.addLog($"机器人1，当前下料坐标{x1},{y1},{row}:下料完成");
                            else
                                LogRecord.addLog($"机器人1，当前下料坐标{x1},{y1},{row}:下料超时");
                        }



                        if (Robot2Action)
                        {
                            if (TestRobot2)
                            {
                                robot2.WaitForSingal(10);

                                if (Re)
                                    LogRecord.addLog($"机器人2，当前下料坐标{x1},{y1},{row}:下料完成");
                                else
                                    LogRecord.addLog($"机器人2，当前下料坐标{x1},{y1},{row}:下料超时");
                            }


                            //如果机器人2也在动作，等待
                            //测试一边时，屏蔽
                            //   robot2.WaitForSingal(10);
                        }

                    }

                    //下料任务执行完毕
                    LogRecord.addLog($"飞巴{FeibaNow.FeiBaNo},下料完毕");
                    // com2PLC.writeCoil(3, true);
                    robot1.writeCoil(11, true);
                    robot2.writeCoil(11, true);


                }
                else
                {

                    LogRecord.addLog($"{FeibaNow.Row}号飞巴参数有误,飞巴码{FeibaNow.FeiBaNo},主板队列和主板总数不一致");

                    RunXiaLiaoZhuBanError = true;
                }



            }
            catch (Exception ex)
            {

                LogRecord.addLog($"机器人主板下料检查流程出错{ex.Message}");
                RunXiaLiaoZhuBanError = true;

            }
        }




        static bool XiaLiaoCheckError = false;
        static int XiaLiaoCheckRowNo = -1;
        internal static void Check3DBeforeXiaLiaoRobot(Object obj)
        {

            Com2RobotM RobootNow = (Com2RobotM)obj;


            int No = RobootNow.RobotNo;
            int RowNo = XiaLiaoCheckRowNo;

            bool Res = true;

            try
            {

                //通知机器人启动下料流程,机器人到下料拍照位，依次触发3D相机拍照
                RobootNow.writeCoil(0, true);

                LogRecord.addLog($"KeyPoint:进入{No}号机器人下料检查环节,飞巴行号{RowNo}");

                Thread.Sleep(500);

                int CaptureCount = 0;


                bool XiaLiaoCheckDone = false;

                while (!XiaLiaoCheckDone)
                {

                    //读检查完成状态，如果完成，则退出循环

                    bool[] checkDone = RobootNow.readCoils(4, 1);

                    if (checkDone[0])
                    {
                        //读到后复位
                        RobootNow.writeCoil(4, false);

                        //下料前检查动作流程结束
                        LogRecord.addLog($"KeyPoint:{No}号机器人下料前检查动作结束");
                        XiaLiaoCheckDone = true;
                        break;
                    }

                    bool GetRes = false;
                    //等待相机拍照触发信号
                    while (!GetRes)
                    {
                        //读机器人的拍照触发信号
                        bool[] Start3DPro = RobootNow.readCoils(1, 1);
                        if (Start3DPro[0])
                        {

                            //读到后复位
                            RobootNow.writeCoil(1, false);

                            //机器人到达拍照位，触发Vision 拍照
                            LogRecord.addLog($"KeyPoint:{No}号机器人到达第{++CaptureCount}个3D检查拍照位置");
                            bool res = Com2Vision.Start3DCaptureXiaLiao(No, RowNo);
                            LogRecord.addLog($"拍照结果{res}");

                            if (res)
                            {
                                //拍照结果OK
                                RobootNow.writeCoil(2, true);

                            }
                            else
                            {

                                LogRecord.addLog($"拍照结果NG");
                                //拍照结果NG
                                RobootNow.writeCoil(3, true);
                                //退出循环
                                Res = false;
                                XiaLiaoCheckError = true;
                                break;

                            }
                            GetRes = true;
                            LogRecord.addLog($"{No}号机器人一次3D拍照完成,等待下一个拍照触发信号");
                        }
                        Thread.Sleep(800);
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {

                LogRecord.addLog($"{No}号机器人下料检查流程出错");
                Res = false;

            }


        }

        internal static void RunShangLiaoSaoMa1()
        {
            LogRecord.addLog("KeyPoint:进行1号传送带主板上料扫码");
            string PanelCode1 = bacodeScan1.StarBarcode();

            if (PanelCode1 == "" || PanelCode1.EndsWith("ead"))
            {
                //1号扫码枪扫码不成功，启动5号扫码枪
                PanelCode1 = bacodeScan5.StarBarcode();
            }

            if (PanelCode1 == "" || PanelCode1.EndsWith("ead"))
            {
                LogRecord.addLog("扫码不成功");
                com2PLC.writeCoil(44, true);
                return;
            }


            FeiBaPanelInfo panelinfoTemp = new FeiBaPanelInfo();
            panelinfoTemp.PanelNo = PanelCode1;

            //检查是否在工单列表内,离线时不检查板码在不在工单里面
            if (WorkOrderCurrent.CodeList.Contains(PanelCode1) || isOfflineDebug)
            {
                //在工单列表内
                LogRecord.addLog("输送线1当前主板码" + PanelCode1);

                //写入数据
                panelinfoTemp.PanelWidth = WorkOrderCurrent.pallet_width;
                panelinfoTemp.PanelHeight = WorkOrderCurrent.pallet_length;
                panelinfoTemp.PanelThickness = WorkOrderCurrent.pallet_thickness;
                panelinfoTemp.PanelWeight = WorkOrderCurrent.pallet_weight;

                ///--------
                double width = WorkOrderCurrent.pallet_width;
                double height = WorkOrderCurrent.pallet_length;
                double thickness = WorkOrderCurrent.pallet_thickness;


                bool sendResult = Com2Vision.SendPanelSizeTo3DVision(width, height, thickness);

                if (sendResult)
                {
                    LogRecord.addLog($"发送板子尺寸到3D：宽度:{width},高度:{height},厚度{thickness}");
                }
                else
                {
                    LogRecord.addLog($"发送板子失败");
                    return;
                }


                    //进入队列
                    PanelInfoList1.Enqueue(panelinfoTemp);
                //  PanelInfoList1.AddLast(panelinfoTemp);

                //写入PLC 写入板子信息
                double w = WorkOrderCurrent.pallet_width;
                double h = WorkOrderCurrent.pallet_length;
                double t = WorkOrderCurrent.pallet_thickness;
                double weight = WorkOrderCurrent.pallet_weight;

                com2PLC.SendPanelSizeOnChuanSongDai1(w, h, t, weight);


                //扫码成功
                com2PLC.writeCoil(42, true);

                LogRecord.addLog($"KeyPoint:输送线1扫码完成,板信息已写入PLC：宽{w},高{h},厚{t}");

            }
            else
            {
                LogRecord.addLog("KeyPoint:输送线1当前主板码:" + PanelCode1 + "不在工单列表内");
                MessageBox.Show("输送线1当前主板码:" + PanelCode1 + "不在工单列表内!请检查来料后复位重启");

                //扫码失败
                com2PLC.writeCoil(44, true);
            }




        }

        internal static void RunShangLiaoSaoMa2()
        {
            LogRecord.addLog("KeyPoint:进行2号传送带主板上料扫码");
            string PanelCode1 = bacodeScan2.StarBarcode();

            if (PanelCode1 == "" || PanelCode1.EndsWith("ead"))
            {
                //2号扫码枪扫码不成功，启动6号扫码枪
                PanelCode1 = bacodeScan6.StarBarcode();
            }

            if (PanelCode1 == "" || PanelCode1.EndsWith("ead"))
            {
                LogRecord.addLog("扫码不成功");
                com2PLC.writeCoil(45, true);
                return;
            }


            FeiBaPanelInfo panelinfoTemp = new FeiBaPanelInfo();
            panelinfoTemp.PanelNo = PanelCode1;

            //检查是否在工单列表内
            if (WorkOrderCurrent.CodeList.Contains(PanelCode1) || isOfflineDebug)
            {
                //在工单列表内
                LogRecord.addLog("输送线2当前主板码" + PanelCode1);

                //写入数据
                panelinfoTemp.PanelWidth = WorkOrderCurrent.pallet_width;
                panelinfoTemp.PanelHeight = WorkOrderCurrent.pallet_length;
                panelinfoTemp.PanelThickness = WorkOrderCurrent.pallet_thickness;
                panelinfoTemp.PanelWeight = WorkOrderCurrent.pallet_weight;

                //进入队列
                PanelInfoList2.Enqueue(panelinfoTemp);
                // PanelInfoList2.AddLast(panelinfoTemp);

                //写入PLC 写入板子信息
                double w = WorkOrderCurrent.pallet_width;
                double h = WorkOrderCurrent.pallet_length;
                double t = WorkOrderCurrent.pallet_thickness;
                double weight = WorkOrderCurrent.pallet_weight;
                com2PLC.SendPanelSizeOnChuanSongDai2(w, h, t, weight);

                //
                double width = WorkOrderCurrent.pallet_width;
                double height = WorkOrderCurrent.pallet_length;
                double thickness = WorkOrderCurrent.pallet_thickness;


                bool sendResult = Com2Vision.SendPanelSizeTo3DVision(width, height, thickness);

                if (sendResult)
                {
                    LogRecord.addLog($"发送板子尺寸到3D：宽度:{width},高度:{height},厚度{thickness}");
                }
                else
                {
                    LogRecord.addLog($"发送板子板子失败");
                    return;
                }


                //扫码成功信号
                com2PLC.writeCoil(43, true);

                LogRecord.addLog($"KeyPoint:输送线2扫码完成,板信息已写入PLC：宽{w},高{h},厚{t}");
            }
            else
            {
                LogRecord.addLog("KeyPoint:输送线2当前主板码:" + PanelCode1 + "不在当前工单列表内");
                MessageBox.Show("输送线2当前主板码:" + PanelCode1 + "不在工单列表内!请检查来料后复位重启");

                //扫码失败
                com2PLC.writeCoil(45, true);
            }


        }





        internal static void ShangLiao2DPositionProcess(int RobotNo)
        {

            try
            {
                //2D拍照定位过程封装，两边代码共用
                LogRecord.addLog($"进入输送线{RobotNo}主板2D定位与机器人从输送线抓取流程");

                Com2RobotM RobotNow;
                int AddrOK = 0;
                int AddrNG = 0;
                int ShangLiaoCount = 0;
                int CurrentShangLiaoCount = 0;


                int ShangLiaoCountCurrentHou = 0;
                int ShangLiaoCountCurrentQian = 0;


                Queue<FeiBaPanelInfo> PanelInfoList = new Queue<FeiBaPanelInfo>();

                if (RobotNo == 1)
                {
                    RobotNow = robot1;
                    AddrOK = 46;
                    AddrNG = 48;
                    PanelInfoList = PanelInfoList1;
                    ShangLiaoCount = ShangLiaoCount1All;

                    ShangLiaoCountCurrentHou = FeiBaCurrentHou.ShangLiaoCount1;
                    ShangLiaoCountCurrentQian = FeiBaCurrentQian.ShangLiaoCount1;


                }
                else
                {
                    RobotNow = robot2;
                    AddrOK = 47;
                    AddrNG = 49;
                    PanelInfoList = PanelInfoList2;
                    ShangLiaoCount = ShangLiaoCount2All;

                    ShangLiaoCountCurrentHou = FeiBaCurrentHou.ShangLiaoCount2;
                    ShangLiaoCountCurrentQian = FeiBaCurrentQian.ShangLiaoCount2;

                }



                if (PanelInfoList.Count() < 1)
                {
                    LogRecord.addLog($"KeyPoint:扫码队列为空,当前物料未经过扫码，需从头放置重来");
                    return;

                }

                LogRecord.addLog($"KeyPoint:机器人{RobotNo}:启动2D拍照定位主板在传送带上的位置");

                DateTime startTime = DateTime.Now;
                //触发2D相机拍照
                bool Res = Com2VisionMaster.StartCapture(out double x, out double y, out double r, RobotNo);

                if (Res)
                {
                    LogRecord.addLog($"机器人{RobotNo}2D拍照成功");

                    double w = WorkOrderCurrent.pallet_width;
                    double h = WorkOrderCurrent.pallet_length;
                    double t = WorkOrderCurrent.pallet_thickness;

                    //向机器人写入坐标,启动机器人抓料
                    RobotNow.SendChuanSongDaiPos(x, y, r, w, h, t);

                    Thread.Sleep(800);
                    //启动抓料
                    RobotNow.writeCoil(35, true);
                    RobotNow.writeCoil(25, true);



                    com2PLC.writeCoil(AddrOK, true);


                    LogRecord.addLog($"KeyPoint:{RobotNo}号机器人:从传送带上取料动作已启动,抓板坐标{x},{y},{r},{w},{h}");
                    LogRecord.addLog($"{RobotNo}号机器人:等待传送带取料完成信号");


                    TimeSpan duration = DateTime.Now - startTime; // 计算耗时
                    LogRecord.addLog($"KeyPoint:机器人{RobotNo}:从2D拍照到启动机器人抓料耗时:{duration.TotalMilliseconds}ms");


                    //等待取料完成信号
                    bool r1 = RobotNow.WaitForSingal(26);
                    if (!r1)
                    {
                        LogRecord.addLog($"{RobotNo}号机器人:信号等待失败");
                        return;
                    }

                    LogRecord.addLog($"{RobotNo}号机器人:从传送带取料完成");


                    /////////////////////////////////////////////////////////////////////////////
                    //如果只测试传送带抓取，程序在这里return
                    //return;


                    ////////////////////////////////////////////////////////////////////
                    ///以下程序向飞巴上料

                    //发送上料目标位置到机器人1

                    FeiBaPanelInfo PannelInfoTemp = PanelInfoList.Dequeue();//PanelInfoList1  当前的PannelInfoTemp只有条码值和基础尺寸信息,还没有坐标数据


                    LogRecord.addLog($"KeyPoint:{RobotNo}号机器人,当前后飞巴上料计数{ShangLiaoCountCurrentHou}/{ShangLiaoCount}");

                    //判断当前工作对象是前飞巴还是后飞巴，机器人1和机器人2分别判断是前飞巴还是后飞巴

                    //////////////////////////////////////////////////////////////////////////
                    if (ShangLiaoCountCurrentHou < ShangLiaoCount)
                    {

                        //前飞巴还没有满,上前飞巴
                        LogRecord.addLog($"KeyPoint:{RobotNo}号机器人,当前为后飞巴上料");

                        //机器人1 从，从队首出队列                    
                        //上料是从中间往两边上的逻辑 在这里控制

                        //int countAll = FeiBaCurrentHou.PosXQueue.Count;
                        //计算当前上料的序号
                        int CurrentColNo = 0;


                        if (RobotNo == 1)
                        {
                            CurrentColNo = ShangLiaoCount - ShangLiaoCountCurrentHou - 1;
                            LogRecord.addLog($"KeyPoint:{RobotNo}号机器人:后飞巴:当前上料的列号{CurrentColNo + 1}");

                            if (CurrentColNo < 0)
                            {
                                LogRecord.addLog($"{RobotNo}号机器人上料当前列号计算出错");
                            }
                        }

                        if (RobotNo == 2)
                        {

                            CurrentColNo = ShangLiaoCountCurrentHou + ShangLiaoCount1All;
                            LogRecord.addLog($"KeyPoint:{RobotNo}号机器人:后飞巴:当前上料的列号{CurrentColNo + 1}");
                            if (CurrentColNo < 0)
                            {
                                LogRecord.addLog($"{RobotNo}号机器人上料当列号计算出错");
                            }

                        }

                        PannelInfoTemp.posX = GetElementAtIndex(FeiBaCurrentHou.PosXQueue, CurrentColNo);
                        PannelInfoTemp.posY = GetElementAtIndex(FeiBaCurrentHou.PosYQueue, CurrentColNo);



                        ////放到流程最后再加，确保流程结束才计数

                        //ShangLiaoCountCurrentHou++;
                        //if (RobotNo == 1)
                        //    FeiBaCurrentHou.ShangLiaoCount1 = ShangLiaoCountCurrentHou;

                        //if (RobotNo == 2)
                        //    FeiBaCurrentHou.ShangLiaoCount2 = ShangLiaoCountCurrentHou;


                        PannelInfoTemp.ColNo = CurrentColNo + 1;

                        //写入飞巴目标位置
                        double w1 = FeiBaCurrentHou.PanelWidth;
                        double h1 = FeiBaCurrentHou.PanelHeight;
                        double t1 = FeiBaCurrentHou.PanelThickness;

                        PannelInfoTemp.PanelWidth = w1;
                        PannelInfoTemp.PanelHeight = h1;
                        PannelInfoTemp.PanelThickness = t1;


                        LogRecord.addLog($"KeyPoint:{RobotNo}号机器人:写入飞巴上料坐标{PannelInfoTemp.posX},{PannelInfoTemp.posY},宽度{w1},高度{h1},行号 2,列号{PannelInfoTemp.ColNo}");
                        RobotNow.SendShangLiaoPos(PannelInfoTemp.posX, PannelInfoTemp.posY, 2, w1, h1, t1);


                        Thread.Sleep(100);
                        LogRecord.addLog($"启动飞巴上料");
                        //启动机器人向飞巴上料
                        RobotNow.writeCoil(37, true);


                        LogRecord.addLog($"{RobotNo}号机器人:等待单次飞巴上料完成信号H38...");
                        //等待飞巴上料完成
                        bool Res1 = RobotNow.WaitForSingal(38);


                        if (Res1)
                        {
                            LogRecord.addLog($"KeyPoint:{RobotNo}号机器人单次飞巴上料完成,后飞巴,板号{PannelInfoTemp.PanelNo},列号{PannelInfoTemp.ColNo}");
                            RobotNow.StatuesShangLiao = "null";

                            //写入飞巴和工单记录
                            FeiBaCurrentHou.PanelList.Enqueue(PannelInfoTemp);

                            //判断条码值是否存在
                            //if (!WorkOrderCurrent.CodeListDone.Contains(PannelInfoTemp.PanelNo))
                            WorkOrderCurrent.CodeListDone.Add(PannelInfoTemp.PanelNo);
                            //else
                            //    LogRecord.addLog($"板码重复:" + PannelInfoTemp.PanelNo);



                            if (ShangLiaoCountCurrentHou + 1 >= ShangLiaoCount)
                            {
                                LogRecord.addLog($"KeyPoint:{RobotNo}号机器人: 后飞巴上料全部结束 ,已上料 {ShangLiaoCountCurrentHou + 1} 个");

                                if (FeiBaCurrentHou.PeiDuBanExist)
                                {

                                    ////告诉plc需要人工去上带夹角的配镀板
                                    //if (CheckPeiDuAnnle(FeiBaCurrentHou.PanelWidth))
                                    //{
                                    //    com2PLC.writeCoil(85, true);  //检测到带夹边的配镀板
                                    //    RobotNow.writeCoil(39, true);
                                    //    LogRecord.addLog($"向PLC发送85信号，等待人工处理");

                                    //    bool res = com2PLC.WaitForSingal(86); 
                                    //    if (!res)
                                    //    {
                                    //        LogRecord.addLog("等待人工处理带夹边配镀板超时");
                                    //        return;
                                    //    }
                                    //    LogRecord.addLog("收到人工处理带夹边陪镀板完成信号86，继续执行陪镀板上料流程");
                                    //}



                                    Thread.Sleep(100);

                                    if (RobotNo == 1)
                                    {


                                        //检查陪镀板是否有重新上料更新
                                        //进入陪镀板检查流程
                                        LogRecord.addLog($"{RobotNo}号机器人 进入陪镀板更新状态检查流程");
                                        Thread threadRobot1 = new Thread(PeiDuBanCheck);
                                        threadRobot1.Start(robot1);
                                        threadRobot1.Join();


                                        RobotNow.writeCoil(39, true);
                                        LogRecord.addLog($"KeyPoint:启动{RobotNo}号机器人后飞巴陪镀板上料");

                                        //读取PLC的陪镀板仓信息
                                        bool[] PeiDuStates1 = com2PLC.readCoils(36, 1);//36,37 1号陪镀仓的状态信息
                                        bool[] PeiDuStates2 = com2PLC.readCoils(37, 1);//36,37 1号陪镀仓的状态信息

                                        LogRecord.addLog($"当前1号陪镀仓状态{PeiDuStates1[0]},{PeiDuStates2[0]}");
                                        int PeiDuNo = 0;
                                        if (PeiDuStates1[0])
                                        {
                                            LogRecord.addLog($"当前:1号陪镀仓取料位1号");
                                            PeiDuNo = 0;

                                        }
                                        else
                                        {
                                            LogRecord.addLog($"当前:1号陪镀仓取料位2号");
                                            PeiDuNo = 1;
                                        }

                                        //写入陪镀板信息
                                        RobotNow.SendPeiDuPos(FeiBaCurrentHou.PeiDuposX1, FeiBaCurrentHou.PeiDuposY1, 2,
                                                FeiBaCurrentHou.PeiDuWidth, FeiBaCurrentHou.PeiDuHeight, FeiBaCurrentHou.PeiDuThickness, FeiBaCurrentHou.PanelWidth, FeiBaCurrentHou.PanelHeight, 1, PeiDuNo);
                                    }
                                    if (RobotNo == 2)
                                    {
                                        //检查陪镀板是否有重新上料更新
                                        //进入陪镀板检查流程
                                        LogRecord.addLog($"{RobotNo}号机器人 进入陪镀板更新状态检查流程");
                                        Thread threadRobot1 = new Thread(PeiDuBanCheck);
                                        threadRobot1.Start(robot2);
                                        threadRobot1.Join();


                                        RobotNow.writeCoil(39, true);
                                        LogRecord.addLog($"KeyPoint:启动{RobotNo}号机器人后飞巴陪镀板上料");


                                        //读取PLC的陪镀板仓信息
                                        bool[] PeiDuStates1 = com2PLC.readCoils(38, 1);//38,39 2号陪镀仓的状态信息
                                        bool[] PeiDuStates2 = com2PLC.readCoils(39, 1);//38,39 2号陪镀仓的状态信息

                                        LogRecord.addLog($"当前2号陪镀仓状态{PeiDuStates1[0]},{PeiDuStates2[0]}");
                                        int PeiDuNo = 0;
                                        if (PeiDuStates1[0])
                                        {
                                            LogRecord.addLog($"当前:2号陪镀仓取料位1号");
                                            PeiDuNo = 0;

                                        }
                                        else
                                        {
                                            LogRecord.addLog($"当前:2号陪镀仓取料位2号");
                                            PeiDuNo = 1;
                                        }

                                        //写入陪镀板信息
                                        RobotNow.SendPeiDuPos(FeiBaCurrentHou.PeiDuposX2, FeiBaCurrentHou.PeiDuposY2, 2,
                                            FeiBaCurrentHou.PeiDuWidth, FeiBaCurrentHou.PeiDuHeight, FeiBaCurrentHou.PeiDuThickness, FeiBaCurrentHou.PanelWidth, FeiBaCurrentHou.PanelHeight, 1, PeiDuNo);
                                    }

                                    Thread.Sleep(100);



                                    //启动陪镀板上料
                                    RobotNow.writeCoil(40, true);
                                    bool ResPeiDuDone = RobotNow.WaitForSingal(42);
                                    if (ResPeiDuDone)
                                    {
                                        LogRecord.addLog($"KeyPoint:{RobotNo}号机器人前飞巴陪镀板上料完成");
                                        if (RobotNo == 1)
                                            com2PLC.writeCoil(29, true);

                                        if (RobotNo == 2)
                                            com2PLC.writeCoil(34, true);
                                    }
                                    else
                                    {
                                        LogRecord.addLog($"KeyPoint:{RobotNo}号机器人前飞巴陪镀板上料超时");
                                        return;

                                    }
                                }
                                else
                                {
                                    LogRecord.addLog($"当前无陪镀板 ");
                                    RobotNow.writeCoil(41, true);
                                }
                            }

                            //向PLC发送单次飞巴上料结束信号，，可以进行下一次的上料,左右缓存地址号分别为55,56
                            com2PLC.writeCoil(54 + RobotNo, true);

                            //放到流程最后再加，确保流程结束才计数
                            ShangLiaoCountCurrentHou++;
                            if (RobotNo == 1)
                                FeiBaCurrentHou.ShangLiaoCount1 = ShangLiaoCountCurrentHou;

                            if (RobotNo == 2)
                                FeiBaCurrentHou.ShangLiaoCount2 = ShangLiaoCountCurrentHou;

                            LogRecord.addLog($"KeyPoint:{RobotNo}号机器人:前飞巴:当前上料的列号{CurrentColNo + 1},上料流程结束");

                            CountShangliao++;
                        }
                        else
                        {
                            LogRecord.addLog($"等待机器人{RobotNo}单次飞巴上料(后飞巴)完成信号H38,超时,需要将当前板子复位到缓寸上料,重新拍照抓取");
                            return;
                        }
                    }
                    else if (ShangLiaoCountCurrentHou == ShangLiaoCount && ShangLiaoCountCurrentQian < ShangLiaoCount)
                    {
                        ////////////////////////////////////////////////////////////////////////////////////////////////////
                        //后飞巴已满，上前飞巴
                        LogRecord.addLog($"KeyPoint:{RobotNo}号机器人当前为前飞巴上料");


                        //int countAll = FeiBaCurrentQian.PosXQueue.Count;

                        //计算当前上料的序号
                        int CurrentColNo = 0;

                        if (RobotNo == 1)
                        {
                            CurrentColNo = ShangLiaoCount - ShangLiaoCountCurrentQian - 1;
                            LogRecord.addLog($"KeyPoint:{RobotNo}号机器人:前飞巴:当前上料的列号{CurrentColNo + 1}");

                            if (CurrentColNo < 0)
                            {
                                LogRecord.addLog($"{RobotNo}号机器人上料当前行号计算出错");
                            }
                        }

                        if (RobotNo == 2)
                        {

                            CurrentColNo = ShangLiaoCountCurrentQian + ShangLiaoCount1All;
                            LogRecord.addLog($"KeyPoint:{RobotNo}号机器人:前飞巴:当前上料的列号{CurrentColNo + 1}");
                            if (CurrentColNo < 0)
                            {
                                LogRecord.addLog($"{RobotNo}号机器人上料当前行号计算出错");
                            }

                        }


                        PannelInfoTemp.posX = GetElementAtIndex(FeiBaCurrentQian.PosXQueue, CurrentColNo);
                        PannelInfoTemp.posY = GetElementAtIndex(FeiBaCurrentQian.PosYQueue, CurrentColNo);

                        ////放到流程最后再加
                        //ShangLiaoCountCurrentQian++;
                        //if (RobotNo == 1)
                        //    FeiBaCurrentQian.ShangLiaoCount1 = ShangLiaoCountCurrentQian;

                        //if (RobotNo == 2)
                        //    FeiBaCurrentQian.ShangLiaoCount2 = ShangLiaoCountCurrentQian;


                        PannelInfoTemp.ColNo = CurrentColNo + 1;


                        //机器人1 从，队首出队列
                        //PannelInfoTemp.posX = FeiBaCurrentQian.PosXQueue.First();
                        //PannelInfoTemp.posY = FeiBaCurrentQian.PosYQueue.First();
                        //FeiBaCurrentQian.PosXQueue.RemoveFirst();
                        //FeiBaCurrentQian.PosYQueue.RemoveFirst();


                        //写入飞巴目标位置
                        double w1 = FeiBaCurrentQian.PanelWidth;
                        double h1 = FeiBaCurrentQian.PanelHeight;
                        double t1 = FeiBaCurrentQian.PanelThickness;



                        LogRecord.addLog($"KeyPoint:{RobotNo}号机器人:写入飞巴上料坐标{PannelInfoTemp.posX},{PannelInfoTemp.posY},宽度{w1},高度{h1},行号 1,列号{PannelInfoTemp.ColNo}");
                        RobotNow.SendShangLiaoPos(PannelInfoTemp.posX, PannelInfoTemp.posY, 1, w1, h1, t1);

                        //启动机器人向飞巴上料
                        RobotNow.writeCoil(37, true);

                        LogRecord.addLog($"等待机器人{RobotNo}单次飞巴上料完成信号H38...");
                        //等待飞巴上料完成
                        bool Res1 = RobotNow.WaitForSingal(38);
                        LogRecord.addLog($"{RobotNo}号机器人单次飞巴上料完成信号");

                        if (Res1)
                        {

                            LogRecord.addLog($"KeyPoint:单次飞巴上料结束,前飞巴,板号{PannelInfoTemp.PanelNo}, 列号{PannelInfoTemp.ColNo}");
                            RobotNow.StatuesShangLiao = "null";
                            //写入飞巴和工单记录
                            FeiBaCurrentQian.PanelList.Enqueue(PannelInfoTemp);
                            WorkOrderCurrent.CodeListDone.Add(PannelInfoTemp.PanelNo);

                            if (ShangLiaoCountCurrentQian + 1 >= ShangLiaoCount)
                            {
                                LogRecord.addLog($"KeyPoint:{RobotNo}号机器人 前飞巴上料全部结束 ,已上料 {ShangLiaoCountCurrentQian + 1} 个");

                                if (FeiBaCurrentQian.PeiDuBanExist)
                                {

                                    //if (FeiBaCurrentQian.PeiDuBanExist)
                                    //{
                                    //    //告诉plc需要人工去上带夹角的配镀板
                                    //    if (CheckPeiDuAnnle(FeiBaCurrentHou.PanelWidth))
                                    //    {
                                    //        com2PLC.writeCoil(85, true);
                                    //        bool res = com2PLC.WaitForSingal(86);
                                    //        if (!res)
                                    //        {
                                    //            LogRecord.addLog("等待人工处理带夹边配镀板超时");
                                    //            return;
                                    //        }
                                    //        LogRecord.addLog("收到人工处理带夹边陪镀板完成信号86，继续执行陪镀板上料流程");
                                    //    }
                                    //}




                                    //陪镀板上料前检查陪镀板尺寸


                                    Thread.Sleep(100);

                                    if (RobotNo == 1)
                                    {

                                        //进入陪镀板检查流程
                                        LogRecord.addLog($"{RobotNo}号机器人 进入陪镀板更新状态检查流程");
                                        Thread threadRobot1 = new Thread(PeiDuBanCheck);
                                        threadRobot1.Start(robot1);
                                        threadRobot1.Join();

                                        RobotNow.writeCoil(39, true);
                                        LogRecord.addLog($"KeyPoint:{RobotNo}号机器人前飞巴陪镀板上料");

                                        //读取PLC的陪镀板仓信息
                                        bool[] PeiDuStates1 = com2PLC.readCoils(36, 1);//36,37 1号陪镀仓的状态信息
                                        bool[] PeiDuStates2 = com2PLC.readCoils(37, 1);//36,37 1号陪镀仓的状态信息

                                        LogRecord.addLog($"当前1号陪镀仓状态{PeiDuStates1[0]},{PeiDuStates2[0]}");
                                        int PeiDuNo = 0;
                                        if (PeiDuStates1[0])
                                        {
                                            LogRecord.addLog($"当前:1号陪镀仓取料位1号");
                                            PeiDuNo = 0;

                                        }
                                        else
                                        {
                                            LogRecord.addLog($"当前:1号陪镀仓取料位2号");
                                            PeiDuNo = 1;
                                        }

                                        //写入陪镀板信息
                                        RobotNow.SendPeiDuPos(FeiBaCurrentQian.PeiDuposX1, FeiBaCurrentQian.PeiDuposY1, 1,
                                            FeiBaCurrentQian.PeiDuWidth, FeiBaCurrentQian.PeiDuHeight, FeiBaCurrentQian.PeiDuThickness, FeiBaCurrentQian.PanelWidth, FeiBaCurrentQian.PanelHeight, 1, PeiDuNo);
                                    }
                                    if (RobotNo == 2)
                                    {
                                        //进入陪镀板检查流程
                                        LogRecord.addLog($"{RobotNo}号机器人 进入陪镀板更新状态检查流程");
                                        Thread threadRobot1 = new Thread(PeiDuBanCheck);
                                        threadRobot1.Start(robot2);
                                        threadRobot1.Join();


                                        RobotNow.writeCoil(39, true);
                                        LogRecord.addLog($"KeyPoint:{RobotNo}号机器人前飞巴陪镀板上料");

                                        bool[] PeiDuStates1 = com2PLC.readCoils(38, 1);//38,39 2号陪镀仓的状态信息
                                        bool[] PeiDuStates2 = com2PLC.readCoils(39, 1);//38,39 2号陪镀仓的状态信息

                                        LogRecord.addLog($"当前2号陪镀仓状态{PeiDuStates1[0]},{PeiDuStates2[0]}");
                                        int PeiDuNo = 0;
                                        if (PeiDuStates1[0])
                                        {
                                            LogRecord.addLog($"当前:2号陪镀仓取料位1号");
                                            PeiDuNo = 0;

                                        }
                                        else
                                        {
                                            LogRecord.addLog($"当前:2号陪镀仓取料位2号");
                                            PeiDuNo = 1;
                                        }

                                        //写入陪镀板信息
                                        RobotNow.SendPeiDuPos(FeiBaCurrentQian.PeiDuposX2, FeiBaCurrentQian.PeiDuposY2, 1,
                                            FeiBaCurrentQian.PeiDuWidth, FeiBaCurrentQian.PeiDuHeight, FeiBaCurrentQian.PeiDuThickness, FeiBaCurrentQian.PanelWidth, FeiBaCurrentQian.PanelHeight, 1, PeiDuNo);
                                    }

                                    Thread.Sleep(100);
                                    //启动陪镀板上料
                                    RobotNow.writeCoil(40, true);
                                    bool ResPeiDuDone = RobotNow.WaitForSingal(42);
                                    if (ResPeiDuDone)
                                    {
                                        LogRecord.addLog($"KeyPoint:{RobotNo}号机器人前飞巴陪镀板上料完成");
                                        if (RobotNo == 1)
                                            com2PLC.writeCoil(29, true);

                                        if (RobotNo == 2)
                                            com2PLC.writeCoil(34, true);
                                    }
                                    else
                                    {
                                        LogRecord.addLog($"KeyPoint:{RobotNo}号机器人前飞巴陪镀板上料超时");
                                        return;

                                    }
                                }
                                else
                                {
                                    LogRecord.addLog($"当前无陪镀板 ");
                                    RobotNow.writeCoil(41, true);
                                }

                                //TODO:前飞巴全部上料结束，可以进行飞巴信息保存，即M57的动作可以移到这里，但只能执行一次
                                //


                                ////////



                            }

                            //向PLC发送单次飞巴上料结束信号，，可以进行下一次的上料,左右缓存地址号分别为55,56
                            com2PLC.writeCoil(54 + RobotNo, true);

                            ////放到流程最后再加
                            ShangLiaoCountCurrentQian++;
                            if (RobotNo == 1)
                                FeiBaCurrentQian.ShangLiaoCount1 = ShangLiaoCountCurrentQian;

                            if (RobotNo == 2)
                                FeiBaCurrentQian.ShangLiaoCount2 = ShangLiaoCountCurrentQian;

                            LogRecord.addLog($"KeyPoint:{RobotNo}号机器人:前飞巴:当前上料的列号{CurrentColNo + 1},上料流程结束");
                            CountShangliao++;
                        }
                        else
                        {
                            LogRecord.addLog($"等待机器人{RobotNo}单次飞巴上料(前飞巴)完成信号H38,超时,需要将当前板子复位到缓寸上料,重新拍照抓取");
                            return;

                        }
                    }
                    else
                    {
                        //前后飞巴都满了
                        LogRecord.addLog($"{RobotNo}号机器人上料前后飞巴都满载了");
                    }
                }
                else
                {

                    LogRecord.addLog($"机器人{RobotNo}2D拍照失败,等待手动确认后继续");


                    // 基本确认对话框
                    DialogResult result = MessageBox.Show($"机器人{RobotNo}2D拍照失败,点击确认继续退出流程。重新触发拍照",
                                                         "确认",
                                                         MessageBoxButtons.OKCancel,
                                                         MessageBoxIcon.Question);

                    if (result == DialogResult.OK)
                    {
                        // 用户点击了确定
                        Console.WriteLine("用户点击了确定");
                    }
                    else
                    {
                        // 用户点击了取消或关闭
                        Console.WriteLine("用户取消操作");
                    }


                    LogRecord.addLog($"向PLC发送机器人{RobotNo}2D拍照失败拍照失败信号");
                    com2PLC.writeCoil(AddrNG, true);
                    return;
                }


            }
            catch (Exception ex)
            {

                LogRecord.addLog($"机器人{RobotNo}上料拍照流程出错" + ex.Message);
            }




        }





        //传送带2D定位，传送带取料，上料到飞巴
        public static bool ShangLiaoOnceRunning1 = false;
        public static bool ShangLiaoOnceRunning2 = false;
        internal static void RunShangLiao2DPosition1()
        {
            if (!ShangLiaoOnceRunning1)
            {
                LogRecord.addLog("进入输送线1主板2D定位与机器人从输送线抓取流程");
                int RobotNo = 1;
                ShangLiaoOnceRunning1 = true;
                ShangLiao2DPositionProcess(RobotNo);
                ShangLiaoOnceRunning1 = false;

            }
            else
            {
                LogRecord.addLog("当前已触发机器人1一次上料循环，但上一次上料循环还没有结束");
            }

            /* LogRecord.addLog("进入输送线1主板2D定位与机器人1从输送线抓取流程");
             int RobotNo = 1;
             ShangLiao2DPositionProcess(RobotNo);*/

        }


        internal static void RunShangLiao2DPosition2()
        {

            if (!ShangLiaoOnceRunning2)
            {
                LogRecord.addLog("进入输送线2主板2D定位与机器人2从输送线抓取流程");
                int RobotNo = 2;
                ShangLiaoOnceRunning2 = true;
                ShangLiao2DPositionProcess(RobotNo);
                ShangLiaoOnceRunning2 = false;

            }
            else
            {
                LogRecord.addLog("当前已触发机器人2一次上料循环，但上一次上料循环还没有结束");
            }

            /*LogRecord.addLog("进入输送线2主板2D定位与机器人2从输送线抓取流程");
            int RobotNo = 2;
            ShangLiao2DPositionProcess(RobotNo);*/

        }

        internal static void RunShangLiaoPostPro()
        {
            //上料完成后处理，PLC 检测到两边的上料循环都循环完毕后，触发这个线程，进行飞巴上主板码的上传

            //当前飞巴信息打印

            try
            {

                LogRecord.addLog("飞巴上料完成,飞巴信息上传MES,保存飞巴文件");


                try
                {
                    LogRecord.addLog("飞巴板载数据重新排序");
                    //需要对飞巴里面的Queue<FeiBaPanelInfo> PanelList 板子顺序进行排序
                    FeiBaCurrentQian.PanelList = FeiBaWorker.SortPanelQueue(FeiBaCurrentQian.PanelList);
                    FeiBaCurrentHou.PanelList = FeiBaWorker.SortPanelQueue(FeiBaCurrentHou.PanelList);

                }
                catch (Exception ex)
                {
                    LogRecord.addLog("飞巴板载数据重新排序出错" + ex.Message);
                }


                //////////
                ///
                FeiBaCurrentQian.PrintFeiBaWorker();
                FeiBaCurrentHou.PrintFeiBaWorker();



                //保存飞巴文件  
                FeiBaWorker.SavefeibaWorker(FeiBaCurrentQian);
                FeiBaWorker.SavefeibaWorker(FeiBaCurrentHou);


                //通知机器人飞巴上料结束
                robot1.writeCoil(43, true);
                robot2.writeCoil(43, true);

                com2PLC.writeCoil(22, true);

                //TODO:向MES上传飞巴信息

            }
            catch (Exception ex)
            {
                LogRecord.addLog("飞巴后处理流程出错" + ex.Message);

            }
        }

        /// <summary>
        ///  移除下一个工单
        /// </summary>
        public static string RemoveNextWorkOrderAndSwith()
        {
            if (WorkOrderQueue == null || WorkOrderQueue.Count == 0)
            {
                LogRecord.addLog("当前工单队列无下一个工单可移除");
                return null;
            }
            WorkOrder removedWorkOrder = WorkOrderQueue.Dequeue();
            string removedLotNum = removedWorkOrder.lot_num;
            string logMessage = $"工单号{removedLotNum}已从工单列表中移除，" +
                 $"主板数量{removedWorkOrder.pnlqty}";

            LogRecord.addLog(logMessage);

            //
            if (WorkOrderQueue.Count > 0)
            {
                WorkOrder nextWorkOrder = WorkOrderQueue.Peek();
                WorkOrderCurrent = nextWorkOrder;
                LogRecord.addLog($"已切换到下一个工单: 工单号 = {WorkOrderCurrent.lot_num}");

                //发送新的工单给plc
                SendWorkOrder2PLC(WorkOrderCurrent);
                //
                com2PLC.writeCoil(84, false);
            }
            else
            {
                WorkOrderCurrent = new WorkOrder();
                LogRecord.addLog("工单队列已空，无下一个工单可切换。当前工单已重置。");

                com2PLC.writeCoil(84, true);
            }
            return removedLotNum;
        }



      
        private static bool ReadHoldingRegistersFunc(int startAddr, int length, out int[] values)
        {
            values = null;
            try
            {
                values = com2PLC.PLCClient.ReadHoldingRegisters(startAddr, length);
                return values != null && values.Length > 0;
            }
            catch (Exception ex)
            {
                LogRecord.addLog($"[ReadHoldingRegisters] 读取失败: {ex.Message}");
                return false;
            }
        }

        // MES数据读取线程函数
        private static void MesDataReadingThreadFunc()
        {
            LogRecord.addLog("[MES数据采集] 启动成功");


            // 初始化数据存储字典
            foreach (var item in com2PLC.MesDataRegister)
            {
                MesDataValues[item.Key] = null;
            }

            while (_mesDataThreadRunning)
            {
                try
                {
                    if (!com2PLC.Connected_Flag)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    var tempData = new Dictionary<int, object>();
                    bool anySuccess = false;

                    // 遍历 Com2PLC 中定义的地址
                    foreach (var item in com2PLC.MesDataRegister)
                    {
                        int address = item.Key;
                        string description = item.Value;
                        object value = null;

                        try
                        {
                            
                            int readLength = 1;

                            // 读取数据
                            bool readSuccess = ReadHoldingRegisters(address, readLength, out int[] registerValues);

                            if (readSuccess && registerValues != null && registerValues.Length > 0)
                            {
                                value = registerValues[0];
                                LogRecord.addLog($"[MES数据采集] 读取成功 地址:{address}({description}) = {value}");
                                anySuccess = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogRecord.addLog($"[MES数据采集] 读取异常 地址:{address}({description}): {ex.Message}");
                        }

                        tempData[address] = value;
                        Thread.Sleep(20);
                    }

                    // 更新全局数据
                    if (anySuccess)
                    {
                        lock (_mesDataLock)
                        {
                            foreach (var item in tempData)
                            {
                                MesDataValues[item.Key] = item.Value;
                            }
                        }
                    }

                    Thread.Sleep(2000); // 2秒读取一次
                }
                catch (Exception ex)
                {
                    LogRecord.addLog($"[MES数据采集] 异常: {ex.Message}");
                    //Thread.Sleep(5000);
                }
            }
        }

       /// <summary>
       /// 启动MES数据采集
       /// </summary>
        public static void StartMesDataReadingThread()
        {
            if (_mesDataReadingThread == null || !_mesDataReadingThread.IsAlive)
            {
                _mesDataThreadRunning = true;
                _mesDataReadingThread = new Thread(MesDataReadingThreadFunc)
                {
                    IsBackground = true,
                    Name = "MesDataReadingThread"
                };
                _mesDataReadingThread.Start();
                LogRecord.addLog("[MES数据采集] 已启动");
            }
        }
        
        











    }
}
