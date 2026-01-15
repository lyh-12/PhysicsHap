// VTGraphManager.cs
using UnityEngine;

public class VTGraphManager : MonoBehaviour
{
    public GameObject pointPrefab; 

    [Header("Axis Ranges & Mapping")]
    public float minVolumeDisplay = 0f;
    public float maxVolumeDisplay = 2500f;
    public float minTemperatureDisplay = 250f; 
    public float maxTemperatureDisplay = 600f; 

    private float graphWidth = 1.5f; 
    private float graphHeight = 1.2f; 

    // --- 修改点：使用 OnEnable() 代替 Start() ---
    void OnEnable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnVTDataPointRecorded += HandleDataPointRecordRequested;
            ExperimentManager.Instance.OnExperimentReset += ClearAllPoints;
        }
    }

    // --- 修改点：使用 OnDisable() 代替 OnDestroy() ---
    void OnDisable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnVTDataPointRecorded -= HandleDataPointRecordRequested;
            ExperimentManager.Instance.OnExperimentReset -= ClearAllPoints;
        }
    }
    // --- (已移除 Start() 和 OnDestroy() ) ---

    private void HandleDataPointRecordRequested(float volumeCm3, float temperatureK)
    {
        var localPos = ConvertVTToLocalPosition(volumeCm3, temperatureK);
        var obj = Instantiate(pointPrefab, transform.GetChild(1), false); 
        obj.transform.localPosition = localPos;
        obj.SetActive(true);
    }

    public Vector3 ConvertVTToLocalPosition(float vValue, float tValue)
    {
        float vNormalized = (vValue - minVolumeDisplay) / (maxVolumeDisplay - minVolumeDisplay);
        float tNormalized = (tValue - minTemperatureDisplay) / (maxTemperatureDisplay - minTemperatureDisplay);

        float xPos = (tNormalized - 0.5f) * graphWidth;
        
        float yPos = (vNormalized - 0.5f) * graphHeight;
        return new Vector3(xPos, yPos, 0f);
    }
    
    public void ClearAllPoints() { 
        Transform pointsParent = transform.GetChild(1);
        foreach (Transform child in pointsParent) { Destroy(child.gameObject); }
        Debug.Log("VT Graph points cleared.");
    }
}