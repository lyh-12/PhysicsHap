// ContainerDefinition.cs
using UnityEngine;

[System.Serializable] // 关键特性：允许这个类的实例在Inspector中被编辑和序列化
public class ContainerDefinition
{
    [Header("Geometric Properties")]
    public float radius = 0.01f;

    // 只读属性，方便获取计算值，并包含null检查
    public float BottomY = 0.888f;
        
    public Vector3 XZCenter = new Vector3(0.274f, 0.98f, 7.959f);
   
}