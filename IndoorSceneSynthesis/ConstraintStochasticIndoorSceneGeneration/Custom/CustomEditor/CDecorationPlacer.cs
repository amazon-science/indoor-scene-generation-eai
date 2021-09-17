# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CDecorationPlacerTool))]
public class CDecorationPlacer : Editor
{
    public CDecorationPlacerTool targetScript;

    private void OnEnable()
    {
        targetScript = (CDecorationPlacerTool)target;
    }
    //Set up GUI on Unity Editor
    public override void OnInspectorGUI()
    {

        if (GUILayout.Button("Set Floor"))
        {
            targetScript.SetFloor();
        }


        if (GUILayout.Button("Load Basic Rule"))
        {
            targetScript.LoadRule();
        }

        if (GUILayout.Button("Custom Place Furniture"))
        {
            targetScript.CustomPlaceDecoration();
        }

    }
}
#endif