using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Log;

namespace PanelHandingSystem
{
    public partial class FormForceVisualization : Form
    {

        public ChartManager _chartManager1;
        public ChartManager _chartManager2;
        private Timer _dataTimer;
        private Random _random = new Random();

        public static bool isActive = false;

        string forceData_old1 = "";
        string forceData_old2= "";


        public FormForceVisualization()
        {
            InitializeComponent();

            // 初始化封装类
            _chartManager1 = new ChartManager(this.chart1);
            _chartManager2 = new ChartManager(this.chart2);


            isActive = true;
            //// 模拟100ms数据采集的定时器
            _dataTimer = new Timer();
            _dataTimer.Interval = 100;
            _dataTimer.Tick += DataTimer_Tick;
            _dataTimer.Start();
        }


        private void DataTimer_Tick(object sender, EventArgs e)
        {
            // 模拟生成三组实时数据
            //double val1 = _random.NextDouble() * 10 + 20;
            //double val2 = _random.NextDouble() * 15 + 40;
            //double val3 = _random.NextDouble() * 5 + 10;
            //double va21 = _random.NextDouble() * 10 + 20;
            //double va22 = _random.NextDouble() * 15 + 40;
            //double va23 = _random.NextDouble() * 5 + 10;

            try
            {

                if (isActive)
                {
                    string forceData1 = Com2ForceControl.ForceData1;
                    string forceData2 = Com2ForceControl.ForceData2;

                    if (forceData1 != "")
                    {

                        if (forceData1 != forceData_old1)
                        {

                            //数据不一样才刷新
                            string[] data1 = forceData1.Split(',');
                            double val1 = Convert.ToDouble(data1[0]);
                            double val2 = Convert.ToDouble(data1[1]);
                            double val3 = Convert.ToDouble(data1[2]);
                            _chartManager1.UpdateData(val1, val2, val3);
                            forceData_old1 = forceData1;

                        }
                    }



                    if (forceData2 != "")
                    {

                        if (forceData2 != forceData_old2)
                        {
                            //数据不一样才刷新
                            string[] data2 = forceData2.Split(',');
                            double va21 = Convert.ToDouble(data2[0]);
                            double va22 = Convert.ToDouble(data2[1]);
                            double va23 = Convert.ToDouble(data2[2]);
                            // 调用封装好的函数
                            _chartManager2.UpdateData(va21, va22, va23);
                            forceData_old2 = forceData2;
                        }


                    }

                }
             
            }
            catch (Exception ex)
            {
                LogRecordVision.addLog("力控数据可视化刷新出错" + ex.Message);
            }

        
        }

        private void btnExport1_Click(object sender, EventArgs e)
        {
            // 导出最近5分钟数据
            string fileName = $"ForceLog\\DataExport_Forece1_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            _chartManager1.ExportRecentDataToExcel(fileName);
        }

        private void btnExport2_Click(object sender, EventArgs e)
        {
            string fileName = $"ForceLog\\DataExport_Forece2_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            _chartManager2.ExportRecentDataToExcel(fileName);

        }

        private void FormForceVisualization_FormClosed(object sender, FormClosedEventArgs e)
        {
            isActive = false;
        }
    }
}
