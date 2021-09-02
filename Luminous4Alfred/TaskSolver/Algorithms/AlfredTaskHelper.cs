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

        GUILayout.Label("Room type", EditorStyles.boldLabel);
        CSceneBuilderTool.samplingRoomType = (CRoomType)EditorGUILayout.EnumPopup("Room Type:", CSceneBuilderTool.samplingRoomType);

        GUILayout.Label("Task type", EditorStyles.boldLabel);
        AlfredTaskHelperTool.alfredTaskType = (AlfredTaskType)EditorGUILayout.EnumPopup("Task Type:", AlfredTaskHelperTool.alfredTaskType);

        GUILayout.Label("Scene Set Index", EditorStyles.boldLabel);
        AlfredTaskHelperTool.sceneSetIdx = (int)EditorGUILayout.IntField("Scene set index:", AlfredTaskHelperTool.sceneSetIdx);

        GUILayout.Label("Target scene path", EditorStyles.boldLabel);
        AlfredTaskHelperTool.targetGeneratingScenePath = (string)EditorGUILayout.TextField(AlfredTaskHelperTool.targetGeneratingScenePath);

        if (GUILayout.Button("Parse all json for one scene(one task)")) {
            targetScript.GenerateTrainingForOneTaskInCurrentScene();
        }

        GUILayout.Label("Task suffix", EditorStyles.boldLabel);
        AlfredAgent.taskSuffix = (string)EditorGUILayout.TextField(AlfredAgent.taskSuffix);

        if (GUILayout.Button("Parse all json for one room type (one task)")) {
            targetScript.GenerateTrainingForOneTaskInAllSceneByRoomType();
        }

    }
}
# endif