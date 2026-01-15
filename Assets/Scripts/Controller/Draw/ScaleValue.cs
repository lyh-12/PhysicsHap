using System.Collections;
using System.Collections.Generic;
using Shapes; // 确保你已经导入了 Shapes 插件
using UnityEngine;

public class ScaleValue : ImmediateModeShapeDrawer
{
    [Header("Graph Definition Parameters")]
    // P轴 (Y轴)
    public float pMin = 0f;     // 压强最小值 (例如 0 atm)
    public float pMax = 2.5f;   // 压强最大值 (例如 2.5 atm)
    public int numberOfPTicks = 6; // P轴上的刻度数量 (例如 0, 0.5, 1.0, 1.5, 2.0, 2.5 就是6个)

    // V轴 (X轴)
    public float vMin = 0f;     // 体积最小值 (例如 0 cm³)
    public float vMax = 2500f;  // 体积最大值 (例如 2500 cm³)
    public int numberOfVTicks = 6; // V轴上的刻度数量 (例如 0, 500, 1000, 1500, 2000, 2500 就是6个)

    // 图表在Unity场景中的视觉尺寸
    public float graphUnityWidth = 10f;  // 图表在X方向的总宽度 (例如从-5到5)
    public float graphUnityHeight = 5f; // 图表在Y方向的总高度 (例如从-2.5到2.5)

    [Header("Label Settings")]
    public float labelFontSize = 0.3f; // 刻度文字的大小 (原先的1可能太大了)
    public string numberFormat = "F1"; // 数字的格式化字符串 ("F0"代表整数, "F1"代表一位小数等)
    public float xAxisLabelOffset = 0.3f; // X轴刻度数值相对于轴线的垂直偏移量 (向下)
    public float yAxisLabelOffset = 0.4f; // Y轴刻度数值相对于轴线的水平偏移量 (向左)
    // public Color labelColor = Color.white; // 如果需要，可以取消注释并设置颜色

    public override void DrawShapes(Camera cam)
    {
        if (cam == null) return; // 如果没有有效的相机，则不绘制

        using (Draw.Command(cam))
        {
            // 设置变换矩阵，使得后续绘制都相对于此GameObject的局部坐标系
            // (0,0,0) 就是图表中心
            Draw.Matrix = transform.localToWorldMatrix;

            Draw.FontSize = labelFontSize;
            // Draw.Color = labelColor; // 设置文字颜色 (如果需要)

            // --- 绘制 V 轴 (X轴) 的刻度数值 ---
            if (numberOfVTicks > 1 && (vMax - vMin != 0)) // 至少需要2个刻度才能形成间隔，且范围不能为0
            {
                float vTickIntervalValue = (vMax - vMin) / (numberOfVTicks - 1); // 每个刻度代表的值的间隔

                for (int i = 0; i < numberOfVTicks; i++)
                {
                    float currentVTickValue = vMin + i * vTickIntervalValue; // 当前刻度的实际V值

                    // 将V值转换为归一化的X位置 (0到1)
                    float vNormalized = (currentVTickValue - vMin) / (vMax - vMin);

                    // 将归一化的X位置转换为图表X坐标 (相对于中心0点)
                    float xPos = (vNormalized - 0.5f) * graphUnityWidth;

                    // Y轴位置：在X轴下方一点
                    float yPosForXLabel = -graphUnityHeight / 2f - xAxisLabelOffset;

                    Vector3 labelPosition = new Vector3(xPos, yPosForXLabel, 0);
                    string labelText = currentVTickValue.ToString(numberFormat);

                    Draw.Text(labelPosition, labelText);
                }
            }

            // --- 绘制 P 轴 (Y轴) 的刻度数值 ---
            if (numberOfPTicks > 1 && (pMax - pMin != 0)) // 至少需要2个刻度才能形成间隔，且范围不能为0
            {
                float pTickIntervalValue = (pMax - pMin) / (numberOfPTicks - 1); // 每个刻度代表的值的间隔

                for (int i = 0; i < numberOfPTicks; i++)
                {
                    float currentPTickValue = pMin + i * pTickIntervalValue; // 当前刻度的实际P值

                    // 将P值转换为归一化的Y位置 (0到1)
                    float pNormalized = (currentPTickValue - pMin) / (pMax - pMin);

                    // 将归一化的Y位置转换为图表Y坐标 (相对于中心0点)
                    float yPos = (pNormalized - 0.5f) * graphUnityHeight;

                    // X轴位置：在Y轴左边一点
                    float xPosForYLabel = -graphUnityWidth / 2f - yAxisLabelOffset;

                    Vector3 labelPosition = new Vector3(xPosForYLabel, yPos, 0);
                    string labelText = currentPTickValue.ToString(numberFormat);

                    // 对于Y轴的标签，如果Shapes插件支持文本对齐，你可能希望右对齐文本
                    // Draw.TextAlignment = TextAlign.Right; (Shapes的Draw.Text可能不支持直接的TextAlign属性)
                    // 如果不支持，你需要通过调整xPosForYLabel来手动模拟对齐，或者接受默认的左对齐/居中对齐。
                    // 或者计算文本宽度来精确偏移，但Shapes的即时模式可能不直接提供文本宽度。
                    // 我们这里简单地使用偏移量。
                    Draw.Text(labelPosition, labelText);
                }
            }

            // 示例：在中心画一个标记 (可以删除)
            // Draw.Rectangle(Vector3.zero, 0.1f, 0.1f);
            // Draw.Text(Vector3.one * 0.1f, "Center (0,0)");
        }
    }
}