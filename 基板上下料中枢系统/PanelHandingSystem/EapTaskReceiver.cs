using Esb;
using Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using static PanelHandingSystem.Com2Mes;

namespace PanelHandingSystem
{
    /// <summary>
    /// HTTP监听服务端
    /// </summary>
    public class EapTaskReceiver
    {
        private HttpListener _listener = null;
        private Thread _listenerThread = null;
        private bool _isRunning = false;
        private readonly int _port = 8080; // 监听端口 
        private readonly string _path = "/Downrecipe"; // 接收任务的URL路径
        private EapClient _eapClient = null; // 调Downrecipe方法
        private Action<Com2Mes.WorkOrder> _onTaskReceived; //  回调


        /// <summary>
        /// 构造函数 
        /// </summary>
        /// <param name="port">监听接口</param>
        /// <param name="onTaskReceived"></param>
        public EapTaskReceiver(int port, Action<Com2Mes.WorkOrder> onTaskReceived)
        {
            _port = port;
            _onTaskReceived = onTaskReceived;
            _eapClient = new EapClient(); //
        }


        public bool StartHttpServer()
        {
            if (_isRunning)
            {
                LogRecord.addLog("EAP任务接收服务已在运行中.");
                return true;
            }
            try
            {
                _listener = new HttpListener();
                // 注意：urlacl权限。需要管理员运行一次程序，或执行netsh命令授权。
                _listener.Prefixes.Add($"http://+:{_port}{_path}");
                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenerWorker);
                _listenerThread.IsBackground = true; // 设置为后台线程
                _listenerThread.Start();

                LogRecord.addLog($"EAP任务接收服务启动成功，监听地址: http://+:{_port}{_path}/");
                LogRecord.addLog($"EAP可通过POST请求此地址主动下发任务。");
                return true;
            }
            catch (HttpListenerException ex)
            {

                _isRunning = false;
                LogRecord.addLog($"启动EAP任务接收服务失败: {ex.Message}");
                LogRecord.addLog("提示: 可能需要以管理员身份运行一次程序，或执行以下命令授权：");
                LogRecord.addLog($"netsh http add urlacl url=http://+:{_port}{_path}/ user=Everyone");
                return false;
            }
            
        }

