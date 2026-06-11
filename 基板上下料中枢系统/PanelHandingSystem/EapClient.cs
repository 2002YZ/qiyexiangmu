using Log;
using PanelHandingSystem;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using static PanelHandingSystem.Com2Mes;
using static System.Net.WebRequestMethods;

namespace Esb
{
    /// <summary>
    /// 设备方调用 EAP 的接口封装类
    /// </summary>
    public class EapClient
    {
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        // 心跳url（正式环境）
        private const string DeviceHeartbeatUrl = "http://10.3.46.90:5012/dataService/webapi/EQP/DeviceHeartbeat";
      
        // 状态上报url（正式环境）
       private const string DeviceStatusReportUrl = "http://10.3.46.90:5012/dataService/webapi/EQP/DeviceStatusReport";
       // private const string DeviceStatusReportUrl = "http://httpbin.org/post";

        // 清线完成上报url（正式环境）
        private const string EqpClearFinishUrl = "http://10.3.46.90:7667/dataService/webapi/EQP/EqpClearFinish";
     

        // 设备主动请求任务url（正式环境）
        private const string AskDownTaskEventUrl = "http://10.3.46.90:7667/dataService/webapi/EQP/askDownTaskEvent";
        //private const string AskDownTaskEventUrl = "http://httpbin.org/post";
       

        // 设备异常/报警上报url（正式环境）
         private const string DeviceAlarmReportUrl = "http://10.3.46.90:5012/dataService/webapi/EQP/DeviceAlarmReport";
      

        // 设备用水用电上报url（正式环境）
          private const string DeviceDataReportUrl = "http://10.3.46.90:5012/dataService/webapi/EQP/DeviceDataReport";
        //private const string DeviceDataReportUrl = "http://httpbin.org/post";

        // 【新增】仿真模式开关：设置为 true 时，不发送真实网络请求，直接返回成功  现场必须是false
        public static bool IsSimulationMode = false;


        //心跳参数初始化
        string Puls = "0";
        //3.超时NG状态
        int outTimeStatus = 10;
        string outTime_reason = "操作超时";

        //从全局参数获取设备编号
        private string GetMachineCode()
        {
            if (ParaSet.recipe != null && !string.IsNullOrEmpty(ParaSet.recipe.myMachinecode))
            {
                return ParaSet.recipe.myMachinecode;
            }
            return "F2_IL01L_TEST_001";
        }
        // 获取线体编号
        private string GetLineCode()
        {
           if (ParaSet.recipe != null && !string.IsNullOrEmpty(ParaSet.recipe.myLinecode))
            {
                return ParaSet.recipe.myLinecode;
            }
            return "F2_LM02L";
        }


        //3.解析JSON状态
        int anaStatus = 20;
        /// <summary>
        /// 5.1 设备心跳接口 (设备→EAP)
        /// </summary>
        /// <param name="outTime">超时时间，单位为ms
        /// <returns>EAP 返回的原始字符串</returns>
        public Response DeviceHeartbeat(int outTime)
        {

            string machineCode = GetMachineCode();
            string puls = Puls;
            Puls = puls == "0" ? "1" : "0";
            string createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string jsonBody = "[{" +
                              "\"machine_code\":\"" + EscapeJson(machineCode) + "\"," +
                              "\"Puls\":\"" + EscapeJson(puls) + "\"," +
                              "\"create_time\":\"" + EscapeJson(createTime) + "\"" +
                              "}]";

            string result = PostJson(DeviceHeartbeatUrl, jsonBody, outTime);
            Response resp = new Response();
            if (result == "操作超时")
            {
                resp.status = outTimeStatus;
                resp.msg = outTime_reason;
            }
            else
            {
                try
                {
                    resp = _serializer.Deserialize<Response>(result);
                }
                catch (Exception ex)
                {
                    resp.status = anaStatus;
                    resp.msg = ex.Message;
    
                }
            }
            return resp;
        }

