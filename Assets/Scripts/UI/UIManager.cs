// UIManager.cs
// (文件的开头部分保持不变)
using System;
using UnityEngine;
using TMPro; // 引入 TextMeshPro 命名空间

// --- 新增：确保对象上有 AudioSource 组件 ---
[RequireComponent(typeof(AudioSource))]
public class UIManager : MonoBehaviour
{
    [Header("Display Elements - TextMeshPro")]
    // (您原有的 TextMeshPro 引用保持不变)
    [Tooltip("用于显示气体分子数量的 TextMeshPro 组件")]
    public TextMeshPro moleculeCountText;

    [Tooltip("用于显示容器体积的 TextMeshPro 组件")]
    public TextMeshPro volumeText;

    [Tooltip("（未来扩展）用于显示气体压强的 TextMeshPro 组件")]
    public TextMeshPro pressureText;
    
    [Tooltip("（未来扩展）用于显示温度的 TextMeshPro 组件")]
    public TextMeshPro temperatureText;

    [Header("图表对象引用")]
    // (您原有的图表引用保持不变)
    [Tooltip("用于显示【等温模式】图表(P-V 和 P-1/V)的【父物体】")]
    [SerializeField]
    private GameObject isothermalGraphGroup; 

    [Tooltip("用于显示V-T图的对象 (固定压强时显示)")]
    [SerializeField]
    private GameObject vtGraphDrawer;

    [Tooltip("用于显示P-T图的对象 (固定体积时显示)")]
    [SerializeField]
    private GameObject ptGraphDrawer;
    
    [Header("参数控制UI (Parameter Controls)")]
    // (您原有的参数控制UI引用保持不变)
    [Tooltip("包含“增/减温度”按钮的父物体 (GameObject)")]
    public GameObject temperatureControls; 

    [Tooltip("包含“增/减压强”按钮的父物体 (GameObject)")]
    public GameObject pressureControls; 
    
    [Header("模式选择按钮 (Mode Select Buttons)")]
    // (您原有的模式按钮引用保持不变)
    [Tooltip("“固定温度”按钮的游戏对象")]
    public GameObject lockTemperatureButton; 

    [Tooltip("“固定压强”按钮的游戏对象")]
    public GameObject lockPressureButton; 

    [Tooltip("“固定体积”按钮的游戏对象")]
    public GameObject lockVolumeButton; 
    
    // --- ↓↓↓ 这是新增的代码 (Part 1) ↓↓↓ ---
    [Header("Audio Guidance Clips")]
    [Tooltip("用于播放指导音频的 AudioSource 组件")]
    [SerializeField]
    private AudioSource guidanceAudioSource;

    [Tooltip("点击【等温】时播放的音频")]
    [SerializeField]
    private AudioClip isothermalGuidanceClip;

    [Tooltip("点击【等容】时播放的音频")]
    [SerializeField]
    private AudioClip isochoricGuidanceClip;

    [Tooltip("点击【等压】时播放的音频")]
    [SerializeField]
    private AudioClip isobaricGuidanceClip;
    
    [SerializeField]
    private AudioClip isochoricInputWarningClip;
    
    [Tooltip("在未选择模式（重置后）时尝试操作Arduino时播放的音频")]
    [SerializeField]
    private AudioClip noModeSelectedWarningClip; // <-- 新增字段
    // --- ↑↑↑ 新增代码结束 (Part 1) ↑↑↑ ---
    
    [Header("Component References")]
    [Tooltip("对 ParameterController 脚本的引用")]
    [SerializeField]
    private ParameterController parameterController; // 引用 ParameterController

    [Header("Limit Display Elements")]
    [Tooltip("用于显示最低温度的 TextMeshPro 组件")]
    public TextMeshPro minTemperatureText;
    [Tooltip("用于显示最高温度的 TextMeshPro 组件")]
    public TextMeshPro maxTemperatureText;


