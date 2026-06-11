using Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PanelHandingSystem
{


    //力控通讯
    public static class Com2ForceControl
    {

        private static TcpClient _tcpClient1;
        private static NetworkStream _stream1;
        public static bool _isConnected1;
        public static string _ip1;
        public static int _port1;
        public static string ForceData1 = "";

        private static TcpClient _tcpClient2;
        private static NetworkStream _stream2;
        public static bool _isConnected2;
        public static string _ip2;
        public static int _port2;
        public static string ForceData2 = "";


        //超时时间设置
        public static int timeout = 3000;//单位ms

        public static bool Error = false;



        public static bool Listeningrunning1 = true;
        public static bool Listeningrunning2 = true;
        public static void Connect()
        {

            

            Task.Run(async () =>
            {
                try
                {

                    _ip1 = ParaSet.recipe.ForceIp1;
                    _port1 = ParaSet.recipe.ForcePort1;

                    _tcpClient1 = new TcpClient();
                    _tcpClient1.Connect(_ip1, _port1);

                    if (_tcpClient1.Connected)
                    {
                        Listeningrunning1 = true;
                        _isConnected1 = true;
                        _stream1 = _tcpClient1.GetStream();
                        LogRecordVision.addLog("连接力控," + _ip1 + ":" + _port1.ToString());
                        Thread receiveThread = new Thread(new ThreadStart(ReceiveData1));
                        receiveThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    LogRecordVision.addLog("连接力控1-左出错:" + ex.Message);
                }

            });



           


            Task.Run(async () => {
                try
                {
                    Listeningrunning2 = true;
                    _ip2 = ParaSet.recipe.ForceIp2;
                    _port2 = ParaSet.recipe.ForcePort2;

                    _tcpClient2 = new TcpClient();
                    _tcpClient2.Connect(_ip2, _port2);

                    if (_tcpClient2.Connected)
                    {
                        _isConnected2 = true;
                        _stream2 = _tcpClient2.GetStream();
                        LogRecordVision.addLog("连接力控," + _ip2 + ":" + _port2.ToString());
                        Thread receiveThread = new Thread(new ThreadStart(ReceiveData2));
                        receiveThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    LogRecordVision.addLog("连接力控2-右出错:" + ex.Message);
                }

            });



        }

        private static void ReceiveData2()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (Listeningrunning2)
                {
                    int bytesRead = _stream2.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                      //  LogRecordVision.addLog("力控2数据: " + message);

                        string strRecieve = message;

                        //处理接收到的信息
                        try
                        {

                            string ResT = ParseSensorData(strRecieve);
                            ForceData2 = ResT;
                            //if (!ResT.Contains("不正确"))
                            //    LogRecordVision.addLog("力控2数据解析: " + ResT);


                        }
                        catch (Exception ex)
                        {
                          //  LogRecordVision.addLog("从Vision接收数据处理出错" + ex.Message);
                        }



                    }
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
             //   LogRecordVision.addLog("Error receiving data: " + ex.Message);

            }
            finally
            {
                _stream2.Close();
                _tcpClient2.Close();
            }
        }

        private static void ReceiveData1()
        {
            byte[] buffer = new byte[512];
            try
            {
                while (Listeningrunning1)
                {
                    int bytesRead = _stream1.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        //LogRecordVision.addLog("力控1数据: " + message);

                        string strRecieve = message;

                        //处理接收到的信息
                        try
                        {

                            string ResT  = ParseSensorData(strRecieve);
                            ForceData1 = ResT;

                            //if (!ResT.Contains("不正确"))
                            //    LogRecordVision.addLog("力控1数据解析: " + ResT);
                        }
                        catch (Exception ex)
                        {
                           // LogRecordVision.addLog("从Vision接收数据处理出错" + ex.Message);
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
                _stream1.Close();
                _tcpClient1.Close();
            }





        }



        /// <summary>
        /// 解析传感器数据，只处理第一条有效数据
        /// </summary>
        /// <param name="rawData">原始字符串数据</param>
        /// <returns>格式化后的字符串，6个数值以逗号分隔</returns>
        public static string ParseSensorData(string rawData)
        {
            if (string.IsNullOrEmpty(rawData))
                return string.Empty;

            // 查找第一条以RD开头，以END结尾的数据
            var pattern = @"RD\d+\s+SSSSSS\s+\d+\s+N\d+\s+([\+\-]\d{6})+END";
            var match = Regex.Match(rawData, @"RD[^E]+\d{6}END");

            if (!match.Success)
            {
                // 尝试更灵活的匹配
                match = Regex.Match(rawData, @"RD.*?END");
            }

            if (!match.Success)
                return string.Empty;

            string dataBlock = match.Value;

            // 提取数字部分
            var numberPattern = @"[\+\-]\d{6}";
            var numberMatches = Regex.Matches(dataBlock, numberPattern);

            if (numberMatches.Count < 6)
                return string.Empty;

            // 解析并格式化数字
            List<string> results = new List<string>();

            for (int i = 0; i < 6; i++)
            {
                string numStr = numberMatches[i].Value;
                bool isNegative = numStr[0] == '-';
                string absValueStr = numStr.Substring(1);

                if (!int.TryParse(absValueStr, out int intValue))
                    return string.Empty;

                // 前2个数字是整数，后4个数字需要除以100
                if (i >= 2)
                {
                    // 后4个数字：整数部分除以100
                    double doubleValue = intValue / 100.0;
                    if (isNegative)
                        doubleValue = -doubleValue;

                    // 格式化为2位小数
                    results.Add(doubleValue.ToString("F2"));
                }
                else
                {
                    // 前2个数字：保持整数
                    int finalValue = isNegative ? -intValue : intValue;
                    results.Add(finalValue.ToString());
                }
            }

            return string.Join(",", results);
        }

        /// <summary>
        /// 解析传感器数据，处理可能的多条数据，但只返回第一条
        /// </summary>
        /// <param name="rawData">可能包含多条数据的原始字符串</param>
        /// <returns>第一条数据的格式化字符串</returns>
        public static string ParseSensorDataWithMultiple(string rawData)
        {
            if (string.IsNullOrEmpty(rawData))
                return string.Empty;

            // 用正则表达式匹配所有以RD开头，以END结尾的数据块
            var matches = Regex.Matches(rawData, @"RD.*?END");

            if (matches.Count == 0)
                return string.Empty;

            // 只处理第一条数据
            return ParseSensorData(matches[0].Value);
        }


        public static string ParseSensorDataExact(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
            {
                return string.Empty;
            }

            try
            {
                // 分割字符串
                var parts = rawData.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 5)
                {
                    throw new FormatException("数据格式不正确");
                }

                // 第四个部分应该包含我们需要的数据
                var dataPart = parts[3];

                // 检查数据格式
                if (dataPart.Length != 42)  // 6个数字 * 7个字符
                {
                    throw new FormatException($"数据长度不正确，应为42个字符，实际为{dataPart.Length}");
                }

                var results = new List<string>();

                // 解析6个7位数字
                for (int i = 0; i < 6; i++)
                {
                    var numStr = dataPart.Substring(i * 7, 7);

                    // 第一个字符是符号位
                    char sign = numStr[0];
                    string magnitudeStr = numStr.Substring(1);

                    // 解析数字
                    if (int.TryParse(magnitudeStr, out int magnitude))
                    {
                        double value = magnitude;

                        // 应用符号
                        if (sign == '-')
                        {
                            value = -value;
                        }

                        // 根据你的示例，所有数字都除以100
                        double resultValue = value / 100.0;

                        // 根据你的输出示例格式，去掉多余的0
                        if (resultValue == Math.Floor(resultValue))
                        {
                            results.Add(resultValue.ToString("F0"));
                        }
                        else
                        {
                            results.Add(resultValue.ToString("F2"));
                        }
                    }
                    else
                    {
                        throw new FormatException($"无法解析数字部分: {magnitudeStr}");
                    }
                }

                return string.Join("，", results);
            }
            catch (Exception ex)
            {
                return $"解析错误: {ex.Message}";
            }
        }






    }
}