        /// <summary>
        /// 5.2 状态上报接口 (设备→EAP)
        /// </summary>
        /// <param name="statusCode">状态编码：1运行，2待机，3故障，4保养...</param>
        /// <param name="statusName">状态名称：运行、待机、故障、保养...</param>
        /// /// <param name="outTime">超时时间，单位为ms
        /// <returns>EAP 返回的原始字符串</returns>
        public Response DeviceStatusReport(string statusCode, string statusName, int outTime)
        {
            string machineCode = GetMachineCode();
            string createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string jsonBody = "[{" +
                              "\"machine_code\":\"" + EscapeJson(machineCode) + "\"," +
                              "\"status_code\":\"" + EscapeJson(statusCode) + "\"," +
                              "\"status_name\":\"" + EscapeJson(statusName) + "\"," +
                              "\"create_time\":\"" + EscapeJson(createTime) + "\"" +
                              "}]";

            string result = PostJson(DeviceStatusReportUrl, jsonBody, outTime);
            Response resp = new Response();
            if (result == "操作超时")
            {
                resp.status = outTimeStatus;
                resp.msg = outTime_reason;
            }
            else
            {
                try
                {
                    resp = _serializer.Deserialize<Response>(result);
                }
                catch (Exception ex)
                {
                    resp.status = anaStatus;
                    resp.msg = ex.Message;

                }
            }
            return resp;
        }

        /// <summary>
        /// 3. 清线完成（设备方提供的接口形态）(EAP→设备)
        /// 在控制台程序中无法真正对外开放 HTTP 服务，
        /// 这里定义一个业务处理方法，供将来 Web 接口调用。
        /// </summary>
        public Response EqpClearNotify(EqpClearNotifyRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            // 此处可以加入真正的业务处理逻辑，例如记录日志、更新状态等
            // 当前示例中简单返回成功
            Response resp = new Response();
            resp.status = 0;
            resp.msg = "成功";
            return resp;
        }

        /// <summary>
        /// 清线完成（设备提供接口）重载：从 JSON 字符串解析请求
        /// </summary>
        public Response EqpClearNotify(string jsonBody)
        {
            EqpClearNotifyRequest req = _serializer.Deserialize<EqpClearNotifyRequest>(jsonBody);
            return EqpClearNotify(req);
        }

        /// <summary>
        /// 5.6 清线完成上报接口 (设备→EAP)
        /// <param name="funcCode">功能名</param>
        /// </summary>
        public Response EqpClearFinish(string funcCode, int outTime)
        {
            string machineCode = GetMachineCode();
            // 生成一个新的 GUID
            string requestUuid = Guid.NewGuid().ToString();
            string requestTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string jsonBody = "[{" +
                              "\"request_uuid\":\"" + EscapeJson(requestUuid) + "\"," +
                              "\"request_time\":\"" + EscapeJson(requestTime) + "\"," +
                              "\"line_code\":\"" + EscapeJson(machineCode) + "\"," +
                              "\"machine_code\":\"" + EscapeJson(machineCode) + "\"," +
                              "\"func_code\":\"" + EscapeJson(funcCode) + "\"," +
                              "}]";
            string result = PostJson(EqpClearFinishUrl, jsonBody, outTime);
            Response resp = new Response();
            if (result == "操作超时")
            {
                resp.status = outTimeStatus;
                resp.msg = outTime_reason;
            }
            else
            {
                try
                {
                    resp = _serializer.Deserialize<Response>(result);
                }
                catch (Exception ex)
                {
                    resp.status = anaStatus;
                    resp.msg = ex.Message;

                }
            }
            return resp;
        }

        /// <summary>
        /// 5.7 异常上报/设备报警信息上报接口 (设备→EAP)
        /// <param name="alarm_code">报警编号</param>
        /// <param name="alarm_des">报警信息</param>
        /// <param name="alarm_level">报警等级:1.低，2.中，3.高</param>
        /// <param name="alarm_staus">报警状态:1.发生，0.消除</param>
        /// </summary>
        public Response DeviceAlarmReport(string alarm_code, string alarm_des, int alarm_level, int alarm_staus, int outTime)
        {
            string machineCode = GetMachineCode();
            string createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string jsonBody = "[{" +
                              "\"machine_code\":\"" + EscapeJson(machineCode) + "\"," +
                              "\"alarm_code\":\"" + EscapeJson(alarm_code) + "\"," +
                              "\"alarm_des\":\"" + EscapeJson(alarm_des) + "\"," +
                              "\"alarm_level\":" + alarm_level + "," +
                              "\"alarm_staus\":" + alarm_staus + "," +
                              "\"create_time\":\"" + EscapeJson(createTime) + "\"" +
                              "}]";

            string result = PostJson(DeviceAlarmReportUrl, jsonBody, outTime);
            Response resp = new Response();
            if (result == "操作超时")
            {
                resp.status = outTimeStatus;
                resp.msg = outTime_reason;
            }
            else
            {
                try
                {
                    //resp = _serializer.Deserialize<Response>(result);

                    // 【新增逻辑】如果是测试环境(httpbin)，强制视为成功
                    if (DeviceHeartbeatUrl.Contains("httpbin.org"))
                    {
                        resp.status = 0;
                        resp.msg = "测试成功：Wireshark已捕获";
                    }
                    else
                    {
                        // 正式环境才真正解析
                        resp = _serializer.Deserialize<Response>(result);
                    }
                }
                catch (Exception ex)
                {
                    // 如果解析失败，但我们确实收到了数据(result不为空)，也可以视为测试通过
                    if (!string.IsNullOrEmpty(result) && DeviceHeartbeatUrl.Contains("baidu"))
                    {
                        resp.status = 0;
                       // resp.msg = "百度连通成功(虽然返回的是HTML)";
                    }
                    else
                    {
                        resp.status = anaStatus;
                        resp.msg = ex.Message;
                    }

                }
            }
            return resp;
        }

