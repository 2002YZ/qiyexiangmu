using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;



namespace PanelHandingSystem
{
    public class ChartManager
    {
        private Chart _chart;
        private List<DataRecord> _dataHistory = new List<DataRecord>();
        private readonly int _maxDisplayPoints = 1000; // 界面显示的采样点数量

        // 数据结构：时间戳 + 三组数据
        public class DataRecord
        {
            public DateTime Timestamp { get; set; }
            public double Value1 { get; set; }
            public double Value2 { get; set; }
            public double Value3 { get; set; }
        }

        public ChartManager(Chart chart)
        {
            _chart = chart;
            InitializeChart();
        }

        private void InitializeChart()
        {
            _chart.Series.Clear();
            _chart.ChartAreas.Clear();

            ChartArea chartArea = new ChartArea("MainArea");
            chartArea.AxisX.Title = "时间";
            chartArea.AxisY.Title = "数值";
            chartArea.AxisX.LabelStyle.Format = "HH:mm:ss";
            // 设置X轴为自动滚动模式
            chartArea.AxisX.ScrollBar.IsPositionedInside = true;
            _chart.ChartAreas.Add(chartArea);

            string[] seriesNames = { "FX", "FY", "FZ" };
            Color[] colors = { Color.Red, Color.Blue, Color.Green };

            for (int i = 0; i < 3; i++)
            {
                Series series = new Series(seriesNames[i]);
                series.ChartType = SeriesChartType.FastLine; // 使用FastLine提高实时性能
                series.BorderWidth = 2;
                series.Color = colors[i];
                series.XValueType = ChartValueType.DateTime;
                _chart.Series.Add(series);
            }
        }

        /// <summary>
        /// 每100ms调用一次的更新函数
        /// </summary>
        public void UpdateData(double v1, double v2, double v3)
        {
            // 确保在UI线程更新
            if (_chart.InvokeRequired)
            {
                _chart.BeginInvoke(new Action(() => UpdateData(v1, v2, v3)));
                return;
            }

            DateTime now = DateTime.Now;
            var record = new DataRecord { Timestamp = now, Value1 = v1, Value2 = v2, Value3 = v3 };

            // 存入历史记录
            _dataHistory.Add(record);

            // 更新图表显示
            double xValue = now.ToOADate();
            _chart.Series[0].Points.AddXY(xValue, v1);
            _chart.Series[1].Points.AddXY(xValue, v2);
            _chart.Series[2].Points.AddXY(xValue, v3);

            // 保持图表窗口只显示最近的N个点（避免界面卡顿）
            foreach (var series in _chart.Series)
            {
                if (series.Points.Count > _maxDisplayPoints)
                {
                    series.Points.RemoveAt(0);
                }
            }

            // 自动调整X轴范围
            _chart.ChartAreas[0].AxisX.Minimum = _chart.Series[0].Points[0].XValue;
            _chart.ChartAreas[0].AxisX.Maximum = xValue;
        }

        /// <summary>
        /// 将最近5分钟的数据保存到Excel(CSV格式)
        /// </summary>
        public void ExportRecentDataToExcel(string filePath)
        {
            DateTime fiveMinutesAgo = DateTime.Now.AddMinutes(-5);

            // 筛选最近5分钟的数据
            var exportData = _dataHistory
                .Where(d => d.Timestamp >= fiveMinutesAgo)
                .ToList();

            if (exportData.Count == 0)
            {
                MessageBox.Show("没有最近5分钟的数据可供导出。");
                return;
            }

            try
            {
                StringBuilder csvContent = new StringBuilder();
                csvContent.AppendLine("时间,数据1,数据2,数据3");

                foreach (var item in exportData)
                {
                    csvContent.AppendLine($"{item.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{item.Value1},{item.Value2},{item.Value3}");
                }

                // 写入文件（CSV格式Excel可以直接打开）
                File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);
                MessageBox.Show($"数据已成功导出至: {filePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败: " + ex.Message);
            }
        }
    }
}