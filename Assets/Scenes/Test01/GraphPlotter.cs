using UnityEngine;
using UnityEngine.UI; // 如果你使用了UI Text等
using System.Collections.Generic;

public class GraphPlotter : MonoBehaviour
{
    [Header("Graph Objects")]
    public LineRenderer plotLineRenderer;       // 拖拽用于绘制曲线的 LineRenderer 对象
    public RectTransform graphContainer;        // 拖拽 GraphContainer 对象 (定义图表区域)

    // (可选) 如果你也想快速测试坐标轴绘制
    public LineRenderer xAxisRenderer;
    public LineRenderer yAxisRenderer;

    [Header("Graph Appearance")]
    public Color plotColor = Color.cyan;
    public float plotLineWidth = 0.05f;
    public Color axisColor = Color.white;
    public float axisLineWidth = 0.03f;

    [Header("Data Generation & Axis Ranges")]
    public int numberOfTestDataPoints = 20;  // 生成多少个数据点
    public float testMinVolume = 0.2f;       // 测试数据的最小体积
    public float testMaxVolume = 2.0f;       // 测试数据的最大体积
    public float testPressureConstantK = 100f; // P*V = K, 这个K值

    // 这些范围用于将数据映射到图表区域，需要根据生成的数据估算或设定
    private float displayMinVolume;
    private float displayMaxVolume;
    private float displayMinPressure;
    private float displayMaxPressure;

    private List<Vector2> testDataPoints = new List<Vector2>();

    void Start()
    {
        if (plotLineRenderer == null || graphContainer == null)
        {
            Debug.LogError("Plot LineRenderer or GraphContainer not assigned!");
            return;
        }

        SetupLineRenderers();
        GenerateTestData(); // 生成测试数据
        CalculateDisplayRanges(); // 根据生成的数据计算显示范围
        
        if (xAxisRenderer && yAxisRenderer)
        {
            DrawAxes(); // 绘制坐标轴
        }
        PlotGraph();    // 绘制数据曲线
    }

    void SetupLineRenderers()
    {
        // Plot Line
        plotLineRenderer.startColor = plotColor;
        plotLineRenderer.endColor = plotColor;
        plotLineRenderer.startWidth = plotLineWidth;
        plotLineRenderer.endWidth = plotLineWidth;
        plotLineRenderer.positionCount = 0;
        plotLineRenderer.useWorldSpace = false; // 确保在UI Canvas下正确工作

        // Axes (Optional)
        if (xAxisRenderer)
        {
            xAxisRenderer.startColor = axisColor;
            xAxisRenderer.endColor = axisColor;
            xAxisRenderer.startWidth = axisLineWidth;
            xAxisRenderer.endWidth = axisLineWidth;
            xAxisRenderer.positionCount = 2;
            xAxisRenderer.useWorldSpace = false;
        }
        if (yAxisRenderer)
        {
            yAxisRenderer.startColor = axisColor;
            yAxisRenderer.endColor = axisColor;
            yAxisRenderer.startWidth = axisLineWidth;
            yAxisRenderer.endWidth = axisLineWidth;
            yAxisRenderer.positionCount = 2;
            yAxisRenderer.useWorldSpace = false;
        }
    }

    void GenerateTestData()
    {
        testDataPoints.Clear();
        if (numberOfTestDataPoints < 2) numberOfTestDataPoints = 2;

        float volumeStep = (testMaxVolume - testMinVolume) / (numberOfTestDataPoints - 1);

        for (int i = 0; i < numberOfTestDataPoints; i++)
        {
            float volume = testMinVolume + i * volumeStep;
            if (volume <= 0) volume = 0.01f; //避免除以零
            float pressure = testPressureConstantK / volume;
            testDataPoints.Add(new Vector2(volume, pressure));
        }
    }

    void CalculateDisplayRanges()
    {
        if (testDataPoints.Count == 0) return;

        displayMinVolume = float.MaxValue;
        displayMaxVolume = float.MinValue;
        displayMinPressure = float.MaxValue;
        displayMaxPressure = float.MinValue;

        foreach (Vector2 point in testDataPoints)
        {
            if (point.x < displayMinVolume) displayMinVolume = point.x;
            if (point.x > displayMaxVolume) displayMaxVolume = point.x;
            if (point.y < displayMinPressure) displayMinPressure = point.y;
            if (point.y > displayMaxPressure) displayMaxPressure = point.y;
        }

        // 添加一些边距，让图像看起来不是顶满的
        float xPadding = (displayMaxVolume - displayMinVolume) * 0.1f;
        float yPadding = (displayMaxPressure - displayMinPressure) * 0.1f;

        displayMinVolume -= xPadding;
        displayMaxVolume += xPadding;
        displayMinPressure -= yPadding;
        displayMaxPressure += yPadding;

        // 避免范围为0
        if (displayMinVolume == displayMaxVolume) displayMaxVolume += 0.1f;
        if (displayMinPressure == displayMaxPressure) displayMaxPressure += 1f;
    }


    Vector2 MapDataToGraphSpace(Vector2 dataValue)
    {
        float graphWidth = graphContainer.rect.width;
        float graphHeight = graphContainer.rect.height;

        float normalizedX = (dataValue.x - displayMinVolume) / (displayMaxVolume - displayMinVolume);
        float normalizedY = (dataValue.y - displayMinPressure) / (displayMaxPressure - displayMinPressure);

        float xPos = (normalizedX - 0.5f) * graphWidth;
        float yPos = (normalizedY - 0.5f) * graphHeight;

        return new Vector2(xPos, yPos);
    }

    void DrawAxes()
    {
        if (!graphContainer || (!xAxisRenderer && !yAxisRenderer)) return;

        float graphWidth = graphContainer.rect.width;
        float graphHeight = graphContainer.rect.height;

        // X 轴: GraphContainer 的底部
        if (xAxisRenderer)
        {
            xAxisRenderer.SetPosition(0, new Vector3(-graphWidth / 2f, -graphHeight / 2f, 0));
            xAxisRenderer.SetPosition(1, new Vector3(graphWidth / 2f, -graphHeight / 2f, 0));
        }

        // Y 轴: GraphContainer 的左边
        if (yAxisRenderer)
        {
            yAxisRenderer.SetPosition(0, new Vector3(-graphWidth / 2f, -graphHeight / 2f, 0));
            yAxisRenderer.SetPosition(1, new Vector3(-graphWidth / 2f, graphHeight / 2f, 0));
        }
    }

    void PlotGraph()
    {
        if (plotLineRenderer == null || testDataPoints.Count < 2)
        {
            if(plotLineRenderer) plotLineRenderer.positionCount = 0;
            return;
        }

        plotLineRenderer.positionCount = testDataPoints.Count;
        for (int i = 0; i < testDataPoints.Count; i++)
        {
            Vector2 graphPointPosition = MapDataToGraphSpace(testDataPoints[i]);
            plotLineRenderer.SetPosition(i, new Vector3(graphPointPosition.x, graphPointPosition.y, 0));
        }
    }
}