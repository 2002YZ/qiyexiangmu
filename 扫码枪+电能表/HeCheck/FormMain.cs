#define PLC
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkyTool;
using System.Threading;
using System.Reflection;
using System.Net.Sockets;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using ScanCheck;
using System.IO;

namespace HeCheck
{
    public partial class FormMain : Form
    {
        /// <summary>
        /// 扫码枪 2024-12-04
        /// 
        /// </summary>
        public FormMain()
        {
            InitializeComponent();
        }

        #region 命名定义
        /// <summary>
        /// 软件运行标志
        /// </summary>
        private TcpClientModbusRtu modbusRtu = new TcpClientModbusRtu();
        private TCPClient tcpHe = new TCPClient();
        public static int countscan1 = 0;
        public static int countscan2 = 0;
        public static int countscan3 = 0;
        public static int countscan4 = 0;
        public static int countscan5 = 0;
        public static int countscan6 = 0;
        public static int countscan7 = 0;

        #endregion

        #region 定义
        // 软件运行标志
        private bool bRun = true;

        // 重启标志
        private bool bRestart = false;

        /// 6个标签+  1 个CIP
        private GroupBox[] gbScan = new GroupBox[scanCount + 1];
        private TcpClientString[] tcpScan = new TcpClientString[scanCount + 1];
        /// <summary>
        /// 0 使用， 1 禁用
        /// </summary>
        private ComboBox[] cboScanEnable = new ComboBox[scanCount + 1];
        private TextBox[] txtScanIP = new TextBox[scanCount + 1];
        private NumericUpDown[] numScanPort = new NumericUpDown[scanCount + 1];
        private Button[] btnScan = new Button[scanCount + 1];

        private ToolStripStatusLabel[] tsslScan = new ToolStripStatusLabel[scanCount + 1];
        /// <summary>
        /// 0 使用, 1 禁用
        /// </summary>
        private int[] iScanEnable = new int[scanCount + 1];
        private string[] strScanIP = new string[scanCount + 1];
        private int[] iScanPort = new int[scanCount + 1];

        /// <summary>
        /// 心跳类型 0 bool ，1 UINT
        /// </summary>
        private int[] iScanBeatType = new int[scanCount + 1];
        private ComboBox[] ScancboBeatType = new ComboBox[scanCount + 1];

        private Thread[] scanThread = new Thread[scanCount + 1];
        private const int scanCount = 8;
        private ScanClass scanClass = new ScanClass();
        private PowerClass powerlabel = new PowerClass();

        /// <summary>
        /// 日志文件夹路径
        /// </summary>
        private string _LogPath = Environment.CurrentDirectory + "\\log";//log目录
        private INIFile iniFile = new INIFile(Environment.CurrentDirectory + "\\ini\\setting.ini");

        /// <summary>
        /// 登录时间
        /// </summary>
        private DateTime loginTime = DateTime.Now;
        /// <summary>
        /// 登录 标志位
        /// </summary>
        private int loginFlag = -1;
        #endregion

        #region --电能表数据
        private int powerRunCount = 0;

        /// <summary>
        /// 数据名称 安科瑞
        /// </summary>
        private string[] powerName = { "Ua", "Ub", "Uc", "Uab", "Ubc", "Uca", "Ia", "Ib", "Ic", "Io", "Pa", "Pb", "Pc", "Pt", "Qa", "Qb", "Qc", "Qt" };
        /// <summary>
        /// 数据名称 正泰名称
        /// </summary>
        private string[] powerNameChint = { "Uab", "Ubc", "Uca", "Ua", "Ub", "Uc", "Ia", "Ib", "Ic", "Pt", "Pa", "Pb", "Pc" };

        /// <summary>
        /// 电能表数据名称
        /// </summary>
        private string strPowerName = "Uab,Ubc,Uca,Ua,Ub,Uc,Ia,Ib,Ic,Pt,Pa,Pb,Pc,Qt,Qa,Qb,Qc,ImpEp,ExpEp";

        /// <summary>
        /// 品牌  电能表时( 0 安科瑞/ 1 正泰) 电子秤时（0 电子秤，1 扫码枪）
        /// </summary>
        private ComboBox cboBrand = new ComboBox();
        /// <summary>
        /// 电能表数量
        /// </summary>
        private NumericUpDown numPowerCount = new NumericUpDown();
        /// <summary>
        /// 电压比
        /// </summary>
        private NumericUpDown numVRate = new NumericUpDown();
        /// <summary>
        /// 电流比
        /// </summary>
        private NumericUpDown numIRate = new NumericUpDown();
        /// <summary>
        /// 类型   电能表时（0 安科瑞/ 1 正泰） 电子秤时（0 电子秤/ 1 扫码枪）
        /// </summary>
        private int iType = new int();
        /// <summary>
        /// 电能表数量
        /// </summary>
        private int iPowerCount = 1;
        /// <summary>
        /// 电流变比
        /// </summary>
        private double iRate = 1;
        /// <summary>
        /// 电压变比
        /// </summary>
        private double vRate = 1;
        #endregion

        #region 窗体加载
        private void FormMain_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(200, 20);
            this.Height = 800;
            this.Width = 960;
            this.MaximizeBox = false;
            AddMsg("打开软件 2024-12-03..." + tsslVer.Text);
            PreLoad();
            InitStatus();
            InitGroup();
            ShowLoadPara();
            InitThread();
            timerUI.Start();
            for (int j = 0; j < scanCount + 1; j++)
            {
                if (iScanEnable[j] == 1) continue;
                btnScan_Click(btnScan[j], e);
            }
        }
        #endregion

