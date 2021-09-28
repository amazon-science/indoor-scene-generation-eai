# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Assertions;
using System.IO;
using System;

[CustomEditor(typeof(DTextureHelperTool))]
public class DTextureHelper : Editor {
    public DTextureHelperTool targetScript;

    private void OnEnable() {
        targetScript = (DTextureHelperTool)target;
    }

    
    public override void OnInspectorGUI() {
        GUILayout.Label("Get Object Textures", EditorStyles.boldLabel);
        if (GUILayout.Button("Get object textures in current scene")) {
            targetScript.GetSOPTexturesInOneScene();
        }

        if (GUILayout.Button("Loop all scenes")) {
            targetScript.LoopOverScenesToGetAllObjectMaterials();
        }
    }
}
# endif