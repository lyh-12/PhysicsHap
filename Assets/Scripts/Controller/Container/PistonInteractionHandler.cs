// 文件: Scripts/Controller/Container/PistonInteractionHandler.cs
// --- 修改版：
// 1. 现在计算 *世界空间* 的移动增量 (delta)。
// 2. 调用父物体 PistonController 上的一个 *新* 方法 ApplyVirtualDelta()。
// ---

using UnityEngine;

public class PistonInteractionHandler : MonoBehaviour
{
    [Header("手动引用")]
    [Tooltip("请拖入场景中挂载了 PistonController 脚本的那个【父物体】")]
    [SerializeField]
    private PistonController _pistonController;

    /// <summary>
    /// 记录此对象 (ButtonVisual) 在上一帧的 *世界* 位置。
    /// </summary>
    private Vector3 _lastFrameWorldPosition;

    void Start()
    {
        if (_pistonController == null)
        {
            // 尝试自动从父物体获取 (以防您忘记拖拽)
            _pistonController = GetComponentInParent<PistonController>();
        }

        if (_pistonController == null)
        {
            Debug.LogError("PistonInteractionHandler: _pistonController 引用未设置，且在父物体中也未找到！脚本已禁用。", this);
            this.enabled = false;
            return;
        }

        // 初始化上一帧位置 (使用世界坐标)
        _lastFrameWorldPosition = transform.position;
    }

    /// <summary>
    /// LateUpdate 每一帧都会运行
    /// </summary>
    void LateUpdate()
    {
        // 1. 获取 ButtonVisual (即 this.gameObject) 的当前 *世界* 位置
        Vector3 currentWorldPosition = transform.position;

        // 2. 计算 *世界空间* 的移动增量
        Vector3 worldSpaceDelta = currentWorldPosition - _lastFrameWorldPosition;

        // 3. 比较增量是否足够大 (避免浮点数抖动)
        if (worldSpaceDelta.sqrMagnitude > float.Epsilon)
        {
            // 4. 如果位置发生了变化，将 *增量* 发送给 PistonController
            //    我们不再调用 SetTargetWorldPosition
            _pistonController.ApplyVirtualDelta(worldSpaceDelta);

            // 5. 记录当前位置，作为下一帧的 "上一帧位置"
            _lastFrameWorldPosition = currentWorldPosition;
        }
    }
}