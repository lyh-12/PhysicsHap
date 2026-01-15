// ExperimentManager.cs
// (文件的开头部分保持不变)
using UnityEngine;
using System;
using System.Drawing.Printing;

public class ExperimentManager : MonoBehaviour
{
    public static ExperimentManager Instance { get; private set; }

    [Header("Component References")]
    [SerializeField] private PistonController pistonController;

    public enum ExperimentProcessType
    {
        NONE,       // <-- 新增：未选择模式
        ISOTHERMAL, // 等温
        ISOBARIC,   // 等压
        ISOCHORIC   // 等容
    }

    [Header("Experiment Process Control")]
    [Tooltip("当前实验过程的类型")]
    public ExperimentProcessType currentProcessType = ExperimentProcessType.NONE;

    [Tooltip("等温过程中设定的目标温度 (Kelvin)")]
    public float targetFixedTemperature = 293.15f;

    // --- 这是修改点 1 ---
    [Tooltip("等压过程中设定的目标压强 (atm)")]
    public float targetFixedPressure = 1.0f; // 默认值从 101325f 改为 1.0f
    // --- 修改结束 ---

    [Tooltip("等容过程中设定的目标体积 (cm³)")]
    public float targetFixedVolume_cm3 = 15000f;


    [Header("Experiment Setup")]
    [Tooltip("每次通过UI请求增减的默认分子数量")]
    public int moleculesPerAction = 10;
    [Tooltip("新加入分子的源温度 (Kelvin)")]
    public float moleculeSourceTemperature = 293.15f;
    [Tooltip("实验开始时的初始分子数量")]
    public int initialMoleculeCount = 10;

    [Header("Physics Parameters")]
    // (相关变量保持不变)
    
    // 内部热力学状态变量
    private float _currentTemperature;
    private float _currentPressure;
    private float _currentVolume;
    private int _currentMoleculeCount; // 可视化分子的数量
    
    [Header("自动记录设置")]
    [Tooltip("定义P,V,T值变化多少才算“显著变化”以记录一个新点")]
    [SerializeField] private float autoRecordThreshold = 0.01f;

    // 用于存储上一个记录点的值，以防止重复记录
    private float _lastRecordedPressure = float.NaN;
    private float _lastRecordedVolume = float.NaN;
    private float _lastRecordedTemperature = float.NaN;

    // 热力学计算器实例
    private ThermodynamicsCalculator thermoCalculator;

    // (事件定义保持不变)
    public event Action<int> MoleculeCountChanged;
    public event Action<float> VolumeChanged;
    public event Action<float> TemperatureChanged;
    public event Action<float> PressureChanged;
    public event Action<float, float> OnPVDataPointRecorded; 
    public event Action<float, float> OnVTDataPointRecorded; 
    public event Action<float, float> OnPTDataPointRecorded; 
    public event Action OnExperimentReset;
    
