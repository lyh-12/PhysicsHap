// 文件: Scripts/Controller/Container/PistonController.cs
// --- 最终修复版：
// 1. 恢复了 UpdateComponentStates() 和 SetUserInputActive() 的原始逻辑，以确保模式互斥。
// 2. 恢复了 gestureHandler 和 pokeInteractableComponent 的字段引用。
// 3. 保留了 ApplyVirtualDelta() 和 _virtualPistonWorldPosition 的逻辑。
// ---

using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PistonController : MonoBehaviour
{
    // --- 1. 控制模式定义 ---
    public enum PistonControlMode
    {
        Keyboard, // 0: 键鼠控制
        Gesture,  // 1: VR手势控制
        Arduino   // 2: 外部硬件控制
    }

    [Header("Input Control Mode")]
    [Tooltip("设置当前激活的活塞控制模式 (0=键鼠, 1=手势, 2=Arduino)")]
    [SerializeField] 
    private PistonControlMode currentControlMode = PistonControlMode.Keyboard; 

    // --- ↓↓↓ 这是恢复的关键字段 ↓↓↓ ---
    [Header("Gesture Component References")]
    [Tooltip("（必填）拖入【子物体 ButtonVisual】上挂载的 PistonInteractionHandler 脚本组件")]
    [SerializeField] 
    private PistonInteractionHandler gestureHandler;

    [Tooltip("（必填）拖入【子物体 ButtonVisual】上的 PokeInteractable 组件")]
    [SerializeField] 
    private MonoBehaviour pokeInteractableComponent; // PokeInteractable 继承自 MonoBehaviour
    // --- ↑↑↑ 字段恢复结束 ↑↑↑ ---
    
    // --- 2. 原始变量 ---
    [Header("Piston Movement Limits (Local Y)")]
    [SerializeField] private float minLocalY = 0.35f;
    [SerializeField] private float maxLocalY = 0.5f;
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private KeyCode moveDownKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode moveUpKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode altMoveDownKey = KeyCode.S;
    [SerializeField] private KeyCode altMoveUpKey = KeyCode.W;
    [Header("Container Geometry (World Space)")]
    [SerializeField] private float containerRadius = 2.0f;
    [SerializeField] private float containerBottomY = 1.097f;

    private float _currentVolume;
    public float CurrentVolume => _currentVolume;
    public event Action<float> VolumeChanged;
    
    private bool isUserInputEnabled = true; 
    private Rigidbody rb;

    // --- 虚拟位置变量 (来自上一版) ---
    private Vector3 targetWorldPosition; // 仅用于键鼠模式的真实目标
    private Vector3 _virtualPistonWorldPosition; // 手势/Arduino模式的虚拟位置
    private Vector3 _lastFramePositionUsedForCalc; // 上一帧用于计算的位置


    // --- 3. Unity 核心方法 ---
    void Awake()
    {
        // _currentVolume = 6597.35f;
        rb = GetComponent<Rigidbody>();
        if (rb == null) { rb = gameObject.AddComponent<Rigidbody>(); }
        rb.isKinematic = true;
        rb.useGravity = false;
        
        float pistonWorldY = transform.position.y; // [3] 使用当前物体的 *世界* Y 坐标

        // [4] containerBottomY 也是在 Inspector 中设置的 *世界* Y 坐标
        float height = pistonWorldY - containerBottomY; 
        if (height < 0) height = 0;

        // [5] 计算出正确的初始体积
        _currentVolume = Mathf.PI * Mathf.Pow(containerRadius, 2) * height * 3000f;

        // [6] 确保虚拟位置和最后计算位置也同步为真实的 *世界* 位置
        //     (这部分逻辑本在 Start() 中, 提前到 Awake() 更安全)
        _virtualPistonWorldPosition = transform.position;
        _lastFramePositionUsedForCalc = transform.position;
    }

    void Start()
    {
        targetWorldPosition = transform.position;
        _virtualPistonWorldPosition = transform.position;
        _lastFramePositionUsedForCalc = transform.position;

        CalculateAndBroadcastVolume(true); 
        
        // --- ↓↓↓ 恢复了关键调用 ↓↓↓ ---
        // 确保启动时，手势组件根据默认模式（键鼠）被正确禁用
        UpdateComponentStates(currentControlMode);
        // --- ↑↑↑ 调用恢复结束 ↑↑↑ ---
    }

    // --- FixedUpdate (来自上一版，逻辑正确) ---
    void FixedUpdate()
    {
        HandleMovementInput(); 

        Vector3 currentPistonPositionToUse;
        
        if (currentControlMode == PistonControlMode.Gesture)
        {
            currentPistonPositionToUse = _virtualPistonWorldPosition;
        }
        else if (currentControlMode == PistonControlMode.Arduino || 
                 currentControlMode == PistonControlMode.Keyboard)
        {
            // 1. 使用与键鼠模式完全相同的移动代码
            if ((targetWorldPosition - transform.position).sqrMagnitude > float.Epsilon)
            {
                rb.MovePosition(targetWorldPosition);
            }
            
            // 2. 用于计算体积的位置，是物体移动后的“真实”位置
            currentPistonPositionToUse = transform.position;
        }
        // --- ↑↑↑ 核心逻辑修改结束 ↑↑↑ ---
        
        else
        {
            // (以防万一，默认使用真实位置)
            currentPistonPositionToUse = transform.position;
        }

        // Debug.LogError("currentPistonPositionToUse数值：" + currentPistonPositionToUse);
        // Debug.LogError("_lastFramePositionUsedForCalc数值：" + _lastFramePositionUsedForCalc);
        
        if ((currentPistonPositionToUse - _lastFramePositionUsedForCalc).sqrMagnitude > float.Epsilon)
        {
            float pistonWorldY = currentPistonPositionToUse.y;
            float height = pistonWorldY - containerBottomY;
            if (height < 0) height = 0;

            float newVolume = Mathf.PI * Mathf.Pow(containerRadius, 2) * height * 3000f;

            if (Mathf.Abs(newVolume - _currentVolume) > float.Epsilon)
            {
                _currentVolume = newVolume; 
                VolumeChanged?.Invoke(_currentVolume); 
            }
        }
        
        _lastFramePositionUsedForCalc = currentPistonPositionToUse;
    }


    // --- 4. 模式守卫的输入方法 ---

    /// <summary>
    /// 【手势输入】由 PistonInteractionHandler (子物体) 调用
    /// </summary>
    public void ApplyVirtualDelta(Vector3 worldSpaceDelta)
    {
        // (此方法来自上一版，逻辑正确)
        if (currentControlMode != PistonControlMode.Gesture) return;
        if (!isUserInputEnabled) return;

        Vector3 newVirtualWorldPos = _virtualPistonWorldPosition + worldSpaceDelta;
        Vector3 newVirtualLocalPos = transform.InverseTransformPoint(newVirtualWorldPos);
        newVirtualLocalPos.y = Mathf.Clamp(newVirtualLocalPos.y, minLocalY, maxLocalY);
        _virtualPistonWorldPosition = transform.TransformPoint(newVirtualLocalPos);
    }

    /// <summary>
    /// 【键鼠输入】由 FixedUpdate 调用 (只在键鼠模式生效)
    /// </summary>
    private void HandleMovementInput()
    {
        // (此方法来自上一版，逻辑正确)
        if (currentControlMode != PistonControlMode.Keyboard) return; 
        if (!isUserInputEnabled) return;

        bool isMovingDown = Input.GetKey(moveDownKey) || Input.GetKey(altMoveDownKey);
        bool isMovingUp = Input.GetKey(moveUpKey) || Input.GetKey(altMoveUpKey);
        if (isMovingDown == isMovingUp) return; 

        Vector3 targetLocalPos = transform.localPosition; 
        float step = moveSpeed * Time.fixedDeltaTime * 0.3f; 

        if (isMovingDown) targetLocalPos.y -= step;
        else if (isMovingUp) targetLocalPos.y += step;

        targetLocalPos.y = Mathf.Clamp(targetLocalPos.y, minLocalY, maxLocalY);

        if (transform.parent != null)
        {
            this.targetWorldPosition = transform.parent.TransformPoint(targetLocalPos);
        }
        else
        {
            this.targetWorldPosition = targetLocalPos;
        }
    }

    /// <summary>
    /// 【程序化输入】由 ArduinoInputManager 和 ExperimentManager 调用
    /// </summary>

    
    // 文件: Scripts/Controller/Container/PistonController.cs
// 方法: SetPistonLocalYPosition()

    // 文件: Scripts/Controller/Container/PistonController.cs
// 方法: SetPistonLocalYPosition()

    // public void SetPistonLocalYPosition(float newLocalY)
    // {
    //     // ... (前面的模式检查保持不变)
    //     
    //     float clampedLocalY = Mathf.Clamp(newLocalY, minLocalY, maxLocalY);
    //     Vector3 newVirtualLocalPos = transform.InverseTransformPoint(_virtualPistonWorldPosition);
    //
    //     if (Mathf.Abs(clampedLocalY - newVirtualLocalPos.y) < float.Epsilon) return; // <-- 这是导致问题的行
    //
    //     newVirtualLocalPos.y = clampedLocalY;
    //     
    //     _virtualPistonWorldPosition = transform.TransformPoint(newVirtualLocalPos);
    // }
    
    /// <summary>
    /// 【程序化输入】由 ArduinoInputManager 和 ExperimentManager 调用
    /// --- 已修改：现在采用与键鼠模式相同的坐标系逻辑 ---
    /// </summary>
    /// <summary>
    /// 【程序化输入】由 ArduinoInputManager 和 ExperimentManager 调用
    /// --- 已修改：采用与键鼠模式相同的坐标系逻辑 ---
    /// </summary>
    public void SetPistonLocalYPosition(float newLocalY)
    {
        var experimentMode = ExperimentManager.Instance.currentProcessType;

        // --- ↓↓↓ 这是新的守卫逻辑 (替换旧的两个 if 语句) ↓↓↓ ---

        // 检查是否是 ExperimentManager 正在进行程序化控制
        bool isExperimentControl = (experimentMode == ExperimentManager.ExperimentProcessType.ISOCHORIC ||
                                    experimentMode == ExperimentManager.ExperimentProcessType.ISOBARIC);

        // 守卫 1:
        // 检查用户输入是否被禁用。
        // 如果被禁用 (isUserInputEnabled == false)，我们只允许在“等容”或“等压”模式下继续执行
        if (!isUserInputEnabled && !isExperimentControl)
        {
            // 例如：在等温模式下，isUserInputEnabled 应该是 true。如果它为 false，阻止调用。
            return;
        }

        // 守卫 2:
        // 检查活塞的控制模式 (PistonControlMode)。
        // 如果控制模式不是 Arduino，我们只允许在“等容”或“等压”模式下继续执行
        if (currentControlMode != PistonControlMode.Arduino && !isExperimentControl)
        {
            // 例如：模式是 Keyboard，并且实验模式是 ISOTHERMAL。
            // 这种情况下，此方法不应被调用（应由 HandleMovementInput 控制），所以阻止它。
            return;
        }

        // --- ↑↑↑ 守卫逻辑结束 ↑↑↑ ---


        // (方法的其余部分保持不变)
        // 1. 钳制Y值
        float clampedLocalY = Mathf.Clamp(newLocalY, minLocalY, maxLocalY);
        
        // 2. 获取当前的本地坐标
        Vector3 targetLocalPos = transform.localPosition; 

        // 3. 检查Y值是否有变化
        if (Mathf.Abs(clampedLocalY - targetLocalPos.y) < float.Epsilon) return;

        // 4. 将本地Y值设置为新值
        targetLocalPos.y = clampedLocalY;
        
        // 5. 【关键】计算目标世界坐标
        if (transform.parent != null)
        {
            this.targetWorldPosition = transform.parent.TransformPoint(targetLocalPos);
        }
        else
        {
            this.targetWorldPosition = targetLocalPos;
        }
        
        // 6. 关键：同时更新 _virtualPistonWorldPosition，
        //    以便 GetPistonCurrentLocalY() (如果被其他脚本使用) 也能获得(近似)正确的值
        this._virtualPistonWorldPosition = this.targetWorldPosition;
    }

    // --- 5. 模式切换的公共方法 (UI调用) ---
    
    public void SetPistonControlMode(int modeIndex)
    {
        if (System.Enum.IsDefined(typeof(PistonControlMode), modeIndex))
        {
            currentControlMode = (PistonControlMode)modeIndex;
            
            // --- ↓↓↓ 恢复了关键调用 ↓↓↓ ---
            // 立即启用/禁用手势组件，确保模式互斥
            UpdateComponentStates(currentControlMode);
            // --- ↑↑↑ 调用恢复结束 ↑↑↑ ---
        }
        else
        {
            Debug.LogWarning($"尝试设置无效的控制模式索引: {modeIndex}");
        }
    }
    public void SetControlModeToKeyboard() { SetPistonControlMode(0); }
    public void SetControlModeToGesture()  { SetPistonControlMode(1); }
    public void SetControlModeToArduino()  { SetPistonControlMode(2); }
    

    // --- 6. 辅助方法 ---
    
    // --- ↓↓↓ 恢复了关键方法 (来自原始代码) ↓↓↓ ---
    /// <summary>
    /// (私有辅助方法) 根据当前激活的模式，启用或禁用对应的手势交互组件
    /// </summary>
    private void UpdateComponentStates(PistonControlMode activeMode)
    {
        // 判断手势组件是否应该激活 (仅在 Gesture 模式下)
        bool gestureActive = (activeMode == PistonControlMode.Gesture);

        // 1. 开关我们自己的 PistonInteractionHandler
        if (gestureHandler != null)
        {
            gestureHandler.enabled = gestureActive;
        }
        else
        {
            // 在 Start() 时我们也会检查，但这里保留日志
            Debug.LogWarning("PistonController: Gesture Handler 引用未设置！", this);
        }

        // 2. 开关 Oculus SDK 的 PokeInteractable
        if (pokeInteractableComponent != null)
        {
            pokeInteractableComponent.enabled = gestureActive;
        }
        else
        {
            Debug.LogWarning("PistonController: Poke Interactable 引用未设置！", this);
        }
    }
    
    /// <summary>
    /// (由 ExperimentManager 调用) 
    /// 在等容模式下 (ISOCHORIC) 会被设为 false
    /// </summary>
    public void SetUserInputActive(bool isActive)
    {
        // (此方法来自原始代码，逻辑正确)
        isUserInputEnabled = isActive;
        
        // 额外保护：如果 ExperimentManager 禁用了用户输入（等容模式）
        // 我们也应该强行禁用手势组件，无论当前是什么模式
        if (isActive == false)
        {
            if (gestureHandler != null) gestureHandler.enabled = false;
            if (pokeInteractableComponent != null) pokeInteractableComponent.enabled = false;
        }
        else
        {
            // 如果 ExperimentManager 重新启用了用户输入（等温/等压模式）
            // 我们应该根据 *当前* 模式来恢复手势组件的状态
            UpdateComponentStates(currentControlMode);
        }
    }
    // --- ↑↑↑ 关键方法恢复结束 ↑↑↑ ---

    
    // --- 7. 原始 Getters 和辅助方法 ---
    
    public float GetContainerRadius() => containerRadius;
    public float GetContainerBottomWorldY() => containerBottomY;

    // (Getters 来自上一版，逻辑正确)
    public float GetPistonCurrentWorldY() => _virtualPistonWorldPosition.y;
    public float GetPistonCurrentLocalY() => transform.InverseTransformPoint(_virtualPistonWorldPosition).y;

    public float GetMinLocalY() => minLocalY;
    public float GetMaxLocalY() => maxLocalY;
    
    private void MoveToLocalPosition(Vector3 localPosition) { }
    
    private void CalculateAndBroadcastVolume(bool forceBroadcast = false)
    {
        float pistonWorldY = transform.position.y; // 启动时基于真实位置
        float height = pistonWorldY - containerBottomY;
        if (height < 0) height = 0;

        float newVolume = Mathf.PI * Mathf.Pow(containerRadius, 2) * height * 3000f;
        
        if (forceBroadcast || Mathf.Abs(newVolume - _currentVolume) > float.Epsilon)
        {
            _currentVolume = newVolume;
            VolumeChanged?.Invoke(_currentVolume);
        }
    }

    void OnValidate()
    {
        // (OnValidate 保持不变, 它只在编辑器中运行)
        if (minLocalY > maxLocalY) minLocalY = maxLocalY;
        if (containerRadius < 0.01f) containerRadius = 0.01f;

        if (Application.isPlaying && isActiveAndEnabled)
        {
            // (此逻辑可能需要重新审视，因为它移动的是真实物体)
            // (暂时保持不变)
        }
        else if (!Application.isPlaying)
        {
            float height = transform.position.y - containerBottomY;
            if (height < 0) height = 0;
            _currentVolume = Mathf.PI * Mathf.Pow(containerRadius, 2) * height * 3000f;
        }
    }
    
    public float GetPistonActualLocalY()
    {
        return transform.localPosition.y;
    }
}