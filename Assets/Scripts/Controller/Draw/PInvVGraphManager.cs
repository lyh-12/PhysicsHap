// 文件名: Scripts/Controller/Draw/PInvVGraphManager.cs
// --- 修复版：使用 Start() 代替 OnEnable() 来避免执行顺序问题 ---

using UnityEngine;

public class PInvVGraphManager : MonoBehaviour
{
    public GameObject Disc; 

    [Header("Axis Ranges & Mapping")]
    public float minPressureDisplay = 0f;
    public float maxPressureDisplay = 2.5f; 
    public float minInvVDisplay = 0.0f;
    public float maxInvVDisplay = 0.002f; 

    [Header("Graph Visual Size")]
    public float graphWidth = 0.7f;
    public float graphHeight = 1.0f;

    // --- ↓↓↓ 修改点：使用 Start() 代替 OnEnable() ↓↓↓ ---
    void Start()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnPVDataPointRecorded += HandleDataPointRecordRequested;
            ExperimentManager.Instance.OnExperimentReset += ClearAllPoints;
            Debug.Log("[PInvVGraphManager] 已在 Start() 中成功订阅事件。");
        }
        else
        {
            // Start() 总是在 Awake() 之后执行，所以这里不应该再报错
            Debug.LogError("[PInvVGraphManager] 致命错误：即使在 Start() 中也无法找到 ExperimentManager.Instance！");
        }
    }

    // --- ↓↓↓ 修改点：使用 OnDestroy() 代替 OnDisable() ↓↓↓ ---
    void OnDestroy()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnPVDataPointRecorded -= HandleDataPointRecordRequested;
            ExperimentManager.Instance.OnExperimentReset -= ClearAllPoints;
            Debug.Log("[PInvVGraphManager] 已在 OnDestroy() 中取消订阅事件。");
        }
    }
    // --- ↑↑↑ 修改结束 ↑↑↑ ---


    private void HandleDataPointRecordRequested(float pressureAtm, float volumeCm3)
    {
        if (!enabled) return;
        
        // (保留所有调试日志)
        Debug.Log($"===== [PInvVGraphManager] 收到数据点 =====\n" +
                  $"原始 P (atm): {pressureAtm:F4}\n" +
                  $"原始 V (cm³): {volumeCm3:F2}");

        if (volumeCm3 <= float.Epsilon)
        {
            Debug.LogWarning("[PInvVGraphManager] 接收到的体积 V 为 0 或太小，跳过 1/V 计算。");
            return;
        }

        float invVolume = 1.0f / volumeCm3;
        Debug.Log($"计算出的 1/V (cm³⁻¹): {invVolume:F6}");
        
        var localPos = ConvertPInvVToLocalPosition(pressureAtm, invVolume);
        Debug.Log($"[PInvVGraphManager] 最终计算出的局部坐标 (Local Position): {localPos}");

        if (Disc == null)
        {
            Debug.LogError("[PInvVGraphManager] 错误：'Disc' 预制件未在 Inspector 中设置！");
            return;
        }
        
        Transform pointsParent = transform.GetChild(1);
        if (pointsParent == null)
        {
             Debug.LogError("[PInvVGraphManager] 错误：找不到 GetChild(1) 来作为点的父对象！");
            return;
        }

        var obj = Instantiate(Disc, pointsParent, true);
        obj.SetActive(true);
        obj.transform.localPosition = localPos;
        
        Debug.Log($"<color=green>[PInvVGraphManager] 成功在 {localPos} 处实例化了一个点。</color>");
    }

    public void ClearAllPoints()
    {
        Transform pointsParent = transform.GetChild(1);
        if (pointsParent == null) return;

        foreach (Transform child in pointsParent)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("[PInvVGraphManager] P-1/V Graph points cleared.");
    }
    
    public Vector3 ConvertPInvVToLocalPosition(float pValue, float invVValue)
    {
        Debug.Log($"--- 开始坐标转换 (ConvertPInvVToLocalPosition) ---\n" +
                  $"P (Y轴) 范围: [{minPressureDisplay:F4}, {maxPressureDisplay:F4}]\n" +
                  $"1/V (X轴) 范围: [{minInvVDisplay:F6}, {maxInvVDisplay:F6}]");

        if (maxPressureDisplay - minPressureDisplay == 0 || maxInvVDisplay - minInvVDisplay == 0)
        {
            Debug.LogError("[PInvVGraphManager] P轴或1/V轴的范围为零，无法计算坐标！");
            return Vector3.zero;
        }

        float pNormalized = (pValue - minPressureDisplay) / (maxPressureDisplay - minPressureDisplay);
        float invVNormalized = (invVValue - minInvVDisplay) / (maxInvVDisplay - minInvVDisplay);
        
        Debug.Log($"归一化 P (Y-norm): {pNormalized:F4}\n" +
                  $"归一化 1/V (X-norm): {invVNormalized:F4}");

        float xPos = (invVNormalized - 0.5f) * graphWidth;
        float yPos = (pNormalized - 0.5f) * graphHeight;
        float zPos = 0f; 

        return new Vector3(xPos, yPos, zPos);
    }
}