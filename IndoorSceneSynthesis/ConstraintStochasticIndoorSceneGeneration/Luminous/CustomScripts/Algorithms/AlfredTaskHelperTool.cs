#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.IO;


using UnityEditor;
using UnityEditor.SceneManagement;


public class AlfredTaskHelperTool : MonoBehaviour {
    //parse json and generate data for ET
    public void ParseAllTaskJsonsForRoomType() {
        string clickmeSceneName = EditorSceneManager.GetActiveScene().path;

        DirectoryInfo sceneDir = new DirectoryInfo("Assets/Custom/BuildTrainingKitchens");

        if (CSceneBuilderTool.samplingRoomType == CRoomType.LivingRoom) {
            sceneDir = new DirectoryInfo("Assets/Custom/BuildTrainingLivingrooms2");
        } else if (CSceneBuilderTool.samplingRoomType == CRoomType.Bedroom) {
            sceneDir = new DirectoryInfo("Assets/Custom/BuildTrainingBedrooms2");
        } else if (CSceneBuilderTool.samplingRoomType == CRoomType.Bathroom) {
            sceneDir = new DirectoryInfo("Assets/Custom/BuildTrainingBathrooms2");
        }

        FileInfo[] sceneList = sceneDir.GetFiles("*.unity");
        foreach (FileInfo sceneFile in sceneList) {
            Debug.Log("Generated tasks from: " + sceneFile.ToString());
            EditorSceneManager.OpenScene(sceneFile.ToString());
            string currentSceneName = EditorSceneManager.GetActiveScene().name;

            //if (int.Parse(currentSceneName) < 2) {
            //    continue;
            //}

            //if (int.Parse(currentSceneName) > 302) {
            //    break;
            //}


            AlfredAgent alfredAgent = GameObject.FindObjectOfType<AlfredAgent>();
            if (alfredAgent == null) {
                GameObject dalibao = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/Dalibao.prefab", typeof(GameObject)));
                dalibao.name = "HelperTool";
            }

            //Set celling!!!
            StructureObject[] structObjs = Resources.FindObjectsOfTypeAll<StructureObject>();
            foreach (StructureObject structObj in structObjs) {
                if (structObj.WhatIsMyStructureObjectTag == StructureObjectTag.Ceiling) {
                    structObj.gameObject.SetActive(true);
                }
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            alfredAgent = GameObject.FindObjectOfType<AlfredAgent>();

            
            AlfredAgent.taskJsonFolder = "Assets/Custom/Json/Floorplans/FloorPlan" + currentSceneName + "/tasks/";

            alfredAgent.ParseAllTaskJsonsForCurrentScene();

        }

        //go back to clickme scene
        EditorSceneManager.OpenScene(clickmeSceneName);
    }
}
#endif