# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Assertions;
using System.IO;
using System;

[CustomEditor(typeof(AlfredAgent))]
public class ADebug : Editor {
    public AlfredAgent targetScript;

    private void OnEnable() {
        targetScript = (AlfredAgent)target;
    }

    public override void OnInspectorGUI() {
        GUILayout.Label("Scene algorithms", EditorStyles.boldLabel);

        GUILayout.Label("Json Folder", EditorStyles.boldLabel);
        AlfredAgent.taskJsonFolder = (string)EditorGUILayout.TextField(AlfredAgent.taskJsonFolder);



        if (GUILayout.Button("Debug")) {
            targetScript.BuildGraph();
        }

        GUILayout.Label("\n\n Json file", EditorStyles.boldLabel);
        AlfredAgent.taskJsonPath = (string)EditorGUILayout.TextField(AlfredAgent.taskJsonPath);

        if (GUILayout.Button("Debug 2")) {
            targetScript.OneJsonForCurrentScene();
        }

        //GUILayout.Label("\n\n One Task", EditorStyles.boldLabel);
        //if (GUILayout.Button("Debug 3")) {
        //    targetScript.GenerateTrainingForOneTaskInCurrentScene();
        //}


    }
}

#endif
