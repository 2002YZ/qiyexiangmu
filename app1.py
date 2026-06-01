from flask import Flask, render_template, request, jsonify
import os
import cv2
import numpy as np
import base64
from ultralytics import YOLO

# 打印当前工作目录，帮助调试
current_dir = os.getcwd()
print(f"当前工作目录: {current_dir}")

# 直接使用标准的模板目录结构
templates_dir = os.path.join(current_dir, 'templates')
print(f"模板目录: {templates_dir}")
print(f"1111.html 是否在模板目录中: {os.path.exists(os.path.join(templates_dir, '1111.html'))}")

# 初始化Flask应用，使用标准模板目录
app = Flask(__name__)

# 加载模型
model_path = r'D:\pycharmqiye\dt\yolov11\ultralytics-main\runs\train\exp9\weights\best.pt'
model = YOLO(model_path)

# 定义类名
class_names = ['bicycle', 'motorcycle', 'person']

# 初始化参数
model.conf = 0.5
model.iou = 0.5
model.only_motorcycles = False

@app.route('/')
def index():
    try:
        return render_template('1111.html')
    except Exception as e:
        # 如果找不到模板，直接读取HTML文件并返回内容
        print(f"渲染模板出错: {str(e)}")
        try:
            # 尝试从当前目录读取
            with open('1111.html', 'r', encoding='utf-8') as f:
                return f.read()
        except Exception as e2:
            print(f"直接读取HTML文件出错: {str(e2)}")
            # 尝试从模板目录读取
            try:
                with open(os.path.join(templates_dir, '1111.html'), 'r', encoding='utf-8') as f:
                    return f.read()
            except Exception as e3:
                print(f"从模板目录读取HTML文件出错: {str(e3)}")
                return f"无法找到模板文件。当前工作目录: {current_dir}, 模板目录: {templates_dir}"

# 添加路由来处理阈值更新
@app.route('/update_thresholds', methods=['POST'])
def update_thresholds():
    data = request.get_json()
    conf_threshold = float(data.get('confThreshold', 0.5))  # 将值转换为浮点数
    iou_threshold = float(data.get('iouThreshold', 0.5))  # 将值转换为浮点数
    only_motorcycles = data.get('onlyMotorcycles', False)  # 获取是否只识别电动车

    # 更新模型的置信度阈值、IoU阈值及只识别电动车设置
    model.conf = conf_threshold
    model.iou = iou_threshold
    model.only_motorcycles = only_motorcycles

    print(
        f'Updated thresholds - Confidence: {conf_threshold}, IoU: {iou_threshold}, Only Motorcycles: {only_motorcycles}')
    return jsonify({
        'status': 'success',
        'confThreshold': conf_threshold,
        'iouThreshold': iou_threshold,
        'onlyMotorcycles': only_motorcycles
    })


@app.route('/predict_frame', methods=['POST'])
def predict_frame():
    try:
        if request.method == 'POST':
            frame_bytes = request.data
            if not frame_bytes:
                return jsonify({'error': '没有收到图像数据'}), 400

            img_array = np.frombuffer(frame_bytes, np.uint8)
            img = cv2.imdecode(img_array, cv2.IMREAD_COLOR)

            if img is None:
                return jsonify({'error': '图像解码失败'}), 400

            # 使用 YOLO 进行推断时，确保使用最新的阈值
            results = model(img, conf=model.conf, iou=model.iou)

            # 获取电动车的类别索引
            motorcycle_class_id = class_names.index('motorcycle')

            # 初始化检测结果
            if model.only_motorcycles:
                detection_counts = {'motorcycle': 0}
            else:
                detection_counts = {name: 0 for name in class_names}

            # 创建一个新的结果列表，用于存储过滤后的结果
            filtered_results = []

            for result in results:
                filtered_boxes = []
                for box in result.boxes:
                    class_id = int(box.cls)
                    if model.only_motorcycles:
                        if class_id == motorcycle_class_id:
                            filtered_boxes.append(box)
                            detection_counts['motorcycle'] += 1
                    else:
                        if class_id < len(class_names):
                            filtered_boxes.append(box)
                            class_name = class_names[class_id]
                            detection_counts[class_name] += 1
                # 更新结果的检测框为过滤后的检测框
                result.boxes = filtered_boxes
                filtered_results.append(result)

            # 绘制结果图像
            if filtered_results:
                result_img = filtered_results[0].plot()
            else:
                result_img = img  # 如果没有检测结果，返回原始图像

            # 编码返回的图像为 base64
            _, buffer = cv2.imencode('.jpg', result_img)
            base64_image = base64.b64encode(buffer).decode('utf-8')

            # 准备返回的检测计数
            counts_list = [
                {'count': count, 'className': class_name}
                for class_name, count in detection_counts.items() if count > 0
            ]

            return jsonify({
                'image': base64_image,
                'counts': counts_list,
                'processing_time': '82.7ms'
            })
    except Exception as e:
        print(f"处理帧时出错: {str(e)}")
        return jsonify({'error': str(e)}), 500


if __name__ == '__main__':
    app.run(debug=True)