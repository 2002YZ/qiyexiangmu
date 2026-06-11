using EasyModbus;
using Esb;
using HalconDotNet;
using Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static PanelHandingSystem.Com2Mes;
using static PanelHandingSystem.ParaSet;
using static PanelHandingSystem.WorkFlowControl;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
namespace PanelHandingSystem
{
    public partial class Form1 : Form
    {


        public static Form1 frm1;

        FormForceVisualization formForceVis = new FormForceVisualization();




        #region

        //手持扫码枪相关
        public static ScannerHelper _scannerHelper;
        private static List<string> _scanHistory = new List<string>();


        /// <summary>
        /// 初始化扫码枪助手
        /// </summary>
        private void InitializeScanner()
        {
            // 创建扫码枪助手实例
            _scannerHelper = new ScannerHelper(this, textBox_HandScaner);

            // 可以自定义配置
            _scannerHelper.MaxKeyInterval = 30;  // 按键间隔30ms以内判定为扫码枪
            _scannerHelper.ScanCompleteDelay = 100;  // 停止输入后100ms判定为扫描完成
            _scannerHelper.MinCharCount = 3;  // 最少3个字符
            _scannerHelper.AutoClearTextBox = true;  // 扫码前自动清空

            // 绑定扫码完成事件
            _scannerHelper.OnScanCompleted += OnScanCompleted;

            //// 绑定扫码进行中事件（可选）
            //_scannerHelper.OnScanning += OnScanning;
        }

        /// <summary>
        /// 初始化扫描历史列表
        /// </summary>
     

        /// <summary>
        /// 扫码完成事件处理
        /// </summary>
        /// <param name="scannedCode">扫描到的条码内容</param>
        private void OnScanCompleted(string scannedCode)
        {
            // 在UI线程上更新
            this.InvokeIfNeeded(() =>
            {
                LogRecord.addLog($"手持扫码枪扫码:{scannedCode}");

                //TODO :扫码结果判定是否为需要的码，如果是,进一步向MES获取工单,使用线程或任务
                Task.Run(() => WorkFlowControl.RunHandheldScanProcess(scannedCode));

            });
        }

  

        #endregion













        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            frm1 = this;
        }


        




        private static object _object = new object();
        public void AddLog(string SystemStatus)
        {
            // 尝试在UI线程上更新日志
            try
            {
                // 使用锁来同步更新日志，确保线程安全
                lock (_object)
                {
                    // 使用Invoke方法在UI线程上更新日志
                    richTextBox_LogRecord.Invoke((MethodInvoker)delegate
                    {
                        UpdateLabel(SystemStatus);
                    });
                }
            }
            catch (Exception ex)
            {
                // 捕获并处理可能出现的异常
            }
        }


