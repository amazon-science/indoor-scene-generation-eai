# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(AddBuildingTool))]
public class AddBuilding : Editor
{
    public List<EditorBuildSettingsScene> currentBuildingScenes;
    void AddScenePath(string scenesAtPath)
    {
        DirectoryInfo scenesDir = new DirectoryInfo(scenesAtPath);
        FileInfo[] sceneFileList = scenesDir.GetFiles("*.unity");
        foreach (FileInfo sceneFile in sceneFileList)
        {
            //Debug.Log(sceneFile.Name);
            EditorSceneManager.OpenScene(sceneFile.ToString());

            foreach (var sop in GameObject.FindObjectsOfType<SimObjPhysics>())
            {
                if (sop.ObjType == SimObjType.Floor)
                {
                    sop.gameObject.GetComponent<SimObjPhysics>().enabled = true;
                }

                if (sop.ObjType == SimObjType.Fridge || sop.ObjType == SimObjType.Microwave)
                {
                    Transform boundingbox1 = sop.gameObject.transform.Find("BoundingBox (1)");
                    if (boundingbox1 != null)
                    {
                        boundingbox1.gameObject.SetActive(true);
                        BoxCollider box = boundingbox1.GetComponent<BoxCollider>();
                        if (box != null)
                        {
                            box.enabled = false;
                        }
                    }

                    Transform boundingbox0 = sop.gameObject.transform.Find("BoundingBox");
                    if (boundingbox0 != null)
                    {
                        boundingbox0.gameObject.SetActive(true);
                        BoxCollider box = boundingbox0.GetComponent<BoxCollider>();
                        if (box != null)
                        {
                            box.enabled = false;
                        }
                    }
                }

            }
            //EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            //foreach (var so in GameObject.FindObjectsOfType<StructureObject>())
            //{
            //    if (so.WhatIsMyStructureObjectTag == StructureObjectTag.Fridge)
            //    {
            //        so.gameObject.SetActive(true);
            //    }
            //}
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            currentBuildingScenes.Add(new EditorBuildSettingsScene(scenesAtPath + sceneFile.Name, true));
        }
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Add buildscenes to build settings"))
        {

            EditorBuildSettings.scenes = null;
            currentBuildingScenes = new List<EditorBuildSettingsScene>();

            AddScenePath("Assets/Custom/CustomSet1/");


            //foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes) {
            //    currentBuildingScenes.Add(buildScene);
            //}

            EditorBuildSettings.scenes = currentBuildingScenes.ToArray();
        }
    }

}
# endif