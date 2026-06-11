
using EasyModbus;
using FolderKeeper;
using HalconDotNet;
using Log;


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

using static EasyModbus.ModbusServer;
using static PanelHandingSystem.Com2PLC;
using static PanelHandingSystem.ParaSet;

namespace PanelHandingSystem
{

    //PLC静态通讯类，包含整体流程控制
    public  class Com2PLC
    {

        //PLC客户端通讯
        public  ModbusClient PLCClient = new ModbusClient();

        public  int RegisterStartAddr = 0;

        public  int AddrOffSet = 0;//现场PLC是这么多
      
        public  int ScanTime = 300;




        //MODBUS数据监听线程标志位
        //PLC连接成功，线程控制变量
        public  bool Connected_Flag = false;



        private  object locker = new object();//创建锁

        public  Thread Thread_HeartBeat;

        public Thread Thread_Read_Singal;

        //读寄存器委托，多个寄存器
        public delegate bool[] ReadCoilsD(int StartingAddr, int quiantity);
        public ReadCoilsD readCoils;



        //写寄存器委托
        public delegate void WriteCoilsD(int location, bool flag);
        public  WriteCoilsD writeCoil;


        //写数据寄存器，字符串
        public delegate void WriteStringD(int coil_num, string str2write);
        public  WriteStringD writeStringD;


        //写数据寄存器，寄存器数组
        public delegate void WriteDataD(int coil_num, double value);
        public  WriteDataD writeDataD;

        //读数据寄存器
        public delegate void ReadDataD(int coil_num, int lenth, out double value);
        public ReadDataD readDataD;


        // 读数据寄存器，字符串
        public delegate void ReadStringD(int coil_num, int length, out string value);
        public ReadStringD readStringD;




        //只检测Logo有无
        public static bool CheckLogoExistOnly = false;
        //强制OK标志
        public static bool ForceOK = true;





