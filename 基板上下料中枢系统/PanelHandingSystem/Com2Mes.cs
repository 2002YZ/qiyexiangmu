using EasyModbus;
using Esb;
using HalconDotNet;
using Log;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using static PanelHandingSystem.Com2Mes;
using static PanelHandingSystem.ParaSet;
using static PanelHandingSystem.WorkFlowControl;


namespace PanelHandingSystem
{

    //MES通讯类
    public static class Com2Mes
    {

        public static Thread Thread_HeartBeat;
        public static Thread Thread_Energry; ///能耗上报线程
        public static bool MesConnected = false;

       //EAP任务接受器
        public static EapTaskReceiver TaskReceiver { get; private set; }
        


        //工单路径
        public static string PathWorkOrder = "WorkOrderLog\\";

        //工单类
        public class WorkOrder
        {
            //批次号码
            public string lot_num = "" ;
            //物料编码
            public string material_code = "";
            // PNL任务数量
            public int pnlqty = -1;
            //板长
            public float pallet_length = -1;
            //板宽
            public float pallet_width = -1;
            //板厚
            public float pallet_thickness = -1;
            //板子重量
            public float pallet_weight = -1;
            //陪镀板宽
            public float peidu_width = -1;
            //陪镀板长
            public float peidu_length = -1;
            //陪镀板厚
            public float peidu_thickness = -1;
            //陪镀板重量
            public float peidu_weight = -1;
            //条码列表
            public List<string> CodeList = new List<string>();
            //处理完成后的条码记录，自动化流程需要，处理完一个入一个
            public List<string> CodeListDone = new List<string>();
            //该工单是否完成
            public bool Done = false;

            //该工是板电还是图电，图电为0
            public int BanDianOrTuDian = 0;

        }


        //输出工单类
        public class OutOrder
        {
            public int status { get; set; } // 0：成功
            public string msg { get; set; }
            public WorkOrder data { get; set; }
        }


        public static void initMes()
        {
            //Mes初始化
          //  MesConnected = true;
            Thread_HeartBeat = new Thread(Heart_Beat_Func);
            //放在后台线程
            Thread_HeartBeat.IsBackground = true;
           Thread_HeartBeat.Start();

            //启动能耗上报线程
            /* Thread_Energry = new Thread(Energy_Report_Func);
             Thread_Energry.IsBackground = true;
             Thread_Energry.Start();*/

            //启动EAP任务接受服务
            InitializeAndStartTaskReceiver();

            // 
            Task.Run(async () =>
            {
                // 
                bool connected = await CheckMesConnection();
                MesConnected = connected;

                // 如果连接失败，
                if (!connected)
                {
                    // 可以添加重试逻辑
                    await Task.Delay(5000);
                    bool retryConnected = await CheckMesConnection();
                    MesConnected = retryConnected;
                }
            });





        }

        private static void InitializeAndStartTaskReceiver()
        {
            try
            {
                LogRecord.addLog("正在初始化EAP任务接收服务...");

                Action<Com2Mes.WorkOrder> onTaskReceivedCallback = (workOrder) =>
                {
                    if (workOrder != null)
                    {
                        LogRecord.addLog($"[EAP主动下发] 接收到新工单: {workOrder.lot_num}，正在加入生产队列...");

                        // 1. 将工单加入生产队列
                        WorkFlowControl.EnqueueWorkOrderList(workOrder);

                        // 2. 保存工单到本地
                        SaveworkOrder(workOrder);

                        LogRecord.addLog($"[EAP主动下发] 工单 {workOrder.lot_num} 已成功加入生产队列。");
                    }
                };
            }
            catch (Exception)
            {

                throw;
            }
        }



        //MES连接检查
        private static async Task<bool> CheckMesConnection()
        {
            try
            {
                // 这里调用一个简单的MES接口来测试连接
                Response resp = await SendHeartBeat(isAuto: true);
                return resp != null && resp.status == 0;
            }
            catch (Exception ex)
            {
                LogRecord.addLog($"MES连接检查失败: {ex.Message}");
                return false;
            }
        }
        #region 能耗上报
        private static void Energy_Report_Func()
        {
            while (MesConnected)
            {
                try
                {
                    // 模拟数据 (现场请修改为从 PLC 读取的真实数据)
                    string water = "10.0";  // 水耗
                    string diWater = "7.0"; // DI水耗
                    string elec = "120.5";  // 电耗
                                            
                    int keyX = WorkFlowControl.com2PLC.DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("实时用电量")).Key;       
                    WorkFlowControl.com2PLC.readDataD(keyX,2,out double eleD);

                    //读出来的值是否要除以1000？


                    var task = SendWaterAndElectricityReport(water, diWater, eleD.ToString());
                    task.Wait();
                }
                catch (Exception ex)
                {
                    LogRecordVision.addLog("自动能耗上报线程异常: " + ex.Message);
                }
                //5分钟一次
                Thread.Sleep(300000);
            }
        }
        #endregion

