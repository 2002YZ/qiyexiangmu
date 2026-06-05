# 扫码枪+电能表 数据采集系统

[![CSharp](https://img.shields.io/badge/C%23-.NET%20Framework-blue.svg)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Platform-Windows%20Forms-brightgreen.svg)](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)
[![Modbus](https://img.shields.io/badge/Protocol-Modbus-orange.svg)](https://en.wikipedia.org/wiki/Modbus)

## 📋 项目概述

**ScanCheck** 是一个基于 C# WinForms 的工业数据采集应用程序，主要用于**氦检仪（扫码枪）和电能表**的多设备实时数据采集、处理和通讯管理。该项目通过 Modbus TCP/RTU 协议与 PLC 和多个传感器设备进行实时通信。

## ✨ 核心功能

### 1. **扫码枪数据采集**
- **设备数量**：支持 7 个独立的扫码枪设备
- **协议支持**：通过 PLC 进行 Modbus 通讯
- **触发控制**：支持自动触发和手动触发模式
- **数据格式**：字符串格式数据传输
- **通讯延时**：可配置延时参数（默认 800ms）

### 2. **电能表数据采集**
- **设备支持**：5 个���能表设备
- **品牌兼容**：支持两种电能表类型
  - **安科瑞（Acrel）**：起始地址 0x50，读取 18 个寄存器
  - **正泰（Chint）**：起始地址 0x2000，读取 17 个寄存器
- **数据周期**：间隔 5 秒采集一次数据
- **参数采集**：
  - 线电压和相电压
  - 相电流
  - 有功功率和无功功率
  - 电能统计

### 3. **数据处理与转换**
- **电压变比**：可配置电压变比系数
- **电流变比**：可配置电流变比系数
- **数据格式转换**：Hex 字符串转浮点数转换
- **实时计算**：自动计算各类电气参数

### 4. **PLC 通讯管理**
- **设备状态监听**：实时心跳检测（Heartbeat）
- **连接状态反馈**：连接成功/失败状态反馈
- **结果回报**：OK（1）/ NG（2）状态
- **多设备同步**：支持高达 12 个设备的并发管理

## 📁 项目结构

```
扫码枪+电能表/
├── README.md                           # 项目文档
├── ScanCheck.sln                       # Visual Studio 解决方案
└── HeCheck/                            # 主应用程序目录
    ├── Program.cs                      # 应用程序入口
    ├── FormMain.cs                     # 主窗体业务逻辑
    ├── FormMain.Designer.cs            # 主窗体 UI 设计
    ├── FormMain.resx                   # 窗体资源文件
    ├── ScanClass.cs                    # 扫码枪和电能表数据类
    ├── ScanCheck.csproj                # 项目配置文件
    ├── ScanCheck.csproj.user           # 用户配置文件
    ├── App.config                      # 应用配置文件
    ├── functions.ico                   # 应用图标
    ├── 扫码.ico                         # 扫码枪图标
    ├── 氦检仪.ico                       # 氦检仪图标
    ├── Properties/                     # 项目属性
    ├── bin/                            # 编译输出目录
    └── obj/                            # 编译临时文件
```

## 🚀 快速开始

### 系统要求

- **操作系统**：Windows XP 及以上
- **.NET Framework**：.NET Framework 4.0 或更高版本
- **开发工具**：Visual Studio 2019 或更高版本（可选）
- **硬件**：支持 Modbus TCP/RTU 通讯的 PLC

### 安装步骤

1. **克隆或下载项目**
   ```bash
   git clone https://github.com/2002YZ/YOLOV.git
   cd YOLOV/扫码枪+电能表
   ```

2. **打开解决方案**
   
   使用 Visual Studio 打开 `ScanCheck.sln` 文件

3. **编译项目**
   ```
   Build → Build Solution（Ctrl+Shift+B）
   ```

4. **运行应用**
   ```
   Debug → Start Without Debugging（Ctrl+F5）
   ```

5. **配置 PLC 连接**
   
   在应用界面中配置 PLC 的 IP 地址和端口号

## 🔧 配置说明

### 扫码枪配置（ScanClass）

```csharp
// 触发命令
public string strSend = "LON\r";           // 启动扫码
public string strEnd = "LOFF\r";           // 停止扫码

// 通讯延时（毫秒）
public int delay = 800;                    // 扫码间隔

// PLC 变量寄存器地址
triger[i] = $"Reader[{i + 1}].Auto_Triger";        // 触发信号
result[i] = $"Reader[{i + 1}].Result";             // 结果反馈
data[i] = $"Reader[{i + 1}].ReturnData_STRING";    // 扫码数据
beat[i] = $"Reader[{i + 1}].Heartbeat";            // 心跳检测
heConnected[i] = $"Reader[{i + 1}].Connect_succeed"; // 连接状态
```

### 电能表配置（PowerClass）

```csharp
// 电能表品牌选择
public int type = 0;                       // 0: 安科瑞, 1: 正泰

// 地址配置
public int[] startAddr = { 0x50, 0x2000 }; // 读取起始地址
public int[] readCount = { 18, 17 };       // 读取寄存器数量

// 通讯延时
public int delay = 3000;                   // 5 秒采集周期

// 变比系数
public double iRate = 1;                   // 电流变比
public double vRate = 1;                   // 电压变比

// PLC 变量寄存器地址
result[i] = $"Power[{i + 1}].Result";              // 结果反馈
data[i] = $"Power[{i + 1}].ReturnData_Real";      // 数据数组
beat[i] = $"Power[{i + 1}].Heartbeat";            // 心跳检测
heConnected[i] = $"Power[{i + 1}].Connect_succeed"; // 连接状态
```

## 📊 数据格式说明

### 扫码枪数据（ScanClass）

| 变量 | 类型 | 说明 | PLC 对应 |
|------|------|------|---------|
| triger | bool | 自动触发信号 | Reader[x].Auto_Triger |
| result | int | 检测结果（1=OK, 2=NG） | Reader[x].Result |
| data | string | 扫码数据 | Reader[x].ReturnData_STRING |
| beat | bool | 心跳信号 | Reader[x].Heartbeat |
| heConnected | bool | 连接状态 | Reader[x].Connect_succeed |

### 电能表数据（PowerClass）

#### 安科瑞电能表
| 参数 | 索引 | 计算公式 | 单位 |
|------|------|---------|------|
| 线电压 Uab/Ub/Uca | 0-2 | datas[i+3] × vRate × 0.1 | V |
| 相电压 Ua/Ub/Uc | 3-5 | datas[i] × vRate × 0.1 | V |
| 相电流 Ia/Ib/Ic | 6-8 | datas[i+6] × iRate × 0.001 | A |
| 总有功功率 Pt | 9 | datas[13] × iRate × vRate × 0.0001 | W |
| 有功功率 Pa/Pb/Pc | 10-12 | datas[i+10] × iRate × vRate × 0.0001 | W |
| 总无功功率 Qt | 13 | datas[17] × iRate × vRate × 0.0001 | VAR |
| 无功功率 Qa/Qb/Qc | 14-16 | datas[i+14] × iRate × vRate × 0.0001 | VAR |

#### 正泰电能表
| 参数 | 说明 | 计算公式 |
|------|------|---------|
| 电压（0-5） | 6 个电压值 | HexString → Float × vRate × 0.1 |
| 电流（6-8） | 3 个电流值 | HexString → Float × iRate × 0.001 |
| 功率（9-16） | 8 个功率值 | HexString → Float × vRate × iRate × 0.1 |
| 电能（17-19） | 3 个电能值 | HexString → Float × vRate × iRate |

## 🎯 主要特性

| 特性 | 描述 |
|------|------|
| **多设备管理** | 同时管理 7 个扫码枪和 5 个电能表 |
| **实时通讯** | 基于 Modbus TCP/RTU 协议 |
| **双品牌支持** | 安科瑞和正泰电能表自适应 |
| **心跳监测** | 实时检测设备连接状态 |
| **参数计算** | 自动计算电压、电流、功率等参数 |
| **易于集成** | 模块化的数据类设计 |
| **单例运行** | 使用互斥量防止多个实例同时运行 |

## 🔗 技术堆栈

- **编程语言**：C# .NET
- **UI 框架**：Windows Forms（WinForms）
- **通讯协议**：Modbus TCP/RTU
- **依赖库**：SkyTool（自定义工具库）
- **并发管理**：System.Threading.Mutex

## 📝 核心类说明

### ScanClass（扫码枪类）

```csharp
public class ScanClass
{
    // 扫码触发命令
    public string strSend = "LON\r";
    public string strEnd = "LOFF\r";
    
    // 通讯延时
    public int delay = 800;
    
    // PLC 变量地址（7 个设备）
    public string[] triger;      // 触发信号
    public string[] result;      // 结果反馈
    public string[] data;        // 扫码数据
    public string[] beat;        // 心跳
    public string[] heConnected; // 连接状态
}
```

### PowerClass（电能表类）

```csharp
public class PowerClass
{
    // 电能表品牌
    public int type = 0;  // 0: 安科瑞, 1: 正泰
    
    // 地址和读取量
    public int[] startAddr = { 0x50, 0x2000 };
    public int[] readCount = { 18, 17 };
    
    // 变比系数
    public double iRate = 1;  // 电流变比
    public double vRate = 1;  // 电压变比
    
    // PLC 变量地址（5 个设备）
    public string[] result;      // 结果反馈
    public string[] data;        // 数据数组
    public string[] beat;        // 心跳
    public string[] heConnected; // 连接状态
    
    // 数据处理方法
    public string ToValueString(short[] datas);     // 安科瑞数据转换
    public string ToValueString(string strData);    // 正泰数据转换
    public string PrintString(string value, string[] names); // 格式化输出
}
```

### Program（应用程序入口）

```csharp
static class Program
{
    private static System.Threading.Mutex mutex; // 互斥量
    
    [STAThread]
    static void Main()
    {
        // 单例控制：防止多个实例运行
        bool createNew = false;
        mutex = new System.Threading.Mutex(true, "ScanCheck", out createNew);
        
        if (createNew && mutex.WaitOne(0, false))
        {
            Application.Run(new FormMain());
        }
        else
        {
            MessageBox.Show("已经有一个程序正在运行！");
        }
    }
}
```

## 🛠️ 故障排除

### 问题 1：应用启动失败
**症状**：无法启动应用或提示找不到依赖库
**解决**：
1. 检查 SkyTool 库是否已安装
2. 验证 .NET Framework 版本是否满足要求
3. 重新编译项目

### 问题 2：PLC 连接失败
**症状**：心跳检测失败，连接状态为 false
**解决**：
1. 检查 PLC IP 地址和端口配置
2. 验证网络连接是否正常
3. 确认 PLC 中变量地址是否正确

### 问题 3：电能表数据异常
**症状**：读取的电能表数据格式错误或无法转换
**解决**：
1. 确认电能表品牌类型设置是否正确（type 参数）
2. 检查变比系数（iRate、vRate）配置
3. 验证寄存器地址和读取量是否符合设备规范

### 问题 4：多个实例同时运行
**症状**：启动应用时提示"已经有一个程序正在运行"
**解决**：
1. 检查任务管理器，关闭已有的 ScanCheck 进程
2. 清除残留的互斥量：重启计算机

## 📚 相关文件说明

### Program.cs
- 应用程序主入口
- 实现单例控制（互斥量）
- 初始化主窗体

### FormMain.cs / FormMain.Designer.cs
- 主业务逻辑实现
- UI 界面设计
- 数据采集和处理

### ScanClass.cs
- 扫码枪数据管理类
- 电能表数据处理类
- 数据格式转换方法

### App.config
- 应用程序配置文件
- PLC 连接参数
- 设备参数配置

## 🎓 应用场景

该项目适用于以下场景：
- **工业制造**：生产线电气参数监测
- **质量检测**：产品检测数据采集
- **能源管理**：电能消耗统计分析
- **数据中心**：多设备实时监控
- **智能工厂**：IIoT 数据采集

## 📄 许可证

本项目为毕业设计项目，仅供学习参考。

## 👤 作者

- **GitHub**：[@2002YZ](https://github.com/2002YZ)
- **项目**：YOLOV 毕业设计 - 扫码枪+电能表模块

## 📞 技术支持

- 如有问题或建议，欢迎通过 GitHub Issues 进行反馈
- 查看主项目 README 了解更多信息

---

**开发环境**：Visual Studio 2019+
**目标框架**：.NET Framework 4.0+
**项目状态**：🚀 活跃开发中
**最后更新**：2026 年