        //信号谁收谁复位
        public Dictionary<int, string> CoilRegister = new Dictionary<int, string>
        {

            //下料区
            [0] = "PLC->IPC:启动下料飞巴扫码,收到后即复位",
            [1] = "IPC->PLC:下料检查异常,需停机后人工干预复位",
            [2] = "IPC->PLC:下料检查完成",
            [3] = "IPC->PLC:下料完成,PLC可以通知飞巴离开或继续等待上料",
            [4] = "IPC->PLC:下料时的料台调整数据已写入，启动调整",

            //上料区
            [20] = "PLC->IPC:启动上料飞巴扫码,收到后即复位",
            [21] = "IPC->PLC:未启用，上料检查异常,需停机后人工干预复位,上料检查已经改为传感器检测",

            [22] = "IPC->PLC:上料完成,PLC可以通知当前飞巴离开",
            [23] = "PLC->IPC:传送带1末端有料状态",
            [24] = "PLC->IPC:传送带2末端有料状态",
            [25] = "IPC->PLC:上料时的料台调整数据已写入，启动调整",
            [26] = "PLC->IPC:1号陪镀仓1号区域,1-1已更新",
            [27] = "PLC->IPC:1号陪镀仓2号区域,1-2已更新",

            [30] = "PLC->IPC:2号陪镀仓1号区域,2-1已更新",
            [31] = "PLC->IPC:2号陪镀仓2号区域,2-2已更新",
            [32] = "IPC->PLC:1号陪镀板拍照报警",
            [33] = "IPC->PLC:2号陪镀板拍照报警",

            [28] = "IPC->PLC:当前是否有陪镀板,让PLC知道这个信号",
            [29] = "IPC->PLC:1号陪镀板上料完成",
            [34] = "IPC->PLC:2号陪镀板上料完成",
            [35] = "PLC->IPC:基座调整完成",  //上位机等待基座调整

            //配镀板上料指示
            
            [36] = "PLC->IPC:配镀板上料1 -1区域",  //再由上位机告诉机器人取料信息
            [37] = "PLC->IPC:配镀板上料1-2区域",
            [38] = "PLC->IPC:配镀板上料2-1区域",
            [39] = "PLC->IPC:配镀板上料2-2区域",

            //缓存机
            [40] = "PLC->IPC:1号缓存机扫码枪启动信号，收到后即复位",
            [41] = "PLC->IPC:2号缓存机扫码枪启动信号，收到后即复位",
            [42] = "IPC->PLC:1号缓存机扫码成功",
            [43] = "IPC->PLC:2号缓存机扫码成功",
            [44] = "IPC->PLC:1号缓存机扫码失败",
            [45] = "IPC->PLC:2号缓存机扫码失败",

            [58] = "PLC->IPC:1号缓存机传送带上料拍照",
            [59] = "PLC->IPC:2号缓存机传送带上料拍照",

            [46] = "IPC->PLC:1号缓存机传送带上料拍照成功",
            [47] = "IPC->PLC:2号缓存机传送带上料拍照成功",
            [48] = "IPC->PLC:1号缓存机传送带上料拍照失败",
            [49] = "IPC->PLC:2号缓存机传送带上料拍照失败",
            [50] = "PLC->IPC:无定义",

            [51] = "IPC->PLC:下料飞巴扫码异常",  //下料扫码异常、失败 写给plc
            [52] = "IPC->PLC:下料飞巴扫码成功",   //下料扫码成功  写给plc


            [53] = "IPC->PLC:1号缓存机启动主板上料程序",
            [54] = "IPC->PLC:2号缓存机启动主板上料程序",
            [55] = "IPC->PLC:1号缓存机单次飞巴上料已结束",
            [56] = "IPC->PLC:2号缓存机单次飞巴上料已结束",
            [57] = "PLC->IPC:上料循环结束",//两边的上料循环都结束后发送
           

            //异常区    
            [70] = "IPC->ROBOT: 心跳信号",//5秒一次，写true ,PLC检测到后复位false，持续检测不到true说明掉线
            [80] = "IPC->ROBOT: 陪镀板尺寸报警",//陪镀板检测完成后的返回信号 
            [81] = "IPC->ROBOT: 后飞巴尾料报警",//上位机计算到当前有尾料，进行报警
            [82] = "IPC->ROBOT: 前飞巴尾料报警",//上位机计算到当前有尾料，进行报警
            [83] = "IPC->ROBOT:力控报警",//收到后立即停机报警，复位后置false

            //
            [84] = "IPC->ROBOT:工单异常信号", //收到M20后 如果没有工单或者工单出错 置位


            [90] = "IPC->ROBOT:软件启动",//软件启动或重启时置位,PLC自动复位

            //尾料
            [77] = "IPC->PLC:当前工单结束，进行尾料处理",   //前后飞巴上料完成

            //
            [78] = "IPC->PLC:当前配镀板左边3D拍照失败",   //提示plc进行人工处理
            [79] = "IPC->PLC:当前配镀板右边3D拍照失败",

            [85] = "IPC->PLC:当前需要带夹边的配镀板==人工上料",  //检测到有夹边的配镀板 需要人工上料
            [86] = "PLC->IPC:当前带夹边的配镀板已经人工上料处理完毕",  //已弃用，上位机不会等待人工处理完毕

            [87] = "PLC->IPC:PLC通知上位机当前为手动模式",  //手动模式定义：PLC仅传送板料，不触发上料拍照和机器人动作。但上料和下料的启动信号M20和M0 正常发送，上位机判断此信号，并对飞巴数据进行标记，标记为手动上料
            [88] = "IPC->PLC:下料触发后，检测到当前飞巴需人工下料",  //PLC触发M0下料流程后，上位机读取飞巴码，若判断当前飞巴码为人工上料的，即通知PLC切换到手动模式进行下料


        };


        //数据寄存器
        public  Dictionary<int, string> DataRegister = new Dictionary<int, string>
        {
            //上位机写入
            [7000] = "当前板子宽度(飞巴)",
            [7005] = "当前板子高度(飞巴)",
            [7010] = "当前板子厚度(飞巴)",
            [7015] = "当前板子重量(飞巴)",

            [7020] = "当前板子宽度(传送带1)",
            [7025] = "当前板子高度(传送带1)",
            [7030] = "当前板子厚度(传送带1)",
            [7035] = "当前板子重量(传送带1)",

            [7040] = "当前板子宽度(传送带2)",
            [7045] = "当前板子高度(传送带2)",
            [7050] = "当前板子厚度(传送带2)",
            [7055] = "当前板子重量(传送带2)",

            [7060] = "上料料台底座调整距离_前飞巴",
            [7065] = "上料料台底座调整距离_后飞巴",

            [7070] = "下料料台底座调整距离_前飞巴",
            [7075] = "下料料台底座调整距离_后飞巴",            
            [7080] = "力控实时数据",//100ms刷新一次

            [7200] = "缓存1的上料循环次数",//PLC根据这个判断上料是否完成，缓存1的循环次数等于缓存2循环次数+1或相等,在飞巴上料扫码时写入,上料完成后又PLC清零
            [7205] = "缓存2的上料循环次数",//在飞巴上料扫码时写入
            [7210] = "陪镀板宽度(上料缓存区)", //备用，PLC暂不需要
            [7215] = "陪镀板高度(上料缓存区)", //备用，PLC暂不需要
            [7220] = "陪镀板宽度(飞巴)", //备用，PLC暂不需要
            [7225] = "陪镀板高度(飞巴)",//备用，PLC暂不需要
         
            //以下3个地址，每次新的工单收到时写入,若连续收到工单，存在覆盖的风险，PLC需自行转移
            [7230] = "当前工单主板数量",
            [7300] = "当前工单缓存机1上料数量",  
            [7305] = "当前工单缓存机2上料数量",
          


            //PLC写入
            //用于控制左右两条上料通道的上料比例，可能用不到
            [7310] = "缓存机1实时数量",
            [7315] = "缓存机2实时数量",
        

        };


