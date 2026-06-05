using SkyTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanCheck
{
    public class ScanClass
    {
        public ScanClass()
        {
            for (int i = 0; i < heCount; i++)
            {
                triger[i] = $"Reader[{i + 1}].Auto_Triger";
                result[i] = $"Reader[{i + 1}].Result";
                data[i] = $"Reader[{i + 1}].ReturnData_STRING";
                beat[i] = $"Reader[{i + 1}].Heartbeat";
                heConnected[i] = $"Reader[{i + 1}].Connect_succeed";
            }
        }
        /// <summary>
        /// 扫码枪读取字符串
        /// </summary>
        public string strSend = "LON\r";

        public string strEnd = "LOFF\r";
        /// <summary>
        /// 扫码通讯延时
        /// </summary>
        public int delay = 800;
        private const int heCount = 7;
        /// <summary>
        /// PLC 触发 bool
        /// </summary>
        public string[] triger = new string[heCount];
        /// <summary>
        /// PC 告知PLC结果 1 OK 2 NG   int
        /// </summary>
        public string[] result = new string[heCount];
        /// <summary>
        /// PC 告知 PLC 数据    real float
        /// </summary>
        public string[] data = new string[heCount];
        /// <summary>
        /// PC 告知 PLC 心跳    bool
        /// </summary>
        public string[] beat = new string[heCount];
        /// <summary>
        /// 连接状态 bool
        /// </summary>
        public string[] heConnected = new string[heCount];
    }

    /// <summary>
    /// 电能表 间隔5S 写一次数据  安科瑞/ 正泰
    /// </summary>
    public class PowerClass
    {
        public PowerClass()
        {
            //int i = 1;
            for (int i = 0; i < heCount; i++)
            {
                // triger[i] = $"Power[{i + 1}].Auto_Triger";
                result[i] = $"Power[{i + 1}].Result";
                data[i] = $"Power[{i + 1}].ReturnData_Real";
                beat[i] = $"Power[{i + 1}].Heartbeat";
                heConnected[i] = $"Power[{i + 1}].Connect_succeed";
            }
        }

        #region 变量定义
        private const int heCount = 5;
        /// <summary>
        /// 打印忽略次数
        /// </summary>
        public int printCount = 5;
        /// <summary>
        /// 品牌  0 为 安科瑞 1 为正泰; 
        /// </summary>
        public int type = 0;
        /// <summary>
        /// 读取地址 安科瑞 0x50 ;  正泰 0x2000  
        /// </summary>
        public int[] startAddr = { 0x50, 0x2000 };
        /// <summary>
        /// 读取数量 0 安科瑞 18 ;  1 正泰 17  
        /// </summary>
        public int[] readCount = { 18, 17 };
        /// <summary>
        /// 通讯延时
        /// </summary>
        public int delay = 3000;
        /// <summary>
        /// 电流变比
        /// </summary>
        public double iRate = 1;
        /// <summary>
        /// 电压变比
        /// </summary>
        public double vRate = 1;
        /// <summary>
        /// PLC 触发 bool
        /// </summary>
       // public string[] triger = new string[heCount];
        /// <summary>
        /// PC 告知PLC结果 1 OK 2 NG   uint
        /// </summary>
        public string[] result = new string[heCount];
        /// <summary>
        /// PC 告知 PLC 数据    real float 数组长度 20 0~19
        /// </summary>
        public string[] data = new string[heCount];
        /// <summary>
        /// PC 告知 PLC 心跳    bool
        /// </summary>
        public string[] beat = new string[heCount];
        /// <summary>
        /// 连接状态 uint 类型
        /// </summary>
        public string[] heConnected = new string[heCount];
        /// <summary>
        /// 数据长度
        /// </summary>
        private int dataLen = 20;

        /// <summary>
        /// 数据名称 正泰名称
        /// </summary>
        private string[] powerNameChint = { "Uab", "Ubc", "Uca", "Ua", "Ub", "Uc", "Ia", "Ib", "Ic", "Pt", "Pa", "Pb", "Pc", "Qt", "Qa", "Qb", "Qc" };

        #endregion

        /// <summary>
        /// 安科瑞 数值 计算为写入的格式 安科瑞
        /// </summary>
        /// <returns></returns>
        public string ToValueString(short[] datas)
        {
            if (datas == null) return "";

            List<double> lf = new List<double>();

            for (int i = 0; i < 3; i++)//线电压
            {
                lf.Add(datas[i + 3] * vRate * 0.1);
            }
            for (int i = 0; i < 3; i++)//相电压
            {
                lf.Add(datas[i] * vRate * 0.1);
            }
            for (int i = 0; i < 3; i++)//相电流
            {
                lf.Add(datas[i + 6] * iRate * 0.001);
            }
            lf.Add(datas[13] * iRate * vRate * 0.0001);//总有功功率
            for (int i = 0; i < 3; i++)//有功功率
            {
                lf.Add(datas[i + 10] * iRate * vRate * 0.0001);
            }

            lf.Add(datas[17] * iRate * vRate * 0.0001);//总无功功率
            for (int i = 0; i < 3; i++)//无功功率
            {
                lf.Add(datas[i + 14] * iRate * vRate * 0.0001);
            }
            //

            while (lf.Count < dataLen)
                lf.Add(0);
            return string.Join(",", lf.ToArray());
        }

        /// <summary>
        /// 正泰数值 计算为写入的格式
        /// </summary>
        /// <param name="strData"></param>
        public string ToValueString(string strData)
        {
            List<double> ld = new List<double>();
            for (int i = 0; i * 8 < strData.Length; i++)
            {
                double d = DataConversion.HexStringToFloat(strData.Substring(i * 8, 8));
                if (i < 6)//电压
                {
                    d = d * vRate * 0.1;
                }
                else if (i < 9)//电流
                {
                    d = d * iRate * 0.001;
                }
                else if (i >= 17)//合向电能
                {
                    d = d * vRate * iRate;
                }
                else //功率
                {
                    d = d * vRate * iRate * 0.1;
                }
                ld.Add(d);
            }
            while (ld.Count < dataLen)
                ld.Add(0);
            return string.Join(",", ld.ToArray());
        }
        /// <summary>
        /// 打印信息
        /// </summary>
        /// <returns></returns>
        public string PrintString(string value, string name)
        {
            string[] values = value.Split(',');
            string[] names = name.Split(',');
            List<string> ls = new List<string>();
            for (int i = 0; i < values.Length && i < names.Length; i++)
            {
                ls.Add($"{values[i]}({names[i].Trim()})");
            }
            return string.Join(",", ls.ToArray());
        }
        /// <summary>
        /// 打印信息
        /// </summary>
        /// <returns></returns>
        public string PrintString(string value, string[] names)
        {
            string[] values = value.Split(',');
            List<string> ls = new List<string>();
            for (int i = 0; i < values.Length && i < names.Length; i++)
            {
                ls.Add($"{values[i]}({names[i].Trim()})");
            }
            return string.Join(",", ls.ToArray());
        }
    }
}
