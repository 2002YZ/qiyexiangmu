using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace PanelHandingSystem
{
    /// <summary>
    /// 扫码枪助手类
    /// 用于处理扫码枪输入（模拟键盘输入设备）
    /// </summary>
    public class ScannerHelper : IDisposable
    {
        #region 私有字段

        /// <summary>
        /// 按键时间记录列表
        /// </summary>
        private List<long> _keyTimeList;

        /// <summary>
        /// 当前扫描的条码内容
        /// </summary>
        private StringBuilder _scannedContent;

        /// <summary>
        /// 扫码完成定时器
        /// </summary>
        private System.Threading.Timer _scanCompleteTimer;

        /// <summary>
        /// 是否正在扫描中
        /// </summary>
        private bool _isScanning;

        /// <summary>
        /// 上次按键时间
        /// </summary>
        private long _lastKeyTime;

        /// <summary>
        /// 目标TextBox控件
        /// </summary>
        private TextBox _targetTextBox;

        /// <summary>
        /// 监控的Form
        /// </summary>
        private Form _targetForm;

        #endregion

        #region 公共事件

        /// <summary>
        /// 扫码完成事件
        /// </summary>
        public event Action<string> OnScanCompleted;

        /// <summary>
        /// 扫码进行中事件（实时更新）
        /// </summary>
        public event Action<string> OnScanning;

        #endregion

        #region 配置属性

        /// <summary>
        /// 判定为扫码枪输入的最大按键间隔（毫秒）
        /// 默认50ms，扫码枪输入速度很快，人工输入较慢
        /// </summary>
        public int MaxKeyInterval { get; set; } = 50;

        /// <summary>
        /// 判定为扫码完成的无输入等待时间（毫秒）
        /// 默认100ms，扫码完成后等待100ms无输入则判定为完成
        /// </summary>
        public int ScanCompleteDelay { get; set; } = 100;

        /// <summary>
        /// 最小字符数量（低于此数量不触发扫码完成）
        /// 避免单次按键被误判为扫码
        /// </summary>
        public int MinCharCount { get; set; } = 3;

        /// <summary>
        /// 是否自动清空TextBox（扫码前清空）
        /// </summary>
        public bool AutoClearTextBox { get; set; } = true;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="targetForm">目标Form</param>
        /// <param name="targetTextBox">目标TextBox</param>
        public ScannerHelper(Form targetForm, TextBox targetTextBox)
        {
            _targetForm = targetForm ?? throw new ArgumentNullException(nameof(targetForm));
            _targetTextBox = targetTextBox ?? throw new ArgumentNullException(nameof(targetTextBox));

            _keyTimeList = new List<long>();
            _scannedContent = new StringBuilder();
            _isScanning = false;

            // 创建定时器
            _scanCompleteTimer = new System.Threading.Timer(
                ScanCompleteTimerCallback,
                null,
                System.Threading.Timeout.Infinite,
                System.Threading.Timeout.Infinite);

            // 绑定键盘事件
            AttachKeyboardEvents();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 绑定键盘事件
        /// </summary>
        private void AttachKeyboardEvents()
        {
            // 优先使用ProcessDialogKey，它能捕获大多数键盘事件
            _targetForm.KeyPreview = true;
            _targetForm.KeyPress += TargetForm_KeyPress;
            _targetForm.KeyDown += TargetForm_KeyDown;

            // 同时绑定到TextBox，确保TextBox获得焦点时也能工作
            _targetTextBox.KeyPress += TargetTextBox_KeyPress;
            _targetTextBox.KeyDown += TargetTextBox_KeyDown;
        }

        /// <summary>
        /// 解除键盘事件绑定
        /// </summary>
        private void DetachKeyboardEvents()
        {
            _targetForm.KeyPress -= TargetForm_KeyPress;
            _targetForm.KeyDown -= TargetForm_KeyDown;
            _targetTextBox.KeyPress -= TargetTextBox_KeyPress;
            _targetTextBox.KeyDown -= TargetTextBox_KeyDown;
        }

        /// <summary>
        /// Form的KeyPress事件处理
        /// </summary>
        private void TargetForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            ProcessKeyPress(e.KeyChar);
        }

        /// <summary>
        /// TextBox的KeyPress事件处理
        /// </summary>
        private void TargetTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            ProcessKeyPress(e.KeyChar);
        }

        /// <summary>
        /// Form的KeyDown事件处理（用于检测特殊按键如回车）
        /// </summary>
        private void TargetForm_KeyDown(object sender, KeyEventArgs e)
        {
            ProcessKeyDown(e);
        }

        /// <summary>
        /// TextBox的KeyDown事件处理
        /// </summary>
        private void TargetTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            ProcessKeyDown(e);
        }

        /// <summary>
        /// 处理按键字符
        /// </summary>
        private void ProcessKeyPress(char keyChar)
        {
            // 跳过控制字符（除了退格和回车）
            if (char.IsControl(keyChar) && keyChar != '\b' && keyChar != '\r')
                return;

            long currentTime = Stopwatch.GetTimestamp();
            long currentTimeMs = currentTime / TimeSpan.TicksPerMillisecond;

            // 如果之前没有在扫描中，或者距离上次按键超过MaxKeyInterval
            // 说明可能是新的扫码操作或人工输入
            if (!_isScanning || (currentTimeMs - _lastKeyTime) > MaxKeyInterval)
            {
                // 开始新的扫描
                StartNewScan();
            }

            // 更新最后按键时间
            _lastKeyTime = currentTimeMs;

            // 添加到扫描内容
            if (keyChar == '\r')
            {
                // 回车键，触发完成检查
                _scannedContent.Append('\r');
            }
            else if (keyChar == '\b')
            {
                // 退格键
                if (_scannedContent.Length > 0)
                    _scannedContent.Length--;
            }
            else
            {
                _scannedContent.Append(keyChar);
            }

            // 更新TextBox显示
            UpdateTextBox();

            // 重置完成定时器
            ResetScanCompleteTimer();
        }

        /// <summary>
        /// 处理按键事件（KeyDown）
        /// </summary>
        private void ProcessKeyDown(KeyEventArgs e)
        {
            long currentTimeMs = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;

            if (!_isScanning || (currentTimeMs - _lastKeyTime) > MaxKeyInterval)
            {
                StartNewScan();
            }

            _lastKeyTime = currentTimeMs;

            // 处理特殊功能键
            if (e.KeyCode == Keys.Back)
            {
                if (_scannedContent.Length > 0)
                    _scannedContent.Length--;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                _scannedContent.Append('\r');
            }
            else if (e.KeyCode == Keys.Tab)
            {
                _scannedContent.Append('\t');
            }
            else if (!char.IsControl((char)e.KeyValue))
            {
                // 如果是普通可打印字符，让KeyPress处理
            }

            UpdateTextBox();
            ResetScanCompleteTimer();
        }

        /// <summary>
        /// 开始新的扫描
        /// </summary>
        private void StartNewScan()
        {
            _isScanning = true;
            _scannedContent.Clear();
            _keyTimeList.Clear();

            // 清空TextBox（如果配置允许）
            if (AutoClearTextBox && _targetTextBox != null && !_targetTextBox.IsDisposed)
            {
                _targetTextBox.InvokeIfNeeded(() => _targetTextBox.Text = "");
            }
        }

        /// <summary>
        /// 更新TextBox显示
        /// </summary>
        private void UpdateTextBox()
        {
            if (_targetTextBox != null && !_targetTextBox.IsDisposed)
            {
                string currentContent = _scannedContent.ToString();

                _targetTextBox.InvokeIfNeeded(() =>
                {
                    _targetTextBox.Text = currentContent;
                    _targetTextBox.SelectionStart = _targetTextBox.Text.Length;
                });

                // 触发扫描中事件
                OnScanning?.Invoke(currentContent);
            }
        }

        /// <summary>
        /// 重置扫描完成定时器
        /// </summary>
        private void ResetScanCompleteTimer()
        {
            _scanCompleteTimer.Change(ScanCompleteDelay, System.Threading.Timeout.Infinite);
        }

        /// <summary>
        /// 扫描完成定时器回调
        /// </summary>
        private void ScanCompleteTimerCallback(object state)
        {
            if (_isScanning && _scannedContent.Length >= MinCharCount)
            {
                string scannedCode = _scannedContent.ToString();

                // 移除结尾的回车符（如果有）
                if (scannedCode.EndsWith("\r"))
                    scannedCode = scannedCode.TrimEnd('\r');

                // 触发完成事件
                OnScanCompleted?.Invoke(scannedCode);

                // 重置扫描状态
                _isScanning = false;
            }
            else
            {
                // 内容太短，可能不是有效的扫码
                _isScanning = false;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 手动触发扫码完成
        /// </summary>
        public void ForceComplete()
        {
            if (_isScanning && _scannedContent.Length > 0)
            {
                string scannedCode = _scannedContent.ToString();
                if (scannedCode.EndsWith("\r"))
                    scannedCode = scannedCode.TrimEnd('\r');

                OnScanCompleted?.Invoke(scannedCode);
            }
            _isScanning = false;
        }

        /// <summary>
        /// 清除当前扫描内容
        /// </summary>
        public void Clear()
        {
            _isScanning = false;
            _scannedContent.Clear();
            _keyTimeList.Clear();

            if (_targetTextBox != null && !_targetTextBox.IsDisposed)
            {
                _targetTextBox.InvokeIfNeeded(() => _targetTextBox.Text = "");
            }
        }

        /// <summary>
        /// 暂停扫描（不再处理按键）
        /// </summary>
        public void Pause()
        {
            DetachKeyboardEvents();
        }

        /// <summary>
        /// 恢复扫描
        /// </summary>
        public void Resume()
        {
            AttachKeyboardEvents();
        }

        /// <summary>
        /// 获取当前扫描内容
        /// </summary>
        /// <returns></returns>
        public string GetCurrentContent()
        {
            return _scannedContent.ToString();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            DetachKeyboardEvents();

            _scanCompleteTimer?.Dispose();
            _scanCompleteTimer = null;

            _keyTimeList?.Clear();
            _scannedContent?.Clear();

            _targetTextBox = null;
            _targetForm = null;
        }

        #endregion
    }

    /// <summary>
    /// 控件扩展方法
    /// </summary>
    public static class ControlExtensions
    {
        /// <summary>
        /// 安全地在控件上执行操作（跨线程调用）
        /// </summary>
        public static void InvokeIfNeeded(this Control control, Action action)
        {
            if (control == null || control.IsDisposed)
                return;

            if (control.InvokeRequired)
            {
                try
                {
                    control.Invoke(action);
                }
                catch (ObjectDisposedException)
                {
                    // 控件已释放，忽略
                }
            }
            else
            {
                action();
            }
        }
    }
}