// 文件: Scripts/Interaction/ArduinoPressureReceiver.cs
// (脚本内的类名是 ArduinoInputManager)
//
// --- 修正版：已将 serialPort.Open() 移入后台线程 ---

using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;

/// <summary>
/// 负责接收和处理来自Arduino的串口数据，
/// 并根据数据控制PistonController。
/// 这个脚本现在可以安全地处理COM端口打开失败而不冻结主线程。
/// </summary>
public class ArduinoInputManager : MonoBehaviour
{
    [Header("Arduino Connection")]
    [Tooltip("你的Arduino连接的COM端口，例如 COM3")]
    [SerializeField] private string portName = "COM12";
    
    [Tooltip("必须与Arduino代码中的波特率完全一致！优化后的速率是115200。")]
    [SerializeField] private int baudRate = 115200;

    [Header("Target Controller")]
    [Tooltip("拖入场景中带有 PistonController 脚本的对象")]
    [SerializeField] private PistonController pistonController;

    [Header("Movement Smoothing")]
    [Tooltip("活塞移动到目标位置的速度。")]
    [SerializeField] private float moveSpeed = 15.0f;
    
    [Tooltip("Arduino 压力值超过此值才被视为“有效按压”")]
    [SerializeField] private float pressThreshold = 0.01f;
    
    [Header("UI Reference")]
    [Tooltip("拖入场景中带有 UIManager 脚本的对象")]
    [SerializeField] private UIManager uiManager;

    // --- 私有变量 ---
    private SerialPort serialPort;
    private Thread readThread;
    private volatile bool isThreadRunning = false;
    private volatile string lastReceivedData = null;

    // 用于平滑移动的目标位置变量
    private float targetLocalY;

    #region Unity Lifecycle Methods

    void Start()
    {
        if (pistonController == null)
        {
            Debug.LogError("错误: PistonController 的引用未设置！", this);
            this.enabled = false;
            return;
        }
        
        if (uiManager == null)
        {
            // 如果忘记在Inspector中拖拽，尝试自动查找
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("UIManager 引用未设置，将无法播放警告音。", this);
            }
        }

        targetLocalY = pistonController.GetPistonCurrentLocalY();