        /// <summary>
        /// 数据采集
        /// </summary>
        public Dictionary<int, string> MesDataRegister = new Dictionary<int, string>
        {

            ///设备状态
           // [7320] = "实时用电量",//5分钟刷新一次
            [7325] = "左机器人运行状态",         //  "1" 运行； "2" 待机  ；"3" 故障
            [7326] = "右机器人运行状态",         //同上
            [7327] = "历史总数量", //左右缓存流出去的总数量


            [7328] = "左缓存实际存板数量",
            [7329] = "右缓存实际存板数量",
            [7330] = "终端实际存板数量",
            [7331] ="设备运行状态",   //总设备

            /// 报警信息
            [7332] = "左机器人手抓报警",
            [7333] = "右机器人手抓报警",
            [7334] = "游标尺伺服报警",
            [7335] = "下料流水线伺服报警",
            [7336] = "左缓存升降伺服报警",
            [7337] = "左缓存旋转伺服报警",
            [7338] = "右缓存升降伺服报警",
            [7339] = "右缓存旋转伺服报警",
            [7340] = "左缓存急停被按下",
            [7341] = "右缓存急停被按下",
            [7342] = "左陪渡急停被按下",
            [7343] = "总急停被按下",
            [7344] = "右陪渡急停被按下",
            [7345] = "左陪渡左仓升降伺服报警",
            [7346] = "左陪渡右仓升降伺服报警",
            [7347] = "右陪渡左仓升降伺服报警",
            [7348] = "右陪渡右仓升降伺服报警",
            [7349] = "右缓存拍照失败",
            [7350] = "左缓存拍照失败",
            [7351] = "左机器人运行中",   
            [7352] = "右机器人运行中",
            [7353] = "机器人暂停中",
            [7354] = "左安全门被打开", 
            [7355] = "右安全门被打开",
            [7356] = "右机器人暂停键被按下",
            [7357] = "双向流水线左推送报警",
            [7358] = "双向流水线右推送报警",
            [7359] = "双向流水线输送带报警",
            [7360] = "终端缓存左急停被按下", 
            [7361] = "终端缓存右急停被按下",
            [7362] = "左机器人暂停键被按下",
            [7363] = "下料流程异常，需人工干预下料",
            [7364] = "左陪渡左仓无料，请求加料",
            [7365] = "左陪渡右仓无料，请求加料",
            [7366] = "右陪渡左仓无料，请求加料",
            [7367] = "右陪渡右仓无料，请求加料",
            [7368] = "上料异常，已切断投板机上料",
            [7369] = "终端缓存升降伺服报警",
            [7370] = "终端缓存旋转轴伺服报警",
            [7371] = "右缓存读码失败",
            [7372] = "右缓存2D拍照失败",
            [7373] = "左缓存读码失败",
            [7374] = "左缓存2D拍照失败",
            [7375] = "右缓存顶料拍照总升降气缸原点异常",
            [7376] = "右缓存顶料拍照总升降气缸动点异常",
            [7377] = "右缓存顶料拍照辅助升降气缸动点异常",
            [7378] = "左缓存顶料拍照总升降气缸原点异常",
            [7379] = "左缓存顶料拍照总升降气缸动点异常",
            [7380] = "左缓存顶料拍照辅助升降气缸动点异常",
            [7381] = "双向流水线左顶升送料气缸原点异常",
            [7382] = "双向流水线左顶升送料气缸动点异常",
            [7383] = "双向流水线右顶升送料气缸原点异常",
            [7384] = "双向流水线右顶升送料气缸动点异常",
            [7384] = "双向流水线挡料升降气缸动点异常",


            //工艺参数
            [7385] = "设备编号",  //machine_code
            [7386] = "物料编码",  // material_code/
            [7387] = "批次号",
            [7388] = "板长",
            [7389] = "板宽",
            [7390] = "板厚",
            [7391] = "陪镀板宽",
            [7392] = "陪镀板高",
            [7393] = "陪镀板厚",
            [7394] = "PNL任务数量",
            [7395] = "工单",
            [7396] = "请求GUID",
        };

      