    // --- ↓↓↓ 这是新增的方法 (Part 2) ↓↓↓ ---
    /// <summary>
    /// Awake 在 Start 之前运行，用于初始化组件
    /// </summary>
    void Awake()
    {
        // 自动获取 AudioSource 组件
        if (guidanceAudioSource == null)
        {
            guidanceAudioSource = GetComponent<AudioSource>();
        }
        
        // 设置 AudioSource 属性，确保它不会在启动时自动播放
        if (guidanceAudioSource != null)
        {
            guidanceAudioSource.playOnAwake = false;
        }
        else
        {
            Debug.LogError("UIManager: 未找到 AudioSource 组件！", this);
        }
    }
    // --- ↑↑↑ 新增方法结束 (Part 2) ↑↑↑ ---

    
    /// <summary>
    /// Start 方法保持您原有的逻辑
    /// </summary>
    void Start()
    {
        // (您原有的 Start 逻辑完全保留)
        // 初始化时可以设置默认文本
        if (moleculeCountText != null)
        {
            moleculeCountText.text = "0.5 mol";
        }
        if (volumeText != null)
        {
            volumeText.text = "0 cm\u00b3";
        }
        if (pressureText != null)
        {
            pressureText.text = "0 atm";
        }
        if (temperatureText != null)
        {
            temperatureText.text = "0 K";
        }

        SubscribeToExperimentEvents();

        // --- 逻辑：启动时 ---
        // 1. 显示所有模式按钮，让用户选择
        ShowAllModeButtons(); 
        // 2. 隐藏所有参数控制按钮
        UpdateParameterControlsUI(false, false);
        
        // 3. 默认将 *逻辑* 设置为等温（即使户未点击）
        if (ExperimentManager.Instance != null)
        {
            // ExperimentManager.Instance.SetProcessMode(ExperimentManager.ExperimentProcessType.ISOTHERMAL);
        }
        
        // 4. 默认显示P-V图组
        SetActiveGraph(isothermalGraphGroup);
        
        UpdateTemperatureLimitDisplay();
    }

    /// <summary>
    /// OnDestroy 方法保持您原有的逻辑
    /// </summary>
    private void OnDestroy()
    {
        // (您原有的 OnDestroy 逻辑完全保留)
        UnsubscribeFromExperimentEvents();
    }

    #region UI Update Methods (文本更新)
    // (您原有的 UI Update Methods 区域完全保留)
    
    public void UpdateMoleculeCount(int count)
    {
        float c = count / 10f;
        if (moleculeCountText != null) { moleculeCountText.text = $"{c} mol"; }
    }
    
    public void UpdateVolumeDisplay(float volume)
    {
        if (volumeText != null) { volumeText.text = $"{volume:F2} cm\u00b3"; }
    }
    
    public void UpdateTemperatureDisplay(float temperature)
    {
        if (temperatureText != null) { temperatureText.text = $"{temperature:F2} K"; }
    }
    
    public void UpdatePressureDisplay(float pressure)
    {
        if (pressureText != null) { pressureText.text = $"{pressure:F2} atm"; }
    }
    
    #endregion
    
    #region Event Subscription (事件订阅)
    // (您原有的 Event Subscription 区域完全保留)
    
