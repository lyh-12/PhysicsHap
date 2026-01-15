using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphTest : MonoBehaviour
{
    public GameObject Disc;
    
    // Start is called before the first frame update
    void Start()
    {
        // 实例化Disc对象，并将其作为transform.GetChild(1)的子对象
        var obj = Instantiate(Disc, transform.GetChild(1), true);
        // 设置obj对象为激活状态
        obj.SetActive(true);
        // 设置obj对象的位置为(0, 0, 0)
        obj.transform.localPosition = new Vector3(0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