        /// <summary>
        /// 5.3 设备主动请求配方/任务 (设备→EAP)
        /// 调用 askDownTaskEvent，返回解析后的 WorkOrder。
        /// </summary>
        /// <param name="machineType">设备类型：FB:放板机/SB：收板机/DJ：单机/NA：其他，必填</param>
        /// <param name="lotNum">批次号（单机/中间设备可以必填），可为空</param>
        /// <param name="palletNum">BOOK 码，必填</param>
        public  OutOrder AskDownTaskEvent(string machineType, string lotNum, string palletNum, int outTime)
        {
            string lineCode = GetLineCode();
            OutOrder resp = new OutOrder();

            // =============== 本地测试开关 =================
            bool isLocalTest = false; // 正式为 false

            if (isLocalTest)
            {
                // 模拟一个成功的返回，不走网络，直接返回假工单
                resp.status = 0;
                resp.msg = "本地测试成功";
                resp.data = new WorkOrder();
                resp.data.lot_num = "TEST-2025-001"; // 假批次
                resp.data.material_code = "MAT-888";
                resp.data.pnlqty = 10;
                resp.data.pallet_length = 500; // 假尺寸
                resp.data.pallet_width = 400;
                resp.data.pallet_thickness = 1.5f;
                // 假条码
                resp.data.CodeList.Add(palletNum); // 把你扫的码加进去
                resp.data.CodeList.Add("TEST_CODE_002");

                // 模拟网络延时
                System.Threading.Thread.Sleep(500);

                return resp; // 直接返回，不往下走了
            }

            string machineCode = GetMachineCode();
            string requestUuid = Guid.NewGuid().ToString();
            string requestTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string jsonBody = "[{" +
                              "\"request_uuid\":\"" + EscapeJson(requestUuid) + "\"," +
                              "\"request_time\":\"" + EscapeJson(requestTime) + "\"," +
                              "\"line_code\":\"" + EscapeJson(lineCode) + "\"," +
                              "\"machine_code\":\"" + EscapeJson(machineCode) + "\"," +
                              "\"machine_type\":\"" + EscapeJson(machineType) + "\"," +
                              "\"lot_num\":\"" + EscapeJson(lotNum) + "\"," +
                              "\"pallet_num\":\"" + EscapeJson(palletNum) + "\"" +
                              "}]";
          //原来   OutOrder resp = new OutOrder();
            string result = PostJson(AskDownTaskEventUrl, jsonBody, outTime);
            if (result == "操作超时")
            {
                resp.status = outTimeStatus;
                resp.msg = outTime_reason;
                resp.data = null;
            }
            else
            {
                try
                {
                    AskDownTaskEventResponse dataOrder = _serializer.Deserialize<AskDownTaskEventResponse>(result);
                    WorkOrder workData = ConvertTaskInfoToWorkOrder(dataOrder.data.task_info);
                    resp.status= dataOrder.status;
                    resp.msg= dataOrder.msg;
                    resp.data= workData;
                }
                catch (Exception ex)
                {

                    resp.status = anaStatus;
                    resp.msg = ex.Message;
                    resp.data = null;
                }
            }
            return resp;
        }