        #region 心跳
        private static void Heart_Beat_Func()
        {

            while (MesConnected)
            {

                 try
                {
                    // 调用封装好的发送心跳方法，只记录日志
                    var task = SendHeartBeat(isAuto: true);
                    task.Wait();
                }
                catch (Exception ex)
                {
                   // LogRecordVision.addLog("自动心跳线程异常: " + ex.Message);
                }
                //发送心跳，5秒一次
                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        /// <param name="isAuto">是否自动发送</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static async Task<Response> SendHeartBeat(bool isAuto=false)
        {
            Response resp=null;
            try
            {
                if (!isAuto) LogRecordVision.addLog("--- [手动] 开始调用心跳接口 ---");

                EapClient client = new EapClient();
                int timeOut = 3000;
                // 异步调用
                resp = await Task.Run(() => client.DeviceHeartbeat(timeOut));

                if (resp != null)
                {
                    if (resp.status == 0)
                    {
                        string prefix = isAuto ? "[自动]" : "[手动]";
                        LogRecordVision.addLog($"{prefix} 心跳成功! EAP返回: {resp.msg}");
                    }
                    else
                    {
                        LogRecordVision.addLog($"心跳失败! 状态码: {resp.status}, 错误: {resp.msg}");
                    }
                }
                else
                {
                    LogRecordVision.addLog("心跳失败：接口无响应");
                }
            }
            catch (Exception ex)
            {

              //  LogRecordVision.addLog("心跳调用异常: " + ex.Message);
            }
            return resp;
        }
        #endregion


        #region 清线完成接口 5.6
        public static async Task<Response> SendClearLineComplete()
        {
            Response resp = null;
            try
            {
                LogRecordVision.addLog("--- 开始调用清线完成接口 ---");
                string lineCode = ParaSet.recipe.myLinecode;
                LogRecordVision.addLog($"参数: LineCode=[{lineCode}]");
                EapClient client = new EapClient();
                int timeOut = 5000;
                resp = await Task.Run(() => client.EqpClearFinish("ClearFinish", timeOut));
                if (resp != null)
                {
                    if (resp.status == 0)
                    {
                        LogRecordVision.addLog($"清线完成接口调用成功! EAP返回: {resp.msg}");
                    }
                    else
                    {
                        LogRecordVision.addLog($"清线完成接口调用失败! 状态码: {resp.status}, 错误: {resp.msg}");
                    }
                }
                else
                {
                    LogRecordVision.addLog("清线完成接口调用失败：接口无响应");
                }
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("清线完成接口调用异常: " + ex.Message);
            }
            return resp;
        }
        #endregion


        #region 状态上报接口 5.4
        public static async Task<Response> SendStatusReport(string statusCode, string statusName)
        {
            Response resp = null;
            try
            {
                LogRecordVision.addLog("--- 开始调用设备状态上报接口 ---");
                EapClient client = new EapClient();
                int timeOut = 5000;
                // 异步调用
                resp = await Task.Run(() => client.DeviceStatusReport(statusCode,statusName, timeOut));
                if (resp != null)
                {
                    if (resp.status == 0)
                    {
                        LogRecordVision.addLog($"设备状态上报接口调用成功! EAP返回: {resp.msg}");
                    }
                    else
                    {
                        LogRecordVision.addLog($"设备状态上报接口调用失败! 状态码: {resp.status}, 错误: {resp.msg}");
                    }
                }
                else
                {
                    LogRecordVision.addLog("设备状态上报接口调用失败：接口无响应");
                }
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("设备状态上报接口调用异常: " + ex.Message);
            }
            return resp;
        }
        #endregion

        #region 异常 报警接口 5.7
        public static async Task<Response> SendAlarmReport(string alarmCode, string alarmDesc, int alarmLevel,int alarmStatus)
        {
            Response resp = null;
            try
            {
                LogRecordVision.addLog("--- 开始调用设备报警上报接口 ---");
                EapClient client = new EapClient();
                int timeOut = 5000;
                // 异步调用
                resp = await Task.Run(() => client.DeviceAlarmReport(alarmCode, alarmDesc, alarmLevel, alarmStatus, timeOut));
                if (resp != null&& resp.status == 0)
                {
                    LogRecordVision.addLog($"设备报警上报接口调用成功! EAP返回: {resp.msg}");
                }
                else
                {
                    LogRecordVision.addLog("设备报警上报接口调用失败：接口无响应");
                }
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("设备报警上报接口调用异常: " + ex.Message);
            }
            return resp;
        }
        #endregion

        #region 用水电量上报接口 5.10
        public static async Task<Response> SendWaterAndElectricityReport(string water, string diWater, string elec, bool isAuto = false)
        {
            Response resp = null;
            try
            {
                EapClient client = new EapClient();
                int timeOut = 3000;
                // 异步调用
                resp = await Task.Run(() => client.DeviceDataReport(water,diWater,elec, timeOut));
                if (resp != null)
                {
                    if (resp.status == 0)
                    {
                        if (!isAuto)
                        {
                           // LogRecordVision.addLog($"用水电量上报成功! EAP返回: {resp.msg}");
                        }
                    }
                    else
                    {
                        LogRecordVision.addLog($"用水电量上报接口调用失败! 状态码: {resp.status}, 错误: {resp.msg}");
                    }
                }
                else
                {
                    LogRecordVision.addLog("用水电量上报接口调用失败：接口无响应");
                }
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("用水电量上报接口调用异常: " + ex.Message);
            }
            return resp;
        }
        #endregion

        public static WorkOrder GenOffLineworkOrder(int pnlqty ,float width, float height, float thickness , float PeiDuwidth, float PeiDuheight, float PeiDuthickness, int BanDianOrTuDian = 0)
        {


            WorkOrder workorder = new WorkOrder();
            try
            {
                DateTime startTime = DateTime.Now;
                string str = "lotnum" + startTime.ToString("_MM_dd_HH_mm_ss_fff");
                //以当前系统时间生成编号
                workorder.lot_num = $"GTEST_Panel_{width}_{height}_{thickness}";
                workorder.material_code = "OfflineTest";
                workorder.pnlqty = pnlqty;


                //排版后计算配镀板参数
                double peiDuWidthOut;
                double baseAdjOut;
                LinkedList<double> posXQueue;
                LinkedList<double> posYQueue;
                Queue<double> posPeiDuXQueue;
                Queue<double> posPeiDuYQueue;

                int res = WorkFlowControl.CalPaiBan(
                width,      // 板宽
                height,     // 板高 (通常 MES 的 Length 对应排版算法的 Height)
                thickness,  // 板厚
                out peiDuWidthOut,
                out double peiDuHeight,
                out double peiDuThickness,
                out baseAdjOut,
                out posXQueue,
                out posYQueue,
                out posPeiDuXQueue,
                out posPeiDuYQueue,
                BanDianOrTuDian
            );

                /////

                workorder.BanDianOrTuDian = BanDianOrTuDian;

                workorder.pallet_width = width;
                workorder.pallet_length = height;
                workorder.pallet_thickness = thickness;

                workorder.peidu_width =(float) peiDuWidthOut;
                workorder.peidu_length = (float)peiDuHeight;
                workorder.peidu_thickness = (float)peiDuThickness;

                for (int i = 0; i < workorder.pnlqty; i++)
                {
               //     DateTime Time = DateTime.Now;
                    //string Code = "panel" + Time.ToString("_MM_dd_HH_mm_ss_fff");
                    string Code = (i+1).ToString("D4");

                    workorder.CodeList.Add(Code);
                    Thread.Sleep(5);
                }

                PrintWorkOrder(workorder);


                //生成飞巴文件

                ////分别生产前后飞巴
                //FeiBaWorker fbworker = FeiBaWorker.GenFeiBaWorkerTest(1);
                //fbworker.PrintFeiBaWorker();
                //FeiBaWorker.SavefeibaWorker(fbworker);

            }
            catch(Exception ex)
            {

                LogRecordVision.addLog("生成离线工单出错"+ex.Message);
            }
            //根据界面输入生成离线工单


          

            return workorder;

        }


        public static void PrintWorkOrder(WorkOrder workOrder)
        {

            string str = "工单编号:" + workOrder.lot_num
                       + "  --材料编号:" + workOrder.material_code
                       + "  --数量:" + workOrder.pnlqty.ToString()
                       + "  --板宽:" + workOrder.pallet_width.ToString()
                       + "  --板高:" + workOrder.pallet_length.ToString()
                       + "  --板厚:" + workOrder.pallet_thickness.ToString()
                       + "  --陪镀板宽:" + workOrder.peidu_width.ToString()
                       + "  --陪镀板高:" + workOrder.peidu_length.ToString()
                       + "  --陪镀板厚:" + workOrder.peidu_thickness.ToString()
                       +"  --条码队列个数:" + workOrder.CodeList.Count.ToString()
                       +"  --已处理队列个数:" + workOrder.CodeListDone.Count.ToString();


            LogRecordVision.addLog(str);

        }




        public static void SaveworkOrder(WorkOrder workOrder)
        {
            string jsonstr = Obj2Json<WorkOrder>(workOrder);
            string path = PathWorkOrder + workOrder.lot_num + ".json";
            SaveJsonFile(path, jsonstr);
            LogRecord.addLog($"Json工单文件已经保存到{path}");
        }





        public static WorkOrder GetworkOrder(string barcode)
        {
            if (string.IsNullOrEmpty(barcode)) return null;

            try
            {
                LogRecordVision.addLog($"[Com2Mes] 正在根据条码[{barcode}]向EAP请求工单...");
                Esb.EapClient client = new Esb.EapClient();
                var response =client.AskDownTaskEvent("NA", "", barcode, 5000);
                if (response != null && response.status == 0 && response.data != null)
                {
                    LogRecordVision.addLog($"[Com2Mes] 获取成功! 工单号: {response.data.lot_num}");
                    return response.data;
                }
                else
                {
                    string msg = response != null ? response.msg : "接口返回为空";
                    LogRecordVision.addLog($"[Com2Mes] 获取失败: {msg}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog($"[Com2Mes] GetworkOrder异常: {ex.Message}");
                return null;
            }
        }

    }
}