        /// <summary>
        /// 监听工作现场循环
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void ListenerWorker()
        {
            while (_listener != null && _listener.IsListening)
            {
                try
                {
                    IAsyncResult result = _listener.BeginGetContext(HandleRequestCallback, _listener);
                    result.AsyncWaitHandle.WaitOne();
                }
                catch (Exception ex)
                {

                    if (_isRunning)
                    {
                        LogRecord.addLog($"EAP任务接收服务监听循环异常：{ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 处理HTTP请求的回调
        /// </summary>
        /// <param name="result"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void HandleRequestCallback(IAsyncResult result)
        {
            if (!_isRunning || _listener == null) return;
            HttpListenerContext context=null;
            try
            {
                context = _listener.EndGetContext(result);
            }
            catch (Exception ex)
            {

                LogRecord.addLog($"EAP任务接收服务获取请求上下文失败: {ex.Message}");
                return;
            }

            //放到线程池
            ThreadPool.QueueUserWorkItem(state =>
                {
                    ProcessHttpRequest(context); 
                }
            );
        }
        
        /// <summary>
        /// 处理HHTP请求
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ProcessHttpRequest(HttpListenerContext context)
        {
            string requestBody = "";
            string responseJson = "{\"status\":1, \"msg\":\"处理失败\"}";

            try
            {
                //  读取请求体
                if (context.Request.HttpMethod.ToUpper() != "POST")
                {
                    context.Response.StatusCode = 405; // Method Not Allowed
                    responseJson = "{\"status\":1, \"msg\":\"只支持POST方法\"}";
                    SendResponse(context, responseJson);
                    return;
                }
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    requestBody = reader.ReadToEnd();
                }
                LogRecord.addLog($"[EAP任务接收] 收到POST请求，Body长度: {requestBody.Length}");
                //接受内容
                LogRecord.addLog($"[EAP任务接收] Body内容: {requestBody}");

                //根据URL路径判断接口类型  5.4  和  5.5
                string path = context.Request.Url.AbsolutePath.TrimEnd('/');
                if (path.EndsWith("/Downrecipe\", StringComparison.OrdinalIgnoreCase"))
                {
                    //5.4  主动下发配方 EAP--设备
                    Com2Mes.WorkOrder receivedWorkOrder = _eapClient.Downrecipe(requestBody);

                    if (receivedWorkOrder != null)
                    {
                        LogRecord.addLog($"[EAP任务接收] 成功解析任务 -> 工单号: {receivedWorkOrder.lot_num}, 数量: {receivedWorkOrder.pnlqty}");

                        //通知主程序
                        if (_onTaskReceived != null)
                        {
                            _onTaskReceived(receivedWorkOrder);
                        }

                        //返回成功相应给EAP
                        responseJson = "{\"status\":0, \"msg\":\"成功接收并处理工单\"}";

                    }
                    else
                    {
                        responseJson = "{\"status\":1, \"msg\":\"解析任务数据失败，WorkOrder为null\"}";
                    }
                }
                else if (path.EndsWith("/EqpClearNotify", StringComparison.OrdinalIgnoreCase))
                {
                    // 5.5 清线通知接口   EAP--设备
                    LogRecord.addLog("[EAP任务接收] 收到清线通知请求");

                    //
                    Response resp = _eapClient.EqpClearNotify(requestBody);
                    if (resp != null)
                    {
                        responseJson = $"{{\"status\":{resp.status}, \"msg\":\"{EscapeJson(resp.msg)}\"}}";
                       // LogRecord.addLog($"[EAP任务接收] 处理清线通知结果: status={resp.status}, msg={resp.msg}");

                        if (resp.status==0)
                        {
                            LogRecord.addLog($"[EAP任务接收] 处理清线通知结果: status={resp.status}, msg={resp.msg}");
                        }
                        //TOOD --清线逻辑


                        responseJson = "{\"status\":1, \"msg\":\"处理清线通知失败\"}";
                    }

                }

               // Com2Mes.WorkOrder receivedWorkOrder = _eapClient.Downrecipe(requestBody);

               
            }
            catch (Exception ex)
            {

                LogRecord.addLog($"[EAP任务接收] 处理请求时发生异常: {ex.Message}");
                responseJson = $"{{\"status\":1, \"msg\":\"处理异常: {EscapeJson(ex.Message)}\"}}";
            }
            
            //发送相应的请求
            SendResponse(context, responseJson);
        }

        /// <summary>
        /// 发送HTTP响应
        /// </summary>
        /// <param name="context"></param>
        /// <param name="jsonResponse"></param>
        private void SendResponse(HttpListenerContext context, string jsonResponse)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
                context.Response.ContentType = "application/json";
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                LogRecord.addLog($"[EAP任务接收] 发送响应失败: {ex.Message}");
            }
        }


        /// <summary>
        /// 停止HTTP监听服务
        /// </summary>
        public void StopHttpServer()
        {

            if (_isRunning)
            {
                _isRunning = false;
                try
                {
                    if (_listener != null && _listener.IsListening)
                    {
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {

                    LogRecord.addLog($"停止EAP任务接收服务时出错: {ex.Message}");
                }
                finally
                {
                    _listenerThread?.Join(2000); // 等待监听线程退出
                    _listener = null;
                    LogRecord.addLog("EAP任务接收服务已停止。");
                }
            }
        }


       /// <summary>
       /// JSON字符串转义
       /// </summary>
       /// <param name="value"></param>
       /// <returns></returns>
        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\r", "\\r")
                        .Replace("\n", "\\n");
        }








    }
}
