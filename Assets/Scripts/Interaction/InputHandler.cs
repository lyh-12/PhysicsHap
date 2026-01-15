// InputHandler.cs
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [Tooltip("Reference to the ExperimentManager to send commands to.")]
    public ExperimentManager experimentManager;

    [Header("Input Keys")]
    public KeyCode addMoleculesKey = KeyCode.Space;
    public KeyCode removeMoleculesKey = KeyCode.Backspace;
    public KeyCode clearAllMoleculesKey = KeyCode.Delete;
    // ### 新增记录数据点的按键 ###
    public KeyCode recordDataPointKey = KeyCode.R; // 例如，使用 R 键

    void Start()
    {
        if (experimentManager == null)
        {
            experimentManager = ExperimentManager.Instance;
            if (experimentManager == null)
            {
                Debug.LogError("InputHandler: ExperimentManager not found. Input handling will be disabled.", this);
                enabled = false;
                return;
            }
        }
    }

    void Update()
    {
        if (!enabled || experimentManager == null)
        {
            return;
        }

        if (Input.GetKeyDown(addMoleculesKey))
        {
            experimentManager.RequestAddMolecules();
        }

        if (Input.GetKeyDown(removeMoleculesKey))
        {
            experimentManager.RequestRemoveMolecules();
        }

        if (Input.GetKeyDown(clearAllMoleculesKey))
        {
            RequestClearAllMolecules();
        }

        // // ### 新增：检测记录数据点按键 ###
        if (Input.GetKeyDown(recordDataPointKey))
        {
            experimentManager.RequestRecordDataPoint();
        }
    }
    
    public void RequestClearAllMolecules()
    {
        if (MoleculeSpawner.Instance != null) MoleculeSpawner.Instance.ClearAllMolecules();
        else Debug.LogError("MoleculeSpawner instance not found for ClearAllMolecules request!");
    }
}