        public void UpdateBarcodeUI(string code)
        {

            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateBarcodeUI), code);
                return;
            }


            textBox8.Text = code;

            AddLog($"[UI] 收到手持扫码: {code}");


            button_getorderbyCode_Click(null, null);
        }



        private void UpdateLabel(string SystemStatus)
        {
            // 添加换行符到日志信息末尾


            string SystemStatusDis = SystemStatus + "\r\n";

            // 在日志文本框中追加新的日志信息
            richTextBox_LogRecord.AppendText(SystemStatusDis);

            // 将光标移动到日志文本框的末尾
            richTextBox_LogRecord.SelectionStart = richTextBox_LogRecord.Text.Length;

            // 滚动日志文本框，使光标所在位置在可视范围内
            richTextBox_LogRecord.ScrollToCaret();
        }



        //机器人日志框刷新
        private static object _object1 = new object();
        public void AddLog1(string SystemStatus)
        {
            // 尝试在UI线程上更新日志
            try
            {
                // 使用锁来同步更新日志，确保线程安全
                lock (_object1)
                {
                    // 使用Invoke方法在UI线程上更新日志
                    richTextBox_LogCom2Robot.Invoke((MethodInvoker)delegate
                    {
                        UpdateLabel1(SystemStatus);
                    });
                }
            }
            catch (Exception ex)
            {
                // 捕获并处理可能出现的异常
            }
        }

        private void UpdateLabel1(string SystemStatus)
        {
            // 添加换行符到日志信息末尾


            string SystemStatusDis = SystemStatus + "\r\n";

            // 在日志文本框中追加新的日志信息
            richTextBox_LogCom2Robot.AppendText(SystemStatusDis);

            // 将光标移动到日志文本框的末尾
            richTextBox_LogCom2Robot.SelectionStart = richTextBox_LogCom2Robot.Text.Length;

            // 滚动日志文本框，使光标所在位置在可视范围内
            richTextBox_LogCom2Robot.ScrollToCaret();
        }

        //////////////////////////////////////////////////////////////////////




        //视觉日志框刷新
        private static object _object2 = new object();
        public void AddLog2(string SystemStatus)
        {
            // 尝试在UI线程上更新日志
            try
            {
                // 使用锁来同步更新日志，确保线程安全
                lock (_object1)
                {
                    // 使用Invoke方法在UI线程上更新日志
                    richTextBox_LogCom2Camera.Invoke((MethodInvoker)delegate
                    {
                        UpdateLabel2(SystemStatus);
                    });
                }
            }
            catch (Exception ex)
            {
                // 捕获并处理可能出现的异常
            }
        }

        private void UpdateLabel2(string SystemStatus)
        {
            // 添加换行符到日志信息末尾


            string SystemStatusDis = SystemStatus + "\r\n";

            // 在日志文本框中追加新的日志信息
            richTextBox_LogCom2Camera.AppendText(SystemStatusDis);

            // 将光标移动到日志文本框的末尾
            richTextBox_LogCom2Camera.SelectionStart = richTextBox_LogCom2Camera.Text.Length;

            // 滚动日志文本框，使光标所在位置在可视范围内
            richTextBox_LogCom2Camera.ScrollToCaret();
        }

        //////////////////////////////////////////////////////////////////////



        private void button_ConnectPLC_Click(object sender, EventArgs e)
        {
            WorkFlowControl.ConnectPLC();
        }

        //程序加载
        private void Form1_Load(object sender, EventArgs e)
        {


            LogRecord.addLog("载入系统参数");
            ParaSet.InitParaSet();
            ParaSet.PrintParaSet();

            //窗口设置
            HOperatorSet.SetSystem("clip_region", "false");
            HOperatorSet.SetWindowParam(hSmartWindowControl1.HalconWindow, "graphics_stack_max_element_num", new HTuple(500));

            HOperatorSet.SetColor(hSmartWindowControl1.HalconWindow, "red");
            HOperatorSet.SetDraw(hSmartWindowControl1.HalconWindow, "margin");
            HOperatorSet.SetLineWidth(hSmartWindowControl1.HalconWindow, 2);
            HOperatorSet.QueryFont(hSmartWindowControl1.HalconWindow, out HTuple font);
            HOperatorSet.SetFont(hSmartWindowControl1.HalconWindow, font[2] + "-15");
            HOperatorSet.SetSystem("temporary_mem_cache", "false");

            HOperatorSet.SetPart(hSmartWindowControl1.HalconWindow, 0, 0, ParaSet.recipe.TotalHeight, ParaSet.recipe.TotalWidth);

            TypeSetAlgorithm.hv_ExpDefaultWinHandle = hSmartWindowControl1.HalconWindow;





            //排版固定项参数
            label_TotalHeight.Text = ParaSet.recipe.TotalHeight.ToString();
            label_FullThres.Text = ParaSet.recipe.FullThres.ToString();
            label_Interval.Text = ParaSet.recipe.PanelInterval.ToString();

            new Thread(WorkFlowInitFunc).Start();



            // 1. 绑定 WorkFlowControl 的事件，用于更新界面
            // WorkFlowControl.OnHandheldScanSuccess = UpdateBarcodeUI;

            // 初始化扫码枪连接 (确保 ParaSet 里配置了 127.0.0.1 和 转发工具的端口)
            //  WorkFlowControl.ConnectBarCodeScaner(1);

        }

        private void WorkFlowInitFunc()
        {

            Thread.Sleep(2000);
            //窗口传入
            WorkFlowControl.WindowHandle = hSmartWindowControl1.HalconWindow;
            WorkFlowControl.Init();
            
            //WorkFlowControl.OnHandheldScanSuccess = UpdateBarcodeUI;
           // WorkFlowControl.ConnectBarCodeScaner(1);
            Com2Mes.initMes();
            WorkFlowControl.StartMesDataReadingThread();
            // 初始化扫码枪助手
            InitializeScanner();

        }



        //连接扫码枪1

        private void button_ConnectBarCode1_Click(object sender, EventArgs e)
        {


            Task task = Task.Run(() =>
            {
                WorkFlowControl.ConnectBarCodeScaner(1);
            });

        }
        private void button_ConnectBarCode2_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {
                WorkFlowControl.ConnectBarCodeScaner(2);
            });

        }

        private void button_StartBarCodeScan1_Click(object sender, EventArgs e)
        {


            Task task = Task.Run(() =>
            {
                string BarCode = WorkFlowControl.StartBarcode(1);
                if (BarCode != "NoRead" || BarCode != "")
                {
                    LogRecord.addLog("扫码结果:" + BarCode);
                }
                else
                {
                    LogRecord.addLog("扫码失败:" + BarCode);
                }
            });



        }

        private void button_StartBarCodeScan2_Click(object sender, EventArgs e)
        {


            Task task = Task.Run(() =>
            {
               
                string BarCode = WorkFlowControl.StartBarcode(2);
                if (BarCode != "NoRead" || BarCode != "")
                {
                    LogRecord.addLog("扫码结果:" + BarCode);
                }
                else
                {
                    LogRecord.addLog("扫码失败:" + BarCode);
                }

            });



        }



        private void button_ReadCoilTest_Click(object sender, EventArgs e)
        {
            try
            {
                Task task = Task.Run(() =>
                {
                    //读coil测试
                    int startAddr = int.Parse(textBox_ReadCoilNum.Text);
                    bool[] res = WorkFlowControl.com2PLC.readCoils(startAddr, 1);

                    LogRecord.addLog($"{startAddr}读取结果{res[0]}");
                });


            }
            catch (Exception ex)
            {
                LogRecord.addLog($"读取PLC寄存器出错" + ex.Message);


            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Task task = Task.Run(() =>
                {
                    //写coil测试
                    int startAddr = int.Parse(textBox5.Text);
                    string s = comboBox1.Text;
                    bool flag = ("true" == s);
                    WorkFlowControl.com2PLC.writeCoil(startAddr, flag);
                    LogRecord.addLog($"{startAddr}写入{flag}");

                });


            }
            catch (Exception ex)
            {
                LogRecord.addLog($"写入PLC寄存器出错" + ex.Message);


            }

        }

        private void button_writeData_Click(object sender, EventArgs e)
        {
            //写寄存器，字符串
            try
            {
                Task task = Task.Run(() =>
                {

                    //写数据寄存器
                    int startAddr = int.Parse(textBox2.Text);
                    string str = textBox1.Text;

                    WorkFlowControl.com2PLC.writeStringD(startAddr, str);
                });



            }
            catch (Exception ex)
            {
                LogRecord.addLog($"写入PLC寄存器出错" + ex.Message);


            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Task task = Task.Run(() =>
                {
                    //写数据寄存器
                    int startAddr = int.Parse(textBox3.Text);
                    double data = double.Parse(textBox4.Text);

                    WorkFlowControl.com2PLC.writeDataD(startAddr, data);

                });

            }
            catch (Exception ex)
            {
                LogRecord.addLog($"写入PLC寄存器出错" + ex.Message);


            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
            //连接VisionMaster
            //Com2VisionMaster.Connect();

            Task.Run(() => {
                Com2VisionMaster.Connect();
            });
        }

        private void button6_Click(object sender, EventArgs e)
        {
           /* string test = textBox6.Text;
            Com2VisionMaster.Send2VisionMaster(test);
*/

            string test = textBox6.Text;

            if (test == "1" || test == "2")
            {
                int camNo = int.Parse(test);

                Task.Run(() =>
                {
                    double x, y, r;
                    bool success = Com2VisionMaster.StartCapture(out x, out y, out r, camNo);

                    this.Invoke((MethodInvoker)delegate
                    {
                        if (success)
                        {
                            string result = $"相机{camNo}拍照成功! 坐标: X={x}, Y={y}, R={r}";
                            LogRecord.addLog(result);

                          
                            label_currentStatues.Text = $"最新坐标: X:{x}, Y:{y}, R:{r}";
                        }
                        else
                        {
                            LogRecord.addLog($"相机{camNo}拍照失败");
                        }
                    });
                });
            }
            else
            {
                Com2VisionMaster.Send2VisionMaster(test);
            }


        }

        private void button4_Click(object sender, EventArgs e)
        {
            Com2Vision.Connect();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string str = textBox7.Text;
            Com2Vision.Send(str);
        }

        private void button_ConnectRobot1_Click(object sender, EventArgs e)
        {
           panelRobot1.BackColor = Color.Red;
            //连接1号机器人
            WorkFlowControl.ConnectRobot(1);
            panelRobot1.BackColor = Color.Green;
        }

        private void button_ConnectRobot2_Click(object sender, EventArgs e)
        {
            panelRobot2.BackColor = Color.Red;
            //连接2号机器人
            WorkFlowControl.ConnectRobot(2);
            panelRobot2.BackColor = Color.Green;
        }

        private void button7_Click(object sender, EventArgs e)
        {

       

        }

        private void button_SendPos2Robot1_Click(object sender, EventArgs e)
        {

            Task task = Task.Run(() =>
            {
                try
                {
                    //读机器人coil
                    int RobotID = int.Parse(comboBox_RobotID.Text);
                    double x1 = double.Parse(textBox_Robot1PosX.Text);
                    double y1 = double.Parse(textBox_Robot1PosY.Text);
                    int row = int.Parse(textBox_Robot1Row.Text);

                    double w = double.Parse(textBox_width.Text);
                    double h = double.Parse(textBox_height.Text);
                    double t = double.Parse(textBox_thickness.Text);


                    if (RobotID == 1)
                    {
                 
                        WorkFlowControl.robot1.SendXiaLiaoPos(x1, y1, row,w,h,t);

                    }
                    else
                    {      
                        WorkFlowControl.robot2.SendXiaLiaoPos(x1, y1, row,w, h, t);
                    }

                }
                catch (Exception ex)
                {
                    LogRecord.addLog("读写出错" + ex.Message);

                }

            });

         
      


        }

 

     
        private void button_OpenParaSetFile_Click(object sender, EventArgs e)
        {

            string filename = ParaSet.ParamPath + "ParaSet.json";
            LogRecord.addLog("打开当前参数文件:" + filename);
            try
            {
                //用记事本打开
                System.Diagnostics.Process.Start("JSONedit.exe", filename);
            }
            catch (Exception ex)
            {
                LogRecord.addLog("打开当前参数文件失败:" + ex.Message);
            }


        }

        private void button_ReloadSet_Click(object sender, EventArgs e)
        {
            LogRecord.addLog("重新载入参数");
            ParaSet.InitParaSet();
            ParaSet.PrintParaSet();

           
        }

        private void button_ReSet_Click(object sender, EventArgs e)
        {
            string RecipefilePath = ParaSet.ParamPath;
            string Recipefilename = ParaSet.ParamPath + "ParaSet.json";
            LogRecord.addLog("参数名路径:" + RecipefilePath);
            try
            {


                LogRecord.addLog("创建参数文件夹,按默认参数:" + Recipefilename);
                Directory.CreateDirectory(RecipefilePath);

                ParaSet.recipe = new ParaSet.Recipe();
                ParaSet.Savejson(recipe);

            }
            catch (Exception ex)
            {
                LogRecord.addLog("参数创建出错" + ex.Message);
            }



        }

        private void button_RestartSystem_Click(object sender, EventArgs e)
        {
            LogRecord.addLog("手动点击电脑重启");
            new Thread(Com2PLC.RestartSystemFunc).Start();
        }

        private void button_OpenCSV_Click(object sender, EventArgs e)
        {



        }

        private void button11_Click(object sender, EventArgs e)
        {
            LogRecord.addLog("重新载入参数");
            ParaSet.InitParaSet();
            ParaSet.PrintParaSet();

            //排版固定项参数
            label_TotalWidth.Text = ParaSet.recipe.TotalWidth.ToString();
            label_TotalHeight.Text = ParaSet.recipe.TotalHeight.ToString();
  
            label_FullThres.Text = ParaSet.recipe.FullThres.ToString();            
            label_Interval.Text = ParaSet.recipe.PanelInterval.ToString();  

        }

        private void button12_Click(object sender, EventArgs e)
        {
            string filename = ParaSet.ParamPath + "ParaSet.json";
            LogRecord.addLog("打开当前参数文件:" + filename);
            try
            {
                //用记事本打开
                System.Diagnostics.Process.Start("JSONedit.exe", filename);
            }
            catch (Exception ex)
            {
                LogRecord.addLog("打开当前参数文件失败:" + ex.Message);
            }

        }

       

        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                float PanelWidth = float.Parse(textBox_PanelWidth.Text);
                float PanelHeight = float.Parse(textBox_PanelHeight.Text);

                float PeiDuWidth = float.Parse(textBox_PeiDuWidth1.Text);

                float PanelThickness = float.Parse(textBox_PanelThickness.Text);


                HOperatorSet.SetPart(hSmartWindowControl1.HalconWindow, 0, 0, ParaSet.recipe.TotalHeight, ParaSet.recipe.TotalWidth);
             
                int Res = WorkFlowControl.CalPaiBan(PanelWidth, PanelHeight, PanelThickness, out double PeiDuWidth_out , out double PeiDuH, out double PeiDuThicnness, out WorkFlowControl.BaseAdj, out WorkFlowControl.PosXQueue, out WorkFlowControl.PosYQueue,
                                                                             out WorkFlowControl.PosPeiDuXQueue, out WorkFlowControl.PosPeiDuYQueue);



                label_PosX.Text = WorkFlowControl.ToStringLinq(WorkFlowControl.PosXQueue);
                label_PosY.Text = WorkFlowControl.ToStringLinq(WorkFlowControl.PosYQueue);

                label_PeiDuX.Text = WorkFlowControl.ToStringLinq(WorkFlowControl.PosPeiDuXQueue);
                label_PeiDuY.Text = WorkFlowControl.ToStringLinq(WorkFlowControl.PosPeiDuYQueue);
                label_BaseAdj.Text = WorkFlowControl.BaseAdj.ToString();
                textBox_PeiDuWidth1.Text = PeiDuWidth_out.ToString();



                if (Res < 0)
                    LogRecord.addLog("排版算法运行失败");


            }
            catch (Exception ex)
            {
                LogRecord.addLog("排版算法运行出错"+ ex.Message);
            }




        }

        private void hSmartWindowControl1_Load(object sender, EventArgs e)
        {
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.MouseWheelImageZoomRun1);
        }


        private void MouseWheelImageZoomRun1(object sender, MouseEventArgs e)
        {

            System.Drawing.Point pt = this.Location;
            int leftBorder = hSmartWindowControl1.Location.X;
            int rightBorder = hSmartWindowControl1.Location.X + hSmartWindowControl1.Size.Width;
            int topBorder = hSmartWindowControl1.Location.Y;
            int bottomBorder = hSmartWindowControl1.Location.Y + hSmartWindowControl1.Size.Height;
            if (e.X > leftBorder && e.X < rightBorder && e.Y > topBorder && e.Y < bottomBorder)
            {
                MouseEventArgs newe = new MouseEventArgs(e.Button, e.Clicks,
                                                     e.X - pt.X, e.Y - pt.Y, e.Delta);
                hSmartWindowControl1.HSmartWindowControl_MouseWheel(sender, newe);

            }
        }

        private void button_ReadRobotCoil_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {
                try
                {
                    //读机器人coil
                    int RobotID = int.Parse(comboBox_RobotID.Text);
                    if (RobotID == 1)
                    {
                        int startAddr = int.Parse(textBox_ReadRobotCoilAddr.Text);
                        bool[] res = WorkFlowControl.robot1.readCoils(startAddr, 8);
                        LogRecord.addLog($"{startAddr}读取结果{res[0]}");
                    }
                    else
                    {
                        int startAddr = int.Parse(textBox_ReadRobotCoilAddr.Text);
                        bool[] res = WorkFlowControl.robot2.readCoils(startAddr, 8);
                        LogRecord.addLog($"{startAddr}读取结果{res[0]}");
                    }

                }
                catch (Exception ex)
                {
                    LogRecord.addLog("读写出错" + ex.Message);

                }

            });

        }

        private void button_WriteRobotCoil_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {
                try
                {
                    //读机器人coil
                    int RobotID = int.Parse(comboBox_RobotID.Text);
                    if (RobotID == 1)
                    {
                        int startAddr = int.Parse(textBox_WriteRobotCoilAddr.Text);
                        string s = comboBox_WriteCoilValue.Text;
                        bool flag = ("true" == s);
                        WorkFlowControl.robot1.writeCoil(startAddr, flag);
                        LogRecord.addLog($"{startAddr}写入{flag}");
                    }
                    else
                    {
                        int startAddr = int.Parse(textBox_WriteRobotCoilAddr.Text);
                        string s = comboBox_WriteCoilValue.Text;
                        bool flag = ("true" == s);
                        WorkFlowControl.robot2.writeCoil(startAddr, flag);
                        LogRecord.addLog($"{startAddr}写入{flag}");

                    }

                }
                catch (Exception ex)
                {
                    LogRecord.addLog("读写出错" + ex.Message);

                }

            });
        }

        //写上料坐标
        private void button7_Click_1(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {
                try
                {
                    //读机器人coil
                    int RobotID = int.Parse(comboBox_RobotID.Text);
                    double x1 = double.Parse(textBox_Robot1PosX.Text);
                    double y1 = double.Parse(textBox_Robot1PosY.Text);
                    int row = int.Parse(textBox_Robot1Row.Text);


                    double w = double.Parse(textBox_width.Text);
                    double h = double.Parse(textBox_height.Text);
                    double t = double.Parse(textBox_thickness.Text);


                    if (RobotID == 1)
                    {

                        WorkFlowControl.robot1.SendShangLiaoPos(x1, y1, row,w,h,t);

                    }
                    else
                    {
                        WorkFlowControl.robot2.SendShangLiaoPos(x1, y1, row, w, h, t);
                    }

                }
                catch (Exception ex)
                {
                    LogRecord.addLog("读写出错" + ex.Message);

                }
            });
        }

        private void button_WriteChuanSongDaiPos_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {
                try
                {
                    
                    int RobotID = int.Parse(comboBox_RobotID.Text);
                    double x1 = double.Parse(textBox_PosXE.Text);
                    double y1 = double.Parse(textBox_PosXE.Text);
                    double r1 = double.Parse(textBox_PosRE.Text);

                    double w = double.Parse(textBox_width.Text);
                    double h = double.Parse(textBox_height.Text);
                    double t = double.Parse(textBox_thickness.Text);



                    int RowNo = int.Parse(comboBox_RowNo.Text);
                    double mainPanelWidth = double.Parse(textBox_PanelWidthT.Text);
                    double mainPanelHeight = double.Parse(textBox_PanelHeightT.Text);

                    int PeiDuNo = 0;//测试时默认0

                    if (RobotID == 1)
                    {
                      
                          WorkFlowControl.robot1.SendPeiDuPos(x1, y1, RowNo, w, h, t,mainPanelWidth,mainPanelHeight,1, PeiDuNo);                     

                    }
                    else
                    {
                        WorkFlowControl.robot2.SendPeiDuPos(x1, y1, RowNo, w, h, t,mainPanelWidth,mainPanelHeight,1,PeiDuNo);
                       
                    }

                }
                catch (Exception ex)
                {
                    LogRecord.addLog("读写出错" + ex.Message);

                }
            });



        }




        //写陪镀板坐标
        private void button_WritePeiduPos_Click(object sender, EventArgs e)
        {

            Task task = Task.Run(() =>
            {
                try
                {
                    //读机器人coil
                    int RobotID = int.Parse(comboBox_RobotID.Text);
                    double x1 = double.Parse(textBox_PosXE.Text);
                    double y1 = double.Parse(textBox_PosXE.Text);


                    double w = double.Parse(textBox_Peiduwidth.Text);
                    double h = double.Parse(textBox_Peiduheight.Text);
                    double t = double.Parse(textBox_Peiduthickness.Text);


                    double MainPanelWidth = double.Parse(textBox_PanelWidthT.Text);
                    double MainPanelHeight = double.Parse(textBox_PanelHeightT.Text);
                    int RowNo = int.Parse(comboBox_RowNo.Text);


                    int PeiDuNo = 0;//测试时默认0
                    if (RobotID == 1)
                    {

                        WorkFlowControl.robot1.SendPeiDuPos(x1, y1,1,w,h,t,MainPanelWidth,MainPanelHeight,1, PeiDuNo);

                    }
                    else
                    {
                        WorkFlowControl.robot2.SendPeiDuPos(x1, y1,2, w, h, t, MainPanelWidth, MainPanelHeight, 1,PeiDuNo);
                    }

                }
                catch (Exception ex)
                {
                    LogRecord.addLog("读写出错" + ex.Message);

                }
            });



        }

        private void button8_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {


                try
                {
                  
                    double w = double.Parse(textBox_PanelWidthT.Text);
                    double h = double.Parse(textBox_PanelHeightT.Text);
                    double t = double.Parse(textBox_PanelThicknessT.Text);
                    double weight = double.Parse(textBox_PanelWeight.Text);

                    WorkFlowControl.com2PLC.SendPanelSizeOnChuanSongDai1(w, h,t,weight);

                   
                    
                }
                catch (Exception ex)
                {
                    LogRecord.addLog("读写出错" + ex.Message);

                }





            });
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {


                try
                {

                    double w = double.Parse(textBox_PanelWidthT.Text);
                    double h = double.Parse(textBox_PanelHeightT.Text);
                    double t = double.Parse(textBox_PanelThicknessT.Text);
                    double weight = double.Parse(textBox_PanelWeight.Text);


                    WorkFlowControl.com2PLC.SendPanelSizeOnFeiBa(w, h,t,weight);


                }
                catch (Exception ex)
                {
                    LogRecord.addLog("读写出错" + ex.Message);

                }

            });
        }

        private void button13_Click(object sender, EventArgs e)
        {

            WorkFlowControl.ConnectBarCodeScaner(3);
        }

        private void button14_Click(object sender, EventArgs e)
        {

            WorkFlowControl.ConnectBarCodeScaner(4);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            string BarCode = WorkFlowControl.StartBarcode(3);
            if (BarCode != "NoRead" || BarCode != "")
            {
                LogRecord.addLog("扫码结果:" + BarCode);
            }
            else
            {
                LogRecord.addLog("扫码失败:" + BarCode);
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            string BarCode = WorkFlowControl.StartBarcode(4);
            if (BarCode != "NoRead" || BarCode != "")
            {
                LogRecord.addLog("扫码结果:" + BarCode);
            }
            else
            {
                LogRecord.addLog("扫码失败:" + BarCode);
            }
        }

        private void button_DubugLogin_Click(object sender, EventArgs e)
        {
            LogRecord.addLog("调试登录");
            if (textBox_PassWord.Text == "123")
            {
                tabControl_Debug.Enabled = true;
                button_OpenParaSetFile.Enabled = true;
                button_ReloadSet.Enabled = true;
                button_ReSet.Enabled = true;
                button_RestartSystem.Enabled = true;

                textBox_PassWord.Text = "";
            }
            else
            {
                LogRecord.addLog("调试登录密码不正确");
            }



        }

        private void button_DubugLogout_Click(object sender, EventArgs e)
        {
            LogRecord.addLog("调试登出");
            textBox_PassWord.Text = "";
            button_OpenParaSetFile.Enabled = false;
            button_ReloadSet.Enabled = false;
            tabControl_Debug.Enabled = false;
            button_ReSet.Enabled = false;
            button_RestartSystem.Enabled = false;



        }

        private void button_OpenLogFolder_Click(object sender, EventArgs e)
        {
            string folderPath = LogRecord.SystemLogPath;
            // 使用ProcessStartInfo来打开文件夹
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = folderPath,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // 如果有错误，打印错误信息
                Console.WriteLine("无法打开文件夹: " + ex.Message);
            }
        }

        private void richTextBox_LogRecord_DoubleClick(object sender, EventArgs e)
        {
            LogRecord.addLog("双击流程日志框，打开日志");
            try
            {
                Task task = Task.Run(() =>
                {

                    //用记事本打开
                    System.Diagnostics.Process.Start("notepad.exe", LogRecord.SystemLogFilePath);


                });

            }
            catch (Exception ex)
            {
                LogRecord.addLog("日志打开失败" + ex.Message);
            }
        }

        private void richTextBox_LogCom2Camera_DoubleClick(object sender, EventArgs e)
        {
            LogRecord.addLog("双击视觉日志框，打开日志");
            try
            {
                Task task = Task.Run(() =>
                {

                    //用记事本打开
                    System.Diagnostics.Process.Start("notepad.exe", LogRecordVision.SystemLogFilePath);


                });

            }
            catch (Exception ex)
            {
                LogRecord.addLog("日志打开失败" + ex.Message);
            }
        }

        private void richTextBox_LogCom2Robot_DoubleClick(object sender, EventArgs e)
        {
            LogRecord.addLog("双击机器人日志框，打开日志");
            try
            {
                Task task = Task.Run(() =>
                {

                    //用记事本打开
                    System.Diagnostics.Process.Start("notepad.exe", LogRecordRobot.SystemLogFilePath);


                });

            }
            catch (Exception ex)
            {
                LogRecord.addLog("日志打开失败" + ex.Message);
            }
        }


        //手动离线工单模拟
        public  WorkOrder workOrderManul = null;
        #region 生成仿真工单
        private void button_GenSimuWorkorder_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {
                try
                {

                    int TuDianOrBanDian = 0;

                    LogRecord.addLog("生成仿真工单");

                    float w = float.Parse(textBox_MesPanelWidth.Text);
                    float h = float.Parse(textBox_MesPanelHeight.Text);
                    float t = float.Parse(textBox_MesPanelThickness.Text);

                    float pw = float.Parse(textBox_MesPeiduWidth.Text);
                    float ph = float.Parse(textBox_MesPeiduHeight.Text);
                    float pt = float.Parse(textBox_MesPeiduThickness.Text);
                    int PanelCount = int.Parse(textBox_MesPanelCount.Text);



                    workOrderManul = Com2Mes.GenOffLineworkOrder(PanelCount, w, h, t, pw, ph, pt, TuDianOrBanDian);




  
                    LogRecord.addLog("保存工单");
                    SaveworkOrder(workOrderManul);
                  

                }
                catch (Exception ex)
                {
                    LogRecord.addLog("数据转换出错" + ex.Message);

                }


            });
        }

        #endregion

        //载入工单
        private void button_LoadWorkOrder_Click(object sender, EventArgs e)
        {

         
            LoadWorkOrderFromFile();
          
        }


        private void LoadWorkOrderFromFile()
        {
            LogRecord.addLog("点击加载工单");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "json文件|*.json";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            // 使用项目启动路径下的子目录
            openFileDialog.InitialDirectory = Application.StartupPath + @"\WorkOrderLog";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                string fName = openFileDialog.FileName;
                LogRecord.addLog("json打开路径:" + fName);
                try
                {
                    if (!File.Exists(fName))
                    {

                        LogRecord.addLog("文件不存在:" + fName);
                        return;
                    }
                    string jsonContent = File.ReadAllText(fName);
                    WorkOrder workorder = Json2Obj<WorkOrder>(jsonContent);
                    PrintWorkOrder(workorder);

                    LogRecord.addLog("进入工单队列");
                    WorkFlowControl.EnqueueWorkOrderList(workorder);

                    workOrderManul = workorder;
                    // LogRecord.addLog("保存工单");
                    // SaveworkOrder(workOrderManul);


                    ////给PLC发送工单的主板数量 指示
                    // int keyX =com2PLC.DataRegister.FirstOrDefault(kvp => kvp.Value.Equals("当前工单主板数量")).Key;
                    // com2PLC.writeDataD(keyX, workorder.pnlqty);



                }
                catch (Exception ex)
                {
                    LogRecord.addLog("加载工单json出错" + ex.Message);
                }
            }




        }

        //连接状态指示灯
        private void UpdateStatusColor(Panel panel, bool isConnected)
        {
          
            if (panel == null) return;
            Color targetColor = isConnected ? Color.Lime : Color.Red;
            if (panel.BackColor != targetColor)
            {
                if (panel.InvokeRequired)
                {
                    panel.Invoke(new Action(() => panel.BackColor = targetColor));
                }
                else
                {
                    panel.BackColor = targetColor;
                }
            }


        }



        private bool IsSocketConnected(System.Net.Sockets.Socket s)
        {
            
            if (s == null || !s.Connected) return false;

         
            try
            {
                bool part1 = s.Poll(1000, System.Net.Sockets.SelectMode.SelectRead);
                bool part2 = (s.Available == 0);
                if (part1 && part2)
                {
                    return false; 
                }
            }
            catch
            {
                return false; 
            }

            return true; 
        }




        private Random _random = new Random();

        //刷新界面实时数据
        private void timer1_Tick(object sender, EventArgs e)
        {

            try
            {
                label_currentStatues.Text = WorkFlowControl.CurrentStatues;
                   
                int CountWorkOrder = WorkFlowControl.WorkOrderQueue.Count;
                for(int i=0;i< CountWorkOrder;i++)
                {

                    if(i==0)
                    {
                        label_CurrentWorkNo.Text = WorkFlowControl.WorkOrderQueue.ElementAt(i).lot_num;
                        label_CurrentWorkNo1.Text = "null";
                    }

                    if(i==1)
                    {

                        label_CurrentWorkNo1.Text = WorkFlowControl.WorkOrderQueue.ElementAt(i).lot_num;
                        label_CurrentWorkNo2.Text = "null";
                    }

                    if(i==2)
                    {
                        label_CurrentWorkNo2.Text = WorkFlowControl.WorkOrderQueue.ElementAt(i).lot_num;

                    }
                             
                }

                label_CurrentFeiBaNo1.Text = WorkFlowControl.FeiBaCurrentQian.FeiBaNo;
                label_CurrentFeiBaNo2.Text = WorkFlowControl.FeiBaCurrentHou.FeiBaNo;

                label_CurrentPanelNo.Text = WorkFlowControl.CurrentPanel;
                label_CountShangBan.Text = WorkFlowControl.CountShangliao.ToString();
                label_CountXiaBan.Text = WorkFlowControl.CountXialiao.ToString();
                label_CountWorkOrder.Text = WorkFlowControl.CountWorkOrderDone.ToString();



                if(checkBox_offlineTest.Checked &&  !WorkFlowControl.isOfflineDebug )
                {
                    LogRecord.addLog("切换到离线测试状态");
                    WorkFlowControl.isOfflineDebug = true;
                }

                if (!checkBox_offlineTest.Checked && WorkFlowControl.isOfflineDebug)
                {
                    LogRecord.addLog("切换到在线测试状态");
                    WorkFlowControl.isOfflineDebug = false;
                }



                if (checkBox_PanelNoCode.Checked && !WorkFlowControl.panelNoCode)
                {
                    LogRecord.addLog("切换到主板无码状态");
                    WorkFlowControl.panelNoCode = true;
                }

                if (!checkBox_PanelNoCode.Checked && WorkFlowControl.panelNoCode)
                {
                    LogRecord.addLog("切换到主板有码状态");
                    WorkFlowControl.panelNoCode = false;
                }



                

                if (!checkBox_CancelXiaLiaoInspect.Checked && WorkFlowControl.CancelXiaLiaoInspect)
                {
                    LogRecord.addLog("保留下料检查环节");
                    WorkFlowControl.CancelXiaLiaoInspect = false;
                }


                if (checkBox_CancelXiaLiaoInspect.Checked && !WorkFlowControl.CancelXiaLiaoInspect)
                {
                    LogRecord.addLog("取消下料检查环节");
                    WorkFlowControl.CancelXiaLiaoInspect = true;
                }



                if (!checkBox_DebugRobot1Only.Checked && WorkFlowControl.TestRobot1)
                {
                    LogRecord.addLog("切换到1号机器人不启动,仅针对下料流程有效");
                    WorkFlowControl.TestRobot1 = false;
                }


                if (checkBox_DebugRobot1Only.Checked && !WorkFlowControl.TestRobot1)
                {
                    LogRecord.addLog("切换到1号机器人启动,仅针对下料流程有效");
                    WorkFlowControl.TestRobot1 = true;
                }


                if (!checkBox_DebugRobot2Only.Checked && WorkFlowControl.TestRobot2)
                {
                    LogRecord.addLog("切换到2号机器人不启动,仅针对下料流程有效");
                    WorkFlowControl.TestRobot2 = false;
                }


                if (checkBox_DebugRobot2Only.Checked && !WorkFlowControl.TestRobot2)
                {
                    LogRecord.addLog("切换到2号机器人启动,仅针对下料流程有效");
                    WorkFlowControl.TestRobot2 = true;
                }


                label_Force1.Text = Com2ForceControl.ForceData1;
                label_Force2.Text = Com2ForceControl.ForceData2;

                try
                {
                    int PanelListCountAll = WorkOrderCurrent.CodeList.Count;
                    int PanelListDone = WorkOrderCurrent.CodeListDone.Count; //已经上完的计数
                    CurrentWorkOrderDoneRatio = $"{PanelListDone}/{PanelListCountAll}";
                    label_CurrentWorkOrderDoneRatio.Text = CurrentWorkOrderDoneRatio;


                }
                catch (Exception ex)
                {

                }



                ////TODO:力控数据处理///////////////////////////////////////


                label_StatusRobot1.Text = robot1.WaitStatues;
                label_StatusRobot2.Text = robot2.WaitStatues;

                label_StatusShangLiaoRobot1.Text = robot1.StatuesShangLiao;
                label_StatusShangLiaoRobot2.Text = robot2.StatuesShangLiao;

                label_StatusXiaLiaoRobot1.Text = robot1.StatuesXiaLiao;
                label_StatusXiaLiaoRobot2.Text = robot2.StatuesXiaLiao;




                text_QianFeiBaFile.Text = WorkFlowControl.offlineFeiBaFile1;
                text_HouFeiBaFile.Text = WorkFlowControl.offlineFeiBaFile2;
                //报警触发//////



                /////////////////////////////////////////////////////////////
                //
                //状态指示灯
                bool robot1Ok = WorkFlowControl.robot1 != null && WorkFlowControl.robot1.isConnected;
                UpdateStatusColor(panelRobot1, robot1Ok);
                bool robot2Ok = WorkFlowControl.robot2 != null && WorkFlowControl.robot2.isConnected;
                UpdateStatusColor(panelRobot2, robot2Ok);

                bool plcOk = WorkFlowControl.com2PLC != null && WorkFlowControl.com2PLC.Connected_Flag;
                UpdateStatusColor(panelPLC, plcOk);

                UpdateStatusColor(panel1, Com2Vision._isConnected);

                bool vmOk = IsSocketConnected(Com2VisionMaster.socket_2D);
                UpdateStatusColor(panel, vmOk);

                bool forceOk = Com2ForceControl._isConnected1 || Com2ForceControl._isConnected2;
                UpdateStatusColor(panelForce, forceOk);

                UpdateStatusColor(panelMES, Com2Mes.MesConnected);

                bool scan1 = WorkFlowControl.bacodeScan1 != null && WorkFlowControl.bacodeScan1.isConnected;
                bool scan2 = WorkFlowControl.bacodeScan2 != null && WorkFlowControl.bacodeScan2.isConnected;
                bool scan3 = WorkFlowControl.bacodeScan3 != null && WorkFlowControl.bacodeScan3.isConnected;
                bool scan4 = WorkFlowControl.bacodeScan4 != null && WorkFlowControl.bacodeScan4.isConnected;

                // 
                // 只要有一个没有掉线，总灯就变绿
                bool allScannersOk = scan1 || scan2 || scan3 || scan4;

                UpdateStatusColor(paneBrocode, allScannersOk);



               
                   



            }
            catch (Exception ex)
            {
                LogRecord.addLog("刷新状态定时器出错"+ex.Message);
            }
     





        }

        private void GenTestFeiBaworker(int row)
        {



            try
            {

                double peiDuWidthOut;
                double baseAdjOut;
                LinkedList<double> posXQueue;
                LinkedList<double> posYQueue;
                Queue<double> posPeiDuXQueue;
                Queue<double> posPeiDuYQueue;

                var currentOrder = workOrderManul;
                LogRecord.addLog($"正在按当前工单[{currentOrder.lot_num}]生成排版信息...");



                int res = WorkFlowControl.CalPaiBan(
                currentOrder.pallet_width,      // 板宽
                currentOrder.pallet_length,     // 板高 (通常 MES 的 Length 对应排版算法的 Height)
                currentOrder.pallet_thickness,  // 板厚
                out peiDuWidthOut,
                out double peiDuHeight,
                out double peiDuThickness,
                out baseAdjOut,
                out posXQueue,
                out posYQueue,
                out posPeiDuXQueue,
                out posPeiDuYQueue
            );
                if (res == 1)
                {
                    LogRecord.addLog("=== 排版计算成功 ===");
                    LogRecord.addLog($"陪镀板宽度: {peiDuWidthOut}");
                    LogRecord.addLog($"基座调整值: {baseAdjOut}");
                    LogRecord.addLog($"主板数量: {posXQueue.Count}, 陪镀板数量: {posPeiDuXQueue.Count}");




                    //分别生成前后飞巴
                    FeiBaWorker fbworker = new FeiBaWorker();

                    fbworker.FeiBaAdaptionValue = baseAdjOut;
                    fbworker.FeiBaNo = row.ToString("D4");

                    fbworker.PanelCount = posXQueue.Count;
                    fbworker.PanelThickness = currentOrder.pallet_thickness;
                    fbworker.PanelWidth = currentOrder.pallet_width;
                    fbworker.PanelHeight = currentOrder.pallet_length;

                    fbworker.PosXQueue = posXQueue; 
                    fbworker.PosYQueue = posYQueue;

                    fbworker.Row = row;



                    //分配主板上料循环次数并写入机器人和PLC，循环总数是全局变量
                    fbworker.ShangLiaoCount1 = (fbworker.PanelCount + 1) / 2;
                    fbworker.ShangLiaoCount2 = fbworker.PanelCount - fbworker.ShangLiaoCount1;
 

                    if (posPeiDuXQueue.Count>0)
                    {

                        fbworker.PeiDuBanExist = true;
                        fbworker.PeiDuposX1 = posPeiDuXQueue.ElementAt(0);
                        fbworker.PeiDuposX2 = posPeiDuXQueue.ElementAt(1);

                        fbworker.PeiDuposY1 = posPeiDuYQueue.ElementAt(0);
                        fbworker.PeiDuposY2 = posPeiDuYQueue.ElementAt(1);

                        fbworker.PeiDuThickness = peiDuThickness;
                        fbworker.PeiDuWeight = 2000;
                        fbworker.PeiDuWidth = peiDuWidthOut;
                        fbworker.PeiDuHeight = peiDuHeight;
                    }
                    else
                    {
                        fbworker.PeiDuBanExist = false;

                    }


                    //////////
                    for (int i = 0; i < fbworker.PanelCount; i++)
                    {
                        FeiBaPanelInfo Panelinfo = new FeiBaPanelInfo();
                        DateTime startTime1 = DateTime.Now;
                        string str1 = (i+1).ToString("D4") ;

                        Panelinfo.PanelNo = str1;


                        Panelinfo.PanelThickness = fbworker.PanelThickness;
                        Panelinfo.PanelWidth = fbworker.PanelWidth;
                        Panelinfo.PanelHeight = fbworker.PanelHeight;
                        Panelinfo.ColNo = i+1;
                        Panelinfo.PanelWeight = fbworker.PanelWeight;
                       

                        Panelinfo.posX = posXQueue.ElementAt(i);
                        Panelinfo.posY = posYQueue.ElementAt(i);

                        fbworker.PanelList.Enqueue(Panelinfo);

                        Thread.Sleep(1);
                    }









                    fbworker.PrintFeiBaWorker();
                     FeiBaWorker.SavefeibaWorker(fbworker);

         



                }
                else
                {
                    LogRecord.addLog("排版计算失败，请检查参数设置。");
                    MessageBox.Show("排版计算失败！");
                }

            }
            catch (Exception ex)
            {

                LogRecord.addLog("生成排版信息异常: " + ex.Message);
            }


        }




        //生成飞巴信息

        private void button18_Click(object sender, EventArgs e)
        {

            LogRecord.addLog("生成前飞巴信息测试");

            GenTestFeiBaworker(1);



        }

        private void button19_Click(object sender, EventArgs e)
        {

            //载入当前飞巴测试R
            LogRecord.addLog("自动载入飞巴文件测试,载入飞巴文件夹内修改时间最新的两个文件,分别作为前后飞巴信息");

            FeiBaWorker.GetLatestFeiBaFile();



        }

        #region 手动载入飞巴文件

      
        private void button20_Click(object sender, EventArgs e)
        {


            LogRecord.addLog("点击手动加载飞巴数据");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "json文件|*.json";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            // 使用项目启动路径下的子目录
            openFileDialog.InitialDirectory = Application.StartupPath + @"\FeiBaLog";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                string fName = openFileDialog.FileName;
                LogRecord.addLog("json打开路径:" + fName);
                try
                {
                    if (!File.Exists(fName))
                    {

                        LogRecord.addLog("文件不存在:" + fName);
                        return;
                    }


                    string jsonContent = File.ReadAllText(fName);
                    FeiBaWorker feibaworker = Json2Obj<FeiBaWorker>(jsonContent);
                    feibaworker.PrintFeiBaWorker();

                    if(feibaworker.Row ==1)
                    {
                        FeiBaCurrentQian = feibaworker;

                    }
                    else if (feibaworker.Row ==2)
                    {
                        FeiBaCurrentHou = feibaworker;
                    }
                    else
                    {
                        LogRecord.addLog("载入的文件行号有误"+ feibaworker.Row);
                    }
                 


                }
                catch (Exception ex)
                {
                    LogRecord.addLog("加载工单json出错" + ex.Message);
                }




            }




        }

        #endregion


        #region  清线完成接口测试
        private async void button21_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Button btn = sender as System.Windows.Forms.Button;
            if (btn != null) btn.Enabled = false;
           
            Response resp = await Com2Mes.SendClearLineComplete();
            if (resp != null && resp.status == 0)
            {
                MessageBox.Show($"上报成功！\n消息: {resp.msg}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                string msg = resp != null ? resp.msg : "接口调用失败";
                MessageBox.Show($"上报失败！\n原因: {msg}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (btn != null) btn.Enabled = true;
        }
        #endregion


        #region 心跳接口
        private async void button22_Click(object sender, EventArgs e)
        {
            // 1. 禁用按钮防止重复点击
            System.Windows.Forms.Button btn = sender as System.Windows.Forms.Button;
            if (btn != null) btn.Enabled = false;

            // 调用封装的方法
            Response resp = await Com2Mes.SendHeartBeat(isAuto:false);
            if (btn != null) btn.Enabled = true;
        }
        #endregion

        #region 状态上报接口测试
        private async void button25_Click(object sender, EventArgs e)
        {
            // 1. 禁用按钮防止重复点击
            System.Windows.Forms.Button btn = sender as System.Windows.Forms.Button;
            if (btn != null) btn.Enabled = false;
            
            //测试数据
            string testStatusCode = "1";
            string testStatusDesc = "运行";

            Response resp = await Com2Mes.SendStatusReport(testStatusCode, testStatusDesc);
            if (resp != null && resp.status == 0)
            {
                MessageBox.Show($"上报成功！\n消息: {resp.msg}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                string msg = resp != null ? resp.msg : "接口调用失败";
                MessageBox.Show($"上报失败！\n原因: {msg}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (btn != null) btn.Enabled = true;
        }
        #endregion

        #region 用电用水接口
        private  async void button24_Click(object sender, EventArgs e)
        {

            System.Windows.Forms.Button btn = sender as System.Windows.Forms.Button;
            if (btn != null) btn.Enabled = false;

            //测试数据
            string waterUsed = "10.5";
            string diWaterUsed = "5.2";
           
            double elec = 11.9;

            WorkFlowControl.com2PLC.readDataD(7320, 4, out elec);
            string electricityUsed = elec.ToString("F2");

            Response resp = await Com2Mes.SendWaterAndElectricityReport(waterUsed, diWaterUsed, electricityUsed);
            if (resp != null && resp.status == 0)
            {
                MessageBox.Show($"上报成功！\n消息: {resp.msg}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                string msg = resp != null ? resp.msg : "接口调用失败";
                MessageBox.Show($"上报失败！\n原因: {msg}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (btn != null) btn.Enabled = true;
        }
        #endregion

        #region 异常上报接口 
        private  async void button23_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Button btn = sender as System.Windows.Forms.Button;
            if (btn != null) btn.Enabled = false;

            // 测试数据
            string alarmCode = "ERR_MOTOR_01";
            string alarmDes = "主轴电机过热";
            int alarmLevel = 3;
            int alarmStatus = 1; // 1=发生

            Response resp = await Com2Mes.SendAlarmReport(alarmCode, alarmDes, alarmLevel, alarmStatus);
            if (resp != null && resp.status == 0)
            {
                MessageBox.Show($"上报成功！\n消息: {resp.msg}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                string msg = resp != null ? resp.msg : "接口调用失败";
                MessageBox.Show($"上报失败！\n原因: {msg}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (btn != null) btn.Enabled = true;
        }
        #endregion



      
         private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        #region 读字符串寄存器
        private void RegisterString_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textBox2.Text)) return;
               int startAddr=int.Parse(textBox2.Text);
                //读取长度
                int readLength = int.Parse(textBox1.Text);
                Task.Run(() =>
                {
                    try
                    {
                        string resultStr = "";
                        WorkFlowControl.com2PLC.readStringD(startAddr, readLength, out resultStr);
                        this.Invoke((MethodInvoker)delegate
                        {
                            textBox1.Text = resultStr; 
                            LogRecord.addLog($"读取字符串成功 [{startAddr}]: {resultStr}");
                        });
                    }
                    catch (Exception ex)
                    {
                        LogRecord.addLog($"读取字符串失败: " + ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("输入地址或长度有误:" + ex.Message);
            }
        }
        #endregion

        #region 读数字寄存器  
        private void RegisterData_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(textBox3.Text)) return;
                int startAddr = int.Parse(textBox3.Text);

                int readLength = 2; 

                Task.Run(() =>
                {
                    try
                    {
                        double resultVal = 0;
                        WorkFlowControl.com2PLC.readDataD(startAddr, readLength, out resultVal);
                        this.Invoke((MethodInvoker)delegate
                        {
                            textBox4.Text = resultVal.ToString(); 
                            LogRecord.addLog($"读取数字成功 [{startAddr}]: {resultVal}");
                        });
                    }
                    catch (Exception ex)
                    {
                        LogRecord.addLog($"读取数字失败: " + ex.Message);
                    }
                });

            }
            catch (Exception ex)
            {

                MessageBox.Show("输入参数有误: " + ex.Message);
            }
        }




        #endregion

        #region 按当前工单生成排版信息
        private void WorkOrderGeneration_Click(object sender, EventArgs e)
        {
            try
            {
                
                    var currentOrder= workOrderManul;
                    LogRecord.addLog($"正在按当前工单[{currentOrder.lot_num}]生成排版信息...");

                    double peiDuWidthOut;
                    double baseAdjOut;
                    LinkedList<double> posXQueue;
                    LinkedList<double> posYQueue;
                    Queue<double> posPeiDuXQueue;
                    Queue<double> posPeiDuYQueue;

                    int res = WorkFlowControl.CalPaiBan(
                    currentOrder.pallet_width,      // 板宽
                    currentOrder.pallet_length,     // 板高 (通常 MES 的 Length 对应排版算法的 Height)
                    currentOrder.pallet_thickness,  // 板厚
                    out peiDuWidthOut,
                    out double peiDuHeight,
                    out double peiDuThickness,
                    out baseAdjOut,
                    out posXQueue,
                    out posYQueue,
                    out posPeiDuXQueue,
                    out posPeiDuYQueue
                );
                    if (res==1)
                    {
                        LogRecord.addLog("=== 排版计算成功 ===");
                        LogRecord.addLog($"陪镀板宽度: {peiDuWidthOut}");
                        LogRecord.addLog($"基座调整值: {baseAdjOut}");
                        LogRecord.addLog($"主板数量: {posXQueue.Count}, 陪镀板数量: {posPeiDuXQueue.Count}");
                    }
                    else
                    {
                        LogRecord.addLog("排版计算失败，请检查参数设置。");
                        MessageBox.Show("排版计算失败！");
                    }
              
            }
            catch (Exception ex)
            {

                LogRecord.addLog("生成排版信息异常: " + ex.Message);
            }

        }

        #endregion

 
        #region 根据条码主动获取工单
        private void button_getorderbyCode_Click(object sender, EventArgs e)
        {
            // 1. 获取条码
            string barcode = textBox_HandScaner.Text.Trim();
            if (string.IsNullOrEmpty(barcode))
            {
                MessageBox.Show("请输入条码！");
                return;
            }
            System.Windows.Forms.Button btn = sender as System.Windows.Forms.Button;
            if (btn != null) btn.Enabled = false;

            // 2. 启动后台线程 
            Task.Run(() =>
            {
                // 直接调用 Com2Mes
                WorkOrder newOrder = Com2Mes.GetworkOrder(barcode);

                // 3. 回到主线程更新界面
                this.Invoke((MethodInvoker)delegate
                {
                    if (newOrder != null)
                    {

                        LogRecord.addLog($"从MES获取工单成功{newOrder.lot_num}");
                        WorkFlowControl.EnqueueWorkOrderList(newOrder);
                        Com2Mes.SaveworkOrder(newOrder);

                        string msg = $"工单获取成功!\n批次: {newOrder.lot_num}\n数量: {newOrder.pnlqty}";
                        MessageBox.Show(msg, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LogRecord.addLog($"[UI] 工单 {newOrder.lot_num} 已加入生产队列");
                    }
                    else
                    {
                        MessageBox.Show("工单获取失败，请检查条码或查看日志。", "失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    if (btn != null) btn.Enabled = true;
                });
            });


        }



        #endregion

        #region 从扫码枪获取条码
        private void btn_StartScan_Click(object sender, EventArgs e)
        {


            textBox8.Text = textBox_HandScaner.Text;
            //Button btn = sender as Button;
            //btn.Enabled = false; // 防止重复点击
            //Task.Run(() =>
            //{


            //    string code = textBox_HandScaner.Text;
            //    WorkFlowControl.RunHandheldScanProcess(code);
            //    this.Invoke((MethodInvoker)delegate { btn.Enabled = true; });
            //});
        }

        #endregion

        private void groupBox9_Enter(object sender, EventArgs e)
        {

        }


        #region 写机器人寄存器
        #endregion
        private void button17_Click(object sender, EventArgs e)
        {
            try
            {
                Task task = Task.Run(() =>
                {
                    //写数据寄存器
                    int startAddr = int.Parse(textBox9.Text);
                    double data = double.Parse(textBox10.Text);

                    int RobotID = int.Parse(comboBox_RobotID.Text);

                    if (RobotID == 1)
                        WorkFlowControl.robot1.writeDataD(startAddr, data);
                    else
                        WorkFlowControl.robot2.writeDataD(startAddr, data);

                });

            }
            catch (Exception ex)
            {
                LogRecord.addLog($"写入PLC寄存器出错" + ex.Message);


            }
        }

        private void button28_Click(object sender, EventArgs e)
        {
            try
            {
                Task task = Task.Run(() =>
                {
                    //写数据寄存器
                    int startAddr = int.Parse(textBox9.Text);
                  //  double data = double.Parse(textBox10.Text);

                    int RobotID = int.Parse(comboBox_RobotID.Text);
                    int readLength = 2; //如果是双精度需要改成4

                    double value = -999;

                    if (RobotID == 1)
                        WorkFlowControl.robot1.readDataD(startAddr, readLength, out  value);
                    else
                        WorkFlowControl.robot2.readDataD(startAddr, readLength, out  value);

                    LogRecord.addLog($"{RobotID}号机器人，地址{startAddr}，数据读出{value}");

                });

            }
            catch (Exception ex)
            {
                LogRecord.addLog($"写入PLC寄存器出错" + ex.Message);


            }
        }

        private void button_StartCapture1_Click(object sender, EventArgs e)
        {
            Task.Run(() => {
                LogRecord.addLog("触发左相机定位拍照");
                Com2VisionMaster.StartCapture(out double x,out double y,out double r,1);
                LogRecord.addLog($"拍照结果{x},{y},{r}");
            });
        }

        private void button_StartCapture2_Click(object sender, EventArgs e)
        {
            Task.Run(() => {
                LogRecord.addLog("触发右相机拍照");
                Com2VisionMaster.StartCapture(out double x, out double y, out double r, 2);
                LogRecord.addLog($"拍照结果{x},{y},{r}");
            });
        }

        private void btn_VIsLeft_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() => {
                LogRecord.addLog("触发左相机拍照");
                Com2Vision.Start3DCapturePeiDuCheck( 1,1,out int PeiduWidth, out int PeiduHeight);
                LogRecord.addLog($"拍照结果{PeiduWidth},{PeiduHeight}");
            });
        }

        private void btn_VIsRight_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {
                LogRecord.addLog("触发右相机拍照");
                Com2Vision.Start3DCapturePeiDuCheck(2, 1,out int PeiduWidth, out int PeiduHeight);
                LogRecord.addLog($"拍照结果{PeiduWidth},{PeiduHeight}");
            });










        }
        private void tabPage8_Click(object sender, EventArgs e)
        {

        }

        private void button_Send2Robot_Click(object sender, EventArgs e)
        {

            Task task = Task.Run(() =>
            {
                //发送排版坐标到机器人
                try
                {
                    int RowNo = int.Parse(comboBox_RowP.Text);
                    int PosNo = int.Parse(comboBox_PosNo.Text);
                    int RobotNo = int.Parse(comboBox_RobotNoP.Text);

                    double panelWidth = double.Parse(textBox_PanelWidth.Text);
                    double panelHeight = double.Parse(textBox_PanelHeight.Text);
                    double panelThickness = double.Parse(textBox_PanelThickness.Text);

                    double PosX = WorkFlowControl.GetElementAtIndex(WorkFlowControl.PosXQueue, PosNo - 1);
                    double PosY = WorkFlowControl.GetElementAtIndex(WorkFlowControl.PosYQueue, PosNo - 1);

                    LogRecord.addLog($"上料过程测试-发送排版结果主板信息:编号{PosNo},坐标({PosX},{PosY}),行号{RowNo}到机器人{RobotNo}");

                    if (RobotNo == 1)
                    {
                        WorkFlowControl.robot1.SendShangLiaoPos(PosX, PosY, RowNo, panelWidth, panelHeight, panelThickness);
                    }
                    else
                    {
                        WorkFlowControl.robot2.SendShangLiaoPos(PosX, PosY, RowNo, panelWidth, panelHeight, panelThickness);
                    }
                }
                catch (Exception ex)
                {

                    LogRecord.addLog("发送排版结果主板信息出错" + ex.Message);
                }
            });


             
        




        }

        private void button30_Click(object sender, EventArgs e)
        {

            Task task = Task.Run(() =>
            {

                //发送排版坐标到机器人
                try
                {
                    int RowNo = int.Parse(comboBox_RowP.Text);
                    int PosNo = int.Parse(comboBox_PosNo.Text);
                    int RobotNo = int.Parse(comboBox_RobotNoP.Text);

                    double panelWidth = double.Parse(textBox_PanelWidth.Text);
                    double panelHeight = double.Parse(textBox_PanelHeight.Text);
                    double panelThickness = double.Parse(textBox_PanelThickness.Text);

                    double PosX = WorkFlowControl.GetElementAtIndex(WorkFlowControl.PosXQueue, PosNo - 1);
                    double PosY = WorkFlowControl.GetElementAtIndex(WorkFlowControl.PosYQueue, PosNo - 1);

                    LogRecord.addLog($"下料过程测试-发送排版结果主板信息:编号{PosNo},坐标({PosX},{PosY}),行号{RowNo}到机器人{RobotNo}");

                    if (RobotNo == 1)
                    {
                        WorkFlowControl.robot1.SendXiaLiaoPos(PosX, PosY, RowNo, panelWidth, panelHeight, panelThickness);
                    }
                    else
                    {
                        WorkFlowControl.robot2.SendXiaLiaoPos(PosX, PosY, RowNo, panelWidth, panelHeight, panelThickness);
                    }
                }
                catch (Exception ex)
                {

                    LogRecord.addLog("发送排版结果主板信息出错" + ex.Message);
                }
            });

        }

        private void button_SendPeiDu1_Click(object sender, EventArgs e)
        {

            Task task = Task.Run(() =>
            {
                try
                {
                    if(WorkFlowControl.PosPeiDuXQueue.Count<2)
                    {
                        LogRecord.addLog($"当前排版无陪镀板");
                        return;

                    }

                    int RowNo = int.Parse(comboBox_RowP.Text);
                    int PosNo = int.Parse(comboBox_PosNo.Text);


                    double PeiDuWidth = double.Parse(textBox_PeiDuWidth1.Text);
                    double PeiDuHeight = double.Parse(textBox_PeiDuHeight1.Text);
                    double PeiDuThickness = double.Parse(textBox_PanelThickness.Text);


                    double MainPanelWidth = double.Parse(textBox_PanelWidthT.Text);
                    double MainPanelHeight = double.Parse(textBox_PanelHeightT.Text);

                    double PosX = WorkFlowControl.PosPeiDuXQueue.First();
                    double PosY = WorkFlowControl.PosPeiDuYQueue.First();
                    WorkFlowControl.robot1.SendPeiDuPos(PosX, PosY, RowNo, PeiDuWidth, PeiDuHeight, PeiDuThickness,MainPanelWidth,MainPanelHeight,1,0);
                    LogRecord.addLog($"发送陪镀板信息:坐标({PosX},{PosY}),行号{RowNo}到机器人1");

                }
                catch (Exception ex)
                {

                    LogRecord.addLog("发送排版结果陪镀板信息到机器人1出错"+ex.Message);
                }



            });



        }

        private void button_SendPeiDu2_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {
                try
                {

                    if (WorkFlowControl.PosPeiDuXQueue.Count < 2)
                    {
                        LogRecord.addLog($"当前排版无陪镀板");
                        return;

                    }

                    int RowNo = int.Parse(comboBox_RowP.Text);
                    int PosNo = int.Parse(comboBox_PosNo.Text);


                    double PeiDuWidth = double.Parse(textBox_PeiDuWidth1.Text);
                    double PeiDuHeight = double.Parse(textBox_PeiDuHeight1.Text);
                    double PeiDuThickness = double.Parse(textBox_PanelThickness.Text);


                    double MainPanelWidth = double.Parse(textBox_PanelWidthT.Text);
                    double MainPanelHeight = double.Parse(textBox_PanelHeightT.Text);


                    double PosX = WorkFlowControl.PosPeiDuXQueue.Last();
                    double PosY = WorkFlowControl.PosPeiDuYQueue.Last();
                    WorkFlowControl.robot2.SendPeiDuPos(PosX, PosY, RowNo, PeiDuWidth, PeiDuHeight, PeiDuThickness,MainPanelWidth,MainPanelHeight, 1,0);
                    LogRecord.addLog($"发送陪镀板信息:坐标({PosX},{PosY}),行号{RowNo}到机器人2");

                }
                catch (Exception ex)
                {

                    LogRecord.addLog("发送排版结果陪镀板信息到机器人1出错" + ex.Message);
                }



            });
        }

        private void button29_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {         
                //向PLC写入基座调整值，进行基座调整，前后飞巴默认一致
                com2PLC.FeibaBaseAdjust(BaseAdj, BaseAdj, 1);

                com2PLC.FeibaBaseAdjust(BaseAdj, BaseAdj, 2);

                //告诉机器人基座调整距离，可能要在y方向做偏移
                robot1.writeDataD(7060, BaseAdj);
                robot2.writeDataD(7060, BaseAdj);

            });
    
        }

        private void button26_Click(object sender, EventArgs e)
        {

        }

        private void button27_Click(object sender, EventArgs e)
        {

        }



        /// <summary>
        /// 程序关闭时完全退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            Application.ExitThread();
            Environment.Exit(0);

        }

       

        private void button_ConnectForceControl_Click(object sender, EventArgs e)
        {
          

            Task task = Task.Run(() =>
            {
                LogRecord.addLog("连接力控");
                Com2ForceControl.Connect();


            });
        }

        private void button32_Click(object sender, EventArgs e)
        {

            LogRecord.addLog("停用力控");
            Com2ForceControl.Listeningrunning1 = false;
            Com2ForceControl.Listeningrunning2 = false;
        }

        private void button33_Click(object sender, EventArgs e)
        {
           
                LoadWorkOrderFromFile();

          
        }

        private void button34_Click(object sender, EventArgs e)
        {
            new Thread(WorkFlowControl.RunXiaLiao).Start();
        }

        private void button36_Click(object sender, EventArgs e)
        {
            new Thread(WorkFlowControl.RunShangLiaoSaoMa1).Start();
        }

        private void button37_Click(object sender, EventArgs e)
        {
            new Thread(WorkFlowControl.RunShangLiaoSaoMa2).Start();
        }

        private void button38_Click(object sender, EventArgs e)
        {
            new Thread(WorkFlowControl.RunShangLiao2DPosition1).Start();
        }

        private void button39_Click(object sender, EventArgs e)
        {
            new Thread(WorkFlowControl.RunShangLiao2DPosition2).Start();
        }

        private void button40_Click(object sender, EventArgs e)
        {
            new Thread(WorkFlowControl.RunShangLiaoPostPro).Start();
        }

        private void button35_Click(object sender, EventArgs e)
        {
            new Thread(WorkFlowControl.RunShangLiaoFeiBaSaoMaAndBaseAdj).Start();
        }


        //载入工单后生成排版
        private void button_genPaiBan_Click(object sender, EventArgs e)
        {
            LogRecord.addLog($"计算当前工单排版");

            WorkOrderCurrent = WorkOrderQueue.ElementAt(0);
            //计算排版,返回陪镀板宽度和基座调整值
            int Res = WorkFlowControl.CalPaiBan(WorkOrderCurrent.pallet_width, WorkOrderCurrent.pallet_length, WorkOrderCurrent.pallet_thickness,
                                                                    out double PeiduWidth,  out double PeiduHeight,out double  peiduT,  out double BaseAdj, out LinkedList<double> PosXQueue, out LinkedList<double> PosYQueue,
                                                                    out Queue<double> PosPeiDuXQueue, out Queue<double> PosPeiDuYQueue);
 




        }


        private void Check_FeiBaLog_Click(object sender, EventArgs e)
        {
            LoadFeiBaFromFile(1);
        }

        private void LoadFeiBaFromFile(int row)
        {
            LogRecord.addLog("点击加载飞巴文件");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "json文件|*.json";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            // 使用项目启动路径下的子目录
            openFileDialog.InitialDirectory = Application.StartupPath + @"\FeiBaLog";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                string fName = openFileDialog.FileName;
                LogRecord.addLog("文件打开路径:" + fName);
                try
                {
                    if (!File.Exists(fName))
                    {

                        LogRecord.addLog("文件不存在:" + fName);
                        return;
                    }
                    if(row == 1 )
                    {

                        text_QianFeiBaFile.Text = fName;
                        WorkFlowControl.offlineFeiBaFile1 = fName;
                    }
                    
                    if(row == 2)
                    {
                        text_HouFeiBaFile.Text = fName;
                        WorkFlowControl.offlineFeiBaFile2 = fName;
                    }
                   


                }
                catch (Exception ex)
                {
                    LogRecord.addLog("加载飞巴信息json出错" + ex.Message);
                }
            }
        }

        private void button_SelectFeiBaFile2_Click(object sender, EventArgs e)
        {
            LoadFeiBaFromFile(2);
        }

        private void button31_Click(object sender, EventArgs e)
        {
         
            
            new Thread(BaseAdjThradFunc).Start(1);


        

        }

        private void BaseAdjThradFunc(object obj)
        {

            int ShangLiaoOrXiaLiao = (int)obj;
            try
            {
                LogRecord.addLog("启动PLC进行基座调整,手动测试");

                double FeiBaAdaptionValue = double.Parse(textBox_BaseAdj.Text);
                LogRecord.addLog($"基座调整调整值{FeiBaAdaptionValue}");

                LogRecord.addLog($"飞巴基座调整{FeiBaAdaptionValue},{FeiBaAdaptionValue}");

               
                com2PLC.FeibaBaseAdjust(FeiBaAdaptionValue, FeiBaAdaptionValue, ShangLiaoOrXiaLiao);


                LogRecord.addLog($"飞巴基座调整{FeiBaAdaptionValue},{FeiBaAdaptionValue},环节: {ShangLiaoOrXiaLiao} :1为上料，2为下料");

                LogRecord.addLog($"等待PLC返回基座调整完成信号,信号35...");
                bool r = com2PLC.WaitForSingal(35);
                if (r)
                {
                    LogRecord.addLog($"PLC已经返回基座调整完成信号,调整完成");
                }
                else
                {
                    LogRecord.addLog($"等待PLC返回基座调整完成信号超时或出错");

                }

            }
            catch (Exception ex)
            {

                LogRecord.addLog("测试PLC进行基座调整出错" + ex.Message);
            }

        }

        private void button_ShowForceVisualForm_Click(object sender, EventArgs e)
        {

            formForceVis = new FormForceVisualization();
            formForceVis.Show();  // 非模态显示，可同时操作主界面


        }

        private void RestartApplication()
        {
            // 使用Invoke确保跨线程安全
            this.Invoke(new Action(() =>
            {
                Application.ExitThread(); // 退出当前线程
              //  Application.ExitThread();
                Environment.Exit(0);

                Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }));
        }

        private void button_Restart_Click(object sender, EventArgs e)
        {


            RestartApplication();

        }

        private void button42_Click(object sender, EventArgs e)
        {

            LogRecord.addLog("生成后飞巴信息测试");


            GenTestFeiBaworker(2);

        }

        private void button_exitWaitManul1_Click(object sender, EventArgs e)
        {
            if (robot1.WaitStatues != "null")
            {
                LogRecord.addLog($"手动退出机器人1等待状态{robot1.WaitStatues}");
                WorkFlowControl.robot1.exitWaitManul = true;
            }
            else 
            {
                LogRecord.addLog($"机器人1不在等待状态");
            }

         



        }

        private void button_exitWaitManul2_Click(object sender, EventArgs e)
        {
            if (robot2.WaitStatues != "null")
            {
                LogRecord.addLog($"手动退出机器人2等待状态{robot2.WaitStatues}");
                WorkFlowControl.robot2.exitWaitManul = true;
            }
            else
            {
                LogRecord.addLog($"机器人2不在等待状态");
            }

        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void button_ClearCount_Click(object sender, EventArgs e)
        {
            WorkFlowControl.CountShangliao = 0;
            WorkFlowControl.CountXialiao = 0;
            WorkFlowControl.CountWorkOrderDone = 0;
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void label83_Click(object sender, EventArgs e)
        {

        }

        private void button_ConnectBarCode5_Click(object sender, EventArgs e)
        {
            WorkFlowControl.ConnectBarCodeScaner(5);
        }

        private void button_ConnectBarCode6_Click(object sender, EventArgs e)
        {
            WorkFlowControl.ConnectBarCodeScaner(6);
        }

        private void button_StartBarCodeScan5_Click(object sender, EventArgs e)
        {
            string BarCode = WorkFlowControl.StartBarcode(5);
            if (BarCode != "NoRead" || BarCode != "")
            {
                LogRecord.addLog("扫码结果:" + BarCode);
            }
            else
            {
                LogRecord.addLog("扫码失败:" + BarCode);
            }
        }

        private void button43_Click(object sender, EventArgs e)
        {
            string BarCode = WorkFlowControl.StartBarcode(6);
            if (BarCode != "NoRead" || BarCode != "")
            {
                LogRecord.addLog("扫码结果:" + BarCode);
            }
            else
            {
                LogRecord.addLog("扫码失败:" + BarCode);
            }
        }

        #region  移除下一个工单
        private void btnRemoveNextWorkOrder_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show(
                "确定要移除工单列表中的下一个工单吗？",
                "确认移除工单",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
                );
            if (dialogResult!=DialogResult.Yes)
            {
                //LogRecord.addLog("取消了移除下一个工单的操作");
                return;
            }
            LogRecord.addLog("确认移除下一个工单");
            string removedLotNum = WorkFlowControl.RemoveNextWorkOrderAndSwith();

           
            if (!string.IsNullOrEmpty(removedLotNum))
            {
               
                MessageBox.Show($"工单 {removedLotNum} 已成功移除。", "操作成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                
                MessageBox.Show("移除失败：当前没有等待处理的工单。", "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }


        }
        #endregion

        private void button46_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {
                try
                {

                    int TuDianOrBanDian = 1;

                    LogRecord.addLog("生成仿真工单");

                    float w = float.Parse(textBox_MesPanelWidth.Text);
                    float h = float.Parse(textBox_MesPanelHeight.Text);
                    float t = float.Parse(textBox_MesPanelThickness.Text);

                    float pw = float.Parse(textBox_MesPeiduWidth.Text);
                    float ph = float.Parse(textBox_MesPeiduHeight.Text);
                    float pt = float.Parse(textBox_MesPeiduThickness.Text);
                    int PanelCount = int.Parse(textBox_MesPanelCount.Text);



                    workOrderManul = Com2Mes.GenOffLineworkOrder(PanelCount, w, h, t, pw, ph, pt, TuDianOrBanDian);





                    LogRecord.addLog("保存工单");
                    SaveworkOrder(workOrderManul);


                }
                catch (Exception ex)
                {
                    LogRecord.addLog("数据转换出错" + ex.Message);

                }


            });
        }
    }
}
