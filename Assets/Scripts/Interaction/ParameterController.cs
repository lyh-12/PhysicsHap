// 文件名: ParameterController.cs

using UnityEngine;

/// <summary>
/// 处理调节温度和压强等参数的UI按钮逻辑。
/// 这个脚本现在包含了主要的控制逻辑，并且增加了键盘输入支持。
/// </summary>
public class ParameterController : MonoBehaviour
{
    [Header("调节步长")]
    [Tooltip("每次点击按钮时温度变化的量 (Kelvin)")]
    public float temperatureStep = 10.0f;
    
    [Header("参数限制 (Limits)")]
    [Tooltip("允许设置的最低温度 (Kelvin)")]
    public float minTemperatureLimit = 200f;
    
    [Tooltip("允许设置的最高温度 (Kelvin)")]
    public float maxTemperatureLimit = 350; // 您可以设置一个合理的默认上限


    // --- 这是修改点 ---
    [Tooltip("每次点击按钮时压强变化的量 (atm)")]
    public float pressureStep = 0.1f; // 默认值从 5000.0f 改为 0.1f
    // --- 修改结束 ---

    // --- 新增代码 ---
    [Header("键盘快捷键 (Key Bindings)")]
    [Tooltip("增加温度的快捷键")]
    public KeyCode increaseTemperatureKey = KeyCode.T; // 例如 T 键

    [Tooltip("减少温度的快捷键")]
    public KeyCode decreaseTemperatureKey = KeyCode.G; // 例如 G 键

    [Tooltip("增加压强的快捷键")]
    public KeyCode increasePressureKey = KeyCode.P; // 例如 P 键

    [Tooltip("减少压强的快捷键")]
    public KeyCode decreasePressureKey = KeyCode.O; // 例如 O 键
    // --- 新增代码结束 ---


    // --- 新增代码 ---
    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        // 监听增加温度按键
        if (Input.GetKeyDown(increaseTemperatureKey))
        {
            IncreaseTemperature();
        }

        // 监听减少温度按键
        if (Input.GetKeyDown(decreaseTemperatureKey))
        {
            DecreaseTemperature();
        }

        // 监听增加压强按键
        if (Input.GetKeyDown(increasePressureKey))
        {
            IncreasePressure();
        }

        // 监听减少压强按键
        if (Input.GetKeyDown(decreasePressureKey))
        {
            DecreasePressure();
        }
    }
    // --- 新增代码结束 ---


    /// <summary>
    /// 增加目标温度。
    /// </summary>
    /// <summary>
    /// 增加目标温度。
    /// </summary>
    public void IncreaseTemperature()
    {
        if (ExperimentManager.Instance == null) return;
        
        ExperimentManager.Instance.targetFixedTemperature += temperatureStep;

        // --- ↓↓↓ 修改后的代码 ↓↓↓ ---
        // 检查是否超过了我们设定的 *上限*
        if (ExperimentManager.Instance.targetFixedTemperature > maxTemperatureLimit)
        {
            ExperimentManager.Instance.targetFixedTemperature = maxTemperatureLimit;
        }
        // --- ↑↑↑ 修改后的代码结束 ↑↑↑ ---
        
        ExperimentManager.Instance.UpdateThermodynamicState();
    }

    /// <summary>
    /// 减少目标温度。
    /// </summary>
    public void DecreaseTemperature()
    {
        if (ExperimentManager.Instance == null) return;

        ExperimentManager.Instance.targetFixedTemperature -= temperatureStep;

        // --- ↓↓↓ 修改后的代码 ↓↓↓ ---
        // 检查是否低于了我们设定的 *下限*
        if (ExperimentManager.Instance.targetFixedTemperature < minTemperatureLimit)
        {
            ExperimentManager.Instance.targetFixedTemperature = minTemperatureLimit;
        }
        // --- ↑↑↑ 修改后的代码结束 ↑↑↑ ---

        ExperimentManager.Instance.UpdateThermodynamicState();
    }

    /// <summary>
    /// 增加目标压强。
    /// </summary>
    public void IncreasePressure()
    {
        if (ExperimentManager.Instance == null) return;

        // --- 这是修改点 ---
        // 1. 直接修改 ExperimentManager 的公开变量 (现在是 atm)
        ExperimentManager.Instance.targetFixedPressure += pressureStep;
        // --- 修改结束 ---

        // 2. 在这里执行数据验证，防止压强为负
        if (ExperimentManager.Instance.targetFixedPressure < 0)
        {
            ExperimentManager.Instance.targetFixedPressure = 0;
        }
        
        // 3. 手动调用 ExperimentManager 的公共更新方法来应用更改
        ExperimentManager.Instance.UpdateThermodynamicState();
    }

    /// <summary>
    /// 减少目标压强。
    /// </summary>
    public void DecreasePressure()
    {
        if (ExperimentManager.Instance == null) return;

        // --- 这是修改点 ---
        // 1. 直接修改 ExperimentManager 的公开变量 (现在是 atm)
        ExperimentManager.Instance.targetFixedPressure -= pressureStep;
        // --- 修改结束 ---

        if (ExperimentManager.Instance.targetFixedPressure < 0)
        {
            ExperimentManager.Instance.targetFixedPressure = 0;
        }
        
        ExperimentManager.Instance.UpdateThermodynamicState();
    }
}