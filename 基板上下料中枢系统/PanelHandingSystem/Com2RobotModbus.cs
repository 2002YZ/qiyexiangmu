using EasyModbus;
using FolderKeeper;
using HalconDotNet;
using Log;

using System;
using System.Collections;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace PanelHandingSystem
{
    public class Com2RobotM
    {
        //与机器人通讯，采用modbusTCP
        public ModbusClient modbusClient = new ModbusClient();

        public int RegisterStartAddr = 0;

        public int AddrOffSet = 0;
                                    
        public int ScanTime = 300;
        public int RobotNo;

        private object locker = new object();//创建锁
        public Thread Thread_HeartBeat;
        public Thread Thread_Read_Singal;


        //坐标转换计算值,可能需要做加减计算
        public double Multiple = 1;
        public double Offset = 0;

        public bool isConnected;
        public string IPAddr = "";
        public int Port = -1;




        //读寄存器委托，多个寄存器
        public delegate bool[] ReadCoilsD(int StartingAddr, int quiantity);
        public ReadCoilsD readCoils;



        //写寄存器委托
        public delegate void WriteCoilsD(int location, bool flag);
        public WriteCoilsD writeCoil;


        //写数据寄存器，字符串
        public delegate void WriteStringD(int coil_num, string str2write);
        public WriteStringD writeStringD;


        //写数据寄存器，寄存器数组
        public delegate void WriteDataD(int coil_num, double value);
        public WriteDataD writeDataD;


        //读数据寄存器
        public delegate void ReadDataD(int coil_num, int lenth, out double value);
        public ReadDataD readDataD;

        //
        public string  WaitStatues = "null";//主控等待机器人等待状态，用于显示到界面上，指示机器人所在的状态

        public string StatuesShangLiao = "null";//机器人上料状态
        public string StatuesXiaLiao = "null";//机器人下料状态
        #region

    


        //数据寄存器
        public static Dictionary<int, string> DataRegister = new Dictionary<int, string>
        {

            //数据位隔一个，占双位 
            [300] = "上料坐标X",
            [302] = "上料坐标Y",
            [304] = "上料行号",  //行号只有1和2
            [306] = "上料板宽度",
            [308] = "上料板高度",
            [310] = "上料板厚度",
            [312] = "上料料台底座调整距离",
            [314] = "上料循环次数",

            [316] = "下料坐标X",
            [318] = "下料坐标Y",
            [320] = "下料行号", //行号只有1和2
            [322] = "下料板宽度",
            [324] = "下料板高度",
            [326] = "下料板厚度",
            [328] = "下料料台底座调整距离",

            [330] = "传送带取料坐标X",
            [332] = "传送带取料坐标Y",
            [334] = "传送带取料坐标R",
            [336] = "传送带板宽度",
            [338] = "传送带板高度",
            [340] = "传送带板厚度",

            //陪镀版坐标数据，上料和下料共用
            [342] = "陪镀板坐标X",
            [344] = "陪镀板坐标Y",
            [346] = "陪镀板行号",//行号只有1和2
            [348] = "陪镀板宽度",
            [350] = "陪镀板高度",
            [352] = "陪镀板厚度",


            [354] = "力控数据1",
            [356] = "力控数据2",
            [358] = "力控数据3",
            [360] = "机器人下料X偏移值", //下料前3D拍照写入



            //以上为数据位，

            //////////////////////////////////////////////////////////////
            //以下为标志位，int数据类型，值为1表示置位，值为0表示复位
            //标志位占单位

            //下料区
            [0] = "IPC->ROBOT:机器人下料检查程序启动",
            [1] = "ROBOT->IPC:机器人到下料前查位,触发3D下料前检查拍照",
            [2] = "IPC->ROBOT:上位机反馈下料前检查结果OK",
            [3] = "IPC->ROBOT:上位机反馈下料前检查结果NG",
            [4] = "ROBOT->IPC:下料前检查动作完成，且无异常,启动下料流程",
            [5] = "IPC->ROBOT: 陪镀板坐标已写入,启动陪镀板下料流程",
            [6] = "IPC->ROBOT: 没有陪镀板，跳过陪镀板下料",
            [7] = "ROBOT->IPC: 陪镀板下料处理完毕",
            [8] = "IPC->ROBOT: 机器人准备进行主板下料，可以去到等待位",
            [9] = "IPC->ROBOT: 当前板子坐标数据已写入,可以下料",
            [10] = "ROBOT->IPC: 当前板子下料完成开始下一个下料",
            [11] = "IPC->ROBOT: 当前飞巴下料已结束,机器人回等待位",

            [12] = "IPC->ROBOT: 机器人准备进行飞巴定位拍照,前往拍照位",
            [13] = "ROBOT->IPC: 机器人到达飞巴定位拍照位置",
            [14] = "IPC->ROBOT: 3D相机飞巴定位OK",
            [15] = "IPC->ROBOT: 3D相机飞巴定位NG",




            //上料区

            [25] = "IPC->ROBOT: 当前板子在传送带上的坐标数据及尺寸数据已写入,可以取料,同35",
            [26] = "IPC->ROBOT: 机器人在传送带上取料完成,准备上料到飞巴，请求飞巴位置数据",

            [30] = "IPC->ROBOT: 机器人上料检查程序启动",
            [31] = "ROBOT->PLC: 机器人到达上料前检查位置,触发3D上料前检查拍照",
            [32] = "IPC->ROBOT:上位机反馈上料前检查结果OK",
            [33] = "IPC->ROBOT:上位机反馈上料前检查结果NG",
            [34] = "ROBOT->IPC:上料前检查动作完成，且无异常,启动上料循环流程",
            [35] = "IPC->ROBOT: 当前板子在传送带上的坐标数据及尺寸数据已写入,可以取料",
            [36] = "ROBOT->IPC: 机器人在传送带上取料完成,准备上料到飞巴，请求飞巴位置数据",
            [37] = "IPC->ROBOT: 当前板子在飞巴上的坐标数据及尺寸数据已写入,可以向飞巴上料",
            [38] = "ROBOT->IPC: 单次上料动作完成",
            [39] = "IPC->ROBOT: 当前飞巴上料已结束,准备处理陪镀板",
            [40] = "IPC->ROBOT: 陪镀板坐标已写入,启动陪镀板上料流程",
            [41] = "IPC->ROBOT: 没有陪镀板，跳过陪镀板上料",
            [42] = "ROBOT->IPC: 陪镀板上料处理完毕",
            [43] = "IPC->ROBOT: 当前飞巴上料已结束,机器人回等待位",

            [44] = "IPC->ROBOT: 启动机器人拍陪镀板流程，位置1",
            [45] = "ROBOT->IPC: 陪镀板拍照位到达，陪镀板拍照启动",
            [46] = "IPC->ROBOT: 陪镀板尺寸确认信号反馈OK",
            [47] = "IPC->ROBOT: 陪镀板尺寸确认信号反馈NG",
            [48] = "IPC->ROBOT: 启动机器人拍陪镀板流程，位置2",

            [49]= "IPC->ROBOT: 机器人取料信号的状态",   //机器人在陪镀板仓的取料位置，0代表1号位置，1代表2号位置

            [50] = "IPC->ROBOT: 检测到当前是需要带夹边的配镀板，停机报警",  //

            //
            //异常区
            [60] = "IPC->ROBOT: 力控报警",//收到后立即停机报警，复位后置false
            [70] = "IPC->ROBOT: 心跳信号",//5秒一次，写true ,机器人检测到后复位false，持续检测不到true说明掉线

            [80] = "IPC->ROBOT: 软件重启信号",// 软件启动或重启时置位,机器人自动复位
          

        };




        #endregion

        public int Connect(string _IPAddr, int _port, int _RobotNo)
        {
            int Res = -1;
            IPAddr = _IPAddr;
            Port = _port;
            RobotNo = _RobotNo;
            try
            {

          

                //连接PLC
                LogRecordRobot.addLog($"连接机器人{RobotNo}号:{IPAddr}:{Port}");
                modbusClient.ConnectionTimeout = 5000;
                modbusClient.IPAddress = IPAddr;
                modbusClient.Port = Port;
                modbusClient.Connect();
             
                isConnected = true;


                if (RobotNo == 1)
                {
                    RegisterStartAddr = ParaSet.recipe.RobotRegisterStartAddr1;
                    AddrOffSet = ParaSet.recipe.RobotAddrOffSet1;

                }

                if (RobotNo == 2)
                {
                    RegisterStartAddr = ParaSet.recipe.RobotRegisterStartAddr2;
                    AddrOffSet = ParaSet.recipe.RobotAddrOffSet2;
                }


                Thread_HeartBeat = new Thread(Heart_Beat_Func);
                Thread_HeartBeat.Start();



                //启动监听线程      //机器人没有要主动触发的逻辑，没有信号监听线程
                //Thread_Read_Singal = new Thread(Dowork_read_singal_Func);
                //Thread_Read_Singal.Start();


                //委托赋值定义
                readCoils = new ReadCoilsD(ReadCoilsDFunc);
                writeCoil = new WriteCoilsD(WriteCoilsDFunc);
                writeStringD = new WriteStringD(WriteStringDFunc);
                writeDataD = new WriteDataD(WriteDataDDFunc);

                readDataD = new ReadDataD(ReadDataFunc);
                //readStringD = new ReadStringD(ReadStringDFunc);

                // readDataD = new ReadDataD(ReadDataDFunc);

                Res = 1;
            }
            catch (Exception ex)
            {
                LogRecordRobot.addLog($"{RobotNo}号机器人连接出错:{ex.Message}");
            }
            return Res;

        }

        //写入寄存器
        private void ReadDataFunc(int coil_num, int length, out double value)
        {
            //throw new NotImplementedException();

            value = -1;
            try
            {
                lock (locker)//加锁
                {
                    if (!DataRegister.ContainsKey(coil_num))
                        LogRecordRobot.addLog($"读数据寄存器{coil_num},地址不在数据通讯表中");
                    else
                        LogRecordRobot.addLog($"读数据寄存器{coil_num},{DataRegister[coil_num]}");

                    int[] result = modbusClient.ReadHoldingRegisters(coil_num, length);
                    int lenthD = result.Length;

                    if (length > 3)
                    {
                        double back = ModbusClient.ConvertRegistersToDouble(result);
                        LogRecordRobot.addLog("读取寄存器结果:" + back.ToString());
                        value = back;
                    }
                    else
                    {
                        int back = ModbusClient.ConvertRegistersToInt(result);
                        LogRecordRobot.addLog("读取寄存器结果:" + back.ToString());
                        value = (double)back;
                    }

                }
            }
            catch (Exception ex)
            {
                LogRecordRobot.addLog($"Modbus 读数据寄存器出错:起始地址{coil_num}");
                LogRecordRobot.addLog(ex.Message);
                throw ex;
            }
        }


        //信号监听线程



        public int[] ConvertStringToRegisters(string stringToConvert)
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


        public string ConvertRegistersToString(int[] registers, int offset, int stringLength)
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
        private void WriteStringDFunc(int StartingAddr, string str2write)
        {
            try
            {
                lock (locker)//加锁
                {

                    if (!DataRegister.ContainsKey(StartingAddr))
                        LogRecordRobot.addLog($"写{RobotNo}号机器人数据寄存器{StartingAddr},地址不在数据通讯表中");
                    else
                        LogRecordRobot.addLog($"写入{RobotNo}号机器人数据寄存器字符串{StartingAddr},{DataRegister[StartingAddr]}");

                    int[] str2int = new int[1024];
                    int lenght = str2write.Length;
                    //  string str2write2 = str2write + $",{lenght}";
                    string str2write2 = str2write;
                    str2int = ConvertStringToRegisters(str2write2);
                    modbusClient.WriteMultipleRegisters(StartingAddr, str2int);

                    //写入后读取一遍
                    int[] result = modbusClient.ReadHoldingRegisters(StartingAddr, lenght + 5);
                    string back = ConvertRegistersToString(result, 0, lenght + 5);
                    Log.LogRecordRobot.addLog("写入后读取寄存器:" + back);

                }




            }
            catch (Exception ex)
            {
                LogRecordRobot.addLog($"Modbus 写入{RobotNo}号机器人数据寄存器出错:起始地址{StartingAddr},{str2write}");
                LogRecordRobot.addLog(ex.Message);
                throw ex;

            }



        }


        private void WriteDataDDFunc(int StartingAddr, double value)
        {
            try
            {
                lock (locker)//加锁
                {
                    if (!DataRegister.ContainsKey(StartingAddr))
                        LogRecordRobot.addLog($"写{RobotNo}号机器人数据寄存器{StartingAddr},地址不在数据通讯表中");
                    else
                        LogRecordRobot.addLog($"写入{RobotNo}号机器人数据寄存器{StartingAddr},{DataRegister[StartingAddr]}");


                    //double 乘1000后发送int 类型
                    int v = (int)(value * 1000);

                   // int v = (int)value;
                    int[] registers1 = ModbusClient.ConvertIntToRegisters(v);

                   // modbusClient.WriteSingleRegister(StartingAddr, registers1);
                   // int[] registers1 = ModbusClient.ConvertDoubleToRegisters(value);       
                   modbusClient.WriteMultipleRegisters(StartingAddr, registers1);

                    //写入后读取一遍
                    //int lenth = registers1.Length;
                    //int[] result = modbusClient.ReadHoldingRegisters(StartingAddr, lenth);
                    //int lenthD = result.Length;

                    //if (lenth > 3)
                    //{
                    //    double back = ModbusClient.ConvertRegistersToDouble(result);
                    //    Log.LogRecordRobot.addLog("写入后读取寄存器:" + back.ToString());
                    //}
                    //else
                    //{
                    //   // float back = ModbusClient.ConvertRegistersToFloat(result);

                    //    int b = ModbusClient.ConvertRegistersToInt(result);
                    //    Log.LogRecordRobot.addLog("写入后读取寄存器:" + b.ToString());
                    //}

                }

            }
            catch (Exception ex)
            {
                LogRecordRobot.addLog($"Modbus 写入{RobotNo}号机器人数据寄存器出错:起始地址{StartingAddr}");
                LogRecordRobot.addLog(ex.Message);
               
            }

        }



        public static bool[] ConvertToBoolArray(int[] intArray)
        {
            if (intArray == null)
            {
                return null;
            }

            bool[] result = new bool[intArray.Length];

            for (int i = 0; i < intArray.Length; i++)
            {
                result[i] = intArray[i] > 0;
            }

            return result;
        }

        //读多个位，改成字通讯
        public bool[] ReadCoilsDFunc(int StartingAddr, int quiantity)
        {
            bool[] Coils = null;
            try
            {

                if(StartingAddr<300)
                {
                    lock (locker)//加锁
                    {

                        int addrResult = (StartingAddr + RegisterStartAddr + AddrOffSet);

                        int[] res = modbusClient.ReadHoldingRegisters(addrResult, quiantity);

                        //Coils = modbusClient.ReadCoils(addrResult, 8);
                        Coils = ConvertToBoolArray(res);


                        //读单个寄存器
                        if (!DataRegister.ContainsKey(StartingAddr))
                            LogRecordRobot.addLog($"读{RobotNo}号机器人单个coil{StartingAddr},地址不在通讯表中");
                        else
                            LogRecordRobot.addLog($"读{RobotNo}号机器人单个coil{StartingAddr}:{Coils[0].ToString()}:{DataRegister[StartingAddr]}");

                    }

                }
                else
                {
                    LogRecordRobot.addLog($"读{RobotNo}号机器人单个coil{StartingAddr},地址不是bool量");

                }

            }
            catch (Exception ex)
            {
                LogRecordRobot.addLog($"Modbus 读{RobotNo}号机器人寄存器出错:起始地址{StartingAddr},数量{quiantity}");
                LogRecordRobot.addLog(ex.Message);
                throw ex;

            }
            return Coils;











        }





        //读多个寄存器
        //public bool[] ReadCoilsFunc(int StartingAddr, int quiantity)
        //{

        //    bool[] Coils = null;
        //    try
        //    {
        //        lock (locker)//加锁
        //        {

        //            int addrResult = (StartingAddr + RegisterStartAddr + AddrOffSet) * 8;
        //            Coils = modbusClient.ReadCoils(addrResult, 8);


        //            //读单个寄存器
        //            if (!CoilRegister.ContainsKey(StartingAddr))
        //                LogRecordRobot.addLog($"读{RobotNo}号机器人单个coil{StartingAddr},地址不在通讯表中");
        //            else
        //                LogRecordRobot.addLog($"读{RobotNo}号机器人单个coil{StartingAddr},{Coils[0].ToString()}:{CoilRegister[StartingAddr]}");


        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        LogRecordRobot.addLog($"Modbus 读{RobotNo}号机器人寄存器出错:起始地址{StartingAddr},数量{quiantity}");
        //        LogRecordRobot.addLog(ex.Message);
        //        throw ex;

        //    }
        //    return Coils;
        //}



        public void WriteCoilsDFunc(int Addr, bool value)
        {
            try
            {
                lock (locker)//加锁
                {

                    //if (!CoilRegister[Addr].Contains("心跳"))
                    //{
                    //    if (!CoilRegister.ContainsKey(Addr))
                    //        LogRecordRobot.addLog($"写{RobotNo}号机器人单个coil{Addr},地址不在通讯表中");
                    //    else
                    //        LogRecordRobot.addLog($"写{RobotNo}号机器人入coils寄存器{Addr},{value}:{CoilRegister[Addr]},");
                    //}
                    //modbusClient.WriteSingleCoil(Addr + RegisterStartAddr + AddrOffSet, value);

                    if(Addr< 300)
                    {
                        int v = 0;
                        if (value)
                            v = 1;

                        try
                        {
                            if (!DataRegister.ContainsKey(Addr))
                                LogRecordRobot.addLog($"{RobotNo}号机器人:写单个coil{Addr},地址不在通讯表中");
                            else
                                LogRecordRobot.addLog($"{RobotNo}号机器人:写入coils寄存器{Addr},{value}:{DataRegister[Addr]}");
                        }
                        catch (Exception ex)
                        {

                        }
              
                        modbusClient.WriteSingleRegister(Addr, v);


                    }
                    else
                    {

                        LogRecordRobot.addLog("写入bool 量的地址大于300");
                    }

             

                }

            }
            catch (Exception ex)
            {
                LogRecordRobot.addLog($"{RobotNo}号机器人:Modbus写寄存器出错,地址:{Addr}：" + ex.Message);
                //throw ex;
            }

        }



        //public void WriteCoilsFunc(int Addr, bool value)
        //{
        //    try
        //    {
        //        lock (locker)//加锁
        //        {

        //            //if (!CoilRegister[Addr].Contains("心跳"))
        //            //{
        //            //    if (!CoilRegister.ContainsKey(Addr))
        //            //        LogRecordRobot.addLog($"写{RobotNo}号机器人单个coil{Addr},地址不在通讯表中");
        //            //    else
        //            //        LogRecordRobot.addLog($"写{RobotNo}号机器人入coils寄存器{Addr},{value}:{CoilRegister[Addr]},");
        //            //}

        //            int addrResult = (Addr + RegisterStartAddr + AddrOffSet)*8;
        //            //modbusClient.WriteSingleCoil(Addr + RegisterStartAddr + AddrOffSet, value);

        //            bool[] Value = { value, false, false, false, false, false, false, false };
        //            modbusClient.WriteMultipleCoils(addrResult, Value);

        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        LogRecordRobot.addLog($"Modbus写{RobotNo}号机器人寄存器出错,地址:{Addr}：" + ex.Message);
        //        throw ex;
        //    }

        //}












        //读取PLC状态
        public List<bool> ReadPLCStatues()
        {


            List<bool> CoilAll = new List<bool>();

            try
            {

                //起始位，加8192偏置，一次读30个寄存器，前30个寄存器  7100  -  7129
                bool[] coil1 = readCoils(RegisterStartAddr, 30);
                Thread.Sleep(50);
                //起始位，加8192偏置，再加30，一次读30个寄存器，后30个寄存器,起点是7130 -  7159
                bool[] coil2 = readCoils(RegisterStartAddr + 30, 30);
                Thread.Sleep(50);
                //CoilAll是以上两次读取后的结果拼接汇总
                CoilAll.AddRange(coil1);
                CoilAll.AddRange(coil2);

            }
            catch (Exception ex)
            {
                LogRecordRobot.addLog("PLC读取状态程出错" + ex.Message);

            }

            return CoilAll;

        }





        private void Heart_Beat_Func()
        {
            LogRecordRobot.addLog($"{RobotNo}号机器人心跳启动");
            while (true)
            {

                try
                {
                    //写心跳
                  //  writeCoil(70, true);

                }
                catch (Exception ex)
                {
                    LogRecordRobot.addLog($"{RobotNo}号机器人:向写心跳 7159 = true 时出错" + ex.Message);
                }

                Thread.Sleep(4900);
            }

        }


        public bool ReCheckSignalandReset(int StartAddr)
        {
            bool Res = false;
            Thread.Sleep(10);
            bool[] coilConfirm = readCoils(StartAddr, 1);
            if (true == coilConfirm[0])
            {
                //复位
                writeCoil(StartAddr, false);
                Res = true;

            }
            else
            {
                LogRecordRobot.addLog("信号误触发:" + StartAddr);

            }

            return Res;
        }



        public void SendXiaLiaoDxData(double dx)
        {
            //向机器人发送下料时的偏移值

            try
            {
                int keyY = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("机器人下料X偏移值")).Key;
                writeDataD(keyY, dx);

            }
            catch (Exception ex)
            {
                LogRecordRobot.addLog($"{RobotNo}号机器人:写下料飞杆偏移值坐标出错" + ex.Message);
            }

        }



        public void SendXiaLiaoPos(double x1, double y1, int row,double panelWidth, double panelHeight ,double thickness)
        {
            try
            {
                //写下料坐标
                // 获取第一个匹配的键
                int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料坐标X")).Key;

                //坐标偏移
                double Dx = x1 + ParaSet.recipe.FeiBaPosOffSet;
                writeDataD(keyX, Dx);

                int keyY = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料坐标Y")).Key;
                writeDataD(keyY, y1);

                int keyR = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料行号")).Key;
                writeDataD(keyR, row);

                int keyW = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料板宽度")).Key;
                writeDataD(keyW, panelWidth);

                int keyH = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料板高度")).Key;
                writeDataD(keyH, panelHeight);

                int keyT = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料板厚度")).Key;
                writeDataD(keyT, thickness);


               StatuesXiaLiao = $"{RobotNo}号机器人:下 料行号{row},偏移后X坐标{Dx},偏移值{ParaSet.recipe.FeiBaPosOffSet},板宽{panelWidth},板高{panelHeight}";

            }
            catch(Exception ex)
            {

                LogRecordRobot.addLog($"{RobotNo}号机器人写下料坐标出错" + ex.Message);
                throw ex;

            }
   
        }

        public void SendShangLiaoPos(double x1, double y1, int row, double panelWidth, double panelHeight, double thickness)
        {
            try
            {
                //写下料坐标
                // 获取第一个匹配的键
                int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料坐标X")).Key;
                //坐标偏移
                double Dx = x1 + ParaSet.recipe.FeiBaPosOffSet;
               /* if (this.RobotNo==1)
                {
                    Dx += 5.0;
                }*/

                writeDataD(keyX, Dx);

                int keyY = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料坐标Y")).Key;
                writeDataD(keyY, y1);

                int keyR = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料行号")).Key;
                writeDataD(keyR, row);

                int keyW = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料板宽度")).Key;
                writeDataD(keyW, panelWidth);

                int keyH = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料板高度")).Key;
                writeDataD(keyH, panelHeight);

                int keyT = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料板厚度")).Key;
                writeDataD(keyT, thickness);

                string status = $"{RobotNo}号机器人:上 料行号{row},偏移后X坐标{Dx},偏移值{ParaSet.recipe.FeiBaPosOffSet},板宽{panelWidth},板高{panelHeight}";

                //机器人上料状态字符串赋值
                StatuesShangLiao = status;


            }
            catch (Exception ex)
            {

                LogRecordRobot.addLog($"{RobotNo}号机器人:写上料坐标出错" + ex.Message);
                throw ex;

            }

        }

        internal void SendChuanSongDaiPos(double x1, double y1, double r1, double panelWidth, double panelHeight, double thickness)
        {
            try
            {
                //写下料坐标
                // 获取第一个匹配的键
                int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("传送带取料坐标X")).Key;
                writeDataD(keyX, x1);

                int keyY = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("传送带取料坐标Y")).Key;
                writeDataD(keyY, y1);

                int keyW = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("传送带板宽度")).Key;
                writeDataD(keyW, panelWidth);

                int keyH = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("传送带板高度")).Key;
                writeDataD(keyH, panelHeight);

                int keyT = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("传送带板厚度")).Key;
                writeDataD(keyT, thickness);

                int keyR = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("传送带取料坐标R")).Key;
                r1 = r1 * 10;
                writeDataD(keyR, r1);

            }
            catch (Exception ex)
            {
                LogRecordRobot.addLog($"{RobotNo}号机器人:写传送带坐标出错" + ex.Message);
                throw ex;
            }
        }







        internal void SendPeiDuPos(double x1, double y1, int row, double panelWidth, double panelHeight, double thickness, double mainPanelWidth, double mainPanelHeight,int ShangLiaoOrXiaLiao,int PeiDuCangNo = 0)
        {
            try
            {

                //PeiDuCangNo表示当前陪镀板仓的取料位置编号，根据PLC的36,37,38,39，四个寄存器的结果进行传入

                //（x ,y,行号,宽 ,高 ,厚）
                //写下料坐标
                // 获取第一个匹配的键
                int keyX = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("陪镀板坐标X")).Key;
                //坐标偏移
                double Dx = x1 + ParaSet.recipe.FeiBaPosOffSet;

                //配镀板坐标根据配镀板宽度偏移,配镀板坐标已经修改为右边给左边缘，左边给右边缘
               // Dx = Dx - panelWidth / 2;
                writeDataD(keyX, Dx);

                int keyY = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("陪镀板坐标Y")).Key;
                writeDataD(keyY, y1);

                int keyW = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("陪镀板宽度")).Key;
                writeDataD(keyW, panelWidth);

                int keyH = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("陪镀板高度")).Key;
                writeDataD(keyH, panelHeight);

                int keyT = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("陪镀板厚度")).Key;
                writeDataD(keyT, thickness);

                int RowT = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("陪镀板行号")).Key;
                writeDataD(RowT, row);


                //区分上料或下料
                if (ShangLiaoOrXiaLiao == 1)
                {
                    //上料
                    int keyMainW = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料板宽度")).Key;
                    writeDataD(keyMainW, mainPanelWidth);

                    int keyMainH = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("上料板高度")).Key;
                    writeDataD(keyMainH, mainPanelHeight);
                }
                else
                {
                    //下料
                    int keyMainW = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料板宽度")).Key;
                    writeDataD(keyMainW, mainPanelWidth);

                    int keyMainH = DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("下料板高度")).Key;
                    writeDataD(keyMainH, mainPanelHeight);
                }

                if(PeiDuCangNo==0)
                    writeCoil(49, false);
                else
                    writeCoil(49, true);




            }
            catch (Exception ex)
            {
                LogRecordRobot.addLog($"{RobotNo}号机器人:写陪镀板坐标出错" + ex.Message);
                throw ex;
            }
        }


        public bool exitWaitManul = false;
        public bool WaitForSingal(int Addr)
        {
            bool Res = false;
            try
            {  
                //等待机器人信号
                bool GetRes = false;
                int TimeOut = 1200000;//超时时间，根据实际情况调整,调试时设置为20分钟，后面再减少

                DateTime startTime = DateTime.Now;
                LogRecordRobot.addLog($"{RobotNo}号机器人:开始等待{Addr}:{DataRegister[Addr]}为true 的信号...");
                WaitStatues = $"{RobotNo}号机器人:等待{Addr}:{DataRegister[Addr]}为true 的信号...";
                //等待相机拍照触发信号
                while (!GetRes && !exitWaitManul)
                {
                    //读机器人的拍照触发信号
                    bool[] SingalTest = readCoils(Addr, 1);
                    if (SingalTest[0])
                    {

                        LogRecordRobot.addLog($"{RobotNo}号机器人:收到{Addr}:{DataRegister[Addr]},为true 的信号");

                        //读到后复位
                        writeCoil(Addr, false);
                        WaitStatues = "null";

                        GetRes = true;
                        Res = true;

                    }


                    TimeSpan duration = DateTime.Now - startTime; // 计算耗时
                    if (duration.TotalMilliseconds > TimeOut)
                    {
                        //等待超时，日志提示，但是还是继续等待
                        LogRecordRobot.addLog($"{RobotNo}号机器人:信号{Addr}:{DataRegister[Addr]},超时未置位");
                        LogRecordRobot.addLog($"超时设定{TimeOut}ms");
                        return Res;
                    }
                    // Res = true;

                    Thread.Sleep(800);
                }

                if (exitWaitManul)
                {
                    LogRecordRobot.addLog($"{RobotNo}号机器人:信号{Addr}:{DataRegister[Addr]},手动退出等待");
                    exitWaitManul = false;
                }   

            }
            catch(Exception ex) 
            {
                LogRecordRobot.addLog($"{RobotNo}号机器人:信号{Addr}:{DataRegister[Addr]},等待出错:{ex.Message}");

            }
            return Res;

        }


    }
}
