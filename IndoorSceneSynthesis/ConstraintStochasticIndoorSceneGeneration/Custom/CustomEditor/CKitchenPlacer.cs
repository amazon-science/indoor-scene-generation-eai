#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CKitchenPlacerTool))]
public class CKitchenPlacer : Editor {


    public CKitchenPlacerTool targetScript;

    private void OnEnable() {
        targetScript = (CKitchenPlacerTool)target;
    }

    //Set up GUI on Unity Editor
    public override void OnInspectorGUI() {
        if (GUILayout.Button("Place a countertop")) {
            targetScript.CustomPlaceACounterTop();
        }

        if (GUILayout.Button("Rename Kitchen Tools")) {
            targetScript.ReNameKitchenSceneObjs();
        }
    }
}
# endif