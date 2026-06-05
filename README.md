# 企业项目集合 | Enterprise Projects Collection

[![C#](https://img.shields.io/badge/Language-C%23-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Python](https://img.shields.io/badge/Language-Python-green.svg)](https://www.python.org/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](#许可证)
[![Status](https://img.shields.io/badge/Status-Active-brightgreen.svg)](#项目状态)

## 📌 项目概述 | Overview

本仓库为企业级项目集合，包含两个独立的模块：

1. **YOLOV11 实时目标检测系统** - 基于 Python + Flask 的深度学习应用
2. **ScanCheck 工业数据采集系统** - 基于 C# WinForms 的企业级应用

| 项目 | 语言 | 比例 | 技术栈 | 说明 |
|------|------|------|--------|------|
| 企业 C# 模块 | C# | 92.6% | .NET Framework, WinForms, Modbus | 工业数据采集 |
| Python 模块 | Python | 7.4% | Flask, YOLOv11, OpenCV | AI 目标检测 |

---

## 📁 项目结构 | Project Structure

```
qiyexiangmu/
├── README.md                                    # 主文档
├── 扫码枪+电能表/                              # [主要] C# 企业级模块
│   ├── README.md                                # 详细使用文档
│   ├── ScanCheck.sln                            # Visual Studio 解决方案
│   └── HeCheck/                                 # 主应用目录
│       ├── Program.cs                           # 入口程序
│       ├── FormMain.cs                          # 主窗体逻辑
│       ├── ScanClass.cs                         # 扫码枪数据类
│       ├── ScanCheck.csproj                     # 项目配置
│       ├── App.config                           # 应用配置
│       ├── bin/                                 # 编译输出
│       └── Properties/                          # 项目属性
├── YOLOV11/                                    # Python AI 模块
│   ├── app1.py                                  # Flask 主应用
│   ├── templates/
│   │   └── 1111.html                            # Web 前端界面
│   └── requirements.txt                         # Python 依赖
└── .gitignore

```

---

## 🎯 核心模块详解

### 1️⃣ ScanCheck - 工业数据采集系统 ⭐ **[主要项目]**

**企业级 C# 应用程序**，负责实时工业数据采集与处理。

#### 主要功能
- ✅ **多设备管理**：7 个扫码枪 + 5 个电能表的实时监控
- ✅ **Modbus 通讯**：支持 TCP/RTU 协议与 PLC 设备通信
- ✅ **双品牌电能表**：安科瑞 (Acrel) & 正泰 (Chint) 自适应
- ✅ **实时数据处理**：心跳检测、连接状态监测、参数自动计算
- ✅ **单例运行机制**：互斥量保证仅单个实例运行
- ✅ **Windows Forms UI**：友好的工业级操作界面

#### 技术架构
- **编程语言**：C# .NET
- **UI 框架**：Windows Forms (WinForms)
- **通讯协议**：Modbus TCP/RTU
- **并发控制**：System.Threading.Mutex
- **.NET 版本**：.NET Framework 4.0+
- **目标平台**：Windows XP 及以上

#### 数据采集能力
```
扫码枪模块：
├── 触发控制（自动/手动）
├── 字符串数据传输
├── 心跳检测
└── 连接状态反馈（OK/NG）

电能表模块：
├── 线电压、相电压采集
├── 相电流采集
├── 有功功率、无功功率计算
├── 电能统计
└── 变比系数配置
```

#### 快速开始
```bash
# 打开项目
Visual Studio → Open → ScanCheck.sln

# 编译
Ctrl+Shift+B

# 运行
Ctrl+F5
```

📖 **详细文档**：[扫码枪+电能表/README.md](./扫码枪+电能表/README.md)

---

### 2️⃣ YOLOV11 - AI 目标检测系统

**Python + Flask 实时视频处理应用**，基于深度学习进行目标检测。

#### 主要功能
- ✅ **实时检测**：支持视频流和单帧图像处理
- ✅ **多类别识别**：自行车、摩托车、行人三类检测
- ✅ **动态参数调整**：置信度、IoU 阈值实时修改
- ✅ **Web 交互界面**：Flask + HTML5 实现的实时控制面板
- ✅ **性能统计**：处理时间和检测数量统计

#### 技术架构
- **后端**：Flask (Python Web 框架)
- **AI 模型**：Ultralytics YOLOv11
- **图像处理**：OpenCV (cv2)
- **数据处理**：NumPy
- **传输格式**：Base64 编码图像

#### 快速开始
```bash
# 安装依赖
pip install flask opencv-python numpy ultralytics

# 配置模型路径（app1.py）
model_path = r'your_model_path/best.pt'

# 运行应用
python app1.py

# 访问
http://localhost:5000
```

📖 **详细文档**：README 位于 YOLOV11 目录

---

## 🛠️ 技术栈总览 | Tech Stack

### 后端服务
| 技术 | 版本 | 用途 |
|------|------|------|
| C# .NET | 4.0+ | 企业数据采集应用 |
| Python | 3.8+ | AI 目标检测系统 |
| Flask | Latest | Web 交互框架 |
| Modbus | TCP/RTU | 工业通讯协议 |

### 前端 & UI
| 技术 | 用途 |
|------|------|
| Windows Forms | 企业应用 UI |
| HTML5 + CSS | Web 界面 |
| JavaScript | 前端交互 |

### 数据处理
| 库 | 功能 |
|----|------|
| OpenCV | 图像处理 |
| NumPy | 数值计算 |
| YOLOv11 | 目标检测 |

---

## 📊 语言分布 | Language Distribution

```
C#       92.6%  ████████████████████████████████████████ (企业级核心)
Python    7.4%  ███ (AI/Web 模块)
```

**说明**：该仓库以 **C# 企业级应用**为主要成分，Python 模块为辅助功能。

---

## 🚀 快速开始 | Getting Started

### 环境要求

#### 运行 ScanCheck (C# 模块)
```
操作系统：Windows XP 及以上
.NET Framework：4.0 或更高版本
IDE：Visual Studio 2019+ (可选)
硬件：支持 Modbus TCP 的 PLC
```

#### 运行 YOLOV11 (Python 模块)
```
Python：3.8+
GPU（可选）：CUDA 支持 (加速推理)
内存：最少 4GB
```

### 安装步骤

#### 方式一：克隆完整项目
```bash
git clone https://github.com/2002YZ/qiyexiangmu.git
cd qiyexiangmu
```

#### 方式二：单独使用模块

**仅使用 C# 模块**
```bash
cd 扫码枪+电能表
# 用 Visual Studio 打开 ScanCheck.sln
```

**仅使用 Python 模块**
```bash
cd YOLOV11
pip install -r requirements.txt
python app1.py
```

---

## 📋 使用说明 | Documentation

### C# 模块使用
| 功能 | 说明 | 文件 |
|------|------|------|
| 扫码枪配置 | 设备地址、触发参数 | ScanClass.cs |
| 电能表配置 | 品牌选择、变比系数 | ScanClass.cs |
| PLC 连接 | 地址、端口、心跳 | FormMain.cs |
| UI 操作 | 数据实时显示、参数调整 | FormMain.Designer.cs |

**详细配置**：[扫码枪+电能表/README.md](./扫码枪+电能表/README.md)

### Python 模块使用
| 功能 | 说明 | 端点 |
|------|------|------|
| 主页面 | 加载控制界面 | GET / |
| 目标检测 | 处理图像帧 | POST /predict_frame |
| 参数调整 | 修改检测阈值 | POST /update_thresholds |

**详细 API**：YOLOV11 目录内 README

---

## 🔗 项目关联 | Related Links

- 📌 **Repository**：[github.com/2002YZ/qiyexiangmu](https://github.com/2002YZ/qiyexiangmu)
- 👤 **Author**：[@2002YZ](https://github.com/2002YZ)
- 📅 **Created**：March 6, 2025
- ⭐ **Repository Stars**：![stars](https://img.shields.io/github/stars/2002YZ/qiyexiangmu)

---

## 🎓 项目意义 | Significance

本项目综合应用以下技术领域：

### C# 企业开发
- ✅ 工业通讯协议实现 (Modbus)
- ✅ 多设备并发管理
- ✅ 实时数据采集与处理
- ✅ Windows Forms 应用架构
- ✅ 系统级编程 (互斥量、多线程)

### Python AI 应用
- ✅ 深度学习模型应用 (YOLOv11)
- ✅ Web 框架集成 (Flask)
- ✅ 计算机视觉实现 (OpenCV)
- ✅ 实时流处理
- ✅ 前后端交互设计

---

## 🛠️ 故障排除 | Troubleshooting

### C# 模块常见问题
| 问题 | 原因 | 解决 |
|------|------|------|
| 应用启动失败 | 依赖库缺失 | 检查 SkyTool 库、重新编译 |
| PLC 连接失败 | 网络/地址错误 | 验证 IP、端口、变量地址 |
| 数据异常 | 参数配置错误 | 检查电能表品牌、变比系数 |
| 多个实例运行 | 互斥量失效 | 关闭进程、重启应用 |

### Python 模块常见问题
| 问题 | 原因 | 解决 |
|------|------|------|
| 模型加载失败 | 路径不正确 | 检查 model_path 配置 |
| 模板未找到 | 工作目录错误 | 确保在项目根目录运行 |
| 性能缓慢 | 配置不优化 | 降低分辨率、提高阈值、使用 GPU |

---

## 📄 许可证 | License

本项目为毕业设计项目，仅供学习参考。
MIT License - 欢迎学习和参考。

---

## 📞 联系与支持 | Contact & Support

- **问题反馈**：[GitHub Issues](https://github.com/2002YZ/qiyexiangmu/issues)
- **讨论交流**：[GitHub Discussions](https://github.com/2002YZ/qiyexiangmu/discussions)
- **作者主页**：[@2002YZ](https://github.com/2002YZ)

---

## 🗂️ 相关文件清单 | File Reference

```
核心文件：
├── 扫码枪+电能表/
│   ├── Program.cs           # C# 程序入口（单例控制）
│   ├── FormMain.cs          # 主窗体业务逻辑
│   ├── ScanClass.cs         # 数据采集核心类
│   ├── App.config           # 连接配置文件
│   └── ScanCheck.csproj     # 项目配置
│
├── YOLOV11/
│   ├── app1.py              # Flask 应用
│   ├── templates/1111.html  # Web 界面
│   └── requirements.txt     # Python 依赖表
│
└── README.md                # 本文件（项目总览）
```

---

**项目状态**：🚀 **活跃开发中**  
**最后更新**：2026 年 6 月  
**维护者**：[@2002YZ](https://github.com/2002YZ)

---

## 📈 项目统计 | Statistics

- 📦 **代码总量**：多模块企业级应用
- 🔧 **主要技术**：C# + Python
- 🌐 **应用场景**：工业数据采集 + AI 视觉识别
- 👥 **开发者**：[@2002YZ](https://github.com/2002YZ)
- 📅 **创建日期**：2025 年 3 月 6 日

---

**感谢访问本项目！** 🎉 如有任何问题，欢迎通过 [GitHub Issues](https://github.com/2002YZ/qiyexiangmu/issues) 联系我。
