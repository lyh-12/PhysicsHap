using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

[ExecuteAlways]
public class DataSetting : MonoBehaviour
{
    private static DataSetting _instance;

    public static DataSetting Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = GameObject.Find(nameof(DataSetting));
                if (obj == null)
                {
                    obj = new GameObject(nameof(DataSetting)); // 创建名称相同的 obj
                    _instance = obj.AddComponent<DataSetting>(); // 添加 _instance
                }
                else
                    _instance = obj.GetComponent<DataSetting>();
            }

            return _instance;
        }
        
        
    }

    private void Awake()
    {
        // englishFont = ResourceMgr.Instance.Load<TMP_FontAsset>("Fonts/TIMES SDF");
        // chineseFont = ResourceMgr.Instance.Load<TMP_FontAsset>("Fonts/STZHONGS SDF");
    }

    /// <summary>
    /// 依据绝对路径获取组件，并赋值给 obj
    /// </summary>
    /// <param name="path">子物体路径</param>
    /// <param name="obj">给哪个组件赋值</param>
    /// <typeparam name="T">组件类型</typeparam>
    public void SetComponent<T>(string path, ref T obj) where T : Object
    {
        T comp = GameObject.Find(path).GetComponent<T>();

        if (comp == null)
            Debug.LogError($"Script DataSetting.cs: \"{path}\" is null");

        if (obj == null)
            obj = comp;
        else if (obj != comp)
            Debug.LogError($"Script DataSetting.cs: {obj} != {comp}, in path \"{path}\"");
    }

    public void SetComponent<T>(Transform trans, ref T obj) where T : Object
    {
        T comp = trans.GetComponent<T>();

        if (comp == null)
            Debug.LogError($"Script DataSetting.cs: \"{trans.name}\" has no component \"{typeof(T)}\"");

        if (obj == null)
            obj = comp;
        else if (obj != comp)
            Debug.LogError($"Script DataSetting.cs: {obj} != {comp}, in \"{trans.name}\"");
    }

    /// <summary>
    /// 从子物体中获取组件，并赋值给 obj
    /// </summary>
    /// <param name="path">子物体路径</param>
    /// <param name="parent">父物体</param>
    /// <param name="obj">给哪个组件赋值</param>
    /// <typeparam name="T">组件类型</typeparam>
    public void SetComponentFromChild<T>(string path, Transform parent, ref T obj) where T : Object
    {
        T comp = parent.Find(path).GetComponent<T>();

        if (comp == null)
            Debug.LogError($"Script DataSetting.cs: \"{parent.name}.{path}\" is null");

        if (obj == null)
            obj = comp;
        else if (obj != comp)
            Debug.LogError($"Script DataSetting.cs: {obj} != {comp}, in path \"{parent.name}.{path}\"");
    }
}