        public int ConnectPLC()
        {

            int Res = -1;
            try
            {
                string IP = ParaSet.recipe.PLCIPAddr;
                int Port = ParaSet.recipe.PLCIPPort;
                //连接PLC
                LogRecordVision.addLog($"连接PLC:{IP}:{Port}");
                PLCClient.ConnectionTimeout = 5000;
                PLCClient.IPAddress = IP;
                PLCClient.Port = Port;
                PLCClient.Connect();

                Connected_Flag = true;


                AddrOffSet = ParaSet.recipe.PLCAddrOffSet;
                RegisterStartAddr = ParaSet.recipe.PLCRegisterStartAddr;



                Thread_HeartBeat = new Thread(Heart_Beat_Func);
                Thread_HeartBeat.Start();
                //启动监听线程
                Thread_Read_Singal = new Thread(Dowork_read_singal_Func);
                Thread_Read_Singal.Start();



             

                //委托赋值定义
                readCoils = new ReadCoilsD(ReadCoilsFunc);
                writeCoil = new WriteCoilsD(WriteCoilsFunc);
                writeStringD = new WriteStringD(WriteStringDFunc);
                writeDataD = new WriteDataD(WriteDataDDFunc);

                readDataD = new ReadDataD(ReadDataFunc);
                readStringD = new ReadStringD(ReadStringDFunc);

                Res = 1;
            }
            catch(Exception ex)
            {
                LogRecordVision.addLog($"PLC连接出错:{ex.Message}");
            }

            return Res;

        }

        //读字符串数据寄存器
        private void ReadStringDFunc(int coil_num, int length, out string value)

        {
           value = "";
            try
            {
                lock (locker)//加锁
                {
                    LogRecordVision.addLog($"[读字符串] 地址:{coil_num}, 长度:{length}");
                    int[] result = PLCClient.ReadHoldingRegisters(coil_num, length);
                    value = ConvertRegistersToString(result, 0, result.Length * 2);
                    value = value.Replace("\0", "").Trim();
                    LogRecordVision.addLog($"[读字符串] 结果: {value}");
                }
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog($"Modbus 读数据寄存器出错:起始地址{coil_num}");
                LogRecordVision.addLog(ex.Message);
                throw ex;
            }
        }

        private void ReadDataFunc(int coil_num, int lenth  , out double value)
        {
            value = -1;

            //lenth默认为2
            //

            lenth = 2;

            try
            {
                lock (locker)//加锁
                {
                    if (!DataRegister.ContainsKey(coil_num))
                        LogRecordVision.addLog($"读数据寄存器{coil_num},地址不在数据通讯表中");
                    else
                        LogRecordVision.addLog($"读数据寄存器{coil_num},{DataRegister[coil_num]}");

                    int[] result = PLCClient.ReadHoldingRegisters(coil_num, lenth);
                    int lenthD = result.Length;

                    if (lenth > 3)
                    {
                        int back = ModbusClient.ConvertRegistersToInt(result);
                        Log.LogRecordVision.addLog("读取寄存器结果:" + back.ToString());
                        value = back;
                    }
                    else
                    {
                        int back = ModbusClient.ConvertRegistersToInt(result);
                        Log.LogRecordVision.addLog("读取寄存器结果:" + back.ToString());
                        value = back;
                    }

                }
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog($"Modbus 读数据寄存器出错:起始地址{coil_num}");
                LogRecordVision.addLog(ex.Message);
                throw ex;
            }
        }