    // --- 用于备份初始参数 ---
    private float _initialTargetTemperature;
    private float _initialTargetPressure;
    private float _initialTargetVolume_cm3;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        thermoCalculator = new ThermodynamicsCalculator();
    }

    void Start()
    {
        if (!ValidateComponents()) return;

        pistonController.VolumeChanged += OnPistonVolumeChanged;
        if (MoleculeSpawner.Instance != null)
        {
            MoleculeSpawner.Instance.ActualMoleculeCountChanged += OnSpawnerMoleculeCountChanged;
        }
        
        // --- 备份初始值 ---
        _initialTargetTemperature = targetFixedTemperature;
        _initialTargetPressure = targetFixedPressure; // 现在这里备份的是 1.0f (atm)
        _initialTargetVolume_cm3 = targetFixedVolume_cm3;
        
        InitializeSystemState();
        BroadcastAllCurrentStateEvents();
    }

    void OnDestroy()
    {
        if (pistonController != null)
        {
            pistonController.VolumeChanged -= OnPistonVolumeChanged;
        }
        if (MoleculeSpawner.Instance != null)
        {
            MoleculeSpawner.Instance.ActualMoleculeCountChanged -= OnSpawnerMoleculeCountChanged;
        }
    }

    private void InitializeSystemState()
    {
        _currentMoleculeCount = initialMoleculeCount;
        float scaledMoleAmount = _currentMoleculeCount / 10f; 

        // --- ↓↓↓ 这是新的修复逻辑 (替换原有的 switch 块) ↓↓↓ ---

        // 无论当前模式是什么 (包括 NONE)，
        // 我们都必须根据场景的物理设置来初始化热力学状态。
        // 我们将使用“等温模式”的默认值作为整个系统的初始状态。

        // 1. 获取默认的初始温度 (来自 targetFixedTemperature 的序列化值)
        _currentTemperature = targetFixedTemperature;

        // 2. 获取活塞在场景中的【实际】初始体积
        // (PistonController 在 Awake() 时已设置好 _currentVolume)
        _currentVolume = pistonController.CurrentVolume; 

        // 3. 根据 T 和 V，计算出初始的压强 P
        //    (确保 moleAmount > 0, T > 0, V > 0)
        if (scaledMoleAmount > 0f && _currentVolume > 0f && _currentTemperature > 0f)
        {
            _currentPressure = thermoCalculator.CalculatePressure(scaledMoleAmount, _currentVolume, _currentTemperature);
        }
        else
        {
            _currentPressure = 0f; // 安全回退
        }
        
        if (currentProcessType == ExperimentProcessType.ISOCHORIC)
        {
            _currentVolume = targetFixedVolume_cm3;
            _currentTemperature = targetFixedTemperature; // 使用等容的目标温度
            _currentPressure = thermoCalculator.CalculatePressure(scaledMoleAmount, _currentVolume, _currentTemperature);
            SetPistonToVolume(_currentVolume); // 锁定活塞
        }

        if (MoleculeSpawner.Instance != null && initialMoleculeCount > 0)
        {
            MoleculeSpawner.Instance.ClearAllMolecules(); 
            MoleculeSpawner.Instance.AddMolecules(initialMoleculeCount);
        }
        
        // 根据当前模式更新活塞的输入状态 
        UpdatePistonUserInputState();
        
        ResetLastRecordedState();
    }

    public void UpdateThermodynamicState()
    {
        // if (currentProcessType == ExperimentProcessType.NONE) return;
        
        
        
        float scaledMoleAmount = _currentMoleculeCount / 10f;
        
        if (currentProcessType == ExperimentProcessType.NONE)
        {
            // 在 NONE 模式下，我们也需要保持P,V,T状态一致
            // 我们假定 T 固定 (来自默认值), V 可变 (来自活塞), P 计算得出
            // float scaledMoleAmount = _currentMoleculeCount / 10f;
            if (scaledMoleAmount <= 0)
            {
                _currentPressure = 0f;
                _currentTemperature = targetFixedTemperature;
            }
            else
            {
                _currentTemperature = targetFixedTemperature; // [2] 使用默认温度
                _currentVolume = pistonController.CurrentVolume; // [3] 从活塞读取当前体积
                
                // [4] 根据 T 和 V 计算出正确的 P
                _currentPressure = thermoCalculator.CalculatePressure(scaledMoleAmount, _currentVolume, _currentTemperature); 
            }

            BroadcastAllCurrentStateEvents(); // [5] 广播这个一致的初始状态
            return; // 退出，不执行下面的 switch
        }

        if (scaledMoleAmount <= 0)
        {
            _currentPressure = 0f;
             switch (currentProcessType)
            {
                case ExperimentProcessType.ISOTHERMAL:
                    _currentTemperature = targetFixedTemperature;
                    break;
                case ExperimentProcessType.ISOBARIC:
                     _currentTemperature = 0f; // 体积为0，温度为0
                    break;
                case ExperimentProcessType.ISOCHORIC:
                    _currentTemperature = targetFixedTemperature;
                    break;
            }
            BroadcastAllCurrentStateEvents();
            return;
        }

        switch (currentProcessType)
        {
            case ExperimentProcessType.ISOTHERMAL:
                _currentTemperature = targetFixedTemperature;
                _currentVolume = pistonController.CurrentVolume; // 读取用户输入的体积
                _currentPressure = thermoCalculator.CalculatePressure(scaledMoleAmount, _currentVolume, _currentTemperature);
                break;

            case ExperimentProcessType.ISOBARIC:
                // --- 这是修改后的“等压”逻辑 (V -> T) ---
        
                // 1. 压强是固定的 (来自 targetFixedPressure)
                _currentPressure = targetFixedPressure;         
        
                // 2. 体积是输入量 (从活塞读取)
                _currentVolume = pistonController.CurrentVolume; 

                // 3. 反向计算温度 T = PV / (nR)
                //    (thermoCalculator 已经在 Scripts/Controller/PhysicsCalculations/ThermodynamicsCalculator.cs 中定义)
                _currentTemperature = thermoCalculator.CalculateTemperature(scaledMoleAmount, _currentVolume, _currentPressure);
        
                // 4. (重要) 将计算出的温度同步回 "targetFixedTemperature"
                //    这样，如果您再次切换回 T -> V 逻辑，它会从正确的值开始。
                targetFixedTemperature = _currentTemperature;
        
                break;

            case ExperimentProcessType.ISOCHORIC:
                _currentVolume = targetFixedVolume_cm3;
                _currentTemperature = targetFixedTemperature; // 读取用户按钮设置的温度
                _currentPressure = thermoCalculator.CalculatePressure(scaledMoleAmount, _currentVolume, _currentTemperature);
                // 活塞位置在 InitializeSystemState 或 SetProcessMode 中已经设置，此处不动
                break;
        }
        
        // 检查当前状态是否与上一个记录的状态有显著不同
        if (HasStateChangedSignificantly())
        {
            // 1. 状态已改变，自动请求记录数据点
            RequestRecordDataPoint();
            
            // 2. 更新“上一个记录的状态”为当前状态
            _lastRecordedPressure = _currentPressure;
            _lastRecordedVolume = _currentVolume;
            _lastRecordedTemperature = _currentTemperature;
        }

        BroadcastAllCurrentStateEvents();
    }

    private void OnPistonVolumeChanged(float newPistonVolume)
    {
        // 在“固定体积”或“固定压强”模式下，活塞是程序锁定的
        if (currentProcessType == ExperimentProcessType.ISOCHORIC)
        {
            // _currentVolume 是由 UpdateThermodynamicState 计算出的目标值
            // 如果活塞（因意外）移动，强制它回去
            SetPistonToVolume(_currentVolume); 
            return;
        }

        // 只有在“固定温度”模式下，才允许用户移动活塞
        if (Mathf.Approximately(_currentVolume, newPistonVolume)) return;

        _currentVolume = newPistonVolume;
        UpdateThermodynamicState(); // 触发状态更新
    }
    
    private void OnSpawnerMoleculeCountChanged(int newTotalMoleculeCount)
    {
        _currentMoleculeCount = newTotalMoleculeCount;
        UpdateThermodynamicState();
    }

    // (RequestAddMolecules, RequestRemoveMolecules, RequestMoleculesChange 保持不变)
    public void RequestAddMolecules() => RequestMoleculesChange(moleculesPerAction, true);
    public void RequestRemoveMolecules() => RequestMoleculesChange(moleculesPerAction, false);
    public void RequestMoleculesChange(int count, bool add)
    {
        if (count <= 0 || MoleculeSpawner.Instance == null) return;
        if (add) MoleculeSpawner.Instance.AddMolecules(count);
        else MoleculeSpawner.Instance.RemoveMolecules(count);
    }
    
    // (RequestRecordDataPoint 保持不变)
    public void RequestRecordDataPoint()
    {
        if (currentProcessType == ExperimentProcessType.NONE) return;
        
        switch (currentProcessType)
        {
            case ExperimentProcessType.ISOTHERMAL:
                Debug.Log($"Recording P-V data point: P={_currentPressure:F2}, V={_currentVolume:F2}");
                OnPVDataPointRecorded?.Invoke(_currentPressure, _currentVolume);
                break;
            case ExperimentProcessType.ISOBARIC:
                Debug.Log($"Recording V-T data point: V={_currentVolume:F2}, T={_currentTemperature:F2}");
                OnVTDataPointRecorded?.Invoke(_currentVolume, _currentTemperature);
                break;
            case ExperimentProcessType.ISOCHORIC:
                Debug.Log($"Recording P-T data point: P={_currentPressure:F2}, T={_currentTemperature:F2}");
                OnPTDataPointRecorded?.Invoke(_currentPressure, _currentTemperature);
                break;
        }
    }
    
    public void SetProcessType(int typeIndex)
    {
        if (Enum.IsDefined(typeof(ExperimentProcessType), typeIndex))
        {
            currentProcessType = (ExperimentProcessType)typeIndex;
            Debug.Log($"Experiment process type changed to: {currentProcessType}");
            
            // (这会调用 InitializeSystemState 和 UpdatePistonUserInputState)
            SetProcessMode(currentProcessType); 
        }
    }
    
    // 文件: Scripts/Core/ExperimentManager.cs

    private void SetPistonToVolume(float targetVolume_cm3)
    {
        if (pistonController == null) return;
 
        // --- ↓↓↓ 这是关键的修复逻辑 ↓↓↓ ---

        // 1. 获取 PistonController 使用的参数
        float radius = pistonController.GetContainerRadius(); // (即 2.0f)
        float magicScalingFactor = 3000f; // (来自 PistonController.cs 的魔法数字)
        float containerBottomWorldY = pistonController.GetContainerBottomWorldY();

        // 2. 使用 PistonController 的【反向公式】来计算 height_m
        float height_m;
        float denominator = (Mathf.PI * Mathf.Pow(radius, 2) * magicScalingFactor);

        if (Mathf.Abs(denominator) < float.Epsilon)
        {
            height_m = 0f; // 防止除以零
        }
        else
        {
            // h = V / (pi * r^2 * 3000)
            height_m = targetVolume_cm3 / denominator;
        }

        // 3. 将 height_m (米) 转换为目标世界 Y 坐标
        float targetWorldY = containerBottomWorldY + height_m;

        // --- ↑↑↑ 修复逻辑结束 (旧的SI单位计算已删除) ↑↑↑ ---


        // (方法的其余部分保持不变：将 WorldY 转换为 LocalY)
        Transform parent = pistonController.transform.parent;
        float targetLocalY;

        if (parent != null)
        {
            Vector3 targetWorldPos = pistonController.transform.position;
            targetWorldPos.y = targetWorldY;
            targetLocalY = parent.InverseTransformPoint(targetWorldPos).y;
        }
        else
        {
            targetLocalY = targetWorldY; 
        }
   
        // (您的日志记录)
        Debug.Log($"[ExperimentManager] 命令活塞: V={targetVolume_cm3:F2} cm³  ==>  H={height_m:F4} m  ==>  LocalY={targetLocalY:F4}");
   
        pistonController.SetPistonLocalYPosition(targetLocalY);
    }


    private void BroadcastAllCurrentStateEvents() 
    {
        MoleculeCountChanged?.Invoke(_currentMoleculeCount);
        VolumeChanged?.Invoke(_currentVolume);
        TemperatureChanged?.Invoke(_currentTemperature);
        PressureChanged?.Invoke(_currentPressure);
    }

    private bool ValidateComponents()
    {
        if (pistonController == null)
        {
            Debug.LogError("PistonController not assigned!", this);
            enabled = false;
            return false;
        }
        if (MoleculeSpawner.Instance == null)
        {
            Debug.LogWarning("MoleculeSpawner not found. Molecule functionality will be unavailable.", this);
        }
        return true;
    }
    
    public void SetProcessMode(ExperimentProcessType newMode)
    {
        // 如果模式没有实际变化，则不执行任何操作
        // if (currentProcessType == newMode) return; 

        Debug.Log("实验模式已切换为: " + newMode);

        // 1. 读取按下按钮这一帧的【当前】热力学状态
        //    (这些值是上一帧 UpdateThermodynamicState() 刚计算出来的)
        float currentPressure = _currentPressure;
        float currentVolume = _currentVolume;
        float currentTemperature = _currentTemperature;

        // 2. 将新模式设为当前模式
        currentProcessType = newMode;

        // 3. 根据新模式，将“目标固定值”更新为【当前】的值
        switch (currentProcessType)
        {
            case ExperimentProcessType.ISOTHERMAL: // 等温
                // 锁定当前温度
                targetFixedTemperature = currentTemperature;
                Debug.Log($"模式切换: 等温。锁定温度为 {currentTemperature:F2} K");
                break;
            
            case ExperimentProcessType.ISOBARIC: // 等压
                // 锁定当前压强
                targetFixedPressure = currentPressure;
                // targetFixedPressure = 6;
                Debug.Log($"模式切换: 等压。锁定压强为 {currentPressure:F2} atm");
                break;

            case ExperimentProcessType.ISOCHORIC: // 等容
                // 锁定当前体积
                targetFixedVolume_cm3 = currentVolume;
                Debug.Log($"模式切换: 等容。锁定体积为 {currentVolume:F2} cm³");
                
                // 【关键】: 我们不再调用 SetPistonToVolume()
                // 活塞会保持在当前位置不动。
                break;
        }
        
        // 4. 更新活塞的用户输入状态（例如，等容模式下禁用活塞输入）
        UpdatePistonUserInputState();
        
        // 5. 广播当前状态（值没变，但UI和图表可能需要知道模式变了）
        BroadcastAllCurrentStateEvents();
        
        // 6. 重置自动记录器，以便新模式的第一个数据点可以被记录
        // ResetLastRecordedState();
        PrimeLastRecordedState();
    }
    
    private void UpdatePistonUserInputState()
    {
        if (pistonController != null)
        {
            // 判断是否为“固定温度”模式
            bool canUserInput = (currentProcessType == ExperimentProcessType.ISOTHERMAL || 
                                 currentProcessType == ExperimentProcessType.ISOBARIC); // <-- 新增 || ISOBARIC

            pistonController.SetUserInputActive(canUserInput);
        }
    }
    
    public float GetCurrentTemperature()
    {
        return _currentTemperature;
    }
    
    // 文件: Scripts/Core/ExperimentManager.cs

    public void ResetExperiment()
    {
        Debug.Log("===== EXPERIMENT RESETTING =====");

        OnExperimentReset?.Invoke();

        // 1. 清空所有分子 (这会通过事件将 _currentMoleculeCount 设为 0)
        if (MoleculeSpawner.Instance != null)
        {
            MoleculeSpawner.Instance.ClearAllMolecules(); 
        }

        // --- 恢复到初始值 ---
        targetFixedTemperature = _initialTargetTemperature;
        targetFixedPressure = _initialTargetPressure; // 恢复为 1.0f (atm)
        targetFixedVolume_cm3 = _initialTargetVolume_cm3;
        
        // --- ↓↓↓ 关键修复 1：将逻辑模式也重置为 NONE，与 UI 保持一致 ↓↓↓ ---
        currentProcessType = ExperimentProcessType.NONE; 
        // --- 修复 1 结束 ---

        // 2. 重新计算初始 P,V,T 状态 (此时 _currentMoleculeCount 仍为 0)
        InitializeSystemState(); 
        
        // --- ↓↓↓ 关键修复 2：在所有状态重置后，【强制】重新添加分子 ↓↓↓ ---
        // (这可以防止 InitializeSystemState 中的复杂逻辑导致分子为 0)
        if (MoleculeSpawner.Instance != null && initialMoleculeCount > 0)
        {
            // 再次确保清空 (以防万一)
            MoleculeSpawner.Instance.ClearAllMolecules(); 
            // 重新添加初始数量的分子
            MoleculeSpawner.Instance.AddMolecules(initialMoleculeCount);
            // 立即【手动】将管理器内的计数器同步为正确的值
            _currentMoleculeCount = initialMoleculeCount;
        }
        else
        {
            // 确保如果初始值为0，计数器也为0
            _currentMoleculeCount = 0; 
        }
        // --- 修复 2 结束 ---

        // 3. 广播【包含分子】的正确 P,V,T 初始值
        // (我们在这里手动调用一次 Update，以确保压强等值是基于 n=10 计算的)
        UpdateThermodynamicState(); 
        BroadcastAllCurrentStateEvents(); 
        
        ResetLastRecordedState();

        Debug.Log("===== EXPERIMENT RESET COMPLETE =====");
    }

    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.R))
    //     {
    //         Debug.Log("'R' key pressed. Requesting data point record.");
    //         RequestRecordDataPoint(); 
    //     }
    // }
    
    /// <summary>
    /// 检查当前热力学状态是否与上一个记录的状态有显著变化。
    /// </summary>
    /// <returns>如果任一变量变化超过阈值，则返回 true</returns>
    private bool HasStateChangedSignificantly()
    {
        // 检查是否是第一次记录 (NaN == Not a Number)
        if (float.IsNaN(_lastRecordedPressure))
        {
            return true; // 总是记录第一个点
        }

        // 比较P, V, T的当前值和上次记录的值
        // 只要有一个值的变化量大于阈值，就返回 true
        
        bool pressureChanged = Mathf.Abs(_currentPressure - _lastRecordedPressure) > autoRecordThreshold;
        bool volumeChanged = Mathf.Abs(_currentVolume - _lastRecordedVolume) > (autoRecordThreshold * 10f); // 体积的阈值可以适当放大
        bool temperatureChanged = Mathf.Abs(_currentTemperature - _lastRecordedTemperature) > autoRecordThreshold;

        return pressureChanged || volumeChanged || temperatureChanged;
    }
    
    /// <summary>
    /// 重置上一个记录的状态
    /// </summary>
    private void ResetLastRecordedState()
    {
        _lastRecordedPressure = float.NaN;
        _lastRecordedVolume = float.NaN;
        _lastRecordedTemperature = float.NaN;
    }
    
    private void PrimeLastRecordedState()
    {
        // "预记录" 当前状态，但不触发绘图
        _lastRecordedPressure = _currentPressure;
        _lastRecordedVolume = _currentVolume;
        _lastRecordedTemperature = _currentTemperature;
    }
}