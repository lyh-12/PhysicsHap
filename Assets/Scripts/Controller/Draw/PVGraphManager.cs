// PVGraphManager.cs
using UnityEngine;
using UnityEngine.UI; 

public class PVGraphManager : MonoBehaviour
{
    public GameObject pointPrefab;

    [Header("Axis Ranges & Mapping")]
    public float minVolumeDisplay = 0f;
    public float maxVolumeDisplay = 2500f; 

    public float minPressureDisplay = 0f;
    public float maxPressureDisplay = 2.5f; 

    private float graphWidth = 1.5f;
    private float graphHeight = 1.2f;
    
    public GameObject Disc; // Disc 属性保持不变

    // --- 修改点：使用 OnEnable() 代替 Start() ---
    void OnEnable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnPVDataPointRecorded += HandleDataPointRecordRequested;
            ExperimentManager.Instance.OnExperimentReset += ClearAllPoints;
        }
        else
        {
            Debug.LogError("PVGraphManager: ExperimentManager.Instance not found!", this);
        }
    }

    // --- 修改点：使用 OnDisable() 代替 OnDestroy() ---
    void OnDisable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnPVDataPointRecorded -= HandleDataPointRecordRequested;
            ExperimentManager.Instance.OnExperimentReset -= ClearAllPoints;
        }
    }
    // --- (已移除 Start() 和 OnDestroy() ) ---

    private void HandleDataPointRecordRequested(float pressurePa, float volumeCm3)
    {
        if (!enabled) return;
        
        var localPos = ConvertPVToLocalPosition(pressurePa, volumeCm3);

        // 您原来的 Disc 逻辑保持不变
        var obj = Instantiate(Disc, transform.GetChild(1), true);
        obj.SetActive(true);
        obj.transform.localPosition = localPos;
    }

    public void ClearAllPoints()
    {
        Transform pointsParent = transform.GetChild(1);
        if (pointsParent == null) return;

        foreach (Transform child in pointsParent)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("PV Graph points cleared.");
    }
    
    public Vector3 ConvertPVToLocalPosition(float pValue, float vValue)
    {
        if (maxPressureDisplay - minPressureDisplay == 0 || maxVolumeDisplay - minVolumeDisplay == 0)
        {
            Debug.LogError("P轴或V轴的范围为零，无法计算坐标！");
            return Vector3.zero;
        }

        float vNormalized = (vValue - minVolumeDisplay) / (maxVolumeDisplay - minVolumeDisplay);
        float pNormalized = (pValue - minPressureDisplay) / (maxPressureDisplay - minPressureDisplay);

        float xPos = (vNormalized - 0.5f) * graphWidth;
        float yPos = (pNormalized - 0.5f) * graphHeight;
        float zPos = 0f; 

        return new Vector3(xPos, yPos, zPos);
    }
}