        //PLC主要流程监听
        private void Dowork_read_singal_Func()
        {

            //PLC寄存器监听线程
            LogRecordVision.addLog($"PLC寄存器监听线程启动");

            while (Connected_Flag)
            {

                //List<bool> CoilAll = new List<bool>();
                //lock (locker)//加锁
                //{

                //    try
                //    {
                //        CoilAll = ReadPLCStatues();


                //    }
                //    catch (Exception ex)
                //    {
                //        LogRecordVision.addLog("PLC读取线程出错" + ex.Message);
                //        continue;
                //    }

                //}


                try
                {
                    //PLC通知扫码枪3和扫码枪4开始扫码,下料飞巴扫码，同时两个扫码枪扫码，扫码后启动下料流程

                    bool[] coil1 = readCoils(0, 1);
                    if (coil1[0])
                    {
                        LogRecordVision.addLog("0=true");

                        if (!ReCheckSignalandReset(0))
                            continue;

                        new Thread(WorkFlowControl.RunXiaLiao).Start();
                        // Thread.Sleep(1000);
                    }


                    //PLC通知上料飞巴扫码，陪镀板检查，并调整基座
                    coil1 = readCoils(20, 1);
                    if (coil1[0])
                    {
                        LogRecordVision.addLog("20=true");
                        if (!ReCheckSignalandReset(20))
                            continue;

                        new Thread(WorkFlowControl.RunShangLiaoFeiBaSaoMaAndBaseAdj).Start();
                    }



                    //PLC通知上料传送带1号扫码
                    coil1 = readCoils(40, 1);
                    if (coil1[0])
                    {
                        LogRecordVision.addLog("40=true");
                        if (!ReCheckSignalandReset(40))
                            continue;

                        new Thread(WorkFlowControl.RunShangLiaoSaoMa1).Start();
                    }

                    //PLC通知上料传送带2号扫码
                    coil1 = readCoils(41, 1);
                    if (coil1[0])
                    {
                        LogRecordVision.addLog("41=true");
                        if (!ReCheckSignalandReset(41))
                            continue;

                        new Thread(WorkFlowControl.RunShangLiaoSaoMa2).Start();
                    }

                    coil1 = readCoils(58, 1);
                    if (coil1[0])
                    {
                        LogRecordVision.addLog("58=true");
                        if (!ReCheckSignalandReset(58))
                            continue;

                        new Thread(WorkFlowControl.RunShangLiao2DPosition1).Start();
                    }

                    coil1 = readCoils(59, 1);
                    if (coil1[0])
                    {
                        LogRecordVision.addLog("59=true");
                        if (!ReCheckSignalandReset(59))
                            continue;

                        new Thread(WorkFlowControl.RunShangLiao2DPosition2).Start();
                    }

                    coil1 = readCoils(57, 1);
                    if (coil1[0])
                    {

                        //上料循环结束
                        LogRecordVision.addLog("57=true");
                        if (!ReCheckSignalandReset(57))
                            continue;

                        new Thread(WorkFlowControl.RunShangLiaoPostPro).Start();
                    }
                }
                catch (Exception ex)
                {

                    LogRecordVision.addLog("监听线程寄存器处理线程出错" + ex.Message);

                }

                Thread.Sleep(1000);



            }


        }





        public bool WaitForSingal(int Addr)
        {
            bool Res = false;
            //等待机器人信号
            bool GetRes = false;
            int TimeOut = 200000;//超时时间，根据实际情况调整

            DateTime startTime = DateTime.Now;

            LogRecordVision.addLog($"等待PLC{Addr}:{CoilRegister[Addr]}为true 的信号...");
            //等待相机拍照触发信号
            while (!GetRes)
            {
                //读机器人的拍照触发信号
                bool[] SingalTest = readCoils(Addr, 1);
                if (SingalTest[0])
                {

                    LogRecordVision.addLog($"收到PLC{Addr}:{CoilRegister[Addr]},为true 的信号");
                    //读到后复位
                    writeCoil(Addr, false);


                    GetRes = true;
                    Res = true;

                }


                TimeSpan duration = DateTime.Now - startTime; // 计算耗时
                if (duration.TotalMilliseconds > TimeOut)
                {
                    //等待超时，日志提示，但是还是继续等待
                    LogRecordVision.addLog($"PLC信号{Addr}:{CoilRegister[Addr]},超时未置位,超时设定");
                    LogRecordVision.addLog($"超时设定{TimeOut}ms");
                    return Res;
                }
                // Res = true;

                Thread.Sleep(1200);
            }

            return Res;

        }


