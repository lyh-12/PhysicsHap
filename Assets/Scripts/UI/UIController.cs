using UnityEngine;
using UnityEngine.UI; // 需要引入UI命名空间
using TMPro; // 如果使用TextMeshPro

public class UIController : MonoBehaviour
{
    public ExperimentManager experimentManager; // 或者直接引用 MoleculeSpawner
    // public MoleculeSpawner moleculeSpawner; // 也可以直接引用

    public Button addMoleculeButton;
    public Button removeMoleculeButton;
    public TMP_InputField moleculeCountInputField; // (可选) 用于精确输入数量
    public TMP_Text currentMoleculeCountText; // (可选) 显示当前分子数

    private int _moleculesToAddRemove = 10; // 默认每次增减10个，可以通过InputField修改

    void Start()
    {
        if (experimentManager == null)
        {
            // experimentManager = ExperimentManager.Instance; // 如果 ExperimentManager 是单例
            Debug.LogError("ExperimentManager not assigned in UIController.");
            // return; // 如果是必要的，可以返回
        }
         if (MoleculeSpawner.Instance == null) // 假设 MoleculeSpawner 也是单例或易于访问
        {
            Debug.LogError("MoleculeSpawner not accessible in UIController.");
            // return;
        }


        if (addMoleculeButton != null)
        {
            addMoleculeButton.onClick.AddListener(OnAddMoleculesClicked);
        }
        if (removeMoleculeButton != null)
        {
            removeMoleculeButton.onClick.AddListener(OnRemoveMoleculesClicked);
        }
        if (moleculeCountInputField != null)
        {
            moleculeCountInputField.onEndEdit.AddListener(SetMoleculesToAddRemove);
            moleculeCountInputField.text = _moleculesToAddRemove.ToString();
        }

        UpdateMoleculeCountDisplay(); // 初始更新
    }

    void OnAddMoleculesClicked()
    {
        if (MoleculeSpawner.Instance != null)
        {
           MoleculeSpawner.Instance.AddMolecules(_moleculesToAddRemove);
           UpdateMoleculeCountDisplay(); // 如果直接调用，需要自己更新UI
        }
    }

    void OnRemoveMoleculesClicked()
    {
        if (MoleculeSpawner.Instance != null)
        {
           MoleculeSpawner.Instance.RemoveMolecules(_moleculesToAddRemove);
           UpdateMoleculeCountDisplay();
        }
    }

    void SetMoleculesToAddRemove(string value)
    {
        if (int.TryParse(value, out int count))
        {
            _moleculesToAddRemove = Mathf.Max(1, count); // 至少为1
        }
        moleculeCountInputField.text = _moleculesToAddRemove.ToString(); // 更新输入框，防止无效输入
    }

    public void UpdateMoleculeCountDisplay()
    {
        if (currentMoleculeCountText != null && MoleculeSpawner.Instance != null) // 假设 Spawner 是单例或可访问
        {
            currentMoleculeCountText.text = $"分子数量: {MoleculeSpawner.Instance.CurrentMoleculeCount}";
        }
    }
}