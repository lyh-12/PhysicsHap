using UnityEngine;

/// <summary>
/// 这个脚本控制实验模式的UI选择逻辑。
/// 它现在通过访问 ExperimentManager 的静态实例来工作，不再需要手动拖拽引用。
/// </summary>
public class ModeController : MonoBehaviour
{

    /// <summary>
    /// 将实验模式设置为“等温”。
    /// </summary>
    public void SetModeToIsothermal()
    {
        // 直接通过静态实例 Instance 来调用方法
        ExperimentManager.Instance.SetProcessMode(ExperimentManager.ExperimentProcessType.ISOTHERMAL);
    }

    /// <summary>
    /// 将实验模式设置为“等压”。
    /// </summary>
    public void SetModeToIsobaric()
    {
        ExperimentManager.Instance.SetProcessMode(ExperimentManager.ExperimentProcessType.ISOBARIC);
    }

    /// <summary>
    /// 将实验模式设置为“等容”。
    /// </summary>
    public void SetModeToIsochoric()
    {
        ExperimentManager.Instance.SetProcessMode(ExperimentManager.ExperimentProcessType.ISOCHORIC);
    }
}