        public  int[] ConvertStringToRegisters(string stringToConvert)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(stringToConvert);
            int[] array = new int[bytes.Length / 2 + bytes.Length % 2];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = bytes[i * 2];
                if (i * 2 + 1 < bytes.Length)
                {
                    array[i] = (ushort)(array[i] | (bytes[i * 2 + 1] << 8));
                }
            }
            return array;
        }


        public  string ConvertRegistersToString(int[] registers, int offset, int stringLength)
        {
            byte[] array = new byte[stringLength];
            for (int i = 0; i < stringLength / 2; i++)
            {
                byte[] bytes = BitConverter.GetBytes(registers[offset + i]);
                array[i * 2] = bytes[0];
                array[i * 2 + 1] = bytes[1];
            }
            return Encoding.UTF8.GetString(array);
        }

        //向PLC数据寄存器写入数据，字符串
        private  void WriteStringDFunc(int StartingAddr, string str2write)
        {
            try
            {
                lock (locker)//加锁
                {

                    if (!DataRegister.ContainsKey(StartingAddr))
                        LogRecordVision.addLog($"写数据寄存器{StartingAddr},地址不在数据通讯表中");
                    else
                        LogRecordVision.addLog($"写入数据寄存器字符串{StartingAddr},{DataRegister[StartingAddr]}");

                    int[] str2int = new int[1024];
                    int lenght = str2write.Length;
                    //  string str2write2 = str2write + $",{lenght}";
                    string str2write2 = str2write;
                    str2int = ConvertStringToRegisters(str2write2);
                    PLCClient.WriteMultipleRegisters(StartingAddr, str2int);

                    //写入后读取一遍
                    int[] result = PLCClient.ReadHoldingRegisters(StartingAddr, lenght + 5);
                    string back = ConvertRegistersToString(result, 0, lenght + 5);
                    Log.LogRecordVision.addLog("写入后读取寄存器:" + back);

                }




            }
            catch(Exception ex) 
            {
                LogRecordVision.addLog($"Modbus 写入数据寄存器出错:起始地址{StartingAddr},{str2write}");
                LogRecordVision.addLog(ex.Message);
                throw ex;

            }

        

        }


        //向PLC数据寄存器写入数据
        private  void WriteDataDDFunc(int StartingAddr, double value)
        {
            try
            {
                lock (locker)//加锁
                {
                    if (!DataRegister.ContainsKey(StartingAddr))
                        LogRecordVision.addLog($"写数据寄存器{StartingAddr},地址不在数据通讯表中,仍会执行写入");
                    else
                        LogRecordVision.addLog($"写入数据寄存器{StartingAddr},{DataRegister[StartingAddr]}");



                    //int[] registers2Double = ModbusClient.ConvertDoubleToRegisters((double)value);
                    //int[] registers2Double = ModbusClient.ConvertFloatToRegisters((float)value);

                    //数据乘1000后发送
                    //double 乘1000后发送int 类型
                    int v = (int)(value * 1000);


                    int[] registers2Double = ModbusClient.ConvertIntToRegisters(v);
                    PLCClient.WriteMultipleRegisters(StartingAddr, registers2Double);


                    //写入后读取一遍
                    int lenth = registers2Double.Length;
                    int[] result = PLCClient.ReadHoldingRegisters(StartingAddr, lenth);
                    int lenthD = result.Length;
                
                    if (lenth > 3)
                    {
                        int back = ModbusClient.ConvertRegistersToInt(result);
                        Log.LogRecordVision.addLog("写入后读取寄存器:" + back.ToString());
                    }
                    else
                    {
                        int back = ModbusClient.ConvertRegistersToInt(result);
                        Log.LogRecordVision.addLog("写入后读取寄存器:" + back.ToString());
                    }
  

                }

        
              
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog($"Modbus 写入数据寄存器出错:起始地址{StartingAddr}");
                LogRecordVision.addLog(ex.Message);
                throw ex;
            }
           
        }


        //读多个寄存器
        public bool[] ReadCoilsFunc(int StartingAddr, int quiantity)
        {

            bool[] Coils = null;
            try
            {
                lock (locker)//加锁
                {
                    Coils = PLCClient.ReadCoils(StartingAddr + RegisterStartAddr +  AddrOffSet, quiantity);
                    if (quiantity == 1)
                    {
                        //读单个寄存器
          
                        //if (!CoilRegister.ContainsKey(StartingAddr))
                        //    LogRecordVision.addLog($"读单个coil{StartingAddr},地址不在通讯表中");
                        //else
                        //    LogRecordVision.addLog($"读单个coil{StartingAddr},{Coils[0].ToString()}:{CoilRegister[StartingAddr]}");
                    }
               

                }          
              
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog($"Modbus 读寄存器出错:起始地址{StartingAddr},数量{quiantity}");
                LogRecordVision.addLog(ex.Message);
                throw ex;

            }
            return Coils; 
        }





        public  void WriteCoilsFunc(int Addr, bool value)
        {
            try
            {
                lock (locker)//加锁
                {
                    if(!CoilRegister[Addr].Contains("心跳"))
                    {

                        try
                        {
                            //心跳不显示
                            if (!CoilRegister.ContainsKey(Addr))
                                LogRecordVision.addLog($"写单个coil{Addr},地址不在通讯表中");
                            else
                                LogRecordVision.addLog($"写入coils寄存器{Addr},{value}:{CoilRegister[Addr]}");

                        }
                        catch (Exception ex)
                        {
                            LogRecordVision.addLog(ex.Message);
                        }

                      
                    }

                    PLCClient.WriteSingleCoil(Addr + RegisterStartAddr + AddrOffSet, value);

                }
              

            }
            catch(Exception ex)
            {
                LogRecordVision.addLog($"Modbus写PLC寄存器出错,地址:{Addr}："+ ex.Message);

               // throw ex;

            }
          
        }





   




        //读取PLC状态
        public  List<bool>  ReadPLCStatues()
        {


            List<bool> CoilAll = new List<bool>();

            try
            {
                //20260111一次性读多个PLC线圈寄存器不行，改成一个一个读

                //起始位，加8192偏置，一次读30个寄存器，前30个寄存器  7100  -  7129
                bool[] coil1 = readCoils(RegisterStartAddr, 10);
                Thread.Sleep(50);
                //起始位，加8192偏置，再加30，一次读30个寄存器，后30个寄存器,起点是7130 -  7159
                bool[] coil2 = readCoils(RegisterStartAddr + 10, 10);
                Thread.Sleep(50);
                //CoilAll是以上两次读取后的结果拼接汇总
                CoilAll.AddRange(coil1);
                CoilAll.AddRange(coil2);

            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("PLC读取状态程出错" + ex.Message);
               
            }


               return CoilAll;



        }





       private  void Heart_Beat_Func()
        {
            LogRecordVision.addLog($"PLC心跳启动");
            while (true) 
            {

                try
                {
                    //写心跳
                    writeCoil(70, true);

                }
                catch (Exception ex)
                {
                    LogRecordVision.addLog("上位机心跳 7159 = true 时出错" + ex.Message);
                }

                Thread.Sleep(4900);
            }       

        }




        public  bool ReCheckSignalandReset(int StartAddr)
        {
            bool Res = false;
            Thread.Sleep(10);
            bool[] coilConfirm = readCoils(StartAddr, 1);
            if (true == coilConfirm[0])
            {
                //复位
                writeCoil(StartAddr, false);
                Res = true ;

            }
            else
            {
                LogRecordVision.addLog("信号误触发:"+ StartAddr);

            }

            return Res;
        }



        static public void RestartSystemFunc()
        {
            LogRecordVision.addLog("重启电脑系统");

   
            try
            {
                Process.Start("shutdown", "/r /f /t 0");
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("重启电脑系统出错" + ex.Message);
            }



        }

        internal void SendPanelSizeOnChuanSongDai1(double w, double h ,double t ,double weight)
        {
            try
            {
                //写下料坐标
                // 获取第一个匹配的键
                int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子宽度(传送带1)")).Key;
                writeDataD(keyX, w);

                int keyY = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子高度(传送带1)")).Key;
                writeDataD(keyY, h);

                int keyT = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子厚度(传送带1)")).Key;    
                writeDataD(keyT, t);

                int keyW = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子重量(传送带1)")).Key;
                writeDataD(keyW, weight);



            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("1号传送带写传送带板子尺寸出错" + ex.Message);
                //throw ex;
            }




        }
        internal void SendPanelSizeOnChuanSongDai2(double w, double h, double t, double weight)
        {
            try
            {
                //写下料坐标
                // 获取第一个匹配的键
                int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子宽度(传送带2)")).Key;
                writeDataD(keyX, w);

                int keyY = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子高度(传送带2)")).Key;
                writeDataD(keyY, h);

                int keyT = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子厚度(传送带2)")).Key;
                writeDataD(keyT, t);

                int keyW = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子重量(传送带2)")).Key;
                writeDataD(keyW, weight);




            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("2号传送带写传送带板子尺寸出错" + ex.Message);
                throw ex;
            }




        }

        internal void SendPanelSizeOnFeiBa(double w, double h ,double t , double weight)
        {
            try
            {
                //写下料坐标
                // 获取第一个匹配的键
                int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子宽度(飞巴)")).Key;     
                writeDataD(keyX, w);

                int keyY = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子高度(飞巴)")).Key;       
                writeDataD(keyY, h);

                int keyT = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子厚度(飞巴)")).Key;  
                writeDataD(keyT, t);

                int keyW = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子重量(飞巴)")).Key;
                writeDataD(keyT, weight);


                ///写两次
                // keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子宽度(飞巴)")).Key;
                //writeDataD(keyX, w);

                // keyY = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子高度(飞巴)")).Key;
                //writeDataD(keyY, h);

                // keyT = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子厚度(飞巴)")).Key;
                //writeDataD(keyT, t);

                // keyW = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前板子重量(飞巴)")).Key;
                //writeDataD(keyT, weight);



            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("写飞巴板子尺寸出错" + ex.Message);
                throw ex;
            }

        }



        //public void SendLiaoTaiDistanceShangLiao(double distance, int Row)
        //{
        //    //根据计算结果将料台需要调整的距离写入
        //    try
        //    {
        //        if (Row == 1)
        //        {
        //            int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料料台底座调整距离_前飞巴")).Key;
        //           // int[] Dis = ModbusClient.ConvertDoubleToRegisters(distance);
        //            writeDataD(keyX, keyX);
        //        }
        //        else if (Row == 2)
        //        {
        //            int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料料台底座调整距离_后飞巴")).Key;
        //           // int[] Dis = ModbusClient.ConvertDoubleToRegisters(distance);
        //            writeDataD(keyX, keyX);
        //        }
        //        else
        //            LogRecordVision.addLog("行号输入有误,应为1或2");

        //    }
        //    catch (Exception ex)
        //    {
        //        LogRecordVisionRobot.addLog("上料写入料台调整值出错" + ex.Message);
        //        throw ex;
        //    }


        //}

        //public void SendLiaoTaiDistanceXiaLiao(double distance,int Row)
        //{
        //    //根据计算结果将料台需要调整的距离写入
        //    try
        //    {
        //        if (Row == 1)
        //        {
        //            int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料料台底座调整距离_前飞巴")).Key;
        //         //   int[] Dis = ModbusClient.ConvertDoubleToRegisters(distance);
        //            writeDataD(keyX, keyX);

        //        }
        //        else if (Row == 2)
        //        {
        //            int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料料台底座调整距离_后飞巴")).Key;
        //      //      int[] Dis = ModbusClient.ConvertDoubleToRegisters(distance);
        //            writeDataD(keyX, keyX);
        //        }
        //        else
        //            LogRecordVision.addLog("行号输入有误,应为1或2");



        //    }
        //    catch (Exception ex)
        //    {
        //        LogRecordVisionRobot.addLog("下料写入料台调整值出错" + ex.Message);
        //        throw ex;
        //    }


        //}

        /// <summary>
        /// 向PLC写入基座调整数据，并启动基座调整,上料时ShangLiaoOrXiaLiao =1 ，下料时 ShangLiaoOrXiaLiao =2
        /// </summary>
        /// <param name="BaseAdjValueRow1"></param>
        /// <param name="BaseAdjValueRow2"></param>
        /// <param name="ShangLiaoOrXiaLiao"></param>

        public void FeibaBaseAdjust(double BaseAdjValueRow1, double BaseAdjValueRow2 ,int ShangLiaoOrXiaLiao)
        {
            
            try
            {
                if(ShangLiaoOrXiaLiao == 1)
                {

                    //结合现场情况如果，调整至小于-50时，再往下偏移20mm
                    double DOffset = 0;
                    if(BaseAdjValueRow1<-50)
                    {
                        BaseAdjValueRow1 = BaseAdjValueRow1 + DOffset;

                    }
                    if (BaseAdjValueRow1 < -50)
                    {
                        BaseAdjValueRow2 = BaseAdjValueRow2 + DOffset;
                    }


                    LogRecordVision.addLog($"实际发送PLC的基座调整值{BaseAdjValueRow1},{BaseAdjValueRow2}");
                    int keyX1 = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料料台底座调整距离_前飞巴")).Key;
                    int keyX2 = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料料台底座调整距离_后飞巴")).Key;
                    writeDataD(keyX1, BaseAdjValueRow1);
                    writeDataD(keyX2, BaseAdjValueRow2);
                   
                    
                    
                    Thread.Sleep(100);
                    //上料基座调整启动
                    writeCoil(25, true);

                }
                else if(ShangLiaoOrXiaLiao == 2)
                {
                    int keyX1 = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料料台底座调整距离_前飞巴")).Key;
                    int keyX2 = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料料台底座调整距离_后飞巴")).Key;
                    writeDataD(keyX1, BaseAdjValueRow1);
                    writeDataD(keyX2, BaseAdjValueRow2);
                    Thread.Sleep(100);
                    //下料基座调整启动
                    writeCoil(4, true);
                }
                else
                {
                    LogRecordVision.addLog("上下料流程标识输入出错");

                }
                LogRecordVision.addLog("基座调整数据已写入,PLC可以启动基座调整");
                
          

            }
            catch(Exception ex)
            {

                LogRecordVision.addLog("飞巴基座距离调整出错"+ex.Message);
            }
    




        }


    }
}
