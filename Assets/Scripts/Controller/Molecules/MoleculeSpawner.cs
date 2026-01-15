using UnityEngine;
using System.Collections.Generic;
using System; // For Action

public class MoleculeSpawner : MonoBehaviour
{
    public static MoleculeSpawner Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] private GameObject moleculePrefab;
    [SerializeField] private Transform moleculeParentTransform;
    [SerializeField] private PistonController pistonController;

    [Header("Container Setup")]
    [SerializeField] private ContainerDefinition containerDefinition;
    [SerializeField] private float pistonEffectiveBottomOffset = 0.22f;

    [Header("Pooling")]
    [SerializeField] private int initialPoolSize = 30;
    [SerializeField] private bool allowPoolToGrow = true;

    [Header("Control")]
    [Tooltip("每次按下按键或点击按钮时增加或减少的分子数量")]
    [SerializeField] private int moleculesPerAction = 5;

    // ### 新增: 分子数量的上下限 ###
    [Header("Limits")]
    [Tooltip("容器中允许的最小分子数")]
    [SerializeField] private int minMoleculeCount = 0;
    [Tooltip("容器中允许的最大分子数")]
    [SerializeField] private int maxMoleculeCount = 30;


    private List<GameObject> activeMolecules = new List<GameObject>();
    private MoleculePool moleculePool;

    public int CurrentMoleculeCount => activeMolecules.Count;

    public event Action<int> ActualMoleculeCountChanged;

    // MoleculePool inner class (same as previous refactor, ensure it's robust)
    private class MoleculePool
    {
        private Queue<GameObject> pooledObjects;
        private GameObject prefab;
        private Transform parentTransform;
        private bool allowGrow;

        public MoleculePool(GameObject prefab, Transform parent, int initialSize, bool allowGrowth)
        {
            this.prefab = prefab;
            this.parentTransform = parent ?? new GameObject("MoleculePool_AutoParent").transform;
            this.allowGrow = allowGrowth;
            this.pooledObjects = new Queue<GameObject>();
            for (int i = 0; i < initialSize; i++) AddObjectToPool();
        }

        private GameObject CreateNewInstance() => Instantiate(prefab, parentTransform);
        
        private void AddObjectToPool()
        {
            GameObject obj = CreateNewInstance();
            obj.SetActive(false);
            pooledObjects.Enqueue(obj);
        }

        public GameObject Get()
        {
            if (pooledObjects.Count > 0)
            {
                GameObject obj = pooledObjects.Dequeue();
                obj.SetActive(true);
                return obj;
            }
            if (allowGrow) return CreateNewInstance();
            Debug.LogWarning("Molecule pool empty and cannot grow. Returning null.");
            return null;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            if (obj.transform.parent != parentTransform) obj.transform.SetParent(parentTransform);
            pooledObjects.Enqueue(obj);
        }
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Debug.LogWarning("Duplicate MoleculeSpawner instance.", gameObject); Destroy(gameObject); return; }

        if (!ValidateReferences()) { enabled = false; return; }

        moleculePool = new MoleculePool(moleculePrefab, moleculeParentTransform, initialPoolSize, allowPoolToGrow);
    }

    void Update()
    {
        // 键盘输入功能保持不变
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            AddMolecules(moleculesPerAction);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            RemoveMolecules(moleculesPerAction);
        }
    }
    
    private bool ValidateReferences()
    {
        if (moleculePrefab == null) { Debug.LogError("Molecule Prefab not set.", this); return false; }
        if (pistonController == null) { Debug.LogError("PistonController not set.", this); return false; }
        if (containerDefinition == null) { Debug.LogError("Container Definition not set.", this); return false; }
        return true;
    }

    // ### 修改: AddMolecules 方法增加了上限检查 ###
    // 请用这个版本完整替换你代码中现有的 AddMolecules 方法
    public void AddMolecules(int count)
    {
        if (count <= 0 || !enabled) return;

        if (CurrentMoleculeCount >= maxMoleculeCount)
        {
            Debug.Log("已达到分子数量上限，无法添加。");
            return;
        }

        int availableSpace = maxMoleculeCount - CurrentMoleculeCount;
        int amountToAdd = Mathf.Min(count, availableSpace);

        // ================== DEBUGGING: 打印计算边界所需的值 ==================
        Debug.Log($"===== 开始计算生成区域 (Spawning Bounds) =====");
        float containerFloor = containerDefinition.BottomY;
        float pistonY = pistonController.GetPistonCurrentWorldY();
        float pistonCeiling = pistonY - pistonEffectiveBottomOffset;
        Debug.Log($"[Y轴] 容器底部 (Floor): {containerFloor}");
        Debug.Log($"[Y轴] 活塞原始位置 (Piston Y): {pistonY}");
        Debug.Log($"[Y轴] 偏移量 (Offset): {pistonEffectiveBottomOffset}");
        Debug.Log($"[Y轴] 生成上限 (Ceiling): {pistonCeiling}");
        // ===================================================================

        Bounds spawnBounds = GetValidSpawnBounds();
        if (spawnBounds.size.y <= 0.001f)
        {
            Debug.LogWarning($"Spawn bounds height negligible ({spawnBounds.size.y}). Cannot spawn molecules.", this);
            return;
        }

        int previousCount = CurrentMoleculeCount;
        for (int i = 0; i < amountToAdd; i++)
        {
            // ================== DEBUGGING: 每次循环开始时打印分隔符 ==================
            Debug.Log($"--- 正在生成第 {i + 1}/{amountToAdd} 个分子 ---");
            // =====================================================================

            GameObject newMolecule = moleculePool.Get();
            if (newMolecule == null) { Debug.LogError("Failed to get molecule from pool.", this); break; }

            // --- 水平位置计算过程 ---
            Vector2 unitCirclePoint = UnityEngine.Random.insideUnitCircle;
            float radius = containerDefinition.radius;
            Vector2 randomInCircle = unitCirclePoint * radius;
            Vector3 center = containerDefinition.XZCenter;
            
            // --- 垂直位置计算过程 ---
            float randomY = UnityEngine.Random.Range(spawnBounds.min.y, spawnBounds.max.y);

            // --- 最终位置组合 ---
            Vector3 finalPosition = new Vector3(
                center.x + randomInCircle.x,
                randomY,
                center.z + randomInCircle.y
            );

            // ================== DEBUGGING: 打印所有计算过程中的具体量 ==================
            Debug.Log($"[XZ平面] 容器中心点 (Center): {center}");
            Debug.Log($"[XZ平面] 容器半径 (Radius): {radius}");
            Debug.Log($"[XZ平面] 单位圆随机点 (Unit Circle): {unitCirclePoint}");
            Debug.Log($"[XZ平面] 缩放后偏移量 (Scaled Offset): {randomInCircle}");
            Debug.Log($"[Y轴] 在 [{spawnBounds.min.y:F2}, {spawnBounds.max.y:F2}] 范围内随机选择的高度: {randomY:F2}");
            Debug.Log($"[最终结果] 计算出的世界坐标 (Final World Position): {finalPosition}");
            // ========================================================================
            
            newMolecule.transform.position = finalPosition;
            newMolecule.transform.rotation = Quaternion.identity;

            if (moleculeParentTransform != null && newMolecule.transform.parent != moleculeParentTransform) {
                 newMolecule.transform.SetParent(moleculeParentTransform);
            }
            activeMolecules.Add(newMolecule);
        }

        if (CurrentMoleculeCount != previousCount)
        {
            ActualMoleculeCountChanged?.Invoke(CurrentMoleculeCount);
        }
    }

    // ### 修改: RemoveMolecules 方法增加了下限检查 ###
    public void RemoveMolecules(int count)
    {
        if (count <= 0 || !enabled || activeMolecules.Count == 0) return;

        // 检查是否已达到或低于下限
        if (CurrentMoleculeCount <= minMoleculeCount)
        {
            Debug.Log("已达到分子数量下限，无法移除。");
            return;
        }

        // 计算实际可以移除多少分子，确保不会低于下限
        int removableAmount = CurrentMoleculeCount - minMoleculeCount;
        int amountToRemove = Mathf.Min(count, removableAmount);
        
        int previousCount = CurrentMoleculeCount;
        for (int i = 0; i < amountToRemove; i++)
        {
            GameObject moleculeToRemove = activeMolecules[activeMolecules.Count - 1];
            activeMolecules.RemoveAt(activeMolecules.Count - 1);
            moleculePool.Return(moleculeToRemove);
        }

        if (CurrentMoleculeCount != previousCount)
        {
            ActualMoleculeCountChanged?.Invoke(CurrentMoleculeCount);
        }
    }

    // ### 新增：为UI按钮提供的公共方法 ###

    /// <summary>
    /// 公开给“增加分子”按钮调用的方法
    /// </summary>
    public void AddMoleculesFromButton()
    {
        Debug.Log("Add button clicked.");
        AddMolecules(moleculesPerAction);
    }

    /// <summary>
    /// 公开给“减少分子”按钮调用的方法
    /// </summary>
    public void RemoveMoleculesFromButton()
    {
        Debug.Log("Remove button clicked.");
        RemoveMolecules(moleculesPerAction);
    }

    public void ClearAllMolecules()
    {
        if (!enabled || activeMolecules.Count == 0) return;

        List<GameObject> toRemove = new List<GameObject>(activeMolecules);
        activeMolecules.Clear();

        foreach (GameObject molecule in toRemove)
        {
            moleculePool.Return(molecule);
        }

        ActualMoleculeCountChanged?.Invoke(0);
    }

    private Bounds GetValidSpawnBounds()
    {
        float containerFloorWorldY = containerDefinition.BottomY;
        float pistonCeilingWorldY = pistonController.GetPistonCurrentWorldY() - pistonEffectiveBottomOffset;
        float spawnHeight = pistonCeilingWorldY - containerFloorWorldY;
        if (spawnHeight < 0.01f) spawnHeight = 0.01f;

        return new Bounds(
            new Vector3(containerDefinition.XZCenter.x, containerFloorWorldY + (spawnHeight / 2f), containerDefinition.XZCenter.z),
            new Vector3(containerDefinition.radius * 2f, spawnHeight, containerDefinition.radius * 2f) // Corrected spawn height calculation
        );
    }

    public List<GameObject> GetActiveMoleculesCopy() => new List<GameObject>(activeMolecules);
}