        /// <summary>
        /// 5.4 EAP 主动下发配方/任务 (EAP→设备)
        /// 设备侧业务方法：把 Downrecipe 请求映射为 WorkOrder。
        /// 一般用于在 Web 接口中接收到 JSON 后，反序列化为 DownrecipeRequest，再调用本方法。
        /// </summary>
        public PanelHandingSystem.Com2Mes.WorkOrder Downrecipe(DownrecipeRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (request.data == null || request.data.task_info == null)
            {
                throw new Exception("Downrecipe 请求中 task_info 为空");
            }

            return ConvertTaskInfoToWorkOrder(request.data.task_info);
        }

        /// <summary>
        /// 便捷方法：直接从 Downrecipe 的 JSON 字符串转换为 WorkOrder。
        /// （用于设备提供的 HTTP 接口中处理原始报文）
        /// </summary>
        public WorkOrder Downrecipe(string jsonBody)
        {
            DownrecipeRequest req = _serializer.Deserialize<DownrecipeRequest>(jsonBody);
            return Downrecipe(req);
        }

        /// <summary>
        /// 5.5 清线通知接口  EAP----设备
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Response EqpClearStartNotify(EqpClearNotifyRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                LogRecord.addLog($"[EapClient] 收到清线通知: line_code={request.line_code}, machine_code={request.machine_code}, func_code={request.func_code}");

                // 返回成功响应
                Response resp = new Response
                {
                    status = 0,
                    msg = "成功接收清线通知"
                };
                return resp;
            }
            catch (Exception ex)
            {
                LogRecord.addLog($"[EapClient] 处理清线通知异常: {ex.Message}");
                return new Response
                {
                    status = 1,
                    msg = $"处理异常: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 5.5清线通知接口 JSon字符串重载
        /// </summary>
        /// <param name="jsonBody"></param>
        /// <returns></returns>
        public Response EqpClearStartNotify(string jsonBody)
        {
            try
            {
                EqpClearNotifyRequest req = _serializer.Deserialize<EqpClearNotifyRequest>(jsonBody);
                return EqpClearNotify(req);
            }
            catch (Exception ex)
            {
                LogRecord.addLog($"[EapClient] EqpClearNotify 解析失败: {ex.Message}");
                return new Response
                {
                    status = 1,
                    msg = $"JSON解析异常: {ex.Message}"
                };
            }
        }


        /// <summary>
        /// 将接口中的 task_info 结构转换为本地 WorkOrder 模型
        /// </summary>
        private WorkOrder ConvertTaskInfoToWorkOrder(TaskInfo info)
        {
            if (info == null)
            {
                return null;
            }

            WorkOrder wo = new WorkOrder();
            wo.lot_num = info.lot_num ?? string.Empty;
            wo.material_code = info.material_code ?? string.Empty;
            wo.pnlqty = info.pnlqty;

            float value;
            if (float.TryParse(info.pnllength, out value)) wo.pallet_length = value;
            if (float.TryParse(info.pnlwidth, out value)) wo.pallet_width = value;
            if (float.TryParse(info.pnlthickness, out value)) wo.pallet_thickness = value;

            // 展开条码列表到 CodeList
            if (info.codelist != null)
            {
                foreach (CodeListItem sideItem in info.codelist)
                {
                    if (sideItem.pnlist == null) continue;
                    foreach (PnlItem pnl in sideItem.pnlist)
                    {
                        if (pnl.setlist == null) continue;
                        foreach (SetItem set in pnl.setlist)
                        {
                            if (!string.IsNullOrEmpty(set.setcode) && !wo.CodeList.Contains(set.setcode))
                            {
                                wo.CodeList.Add(set.setcode);
                            }
                        }
                    }
                }
            }

            // 其它字段（重量、陪镀板等）可根据后续实际数据源再填充

            return wo;
        }

        /// <summary>
        /// 5.8 用电量/用水量上报接口 (设备→EAP)
        /// 方法入参：water_used，di_water_used，electricity_used
        /// </summary>
        public Response DeviceDataReport(string water_used, string di_water_used, string electricity_used, int outTime)
        {

            string machineCode = GetMachineCode();
            string createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string jsonBody = "[{" +
                              "\"machine_code\":\"" + EscapeJson(machineCode) + "\"," +
                              "\"water_used\":\"" + EscapeJson(water_used) + "\"," +
                              "\"di_water_used\":\"" + EscapeJson(di_water_used) + "\"," +
                              "\"electricity_used\":\"" + EscapeJson(electricity_used) + "\"," +
                              "\"create_time\":\"" + EscapeJson(createTime) + "\"" +
                              "}]";

            string result = PostJson(DeviceDataReportUrl, jsonBody, outTime);
            Response resp = new Response();
            if (result == "操作超时")
            {
                resp.status = 2;
                resp.msg = "操作超时";
            }
            else
            {
                resp = _serializer.Deserialize<Response>(result);
            }
            return resp;
        }

        /// <summary>
        /// 发送 POST JSON 请求的通用方法（同步）
        /// </summary>
        private string PostJson(string url, string jsonBody, int TimeOut)
        {

            // ================= 【新增代码开始】 =================
            // 如果开启了仿真模式，或者 URL 是百度的测试地址，直接返回模拟的成功 JSON
           if (IsSimulationMode)
            {
                System.Threading.Thread.Sleep(500);
                Console.WriteLine($"[模拟模式] 向 {url} 发送数据: {jsonBody}");
                return "{\"status\": 0, \"msg\": \"模拟环境：接口调用成功\"}";
            }
            // ================= 【新增代码结束】 =================
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Timeout = TimeOut;
            request.ContentType = "application/json";
            byte[] data = Encoding.UTF8.GetBytes(jsonBody);
            request.ContentLength = data.Length;
            try
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(data, 0, data.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream respStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(respStream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                //if (ex.Message == "操作超时") { return ex.Message; }
                LogRecordVision.addLog("POST出错" + ex.Message.Trim());
                return "";
                //   throw;
            }


        }

        /// <summary>
        /// 简单 JSON 字符串转义（只处理必要字符）
        /// </summary>
        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\r", "\\r")
                        .Replace("\n", "\\n");
        }

       
    }

    /// <summary>
    /// 清线完成（设备提供接口）请求模型
    /// </summary>
    public class EqpClearNotifyRequest
    {
        public string request_uuid { get; set; }
        public string request_time { get; set; }
        public string line_code { get; set; }
        public string machine_code { get; set; }
        public string func_code { get; set; } // 固定为 "Clear"
    }

    /// <summary>
    /// 接口调用结果
    /// </summary>
    public class Response
    {
        public int status { get; set; } // 0：成功
        public string msg { get; set; }
    }

    #region 任务/配方相关模型（用于 5.3 / 5.4）

    
    /// <summary>
    /// askDownTaskEvent 返回结构
    /// </summary>
    public class AskDownTaskEventResponse
    {
        public int status { get; set; }
        public string msg { get; set; }
        public AskDownTaskEventData data { get; set; }
    }

    public class AskDownTaskEventData
    {
        public TaskInfo task_info { get; set; }
    }

    /// <summary>
    /// Downrecipe 请求结构（EAP→设备）
    /// </summary>
    public class DownrecipeRequest
    {
        public string request_uuid { get; set; }
        public string request_time { get; set; }
        public string line_code { get; set; }
        public string machine_code { get; set; }
        public DownrecipeData data { get; set; }
    }

    public class DownrecipeData
    {
        public TaskInfo task_info { get; set; }
    }

    /// <summary>
    /// 任务信息，与接口中的 task_info 对应
    /// </summary>
    public class TaskInfo
    {
        public string lot_num { get; set; }   //批次
        public string material_code { get; set; }  // 料号
        public int pnlqty { get; set; }
        public int set_count { get; set; }
        public int pcs_count { get; set; }

        // pnllength/pnlwidth/pnlthickness 在示例中是字符串，这里先按字符串接收，再在转换时解析为 float
        public string pnllength { get; set; }
        public string pnlwidth { get; set; }
        public string pnlthickness { get; set; }

        public System.Collections.Generic.List<CodeListItem> codelist { get; set; }
        public System.Collections.Generic.List<MiItem> mi { get; set; }
    }

    public class CodeListItem
    {
        public string side { get; set; }              // T/B
        public System.Collections.Generic.List<PnlItem> pnlist { get; set; }
    }

    public class PnlItem
    {
        public string pnlcode { get; set; }
        public System.Collections.Generic.List<SetItem> setlist { get; set; }
    }

    public class SetItem
    {
        public string setcode { get; set; }
    }

    public class MiItem
    {
        public string item_name { get; set; }
        public string item_value { get; set; }
        public string item_type { get; set; }   // 0=路径，1=参数
        public string recipe { get; set; }
        public string machine_type { get; set; }
    }

    #endregion
}


