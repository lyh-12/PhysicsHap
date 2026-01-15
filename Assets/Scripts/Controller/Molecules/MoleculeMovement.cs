// Scripts/Controller/Molecules/MoleculeMovement.cs
using UnityEngine;

public class MoleculeMovement : MonoBehaviour
{
    [Header("运动逻辑控制")]
    [Tooltip("改变运动方向的时间间隔(秒)")]
    public float directionChangeInterval = 1.5f;

    [Header("速度与温度控制")]
    [Tooltip("在参考温度下的分子运动速度")]
    public float speedAtReferenceTemp = 0.02f;
    [Tooltip("用于计算速度的参考温度 (Kelvin)")]
    public float referenceTemperature = 293.15f;

    // --- 私有变量 ---
    private Rigidbody rb;
    private float _currentMaxSpeed; // 当前温度对应的目标速度
    private Vector3 _currentDirection; 
    private float _timeSinceDirectionChange; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("MoleculeMovement script requires a Rigidbody component!");
            enabled = false;
            return;
        }

        rb.useGravity = false;
        rb.drag = 0f;
        rb.angularDrag = 0.5f;
    }

    void OnEnable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.TemperatureChanged += OnTemperatureChanged;
            
            // 立即获取当前温度来设置 _currentMaxSpeed
            OnTemperatureChanged(ExperimentManager.Instance.GetCurrentTemperature());
            // 立即应用一个初始方向和速度
            ChangeDirection();
        }
    }

    void OnDisable()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.TemperatureChanged -= OnTemperatureChanged;
        }
    }

    void FixedUpdate()
    {
        // 1. 计时
        _timeSinceDirectionChange += Time.fixedDeltaTime;
        if (_timeSinceDirectionChange >= directionChangeInterval)
        {
            ChangeDirection(); 
        }

        // 2. (我们假设您已采纳之前的修改，移除了这里的 rb.velocity 设置)
        // (如果您的代码这里仍有 rb.velocity = ... ，请使用我们之前防穿透的版本)
    }

    /// <summary>
    /// 响应温度变化的事件处理函数，更新目标速度
    /// </summary>
    public void OnTemperatureChanged(float newTemperature)
    {
        if (referenceTemperature > 0 && newTemperature > 0)
        {
            // --- 这是关键的修改 ---
            // 我们移除了 Mathf.Sqrt()
            // 现在速度与 (newTemperature / referenceTemperature) 线性相关
            _currentMaxSpeed = speedAtReferenceTemp * (newTemperature / referenceTemperature);
            // --- 修改结束 ---
        }
        else
        {
            _currentMaxSpeed = 0f;
        }
        
        // 当温度变化时，立即更新分子的当前速度
        if (rb != null && rb.velocity.sqrMagnitude > float.Epsilon)
        {
            rb.velocity = rb.velocity.normalized * _currentMaxSpeed;
        }
        else if (rb != null)
        {
            rb.velocity = Random.onUnitSphere * _currentMaxSpeed;
        }
    }

    /// <summary>
    /// 随机选择一个新的方向，【并立即应用该速度】，然后重置计时器
    /// </summary>
    private void ChangeDirection()
    {
        _currentDirection = Random.onUnitSphere;
        _timeSinceDirectionChange = 0f;
        
        if (rb != null)
        {
            rb.velocity = _currentDirection * _currentMaxSpeed;
        }
    }
}