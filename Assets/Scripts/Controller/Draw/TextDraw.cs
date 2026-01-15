using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;

public class TextDraw : ImmediateModeShapeDrawer
{
    // TextElement elemA, elemB;

    void Awake()
    {
        // elemA = new TextElement();
        // elemB = new TextElement();
    }

    public override void DrawShapes(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.FontSize = 1;
            Draw.Matrix = transform.localToWorldMatrix;

            Draw.Text(Vector3.zero, "aaaaaaaaa");
        }
    }

    void OnDestroy()
    {
        // elemA.Dispose(); // Important - you have to dispose text elements when you are done with them
        // elemB.Dispose();
    }
}