        // --- 核心修改点 1 ---
        // Start() 方法现在只负责启动线程。
        // 它不再尝试打开串口，因此永远不会阻塞主线程。
        StartSerialPortThread();
        // --- 修改结束 ---
    }

    // (请用这个版本完整替换 ArduinoInputManager.cs 中的 Update 方法)
    // (请用这个版本完整替换 ArduinoInputManager.cs 中的 Update 方法)
    // (请用这个版本完整替换 ArduinoInputManager.cs 中的 Update 方法)
    void Update()
    {
        // 1. 获取当前模式
        var currentMode = ExperimentManager.ExperimentProcessType.ISOTHERMAL; // 给一个默认“允许”值
        if (ExperimentManager.Instance != null)
        {
            currentMode = ExperimentManager.Instance.currentProcessType;
        }
        
        // 2. 检查模式是否允许Arduino（此检查每帧都需要）
        bool isArduinoAllowed = (currentMode != ExperimentManager.ExperimentProcessType.ISOCHORIC &&
                                 currentMode != ExperimentManager.ExperimentProcessType.NONE);

        // 3. 检查是否有新数据
        if (lastReceivedData != null)
        {
            string dataToProcess = lastReceivedData;
            lastReceivedData = null;

            // 尝试解析数据
            if (dataToProcess.StartsWith("P:"))
            {
                string valueString = dataToProcess.Substring(2);
                if (float.TryParse(valueString, out float pressValue))
                {
                    // *** 核心修复：检查是否为“有效按压” ***
                    bool isPressed = pressValue > pressThreshold;

                    if (isPressed)
                    {
                        // --- 确实是一次有效按压 ---
                        
                        if (!isArduinoAllowed)
                        {
                            // --- 情况 1：按压了，但模式不允许 ---
                            if (currentMode == ExperimentManager.ExperimentProcessType.ISOCHORIC)
                            {
                                uiManager?.PlayIsochoricWarningAudio();
                            }
                            else if (currentMode == ExperimentManager.ExperimentProcessType.NONE)
                            {
                                uiManager?.PlayNoModeSelectedWarningAudio();
                            }
                        }
                        else
                        {
                            // --- 情况 2：按压了，且模式允许 ---
                            // (这是原来的计算逻辑)
                            Debug.LogError($"[ArduinoInput] 成功解析数据: {pressValue}");
                            
                            float originalMinY = pistonController.GetMinLocalY();
                            float originalMaxY = pistonController.GetMaxLocalY();

                            targetLocalY = Mathf.Lerp(
                                    originalMaxY, 
                                    originalMinY, 
                                    pressValue
                                );
                        }
                    }
                    // else (isPressed == false)
                    // --- 情况 3：收到了数据，但只是 "P:0.0"，忽略 ---
                    // (我们什么都不做)
                }
            }
        } // --- (数据处理结束) ---


        // 4. (活塞锁定和移动逻辑)
        //    此逻辑必须在数据检查之外，每帧都运行
        
        if (!isArduinoAllowed)
        {
            // 模式不允许，强制锁定活塞位置
            if (pistonController != null)
            {
                targetLocalY = pistonController.GetPistonActualLocalY();
            }
            return; // 退出，不执行移动
        }
        
        // (模式允许，执行平滑移动)
        float currentY = pistonController.GetPistonActualLocalY(); 
        
        if (Mathf.Abs(currentY - targetLocalY) > 0.001f)
        {
            float newY = Mathf.Lerp(currentY, targetLocalY, Time.deltaTime * moveSpeed);
            pistonController.SetPistonLocalYPosition(newY);
        }
    }

    // (OnDestroy 和 OnApplicationQuit 保持不变)
    void OnDestroy()
    {
        CloseSerialPort();
    }

    void OnApplicationQuit()
    {
        CloseSerialPort();
    }

    #endregion

    #region Serial Communication Handling

    /// <summary>
    /// 【已修改】此方法现在只创建并启动后台线程。
    /// </summary>
    private void StartSerialPortThread()
    {
        isThreadRunning = true;
        readThread = new Thread(ReadThread); // 将 ReadThread 作为线程的目标方法
        readThread.IsBackground = true;
        readThread.Start();
        Debug.Log("串口读取线程已启动。等待端口打开...");
    }

    /// <summary>
    /// 【已修改】此方法现在运行在后台线程中。
    /// 它负责 *所有* 阻塞操作：Open() 和 ReadLine()。
    /// </summary>
    private void ReadThread()
    {
        // --- 核心修改点 2 ---
        // 步骤 1: 在后台线程中尝试打开端口
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 50; // 设置一个合理的读取超时
            serialPort.Open(); // <-- 这个阻塞操作现在安全地在后台线程执行

            // 如果成功，在 Unity 控制台打印（使用主线程安全的 Debug.Log）
            Debug.Log($"<color=green>成功打开串口: {portName} @ {baudRate}bps</color>");
        }
        catch (Exception e)
        {
            // 如果打开失败 (例如 COM12 不存在或被占用)
            Debug.LogError($"<color=red>打开串口失败: {e.Message}</color>\n请检查: \n1. COM端口号是否正确?\n2. Arduino是否已连接?\n3. 其他程序(如Arduino IDE的串口监视器)是否占用了该端口?");
            isThreadRunning = false; // 停止线程运行
            return; // 退出 ReadThread
        }
        // --- 修改结束 ---


        // 步骤 2: 在后台线程中循环读取数据
        // (这部分是您原有的逻辑，它是正确的)
        while (isThreadRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                // ReadLine() 是一个“阻塞”操作，但它现在安全地在后台线程
                lastReceivedData = serialPort.ReadLine();
            }
            catch (TimeoutException)
            {
                // 读取超时是正常现象，忽略。
            }
            catch (Exception e)
            {
                if (isThreadRunning)
                {
                    Debug.LogError($"串口读取错误: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// (此方法保持不变)
    /// 安全地关闭线程和串口。
    /// </summary>
    private void CloseSerialPort()
    {
        isThreadRunning = false;

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join(100);
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("串口已关闭。");
        }
    }

    #endregion
}