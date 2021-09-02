#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(AlfredTaskHelperTool))]
public class AlfredTaskHelper : Editor {
    public AlfredTaskHelperTool targetScript;

    private void OnEnable() {
        targetScript = (AlfredTaskHelperTool)target;
    }

    public override void OnInspectorGUI() {
        GUILayout.Label("Scene algorithms", EditorStyles.boldLabel);


        if (GUILayout.Button("Parse tasks from room type")) {
            targetScript.ParseAllTaskJsonsForRoomType();
        }


    }
}
# endif