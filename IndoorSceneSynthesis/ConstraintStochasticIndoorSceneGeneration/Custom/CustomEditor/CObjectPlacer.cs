#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(CObjectPlacerTool))]
public class CObjectPlacer : Editor
{
    public CObjectPlacerTool targetScript;

    private void OnEnable()
    {
        targetScript = (CObjectPlacerTool)target;
    }
    //Set up GUI on Unity Editor
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Place Object Test"))
        {
            targetScript.PlaceObject();
        }

        if (GUILayout.Button("Load Obj Rule & Prefab"))
        {
            targetScript.LoadObjectPrefab();
        }

        if (GUILayout.Button("Custom Place Object"))
        {
            targetScript.CustomPlaceObject();
        }

        GUILayout.Space(8);
        GUILayout.Label("Set Obj for ET", EditorStyles.boldLabel);
        if (GUILayout.Button("Get Obj Poses and Write")) {
            targetScript.SetSpawner();
            targetScript.objectPool.GetObjectPoses();
        }

        if (GUILayout.Button("Clear Generated Objects")) {
            targetScript.objectPool.ClearGeneratedObjects();
        }
        if (GUILayout.Button("Clear Json Mentioned Objects")) {
            targetScript.objectPool.ClearObjInJson();
        }
        if(GUILayout.Button("Restore scene")) {
            targetScript.objectPool.RestoreScene();
        }

        if (GUILayout.Button("Load Merged Json")) {
            targetScript.SetSpawner();
            targetScript.objectPool.LoadMergedJson();
        }

        if (GUILayout.Button("Load Next Obj Json")) {
            targetScript.SetSpawner();
            targetScript.objectPool.LoadNextObjJson();
        }

        GUILayout.Space(8);
        GUILayout.Label("One Click", EditorStyles.boldLabel);
        if (GUILayout.Button("Parse this scene for one click")) {
            targetScript.SetSpawner();
            targetScript.objectPool.LoopOverJsonFolderToGetObjPos();
        }
    }
}
# endif