    private void SubscribeToExperimentEvents()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.MoleculeCountChanged += HandleMoleculeCountChanged;
            ExperimentManager.Instance.VolumeChanged += HandleVolumeChanged;
            ExperimentManager.Instance.TemperatureChanged += HandleTemperatureChanged;
            ExperimentManager.Instance.PressureChanged += HandlePressureChanged;
        }
        else
        {
            Debug.LogError("UIManager: ExperimentManager.Instance 在 Start 时未找到！UI 将不会自动更新。", this);
        }
    }

    private void UnsubscribeFromExperimentEvents()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.MoleculeCountChanged -= HandleMoleculeCountChanged;
            ExperimentManager.Instance.VolumeChanged -= HandleVolumeChanged;
            ExperimentManager.Instance.TemperatureChanged -= HandleTemperatureChanged;
            ExperimentManager.Instance.PressureChanged -= HandlePressureChanged;
        }
    }
    
    #endregion

    #region Event Handlers (事件处理器)
    // (您原有的 Event Handlers 区域完全保留)

    private void HandleMoleculeCountChanged(int count)
    {
        float c = count / 10f;
        if (moleculeCountText != null) { moleculeCountText.text = $"{c} mol"; }
    }

    private void HandleVolumeChanged(float volume)
    {
        if (volumeText != null) { volumeText.text = $"{volume:F2} cm³"; }
    }
    
    private void HandleTemperatureChanged(float temperature)
    {
        if (temperatureText != null) { temperatureText.text = $"{temperature:F2} K"; }
    }

    private void HandlePressureChanged(float pressure)
    {
        if (pressureText != null) { pressureText.text = $"{pressure:F2} atm"; }
    }
    
    #endregion
    
    #region Public Methods for Unity Buttons (按钮调用)

    /// <summary>
    /// 按钮调用：将实验模式设置为【固定温度】
    /// </summary>
    public void SetModeToIsothermal()
    {
        if (ExperimentManager.Instance == null) return;
        
        // --- ↓↓↓ 这是修改点 (Part 3) ↓↓↓ ---
        // 1. 播放音频
        PlayGuidanceAudio(isothermalGuidanceClip);
        // --- ↑↑↑ 修改点结束 (Part 3) ↑↑↑ ---

        // (您原有的逻辑完全保留)
        // 2. 设置实验核心逻辑
        ExperimentManager.Instance.SetProcessMode(ExperimentManager.ExperimentProcessType.ISOTHERMAL);
        
        // 3. 更新图表 (激活等温图表组)
        SetActiveGraph(isothermalGraphGroup);
        
        // 4. 更新参数按钮（等温时全隐藏）
        UpdateParameterControlsUI(false, false);
        // 5. 更新模式按钮（只显示“固定温度”按钮）
        UpdateModeButtonsUI(lockTemperatureButton);
    }

    /// <summary>
    /// 按钮调用：将实验模式设置为【固定体积】
    /// </summary>
    public void SetModeToIsochoric()
    {
        if (ExperimentManager.Instance == null) return;
        
        // --- ↓↓↓ 这是修改点 (Part 3) ↓↓↓ ---
        // 1. 播放音频
        PlayGuidanceAudio(isochoricGuidanceClip);
        // --- ↑↑↑ 修改点结束 (Part 3) ↑↑↑ ---
        
        // (您原有的逻辑完全保留)
        // 2. 设置实验核心逻辑
        ExperimentManager.Instance.SetProcessMode(ExperimentManager.ExperimentProcessType.ISOCHORIC);
        // 3. 更新图表
        SetActiveGraph(ptGraphDrawer);
        // 4. 更新参数按钮（显示温度控制）
        UpdateParameterControlsUI(true, false);
        // 5. 更新模式按钮（只显示“固定体积”按钮）
        UpdateModeButtonsUI(lockVolumeButton);
    }

    /// <summary>
    /// 按钮调用：将实验模式设置为【固定压强】
    /// </summary>
    public void SetModeToIsobaric()
    {
        if (ExperimentManager.Instance == null) return;
        
        // --- ↓↓↓ 这是修改点 (Part 3) ↓↓↓ ---
        // 1. 播放音频
        PlayGuidanceAudio(isobaricGuidanceClip);
        // --- ↑↑↑ 修改点结束 (Part 3) ↑↑↑ ---

        // (您原有的逻辑完全保留)
        // 2. 设置实验核心逻辑
        ExperimentManager.Instance.SetProcessMode(ExperimentManager.ExperimentProcessType.ISOBARIC);
        // 3. 更新图表
        SetActiveGraph(vtGraphDrawer);
        // 4. 更新参数按钮（等压时显示“温度”控制）
        UpdateParameterControlsUI(false, false);
        // 5. 更新模式按钮（只显示“固定压强”按钮）
        UpdateModeButtonsUI(lockPressureButton);
    }
    
    /// <summary>
    /// 按钮调用：请求 ExperimentManager 重置整个实验。
    /// </summary>
    public void OnResetButtonPressed()
    {
        // (您原有的 OnResetButtonPressed 逻辑完全保留)
        // 1. 重置核心实验逻辑（这将触发 OnExperimentReset 事件，清空图表）
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.ResetExperiment(); //
        }
        else
        {
            Debug.LogError("Cannot reset, ExperimentManager instance not found!");
        }
        
        // 2. 将UI重置回“模式选择”状态：显示所有模式按钮
        ShowAllModeButtons();
        // 3. 隐藏所有参数控制按钮
        UpdateParameterControlsUI(false, false);
        
        // 4. 默认激活等温图表组
        SetActiveGraph(isothermalGraphGroup);
    }

    #endregion

    #region Private Helper Methods (私有辅助方法)
    // (您原有的 Private Helper Methods 区域完全保留)

    /// <summary>
    /// 辅助方法：激活指定的图表，并确保其他两个图表被隐藏。
    /// </summary>
    private void SetActiveGraph(GameObject graphToActivate)
    {
        if (isothermalGraphGroup != null) { isothermalGraphGroup.SetActive(isothermalGraphGroup == graphToActivate); }
        if (vtGraphDrawer != null) { vtGraphDrawer.SetActive(vtGraphDrawer == graphToActivate); }
        if (ptGraphDrawer != null) { ptGraphDrawer.SetActive(ptGraphDrawer == graphToActivate); }
    }
    
    /// <summary>
    /// 辅助方法：更新参数控制按钮的可见性
    /// </summary>
    private void UpdateParameterControlsUI(bool showTemperature, bool showPressure)
    {
        if (temperatureControls != null) { temperatureControls.SetActive(showTemperature); }
        if (pressureControls != null) { pressureControls.SetActive(showPressure); }
    }
    
    /// <summary>
    /// 辅助方法：更新模式切换按钮的可见性
    /// 它会显示传入的按钮，并隐藏另外两个。
    /// </summary>
    private void UpdateModeButtonsUI(GameObject buttonToActivate)
    {
        if (lockTemperatureButton != null) { lockTemperatureButton.SetActive(lockTemperatureButton == buttonToActivate); }
        if (lockPressureButton != null) { lockPressureButton.SetActive(lockPressureButton == buttonToActivate); }
        if (lockVolumeButton != null) { lockVolumeButton.SetActive(lockVolumeButton == buttonToActivate); }
    }
    
    /// <summary>
    /// 辅助方法：显示所有模式按钮
    /// </summary>
    private void ShowAllModeButtons()
    {
        if (lockTemperatureButton != null) lockTemperatureButton.SetActive(true);
        if (lockPressureButton != null) lockPressureButton.SetActive(true);
        if (lockVolumeButton != null) lockVolumeButton.SetActive(true);
    }
    
    // --- ↓↓↓ 这是新增的方法 (Part 4) ↓↓↓ ---
    /// <summary>
    /// 辅助方法：播放一个指导音频片段
    /// </summary>
    private void PlayGuidanceAudio(AudioClip clip)
    {
        // 检查 AudioSource 和 AudioClip 是否都有效
        if (guidanceAudioSource != null && clip != null)
        {
            // 立即停止当前可能正在播放的任何片段（警告音或指导音）
            guidanceAudioSource.Stop();
            // 设置新的剪辑并播放
            guidanceAudioSource.clip = clip;
            guidanceAudioSource.Play();
        }
    }
    // --- ↑↑↑ 新增方法结束 (Part 4) ↑↑↑ ---
    
    /// <summary>
    /// 辅助方法：从 ParameterController 获取温度限制并更新UI
    /// </summary>
    private void UpdateTemperatureLimitDisplay()
    {
        if (parameterController == null)
        {
            // 尝试在场景中自动查找，以防万一
            parameterController = FindObjectOfType<ParameterController>();
            if (parameterController == null)
            {
                Debug.LogError("UIManager: ParameterController 引用未设置，也无法在场景中自动找到！无法显示温度限制。", this);
                return;
            }
        }

        // 从 ParameterController 获取限制值
        float minTemp = parameterController.minTemperatureLimit;
        float maxTemp = parameterController.maxTemperatureLimit;

        // 更新 TextMeshPro 文本
        if (minTemperatureText != null)
        {
            // 使用 "F0" 格式化为整数
            minTemperatureText.text = $"({minTemp:F0} K - "; 
        }

        if (maxTemperatureText != null)
        {
            maxTemperatureText.text = $"{maxTemp:F0} K)";
        }
    }
    
    // (请用这个版本替换现有的 PlayIsochoricWarningAudio 方法)
    /// <summary>
    /// 公共方法：播放“等容模式下的错误操作”警告音
    /// </summary>
    public void PlayIsochoricWarningAudio()
    {
        if (guidanceAudioSource == null || isochoricInputWarningClip == null) return;

        // *** 核心修复：防止音频垃圾邮件 ***
        // 检查：如果音频源正在播放，并且播放的 *正是* 这个剪辑
        if (guidanceAudioSource.isPlaying && guidanceAudioSource.clip == isochoricInputWarningClip)
        {
            // ...那么就让它继续播放，不要打断它。
            return;
        }

        // 如果不播放，或者在播放 *其他* 剪辑（例如指导音），
        // 立即停止并播放这个更重要的警告音。
        guidanceAudioSource.Stop();
        guidanceAudioSource.clip = isochoricInputWarningClip;
        guidanceAudioSource.Play();
    }
    
    // (请用这个版本替换现有的 PlayNoModeSelectedWarningAudio 方法)
    /// <summary>
    /// 公共方法：播放“未选择模式”警告音
    /// </summary>
    public void PlayNoModeSelectedWarningAudio()
    {
        if (guidanceAudioSource == null || noModeSelectedWarningClip == null) return;

        // *** 核心修复：防止音频垃圾邮件 ***
        // 检查：如果音频源正在播放，并且播放的 *正是* 这个剪辑
        if (guidanceAudioSource.isPlaying && guidanceAudioSource.clip == noModeSelectedWarningClip)
        {
            // ...那么就让它继续播放，不要打断它。
            return;
        }
    
        // 如果不播放，或者在播放 *其他* 剪辑，
        // 立即停止并播放这个警告音。
        guidanceAudioSource.Stop();
        guidanceAudioSource.clip = noModeSelectedWarningClip;
        guidanceAudioSource.Play();
    }

    #endregion
}