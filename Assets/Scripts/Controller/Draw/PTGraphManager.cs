// PTGraphManager.cs
using UnityEngine;

public class PTGraphManager : MonoBehaviour
{
    public GameObject pointPrefab;

    [Header("Axis Ranges & Mapping")]
    public float minPressureDisplay = 0f;
    public float maxPressureDisplay = 2.5f; // atm
    public float minTemperatureDisplay = 250f;
    public float maxTemperatureDisplay = 600f;

    private float graphWidth = 1.5f;
    private float graphHeight = 1.2f;

    // --- 修改点：使用 OnEnable() 代替 Start() ---
    void OnEnable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnPTDataPointRecorded += HandleDataPointRecordRequested;
            ExperimentManager.Instance.OnExperimentReset += ClearAllPoints;
        }
    }

    // --- 修改点：使用 OnDisable() 代替 OnDestroy() ---
    void OnDisable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnPTDataPointRecorded -= HandleDataPointRecordRequested;
            ExperimentManager.Instance.OnExperimentReset -= ClearAllPoints;
        }
    }
    // --- (已移除 Start() 和 OnDestroy() ) ---

    private void HandleDataPointRecordRequested(float pressureAtm, float temperatureK)
    {
        var localPos = ConvertPTToLocalPosition(pressureAtm, temperatureK);
        var obj = Instantiate(pointPrefab, transform.GetChild(1), false);
        obj.transform.localPosition = localPos;
        obj.SetActive(true);
    }

    public Vector3 ConvertPTToLocalPosition(float pValue, float tValue)
    {
        float tNormalized = (tValue - minTemperatureDisplay) / (maxTemperatureDisplay - minTemperatureDisplay);
        float pNormalized = (pValue - minPressureDisplay) / (maxPressureDisplay - minPressureDisplay);

        float xPos = (tNormalized - 0.5f) * graphWidth;  // T -> X轴
        float yPos = (pNormalized - 0.5f) * graphHeight; // P -> Y轴
        return new Vector3(xPos, yPos, 0f);
    }
    
    public void ClearAllPoints() { 
        Transform pointsParent = transform.GetChild(1);
        foreach (Transform child in pointsParent) { Destroy(child.gameObject); }
        Debug.Log("PT Graph points cleared.");
    }
}