        #region ---发送读取线程和心跳线程
        private void ScanThread1()
        {
            int index = 0;
            while (bRun)
            {
                Thread.Sleep(scanClass.delay);
                try
                {
                    ScanCommRun1(index);
                }
                catch
                { }
            }
        }
        private void ScanThread2()
        {
            int index = 1;
            while (bRun)
            {
                Thread.Sleep(scanClass.delay);
                try
                {
                    ScanCommRun2(index);
                }
                catch
                { }
            }
        }
        private void ScanThread3()
        {
            int index = 2;
            while (bRun)
            {
                Thread.Sleep(scanClass.delay);
                try
                {
                    ScanCommRun3(index);
                }
                catch
                { }
            }
        }
        private void ScanThread4()
        {
            int index = 3;
            while (bRun)
            {
                Thread.Sleep(scanClass.delay);
                try
                {
                    ScanCommRun4(index);
                }
                catch
                { }
            }
        }
        private void ScanThread5()
        {
            int index = 4;
            while (bRun)
            {
                Thread.Sleep(scanClass.delay);
                try
                {
                    ScanCommRun5(index);
                }
                catch
                { }
            }
        }
        private void ScanThread6()
        {
            int index = 5;
            while (bRun)
            {
                Thread.Sleep(scanClass.delay);
                try
                {
                    ScanCommRun6(index);
                }
                catch
                { }
            }
        }
        private void ScanThread7()
        {
            int index = 6;
            while (bRun)
            {
                Thread.Sleep(scanClass.delay);
                try
                {
                    ScanCommRun7(index);
                }
                catch
                { }
            }
        }
        private void PowerThread1()
        {
            int index = 5;
            while (bRun)
            {
                Thread.Sleep(powerlabel.delay);
                try
                {
                    PowerCommRun(index);
                }
                catch
                { }
            }
        }
        public void HeartBeat()
        {
            bool status = false;
            while (bRun)
            {
                Thread.Sleep(1000);
                try
                {
                    if (iScanEnable[scanCount] == 1) continue;

                    if (tcpHe.isConnected && tcpHe.IsConnectOM() && !status)
                    {
                        for (int i = 0; i < scanCount; i++)
                        {
                            if (iScanEnable[i] == 0 && i < scanCount - 1)
                                tcpHe.OMWriteNode(scanClass.beat[i], GetOMType(iScanBeatType[i]), 1.ToString());
                            if (iScanEnable[i] == 0 && i == scanCount - 1)
                                tcpHe.OMWriteNode(powerlabel.beat[0], OMType.OMuint, 1.ToString());
                        }
                        status = true;
                    }
                    else if (tcpHe.isConnected && tcpHe.IsConnectOM() && status)
                    {
                        for (int i = 0; i < scanCount; i++)
                        {
                            if (iScanEnable[i] == 0 && i < scanCount - 1)
                                tcpHe.OMWriteNode(scanClass.beat[i], GetOMType(iScanBeatType[i]), 0.ToString());
                            if (iScanEnable[i] == 0 && i == scanCount - 1)
                                tcpHe.OMWriteNode(powerlabel.beat[0], OMType.OMuint, 0.ToString());
                        }
                        status = false;
                    }
                    else
                    {
                        continue;
                    }
                    Thread.Sleep(100);
                    for (int index = 0; index < scanCount; index++)
                    {
                        if (!tcpHe.isConnected || !tcpHe.IsConnectOM()) continue;
                        if (iScanEnable[index] == 1) continue;
                        if (iScanEnable[index] == 0 && index < scanCount - 1)
                            tcpHe.OMWriteNode(scanClass.heConnected[index], GetOMType(iScanBeatType[index]), tcpScan[index].IsConnected() ? "1" : "0");
                        if (iScanEnable[index] == 0 && index == scanCount - 1)
                            tcpHe.OMWriteNode(powerlabel.heConnected[0], OMType.OMuint, modbusRtu.IsConnected() ? "1" : "0");
                        Thread.Sleep(10);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        /// <summary>
        /// 0 OMbool, 1 OMuint
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private OMType GetOMType(int type)
        {
            switch (type)
            {
                case 0:
                    return OMType.OMbool;
                case 1:
                    return OMType.OMuint;
                default:
                    return OMType.OMbool;
            }
        }
        #endregion

        #region --读取线程的实现方法和Tcp客户端的方法实现
        private void ScanCommRun1(int index)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM() || !tcpScan[index].IsConnected()) return;
            if (tcpHe.OMReadNode(scanClass.triger[index]) != "1")
                return;
            tcpScan[index].SendMsg(scanClass.strSend);
        }
        private void ScanCommRun2(int index)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM() || !tcpScan[index].IsConnected()) return;
            if (tcpHe.OMReadNode(scanClass.triger[index]) != "1")
                return;
            tcpScan[index].SendMsg(scanClass.strSend);
        }
        private void ScanCommRun3(int index)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM() || !tcpScan[index].IsConnected()) return;
            if (tcpHe.OMReadNode(scanClass.triger[index]) != "1")
                return;
            tcpScan[index].SendMsg(scanClass.strSend);
        }
        private void ScanCommRun4(int index)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM() || !tcpScan[index].IsConnected()) return;
            if (tcpHe.OMReadNode(scanClass.triger[index]) != "1")
                return;
            tcpScan[index].SendMsg(scanClass.strSend);
        }
        private void ScanCommRun5(int index)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM() || !tcpScan[index].IsConnected()) return;
            if (tcpHe.OMReadNode(scanClass.triger[index]) != "1")
                return;
            tcpScan[index].SendMsg(scanClass.strSend);
        }
        private void ScanCommRun6(int index)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM() || !tcpScan[index].IsConnected()) return;
            if (tcpHe.OMReadNode(scanClass.triger[index]) != "1")
                return;
            tcpScan[index].SendMsg(scanClass.strSend);
        }
        private void ScanCommRun7(int index)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM() || !tcpScan[index].IsConnected()) return;
            if (tcpHe.OMReadNode(scanClass.triger[index]) != "1")
                return;
            tcpScan[index].SendMsg(scanClass.strSend);
        }
        private void PowerCommRun(int index)
        {
#if PLC
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM()) return;
#endif
            // string s = tcpHe[heCount].OMReadNode(scanlabel.triger[index]);

            //if (tcpHe[heCount].OMReadNode(scanlabel.triger[index]) != "1") return;

            if (!modbusRtu.IsConnected()) return;
            /*
            if(powerRunCount>powerlabel.printCount)
                AddMsg($"{strname[index]} 发送读取命令.....");
            */
            if (powerlabel.type == 1)//正泰
            {
                for (int i = 0; i < iPowerCount; i++)
                {
                    string data = modbusRtu.Read03HexString(1 + i, powerlabel.startAddr[powerlabel.type], powerlabel.readCount[powerlabel.type] * 2);//正泰 一次性读取所有数据
                    if (data == "" || data.Length != powerlabel.readCount[powerlabel.type] * 4 * 2)
                    {
#if PLC
                        tcpHe.OMWriteNode(powerlabel.result[i], OMType.OMuint, 2.ToString());
#endif
                        return;
                    }
                    //ImpEp （当前）正向有功总电能
                    string ImpEp = modbusRtu.Read03HexString(1 + i, 0x101E, 1 * 2);

                    //ExpEp （当前）反向有功总电能
                    string ExpEp = modbusRtu.Read03HexString(1 + i, 0x1028, 1 * 2);


                    string value = powerlabel.ToValueString(data + ImpEp + ExpEp);
                    if (powerRunCount > powerlabel.printCount)
                    {
                        //AddMsg(strPowerName);
                        //AddMsg(value);
                        AddMsg(powerlabel.PrintString(value, strPowerName));
                    }
#if PLC
                    tcpHe.OMWriteNode(powerlabel.data[i], OMType.OMfloat, value);
                    tcpHe.OMWriteNode(powerlabel.result[i], OMType.OMuint, 1.ToString());
#endif
                }
                if (powerRunCount > powerlabel.printCount)
                {
                    powerRunCount = 0;
                }
                else
                {
                    powerRunCount++;
                }

                //Ua测量值=0x45097000(单精度浮点)×电压变比×0.1=2199(十进制)×(10×0.1)×0.1=219.9V
                //data = modbusRtu[index].Read03HexString(1, 0x200C, 2);//正泰 A相相电流
                //Ia测量值=0x459C3800(单精度浮点)×电流变比×0.001=4999(十进制)×20×0.001=99.98A。

                //data = modbusRtu[index].Read03HexString(1, 0x101E, 2);//正泰 正向有功总电能ImpEp (101EH)
                //ImpEp测量值=0x3FF1EB85(单精度浮点)×电流变比×电压变比=1.89(十进制)×20×(10×0.1)= 37.8kWH

                //tcpHe[heCount].OMWriteNode(scanlabel.triger[index], (OMType)0, "0");
            }
            else//安科瑞
            {
                for (int i = 0; i < iPowerCount; i++)
                {
                    //short[] datas = modbusRtu[index].Read03Short(1+i, powerlabel.startAddr[powerlabel.type],2);//安科瑞 
                    short[] datas = modbusRtu.Read03Short(1 + i, powerlabel.startAddr[powerlabel.type], powerlabel.readCount[powerlabel.type]);//安科瑞 
                    if (datas == null || datas.Length != powerlabel.readCount[powerlabel.type])
                    {
#if PLC
                        tcpHe.OMWriteNode(powerlabel.result[i], OMType.OMuint, 2.ToString());
#endif
                        return;
                    }
                    string value = powerlabel.ToValueString(datas);
                    //string name = string.Join(",", powerNameChint);
                    if (powerRunCount > powerlabel.printCount)
                    {
                        // AddMsg(name);
                        //AddMsg(value);
                        AddMsg(powerlabel.PrintString(value, strPowerName));
                    }
#if PLC
                    tcpHe.OMWriteNode(powerlabel.data[i], OMType.OMfloat, value);
                    tcpHe.OMWriteNode(powerlabel.result[i], OMType.OMuint, 1.ToString());
#endif
                }
                if (powerRunCount > powerlabel.printCount)
                {
                    powerRunCount = 0;
                }
                else
                {
                    powerRunCount++;
                }
            }
        }
        private void ScanCommRunWrite1(int index, string msg)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM()) return;
            if (msg.Trim() != "NG")
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 1.ToString());
            }
            else
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 2.ToString());
            }
        }
        private void ScanCommRunWrite2(int index, string msg)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM()) return;
            if (msg.Trim() != "NG")
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 1.ToString());
            }
            else
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 2.ToString());
            }
        }
        private void ScanCommRunWrite3(int index, string msg)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM()) return;
            if (msg.Trim() != "NG")
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 1.ToString());
            }
            else
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 2.ToString());
            }
        }
        private void ScanCommRunWrite4(int index, string msg)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM()) return;
            if (msg.Trim() != "NG")
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 1.ToString());
            }
            else
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 2.ToString());
            }
        }
        private void ScanCommRunWrite5(int index, string msg)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM()) return;
            if (msg.Trim() != "NG")
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 1.ToString());
            }
            else
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 2.ToString());
            }
        }
        private void ScanCommRunWrite6(int index, string msg)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM()) return;
            if (msg.Trim() != "NG")
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 1.ToString());
            }
            else
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 2.ToString());
            }
        }
        private void ScanCommRunWrite7(int index, string msg)
        {
            if (!tcpHe.isConnected || !tcpHe.IsConnectOM()) return;
            if (msg.Trim() != "NG")
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 1.ToString());
            }
            else
            {
                tcpHe.OMWriteNode(scanClass.data[index], OMType.OMstring, msg.Trim());
                tcpHe.OMWriteNode(scanClass.result[index], OMType.OMuint, 2.ToString());
            }
        }
        #endregion

        #region  初始化功能
        /// <summary>
        /// 事件注册
        /// </summary>
        private void RegisterScanEvent(int i)
        {
            switch (i)
            {
                case 0:
                    tcpScan[i].showClientRecvDataEvent += new TcpClientString.showClientRecvDataHandler(ScanClientReceived1);
                    tcpScan[i].showClientDebugEvent += new TcpClientBaseString.showClientDebugHandler(ScanClientDebug1);
                    break;
                case 1:
                    tcpScan[i].showClientRecvDataEvent += new TcpClientString.showClientRecvDataHandler(ScanClientReceived2);
                    tcpScan[i].showClientDebugEvent += new TcpClientBaseString.showClientDebugHandler(ScanClientDebug2);
                    break;
                case 2:
                    tcpScan[i].showClientRecvDataEvent += new TcpClientString.showClientRecvDataHandler(ScanClientReceived3);
                    tcpScan[i].showClientDebugEvent += new TcpClientBaseString.showClientDebugHandler(ScanClientDebug3);
                    break;
                case 3:
                    tcpScan[i].showClientRecvDataEvent += new TcpClientString.showClientRecvDataHandler(ScanClientReceived4);
                    tcpScan[i].showClientDebugEvent += new TcpClientBaseString.showClientDebugHandler(ScanClientDebug4);
                    break;
                case 4:
                    tcpScan[i].showClientRecvDataEvent += new TcpClientString.showClientRecvDataHandler(ScanClientReceived5);
                    tcpScan[i].showClientDebugEvent += new TcpClientBaseString.showClientDebugHandler(ScanClientDebug5);
                    break;
                case 5:
                    tcpScan[i].showClientRecvDataEvent += new TcpClientString.showClientRecvDataHandler(ScanClientReceived6);
                    tcpScan[i].showClientDebugEvent += new TcpClientBaseString.showClientDebugHandler(ScanClientDebug6);
                    break;
                case 6:
                    tcpScan[i].showClientRecvDataEvent += new TcpClientString.showClientRecvDataHandler(ScanClientReceived7);
                    tcpScan[i].showClientDebugEvent += new TcpClientBaseString.showClientDebugHandler(ScanClientDebug7);
                    break;
                case 7:
                    modbusRtu.showClientDebugEvent += new TcpClientBase.showClientDebugHandler(ClientPowerDebug1);
                    break;
                case 8:
                    tcpHe.SetCommType(TcpCommType.OMPlc);
                    tcpHe.showClientRecvDataEvent += new TCPClient.showClientRecvDataHandler(ClientReceived6);
                    tcpHe.showClientDebugEvent += new TCPClient.showClientDebugHandler(AddMsg);
                    break;
                default:
                    break;
            }
        }//扫码枪事件

        /// <summary>
        /// 线程初始化
        /// </summary>
        private void InitThread()
        {
            int k = 0;
            scanThread[k++] = new Thread(ScanThread1);
            scanThread[k++] = new Thread(ScanThread2);
            scanThread[k++] = new Thread(ScanThread3);
            scanThread[k++] = new Thread(ScanThread4);
            scanThread[k++] = new Thread(ScanThread5);
            scanThread[k++] = new Thread(ScanThread6);
            scanThread[k++] = new Thread(ScanThread7);
            scanThread[k++] = new Thread(PowerThread1);
            scanThread[k++] = new Thread(HeartBeat);
            for (int i = 0; i < scanCount + 1; i++)
            {
                if (iScanEnable[i] == 0)
                    scanThread[i].Start();
            }
        }
        /// <summary>
        /// 状态栏初始化
        /// </summary>
        private void InitStatus()
        {
            for (int i = scanCount; i >= 0; i--)
            {
                if (i == scanCount)
                {
                    tsslScan[i] = new ToolStripStatusLabel();
                    tsslScan[i].Text = "PLC";
                }
                else if (i == scanCount - 1)
                {
                    tsslScan[i] = new ToolStripStatusLabel();
                    tsslScan[i].Text = "电能表";
                }
                else
                {
                    tsslScan[i] = new ToolStripStatusLabel();
                    if (i >= 0 && i < 2)
                        tsslScan[i].Text = $"壳{(i + 1)}";

                    else if (i >= 2 && i < 4)
                        tsslScan[i].Text = $"电芯{(i - 1)}";
                    else if(i==4)
                        tsslScan[i].Text = "盖";
                    else if(i>4)
                        tsslScan[i].Text = $"下料扫码{(i-4)}";
                }
                ToolStripStatusLabel tsslSpace = new ToolStripStatusLabel();
                tsslSpace.Text = " ";
                if (iScanEnable[i] == 0)
                {
                    statusStrip1.Items.Insert(1, tsslSpace);
                    statusStrip1.Items.Insert(1, tsslScan[i]);
                    tsslScan[i].BackColor = Color.Red;
                }
            } //扫码枪

        }
        #endregion

        #region ---Tcp客户端接收到Received数据响应方法
        private void ScanClientReceived1(string str)
        {
            int i = 0;
            if (str == null || str == "")
            {
                tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                tcpHe.OMWriteNode(scanClass.result[i], OMType.OMuint, 2.ToString());
                return;
            }
            else
            {
                if (countscan1 < 2 && str.Trim() == "NG")
                {
                    AddMsg("壳1扫码枪:扫码" + str);
                    countscan1++;
                    Thread.Sleep(100);
                    tcpScan[i].SendMsg(scanClass.strSend);
                }
                else
                {
                    countscan1 = 0;
                    ScanCommRunWrite1(i, str);
                    tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                }
            }
        }
        private void ScanClientReceived2(string str)
        {
            int i = 1;
            if (str == null || str == "")
            {
                tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                tcpHe.OMWriteNode(scanClass.result[i], OMType.OMuint, 2.ToString());
                return;
            }
            else
            {
                if (countscan2 < 2 && str.Trim() == "NG")
                {
                    AddMsg("壳2扫码枪:扫码" + str);
                    countscan2++;
                    Thread.Sleep(100);
                    tcpScan[i].SendMsg(scanClass.strSend);
                }
                else
                {
                    countscan2 = 0;
                    ScanCommRunWrite2(i, str);
                    tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                }
            }
        }
        private void ScanClientReceived3(string str)
        {
            int i = 2;
            if (str == null || str == "")
            {
                tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                tcpHe.OMWriteNode(scanClass.result[i], OMType.OMuint, 2.ToString());
                return;
            }
            else
            {
                if (countscan3 < 2 && str.Trim() == "NG")
                {
                    AddMsg("电芯1扫码枪:扫码" + str);
                    countscan3++;
                    Thread.Sleep(100);
                    tcpScan[i].SendMsg(scanClass.strSend);
                }
                else
                {
                    countscan3 = 0;
                    ScanCommRunWrite3(i, str);
                    tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                }
            }

        }
        private void ScanClientReceived4(string str)
        {
            int i = 3;
            if (str == null || str == "")
            {
                tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                tcpHe.OMWriteNode(scanClass.result[i], OMType.OMuint, 2.ToString());
                return;
            }
            else
            {
                if (countscan4 < 2 && str.Trim() == "NG")
                {
                    AddMsg("电芯2扫码枪:扫码" + str);
                    countscan4++;
                    Thread.Sleep(100);
                    tcpScan[i].SendMsg(scanClass.strSend);
                }
                else
                {
                    countscan4 = 0;
                    ScanCommRunWrite4(i, str);
                    tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                }
            }
        }
        private void ScanClientReceived5(string str)
        {
            int i = 4;
            if (str == null || str == "")
            {
                tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                tcpHe.OMWriteNode(scanClass.result[i], OMType.OMuint, 2.ToString());
                return;
            }
            else
            {
                if (countscan5 < 2 && str.Trim() == "NG")
                {
                    AddMsg("盖扫码枪:扫码" + str);
                    countscan5++;
                    Thread.Sleep(100);
                    tcpScan[i].SendMsg(scanClass.strSend);
                }
                else
                {
                    countscan5 = 0;
                    ScanCommRunWrite5(i, str);
                    tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                }
            }
        }

        private void ScanClientReceived6(string str)
        {
            int i = 5;
            if (str == null || str == "")
            {
                tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                tcpHe.OMWriteNode(scanClass.result[i], OMType.OMuint, 2.ToString());
                return;
            }
            else
            {
                if (countscan6 < 2 && str.Trim() == "NG")
                {
                    AddMsg("下料扫码枪1:扫码" + str);
                    countscan6++;
                    Thread.Sleep(100);
                    tcpScan[i].SendMsg(scanClass.strSend);
                }
                else
                {
                    countscan6 = 0;
                    ScanCommRunWrite6(i, str);
                    tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                }
            }
        }
        private void ScanClientReceived7(string str)
        {
            int i = 6;
            if (str == null || str == "")
            {
                tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                tcpHe.OMWriteNode(scanClass.result[i], OMType.OMuint, 2.ToString());
                return;
            }
            else
            {
                if (countscan7 < 2 && str.Trim() == "NG")
                {
                    AddMsg("下料扫码枪2:扫码" + str);
                    countscan7++;
                    Thread.Sleep(100);
                    tcpScan[i].SendMsg(scanClass.strSend);
                }
                else
                {
                    countscan7 = 0;
                    ScanCommRunWrite7(i, str);
                    tcpHe.OMWriteNode(scanClass.triger[i], OMType.OMbool, 0.ToString());
                }
            }
        }
        private void ClientReceived6(string str)
        {
        }
        #endregion

        #region ---Tcp客户端Debug响应方法
        private void ScanClientDebug1(string msg)
        {
            AddMsg("壳1扫码枪: " + msg);
        }
        private void ScanClientDebug2(string msg)
        {
            AddMsg("壳2扫码枪: " + msg);
        }
        private void ScanClientDebug3(string msg)
        {
            AddMsg("电芯1扫码枪: " + msg);
        }
        private void ScanClientDebug4(string msg)
        {
            AddMsg("电芯2扫码枪: " + msg);
        }
        private void ScanClientDebug5(string msg)
        {
            AddMsg("盖扫码枪: " + msg);
        }
        private void ScanClientDebug6(string msg)
        {
            AddMsg("下料1扫码枪: " + msg);
        }
        private void ScanClientDebug7(string msg)
        {
            AddMsg("下料2扫码枪: " + msg);
        }
        private void ClientPowerDebug1(string msg)
        {
            AddMsg("电能表: " + msg);
        }
        #endregion

        #region 窗体关闭 和 UI定时器
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (txtPassword.Text.Trim() != "555" && textBox1.Text.Trim() != "555")
            {
                MessageBox.Show("请输入密码再关闭...");
                e.Cancel = true;
                return;
            }
            if (MessageBoxEx.Show("确定关闭软件吗？", "提示", MessageBoxButtons.YesNo, "确定,取消".Split(',')) == DialogResult.Yes)
            {
                // FormClose();
            }
            else
            {
                e.Cancel = true;
                return;
            }

            bRun = false;
            Thread.Sleep(200);
            for (int i = 0; i < scanCount + 1; i++)
            {
                if (i == scanCount)
                    tcpHe.Close();
                if (i == scanCount - 1)
                    modbusRtu.Close();
                tcpScan[i].Close();
            }
            AddMsg("关闭软件...");
            timerUI.Stop();
            Thread.Sleep(200);
            Environment.Exit(0);
        }
        private void timerUI_Tick(object sender, EventArgs e)
        {
            tsslTimer.Text = DateTime.Now.ToString();
            if (loginFlag >= 0 && (DateTime.Now - loginTime).TotalSeconds > 8 * 60)//8分钟后 退出登录
            {
                txtPassword.Text = "";
                loginFlag = -1;
                AddMsg("退出登录...");
            }
            for (int i = 0; i < scanCount + 1; i++)
            {
                if (i == scanCount)
                {
                    if (tcpHe.IsConnectOM())
                    {
                        tsslScan[i].BackColor = Color.YellowGreen;
                        txtScanIP[i].Enabled = false;
                        numScanPort[i].Enabled = false;
                        btnScan[i].Text = "断开";
                    }
                    else
                    {
                        tsslScan[i].BackColor = Color.Red;
                        txtScanIP[i].Enabled = true;
                        numScanPort[i].Enabled = true;
                        btnScan[i].Text = "连接";
                    }
                }
                else if (i == scanCount - 1)
                {
                    if (modbusRtu.IsConnected())
                    {
                        tsslScan[i].BackColor = Color.YellowGreen;
                        txtScanIP[i].Enabled = false;
                        numScanPort[i].Enabled = false;
                        btnScan[i].Text = "断开";
                    }
                    else
                    {
                        tsslScan[i].BackColor = Color.Red;
                        txtScanIP[i].Enabled = true;
                        numScanPort[i].Enabled = true;
                        btnScan[i].Text = "连接";
                    }

                }
                else if (i < scanCount - 1)
                {


                    if (tcpScan[i].IsConnected())
                    {
                        tsslScan[i].BackColor = Color.YellowGreen;
                        txtScanIP[i].Enabled = false;
                        numScanPort[i].Enabled = false;
                        btnScan[i].Text = "断开";
                    }
                    else
                    {
                        if (iScanEnable[i] == 0)
                            tsslScan[i].BackColor = Color.Red;
                        txtScanIP[i].Enabled = true;
                        numScanPort[i].Enabled = true;
                        btnScan[i].Text = "连接";
                    }
                }
                else
                {
                    continue;
                }
            }
        }

        #endregion

        #region 参数保存和加载连接
        private void btnSave_Click(object sender, EventArgs e)
        {

            bool bRestart = false;
            for (int i = 0; i < scanCount + 1; i++)
            {
                strScanIP[i] = txtScanIP[i].Text.Trim();
                iScanPort[i] = Convert.ToUInt16(numScanPort[i].Value);
                iScanBeatType[i] = ScancboBeatType[i].SelectedIndex;
                if (iScanEnable[i] != cboScanEnable[i].SelectedIndex)
                {
                    bRestart = true;
                    iniFile.IniWriteValueInt("Scan", "Enable" + i, cboScanEnable[i].SelectedIndex);
                }
                iniFile.IniWriteValueString("Scan", "IP" + i, strScanIP[i]);
                iniFile.IniWriteValueInt("Scan", "Port" + i, iScanPort[i]);
                iniFile.IniWriteValueInt("Scan", "BeatTyte" + i, iScanBeatType[i]);
                if (i == scanCount - 1)
                {
                    iniFile.IniWriteValueInt("Power", "Count", Convert.ToInt32(numPowerCount.Value));
                    iniFile.IniWriteValueDouble("Power", "VRate", Convert.ToDouble(numVRate.Value));
                    iniFile.IniWriteValueDouble("Power", "IRate", Convert.ToDouble(numIRate.Value));
                    iniFile.IniWriteValueInt("System", "Type", cboBrand.SelectedIndex);
                }
            }
            AddMsg($"参数保存成功... {(bRestart ? "请重启软件！" : "")}");
            MessageBox.Show($"参数保存成功... {(bRestart ? "请重启软件！" : "")}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (bRestart)
            {
                bRun = false;
                Thread.Sleep(200);
                for (int i = 0; i < scanCount + 1; i++)
                {
                    tcpScan[i].Close();
                    if (i == scanCount - 1)
                        modbusRtu.Close();
                    if (i == scanCount)
                        tcpHe.Close();
                }
                AddMsg("关闭软件...");
                timerUI.Stop();
                Thread.Sleep(200);
                Environment.Exit(0);
            }
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            int index = Convert.ToInt16(((Button)sender).Tag);
            if (index == scanCount)
            {
                if (!tcpHe.isConnected)
                {
                    try
                    {
                        tcpHe.ConnectServer(strScanIP[index], iScanPort[index]);
                    }
                    catch
                    {

                    }
                }
                else
                {
                    tcpHe.Close();
                }
            }
            else if (index == scanCount - 1)
            {
                if (!modbusRtu.IsConnected())
                {
                    try
                    {
                        modbusRtu.ConnectServer(strScanIP[index], iScanPort[index]);
                    }
                    catch
                    {

                    }
                }
                else
                {
                    modbusRtu.Close();
                }
            }
            else
            {
                if (!tcpScan[index].IsConnected())
                {
                    try
                    {
                        tcpScan[index].ConnectServer(strScanIP[index], iScanPort[index]);
                    }
                    catch
                    {

                    }
                }
                else
                {
                    tcpScan[index].Close();
                }
            }
        }

        private void PreLoad()
        {
            for (int i = 0; i < scanCount + 1; i++)//扫码枪
            {
                strScanIP[i] = iniFile.IniReadValueString("Scan", "IP" + i, $"192.168.1.{30 + i}");
                iScanPort[i] = iniFile.IniReadValueInt("Scan", "Port" + i, 9000 + i);
                iScanEnable[i] = iniFile.IniReadValueInt("Scan", "Enable" + i, 1);
                iScanBeatType[i] = iniFile.IniReadValueInt("Scan", "BeatTyte" + i, 0);
                if (i == scanCount - 1)
                {
                    iPowerCount = iniFile.IniReadValueInt("Power", "Count", iPowerCount);
                    vRate = iniFile.IniReadValueDouble("Power", "VRate", vRate);
                    iRate = iniFile.IniReadValueDouble("Power", "IRate", iRate);
                    iType = iniFile.IniReadValueInt("System", "Type", 1);
                    powerlabel.type = iType;
                    powerlabel.iRate = iRate;
                    powerlabel.vRate = vRate;
                }
            }
            AddMsg("加载参数成功...");
        }

        /// <summary>
        /// 显示参数
        /// </summary>
        private void ShowLoadPara()
        {
            for (int j = 0; j < scanCount + 1; j++)//扫码枪
            {
                txtScanIP[j].Text = strScanIP[j];
                numScanPort[j].Value = iScanPort[j];
                cboScanEnable[j].SelectedIndex = iScanEnable[j];
                ScancboBeatType[j].SelectedIndex = iScanBeatType[j];
                btnScan[j].Visible = iScanEnable[j] == 0;
                if (j == scanCount - 1)
                {
                    cboBrand.SelectedIndex = powerlabel.type;
                    numPowerCount.Value = iPowerCount;
                    numIRate.Value = (decimal)iRate;
                    numVRate.Value = (decimal)vRate;
                }
            }
            button1.Enabled = false;
            for (int i = 0; i < scanCount + 1; i++) //扫码枪
            {
                gbScan[i].Enabled = false;
            }
        }

        #endregion

        #region  初始化功能
        /// <summary>
        /// 初始化
        /// </summary>
        private void InitGroup()
        {
            panel1.Location = new Point(340, 780);
            int yoffset = 30;
            tcpHe = new TCPClient();
            modbusRtu = new TcpClientModbusRtu();
            for (int i = 0; i < scanCount + 1; i++)
            {
                tcpScan[i] = new TcpClientString();
                RegisterScanEvent(i);
                gbScan[i] = new GroupBox();
                if (i == scanCount)
                {
                    gbScan[i].Text = "PLC";
                }
                else if (i == scanCount - 1)
                {
                    gbScan[i].Text = "电能表";
                }
                else
                {
                    if (i >= 0 && i < 2)
                        gbScan[i].Text = $"壳{(i + 1)}扫码枪";

                    else if (i >= 2 && i < 4)
                        gbScan[i].Text = $"电芯{(i - 1)}扫码枪";
                    else if (i == 4)
                        gbScan[i].Text = "盖扫码枪";
                    else
                        gbScan[i].Text = $"下料{(i - 4)}扫码枪";
                }


                gbScan[i].Width = 300;
                gbScan[i].Height = 160;
                gbScan[i].Location = new Point(10 + ((gbScan[i].Width + 50) * (i % 2)), 12 + ((gbScan[i].Height + 10) * (i / 2)));
                if (i == scanCount - 1)
                    gbScan[i].Height = 240;
                gbScan[i].Parent = tabSetting;

                Label label = new Label();
                label.Text = "状态:";
                label.Width = 50;
                label.Location = new Point(10, 26);
                label.Parent = gbScan[i];

                cboScanEnable[i] = new ComboBox();
                cboScanEnable[i].DropDownStyle = ComboBoxStyle.DropDownList;
                cboScanEnable[i].Items.Add("使用");
                cboScanEnable[i].Items.Add("禁用");
                cboScanEnable[i].Width = 80;
                cboScanEnable[i].Location = new Point(60, 24);
                cboScanEnable[i].Parent = gbScan[i];


                label = new Label();
                label.Text = "IP:";
                label.Width = 50;
                label.Location = new Point(10, 30 + yoffset);
                label.Parent = gbScan[i];

                txtScanIP[i] = new TextBox();
                txtScanIP[i].Width = 140;
                txtScanIP[i].Location = new Point(60, 28 + yoffset);
                txtScanIP[i].Parent = gbScan[i];

                label = new Label();
                label.Text = "Port:";
                label.Width = 50;
                label.Location = new Point(10, 68 + yoffset);
                label.Parent = gbScan[i];

                numScanPort[i] = new NumericUpDown();
                numScanPort[i].Maximum = ushort.MaxValue;
                numScanPort[i].Width = 80;
                numScanPort[i].Location = new Point(60, 64 + yoffset);
                numScanPort[i].Parent = gbScan[i];

                btnScan[i] = new Button();
                btnScan[i].Width = 60;
                btnScan[i].Height = 40;
                btnScan[i].Text = "连接";
                btnScan[i].Tag = i;
                btnScan[i].Location = new Point(220, 40 + yoffset);
                btnScan[i].Parent = gbScan[i];
                btnScan[i].Click += new EventHandler(btnScan_Click);

                label = new Label();
                label.Text = "Beat:";
                label.Width = 50;
                label.Location = new Point(10, 98 + yoffset);
                if (i < scanCount)
                    label.Parent = gbScan[i];
                ScancboBeatType[i] = new ComboBox();
                ScancboBeatType[i].DropDownStyle = ComboBoxStyle.DropDownList;
                ScancboBeatType[i].Width = 80;
                ScancboBeatType[i].Items.Add("Bool");
                ScancboBeatType[i].Items.Add("UInt");
                ScancboBeatType[i].Location = new Point(60, 96 + yoffset);
                if (i < scanCount)
                    ScancboBeatType[i].Parent = gbScan[i];
                if (i == scanCount - 1)
                {
                    label = new Label();
                    label.Text = "品牌:";
                    label.Width = 50;
                    label.Location = new Point(10, 140 + yoffset);
                    label.Parent = gbScan[i];


                    cboBrand = new ComboBox();
                    cboBrand.DropDownStyle = ComboBoxStyle.DropDownList;
                    cboBrand.Items.AddRange(new string[] { "安科瑞", "正泰" });
                    cboBrand.Width = 80;
                    cboBrand.Location = new Point(60, 136 + yoffset);
                    cboBrand.Parent = gbScan[i];

                    label = new Label();
                    label.Text = "数量:";
                    label.Width = 50;
                    label.Location = new Point(150, 140 + yoffset);
                    label.Parent = gbScan[i];

                    numPowerCount.Minimum = 1;
                    numPowerCount.Maximum = 5;
                    numPowerCount.Width = 70;
                    numPowerCount.Location = new Point(200, 136 + yoffset);
                    numPowerCount.Parent = gbScan[i];

                    label = new Label();
                    label.Text = "电压比:";
                    label.Width = 70;
                    label.Location = new Point(10, 180 + yoffset);
                    label.Parent = gbScan[i];

                    numVRate.Minimum = 0;
                    numVRate.Maximum = decimal.MaxValue;
                    numVRate.DecimalPlaces = 2;
                    numVRate.Width = 60;
                    numVRate.Location = new Point(80, 178 + yoffset);
                    numVRate.Parent = gbScan[i];

                    label = new Label();
                    label.Text = "电流比:";
                    label.Width = 70;
                    label.Location = new Point(150, 180 + yoffset);
                    label.Parent = gbScan[i];

                    numIRate.Minimum = 0;
                    numIRate.Maximum = decimal.MaxValue;
                    numIRate.DecimalPlaces = 2;
                    numIRate.Width = 60;
                    numIRate.Location = new Point(220, 178 + yoffset);
                    numIRate.Parent = gbScan[i];
                }
            }
        }
        #endregion

        #region 日志操作

        private void rtbLog_DoubleClick(object sender, EventArgs e)
        {
            rtbLog.Clear();
        }
        /// <summary> 委托
        /// </summary>
        /// <param name="msg"></param>
        private delegate void AddMsgDelegate(string msg);

        /// <summary> 消息列表
        /// </summary>
        /// <param name="msg"></param>
        public void AddMsg(string msg)
        {

            if (rtbLog.InvokeRequired)
            {
                AddMsgDelegate dlgMessage = new AddMsgDelegate(AddMsg);
                this.Invoke(dlgMessage, new object[] { msg });
            }
            else
            {
                if (msg == null)
                {
                    rtbLog.SelectionColor = Color.Red;
                    rtbLog.AppendText("\r\n" + DateTime.Now.ToString("HH:mm:ss.fff") + " --> " + "操作对象为null");
                    rtbLog.ScrollToCaret();
                    return;
                }
                //listBox_log.MeasureItem;
                //  WriteLogs(DateTime.Now.ToString("HH:mm:ss.fff") + " --> " + msg);
                if (msg.Contains("失败") || msg.Contains("出错") || msg.Contains("不存在"))
                {
                    rtbLog.SelectionColor = Color.Red;
                }
                else if (msg.Contains("成功"))
                {
                    rtbLog.SelectionColor = Color.Blue;
                }
                else if (msg.Contains("已存在"))
                {
                    rtbLog.SelectionColor = Color.Green;
                }
                else
                {
                    rtbLog.SelectionColor = Color.Black;
                }
                while (msg.Length > 150)
                {
                    rtbLog.AppendText("\r\n" + DateTime.Now.ToString("HH:mm:ss.fff") + " --> " + msg.Substring(0, 150));
                    WriteLogs(DateTime.Now.ToString("HH:mm:ss.fff") + " --> " + msg.Substring(0, 150));
                    msg = msg.Substring(150);
                }
                if (msg.Trim().Length <= 150 && msg.Trim().Length > 0)
                {
                    rtbLog.AppendText("\r\n" + DateTime.Now.ToString("HH:mm:ss.fff") + " --> " + msg);
                    WriteLogs(DateTime.Now.ToString("HH:mm:ss.fff") + " --> " + msg);
                }
                if (rtbLog.TextLength > 30000)
                { rtbLog.Clear(); }
                rtbLog.ScrollToCaret();
            }
        }

        /// <summary>
        /// 保存日志
        /// </summary>
        /// <param name="msg">日志信息</param>
        private void WriteLogs(string msg)
        {
            try
            {
                if (!Directory.Exists(_LogPath))
                {
                    Directory.CreateDirectory(_LogPath);
                }
                string newPath = _LogPath + "\\" + System.DateTime.Now.ToString("yyyy-MM-dd ") + "log.txt";
                if (!File.Exists(newPath))
                {
                    StreamWriter sw = new StreamWriter(newPath);
                    sw.WriteLine(msg);
                    sw.Flush();
                    sw.Close();
                }
                else
                {
                    StreamWriter sw = new StreamWriter(newPath, true);
                    sw.WriteLine(msg);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception ex)//wu
            {
                //MessageBox.Show("写日志异常:[" + ex.Message + "]");
                AddMsg("\r\nStackTrace:" + ex.StackTrace.ToString());
                AddMsg("\r\nTargetSite:" + ex.TargetSite.ToString());
                AddMsg("写日志异常出错！[" + ex.Message + "]");
            }
        }
        #endregion

        #region 密码输入
        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            if (txtPassword.Text == "555")
            {
                loginFlag = 0;
                loginTime = DateTime.Now;
                btnSave.Enabled = true;
                for (int i = 0; i < scanCount + 1; i++)
                {
                    gbScan[i].Enabled = true;
                }
                tabLog.Parent = tabcontrol1;
            }
            else
            {
                btnSave.Enabled = false;
                for (int i = 0; i < scanCount + 1; i++)
                {
                    gbScan[i].Enabled = false;
                }
                tabLog.Parent = tabcontrol1;
            }
        }
        #endregion

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text == "555")
            {
                loginFlag = 0;
                loginTime = DateTime.Now;
                btnSave.Enabled = true;
                button1.Enabled = true;
                for (int i = 0; i < scanCount + 1; i++)
                {
                    gbScan[i].Enabled = true;
                }
                tabLog.Parent = tabcontrol1;
            }
            else
            {
                btnSave.Enabled = false;
                button1.Enabled = false;
                for (int i = 0; i < scanCount + 1; i++)
                {
                    gbScan[i].Enabled = false;
                }
                tabLog.Parent = tabcontrol1;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool bRestart = false;
            for (int i = 0; i < scanCount + 1; i++)
            {
                strScanIP[i] = txtScanIP[i].Text.Trim();
                iScanPort[i] = Convert.ToUInt16(numScanPort[i].Value);
                iScanBeatType[i] = ScancboBeatType[i].SelectedIndex;
                if (iScanEnable[i] != cboScanEnable[i].SelectedIndex)
                {
                    bRestart = true;
                    iniFile.IniWriteValueInt("Scan", "Enable" + i, cboScanEnable[i].SelectedIndex);
                }
                iniFile.IniWriteValueString("Scan", "IP" + i, strScanIP[i]);
                iniFile.IniWriteValueInt("Scan", "Port" + i, iScanPort[i]);
                iniFile.IniWriteValueInt("Scan", "BeatTyte" + i, iScanBeatType[i]);
                if (i == scanCount - 1)
                {
                    iniFile.IniWriteValueInt("Power", "Count", Convert.ToInt32(numPowerCount.Value));
                    iniFile.IniWriteValueDouble("Power", "VRate", Convert.ToDouble(numVRate.Value));
                    iniFile.IniWriteValueDouble("Power", "IRate", Convert.ToDouble(numIRate.Value));
                    iniFile.IniWriteValueInt("System", "Type", cboBrand.SelectedIndex);
                }
            }
            AddMsg($"参数保存成功... {(bRestart ? "请重启软件！" : "")}");
            MessageBox.Show($"参数保存成功... {(bRestart ? "请重启软件！" : "")}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (bRestart)
            {
                bRun = false;
                Thread.Sleep(200);
                for (int i = 0; i < scanCount + 1; i++)
                {
                    tcpScan[i].Close();
                    if (i == scanCount - 1)
                        modbusRtu.Close();
                    if (i == scanCount)
                        tcpHe.Close();
                }
                AddMsg("关闭软件...");
                timerUI.Stop();
                Thread.Sleep(200);
                Environment.Exit(0);
            }
        }
    }
}
