# YOLOV11 毕业设计项目

[![Python](https://img.shields.io/badge/Python-100%25-blue.svg)](https://www.python.org/)
[![Flask](https://img.shields.io/badge/Framework-Flask-green.svg)](https://flask.palletsprojects.com/)
[![YOLO](https://img.shields.io/badge/AI%20Model-YOLOv11-orange.svg)](https://github.com/ultralytics/ultralytics)

## 📋 项目概述

本项目是基于 **YOLOv11** 目标检测模型的毕业设计项目，实现了一个**实时视频目标检测系统**，主要用于识别自行车、摩托车和行人等目标对象。项目包含后端 Python Flask 应用和相关的前端界面。

## ✨ 核心功能

### 1. **实时目标检测**
- 使用 YOLOv11 预训练模型进行实时视频流处理
- 支持识别三类对象：**自行车（bicycle）、摩托车（motorcycle）、行人（person）**
- 实时计算检测框和置信度

### 2. **动态阈值调整**
- **置信度阈值（Confidence Threshold）**：调整检测灵敏度，范围 0-1
- **IoU 阈值（IOU Threshold）**：非极大值抑制参数，优化检测框合并
- **单类别过滤**：支持仅识别摩托车（电动车检测模式）

### 3. **Web 交互界面**
- 基于 Flask 的实时交互系统
- 前端渲染实时检测结果
- 动态参数调整和效果实时反馈

### 4. **检测统计**
- 实时显示各类别目标检测数量
- 处理时间统计
- Base64 图像编码传输

## 📁 项目结构

```
YOLOV/
├── README.md                          # 项目文档
├── app1.py                            # Flask 主应用程序
├── templates/
│   └── 1111.html                      # 前端交互界面
└── 扫码枪+电能表/                      # 扫码枪和电能表相关模块
    ├── ScanCheck.sln                  # Visual Studio 解决方案
    └── HeCheck/                       # 相关工具目录
```

## 🚀 快速开始

### 环境要求

- **Python** >= 3.8
- **依赖库**：
  ```bash
  pip install flask
  pip install opencv-python
  pip install numpy
  pip install ultralytics
  ```

### 安装步骤

1. **克隆项目**
   ```bash
   git clone https://github.com/2002YZ/YOLOV.git
   cd YOLOV
   ```

2. **安装依赖**
   ```bash
   pip install -r requirements.txt
   ```

3. **配置模型路径**
   
   编辑 `app1.py` 文件，修改模型路径：
   ```python
   model_path = r'your_model_path/best.pt'
   model = YOLO(model_path)
   ```

4. **运行应用**
   ```bash
   python app1.py
   ```

5. **访问界面**
   
   打开浏览器访问：`http://localhost:5000`

## 🔧 使用说明

### API 端点

#### 1. 主页面
- **路由**：`GET /`
- **功能**：加载主控制界面
- **返回**：HTML 页面

#### 2. 预测分析
- **路由**：`POST /predict_frame`
- **功能**：处理单帧图像，执行目标检测
- **请求**：二进制图像数据
- **返回**：
  ```json
  {
    "image": "base64_encoded_image",
    "counts": [
      {"count": 3, "className": "motorcycle"},
      {"count": 2, "className": "person"}
    ],
    "processing_time": "82.7ms"
  }
  ```

#### 3. 阈值调整
- **路由**：`POST /update_thresholds`
- **功能**：动态更新检测参数
- **请求体**：
  ```json
  {
    "confThreshold": 0.5,
    "iouThreshold": 0.5,
    "onlyMotorcycles": false
  }
  ```
- **返回**：更新确认和当前参数

## 🎯 主要特性

| 功能特性 | 描述 |
|---------|------|
| **实时检测** | 支持视频流实时处理和单帧图像检测 |
| **多类别识别** | 自行车、摩托车、行人三类检测 |
| **参数调整** | 动态修改置信度、IoU 等核心参数 |
| **模式切换** | 支持普通模式和摩托车单类别模式 |
| **性能统计** | 实时显示处理时间和检测统计 |
| **Web 界面** | 用户友好的实时交互控制面板 |

## 📊 检测类别说明

| 类别 | 英文名 | 说明 |
|------|--------|------|
| 自行车 | bicycle | 人力驱动的两轮车 |
| 摩托车 | motorcycle | 电动或燃油动力的两轮车 |
| 行人 | person | 街道和公共场所的人类 |

## ⚙️ 参数配置

### 置信度阈值（Confidence）
- **范围**：0.0 - 1.0
- **默认值**：0.5
- **说明**：越高越严格，只检测高置信度目标
- **适用场景**：提高准确率时增加该值

### IoU 阈值（IOU）
- **范围**：0.0 - 1.0
- **默认值**：0.5
- **说明**：用于非极大值抑制，避免重复检测
- **适用场景**：拥挤场景可适当降低该值

### 单类别模式
- **说明**：启用时仅检测摩托车（电动车检测）
- **用途**：针对特定场景的优化识别

## 🔗 项目依赖

- **Flask**：Web 框架
- **OpenCV (cv2)**：图像处理
- **NumPy**：数值计算
- **Ultralytics YOLOv11**：目标检测模型

## 📝 模型信息

- **模型版本**：YOLOv11
- **训练结果**：`runs/train/exp9/weights/best.pt`
- **输入尺寸**：标准 YOLO 输入
- **输出**：边界框、置信度、类别

## 🛠️ 故障排除

### 问题 1：模型路径错误
**症状**：运行时提示模型文件未找到
**解决**：检查 `app1.py` 中的 `model_path` 是否正确

### 问题 2：模板文件找不到
**症状**：访问首页时返回错误信息
**解决**：确保 `templates/1111.html` 文件存在，或检查工作目录

### 问题 3：性能不佳
**症状**：处理速度缓慢
**解决**：
- 降低输入分辨率
- 增加置信度阈值
- 使用 GPU 加速（若可用）

## 📚 相关文件说明

### app1.py
Flask 主应用程序，包含：
- 模型加载和初始化
- 路由定义（主页、预测、阈值更新）
- 图像处理和 YOLO 推理逻辑
- 检测结果后处理

### 1111.html
前端交互界面（位于 templates 目录），提供：
- 实时视频预览
- 参数调整控制
- 检测结果显示
- 统计信息面板

### 扫码枪+电能表/
独立模块目录，可能包含：
- 二维码扫描功能
- 电能表数据读取
- 相关 C# 工具（ScanCheck.sln）

## 🎓 毕业设计意义

本项目综合应用了以下技术：
- **深度学习**：YOLO 目标检测算法
- **Web 开发**：Flask 框架
- **计算机视觉**：OpenCV 图像处理
- **实时处理**：流式视频分析
- **交互设计**：参数动态调整

## 📄 许可证

本项目为毕业设计项目，仅供学习参考。

## 👤 作者

- **GitHub**：[@2002YZ](https://github.com/2002YZ)
- **项目**：YOLOV11 毕业设计

## 📞 联系方式

如有问题或建议，欢迎通过 GitHub Issues 进行反馈。

---

**更新时间**：2026 年
**项目状态**：🚀 活跃开发中
