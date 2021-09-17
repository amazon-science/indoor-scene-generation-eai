# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;

using System;
using Unity.EditorCoroutines.Editor; 

[CustomEditor(typeof(CSceneBuilderTool))]
public class CSceneBuilder : Editor {

    public CSceneBuilderTool targetScript;
    private void OnEnable() {
        targetScript = (CSceneBuilderTool)target;
    }  

    public override void OnInspectorGUI() {
        GUILayout.Label("Json file", EditorStyles.boldLabel);
        CSceneBuilderTool.jsonPath = (string)EditorGUILayout.TextField(CSceneBuilderTool.jsonPath);
        GUILayout.Label("Build path", EditorStyles.boldLabel);
        CSceneBuilderTool.buildPath = (string)EditorGUILayout.TextField(CSceneBuilderTool.buildPath);
        GUILayout.Label("Global random seed", EditorStyles.boldLabel);
        CSceneBuilderTool.globalRandomSeed = (int)EditorGUILayout.IntField(CSceneBuilderTool.globalRandomSeed);
        GUILayout.Space(8);
        //GUILayout.Label("Room layout path (.unity)", EditorStyles.boldLabel);
        //CSceneBuilderTool.roomLayoutFolder = (string)EditorGUILayout.TextField(CSceneBuilderTool.roomLayoutFolder);

        GUILayout.Label("\n Log");
        CSceneBuilderTool.samplingRoomType = (CRoomType)EditorGUILayout.EnumPopup("Room Type:", CSceneBuilderTool.samplingRoomType);
        //CSceneBuilderTool.currentJsonIndex = (int)EditorGUILayout.IntField("current json index:", CSceneBuilderTool.currentJsonIndex);
        //CSceneBuilderTool.logFilePath = (string)EditorGUILayout.TextField("log file", CSceneBuilderTool.logFilePath);

        CSceneBuilderTool.loadDrawerShelfCabinet = (bool)EditorGUILayout.Toggle("Load shelf, cabinet or drawer", CSceneBuilderTool.loadDrawerShelfCabinet);

        //if (GUILayout.Button("Click me")) {
        //    targetScript.GenerateScene();
        //}

        //if (GUILayout.Button("Generate Scene from scratch")) {
        //    targetScript.GenerateSceneFromScratch();
        //}

        GUILayout.Label("\n\n Generation for one json in specific scene prefab");

        if (GUILayout.Button("Load Json Test")) {
            Debug.Log("Loading json from file: " + CSceneBuilderTool.jsonPath);
            CSceneBuilderTool.LoadJsonInfo();
        }

        if (GUILayout.Button("Generate Scene from Current Prefab")) {
            EditorCoroutineUtility.StartCoroutineOwnerless(targetScript.SetUpNewSceneFromSelf(true));
        }

        //if (GUILayout.Button("Generate Scene One by One")) {
        //    //Debug.Log(CSceneBuilderTool.jsonRule.taskDesc);
        //    targetScript.GenerateSceneOneByOne();
        //}

        //if (GUILayout.Button("Load next json (debug)")) {
        //    //Debug.Log(CSceneBuilderTool.jsonRule.taskDesc);
        //    targetScript.LoadNextJson();
        //}




        GUILayout.Label("\n\n Batch generation for mutiple json");
        if (GUILayout.Button("One merged json & generate from current scene")) {

            CObjectPool objectPool = GameObject.Find("ObjectPlacer").GetComponent<CObjectPool>();
            string scene_name = EditorSceneManager.GetActiveScene().name;
            string sceneIndexStr = scene_name.Substring(scene_name.Length - 3);
            if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen) {
                sceneIndexStr = scene_name.Substring(8);
            }
            objectPool.taskPath = "Assets/Custom/Json/Floorplans/FloorPlan" + sceneIndexStr;

            Debug.Log("sceneIndexStr: " + sceneIndexStr);
            objectPool.LoadMergedJson();

            IEnumerator MergeSceneGeneration(string sceneIndexss) {
                CSceneBuilderTool.currentTrial++;
                CSceneBuilderTool.isSampling = true;
                Debug.Log("Start MergeSceneGeneration");
                EditorCoroutineUtility.StartCoroutineOwnerless(targetScript.SetUpNewSceneFromSelf(setRandomSeed: true, objectRandomSeed: int.Parse(sceneIndexss)));

                while (CSceneBuilderTool.isSampling) {
                    yield return new EditorWaitForSeconds(2);
                }

                if (CSceneBuilderTool.last_scene_successful || CSceneBuilderTool.currentTrial > 5) {
                    //if successful and reach max trials
                    //reset and return
                    CSceneBuilderTool.oneJsonLock = false;

                    yield return null;
                } else {
                    //start again
                    EditorCoroutineUtility.StartCoroutineOwnerless(MergeSceneGeneration(sceneIndexss));
                }
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(MergeSceneGeneration(sceneIndexStr));
        }



        if (GUILayout.Button("Load merged json & generate from all scenes")) {

            CSceneBuilderTool.currentTrial = 0;
            //CSceneBuilderTool.samplingRoomType = CRoomType.Kitchen;

            IEnumerator MergeSceneGeneration(string sceneIndexss) {
                CSceneBuilderTool.currentTrial++;
                CSceneBuilderTool.isSampling = true;
                EditorCoroutineUtility.StartCoroutineOwnerless(targetScript.SetUpNewSceneFromSelf(setRandomSeed: true,
                    objectRandomSeed: int.Parse(sceneIndexss), deleteUnsuccessful: false));

                while (CSceneBuilderTool.isSampling) {
                    yield return new EditorWaitForSeconds(2);
                }

                //CSceneBuilderTool.oneJsonLock = false;
                if (CSceneBuilderTool.last_scene_successful || CSceneBuilderTool.currentTrial > 5) {
                    //if successful and reach max trials
                    //reset and return
                    CSceneBuilderTool.oneJsonLock = false;

                    yield return null;
                } else {
                    //start again
                    EditorCoroutineUtility.StartCoroutineOwnerless(MergeSceneGeneration(sceneIndexss));
                }
            }

            IEnumerator GenerateAllMergeScene() {
                DirectoryInfo sceneDir = new DirectoryInfo("Assets/Custom/Kitchens");
                CRoomType mergedSampleRoomType = CSceneBuilderTool.samplingRoomType;

                if (CSceneBuilderTool.samplingRoomType == CRoomType.LivingRoom) {
                    sceneDir = new DirectoryInfo("Assets/Custom/LivingRooms");
                }
                else if (CSceneBuilderTool.samplingRoomType == CRoomType.Bedroom) {
                    sceneDir = new DirectoryInfo("Assets/Custom/Bedrooms");
                }
                else{ //(CSceneBuilderTool.samplingRoomType == CRoomType.Bathroom)
                    sceneDir = new DirectoryInfo("Assets/Custom/Bathrooms");
                }


                FileInfo[] sceneList = sceneDir.GetFiles("*.unity");
                foreach (FileInfo sceneFile in sceneList) {
                    Debug.Log("Generated scene from: " + sceneFile.ToString());
                    EditorSceneManager.OpenScene(sceneFile.ToString());
                    CSceneBuilderTool.currentTrial = 0;

                    //????
                    CSceneBuilderTool.samplingRoomType = CRoomType.Bedroom; 

                    CObjectPool objectPool = GameObject.Find("ObjectPlacer").GetComponent<CObjectPool>();
                    string scene_name = EditorSceneManager.GetActiveScene().name;
                    string sceneIndexStr = scene_name.Substring(scene_name.Length - 3);

                   

                    if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen) {
                        sceneIndexStr = scene_name.Substring(8);
                    }

                    objectPool.taskPath = "Assets/Custom/Json/Floorplans/FloorPlan" + sceneIndexStr;
                    List<string> validation_scene_indexes = new List<string>() { "9", "10", "29", "215", "219", "226", "308",
                        "315", "325", "404", "424", "425"};
                    if (validation_scene_indexes.Contains(sceneIndexStr))
                        continue;


                    Debug.Log("sceneIndexStr: " + sceneIndexStr);
                    objectPool.LoadMergedJson();

                    CSceneBuilderTool.oneJsonLock = true;
                    EditorCoroutineUtility.StartCoroutineOwnerless(MergeSceneGeneration(sceneIndexStr));

                    while (CSceneBuilderTool.oneJsonLock) {
                        yield return new EditorWaitForSeconds(2);
                    }
                    yield return null;
                }
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(GenerateAllMergeScene());
        }
    }
}
#endif