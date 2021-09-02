#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;


using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.EditorCoroutines.Editor;

public class AlfredTaskHelperTool : MonoBehaviour {
    public static int sceneSetIdx = 0;
    public static string targetGeneratingScenePath;
    public static AlfredTaskType alfredTaskType = AlfredTaskType.all;
    public static bool oneSceneLock = false;
    //parse json and generate data for ET
    public void ParseAllTaskJsonsForRoomType() {
        string clickmeSceneName = EditorSceneManager.GetActiveScene().path;

        string sceneDirString = "Assets/Custom/BuildTrainingKitchens";

        if (CSceneBuilderTool.samplingRoomType == CRoomType.LivingRoom) {
            sceneDirString = "Assets/Custom/BuildTrainingLivingrooms";
        } else if (CSceneBuilderTool.samplingRoomType == CRoomType.Bedroom) {
            sceneDirString = "Assets/Custom/BuildTrainingBedrooms";
        } else if (CSceneBuilderTool.samplingRoomType == CRoomType.Bathroom) {
            sceneDirString = "Assets/Custom/BuildTrainingBathrooms";
        }

        sceneDirString = sceneDirString + sceneSetIdx.ToString();

        DirectoryInfo sceneDir = new DirectoryInfo(sceneDirString);

        FileInfo[] sceneList = sceneDir.GetFiles("*.unity");
        int debug_idx = 0;
        foreach (FileInfo sceneFile in sceneList) {
            Debug.Log("Generated tasks from: " + sceneFile.ToString());
            EditorSceneManager.OpenScene(sceneFile.ToString());
            string currentSceneName = EditorSceneManager.GetActiveScene().name;

            debug_idx++;
            if (debug_idx > 10)
                break;
            //if ((int.Parse(currentSceneName) >=  11 && int.Parse(currentSceneName) <= 21) || int.Parse(currentSceneName) == 1) {
            //    continue;
            //}

            //if (int.Parse(currentSceneName) > 302) {
            //    break;
            //}

            //if (int.Parse(currentSceneName) > 302) {
            //    break;
            //}

            FixScene();

            AlfredAgent alfredAgent = GameObject.FindObjectOfType<AlfredAgent>();
            if (alfredAgent == null) {
                GameObject dalibao = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/Dalibao.prefab", typeof(GameObject)));
                dalibao.name = "HelperTool";
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            alfredAgent = GameObject.FindObjectOfType<AlfredAgent>();


            AlfredAgent.taskJsonFolder = "Assets/Custom/Json/Floorplans/FloorPlan" + currentSceneName + "/tasks/";

            alfredAgent.ParseAllTaskJsonsForCurrentScene();
        }

        //go back to clickme scene
        EditorSceneManager.OpenScene(clickmeSceneName);
    }

    public void FixScene() {
        //Set celling!!!
        StructureObject[] structObjs = Resources.FindObjectsOfTypeAll<StructureObject>();
        foreach (StructureObject structObj in structObjs) {
            if (structObj.WhatIsMyStructureObjectTag == StructureObjectTag.Ceiling) {
                structObj.gameObject.SetActive(true); 
            }
        }

        //fix no contain parent in microwave
        foreach (var receptacleSop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
            //Debug.Log("derecept:" + receptacleSop.gameObject.name);
            if (receptacleSop.ReceptacleTriggerBoxes != null && receptacleSop.ReceptacleTriggerBoxes.Length > 0) {
                foreach (GameObject rtb in receptacleSop.ReceptacleTriggerBoxes) {
                    if(rtb == null) { break; }
                    Contains containsScript = rtb.GetComponent<Contains>();
                    if (containsScript != null && containsScript.myParent == null) {
                        containsScript.myParent = receptacleSop.gameObject;
                    }

                }

            }
        }
    }

    public void GenerateTrainingForOneTaskInAllSceneByRoomType() {
        IEnumerator GenerateTrainingForOneTaskInAllSceneByRoomTypeIE() {
            List<string> validation_scene_indexes = new List<string>() { "9", "10", "29", "215", "219", "226", "308", "315", "325", "404", "424", "425" };
            string sceen_root = "Assets/Custom/Bedrooms/Bedroom";
            for (int i = 1; i <= 30; ++i) {
                string sceneIndex = (300 + i).ToString();
                if (validation_scene_indexes.Contains(sceneIndex))
                    continue;

                AlfredTaskHelperTool.targetGeneratingScenePath = sceen_root + sceneIndex + ".unity";
                oneSceneLock = true;
                GenerateTrainingForOneTaskInCurrentScene();
                while (oneSceneLock) {
                    yield return new EditorWaitForSeconds(1f);
                }
            }
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(GenerateTrainingForOneTaskInAllSceneByRoomTypeIE());
    }

    //generate task only
    public void GenerateTrainingForOneTaskInCurrentScene() {
        //open scene
        EditorSceneManager.OpenScene(AlfredTaskHelperTool.targetGeneratingScenePath);
        var currentScene = EditorSceneManager.GetActiveScene();
        string currentScenePath = currentScene.path;
        string scene_name = currentScene.name;
        string sceneIndexStr = scene_name.Substring(scene_name.Length - 3);
        if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen) {
            sceneIndexStr = scene_name.Substring(8);
        }


        IEnumerator GenerateTrainingForOneTaskIE() {
            int debug_idx = 0;
            string jsonFolder = "Assets/Custom/Json/Floorplans/FloorPlan" + sceneIndexStr + "/tasks";
            DirectoryInfo scenesDir = new DirectoryInfo(jsonFolder);
            FileInfo[] sceneFileList = scenesDir.GetFiles("*.json");
            foreach (FileInfo jsonFile in sceneFileList) {
                //debug_idx++;
                //if(debug_idx > 2) {
                //    break;
                //}
                Debug.Log("Generate Scene and Render Training Data At:" + jsonFile.ToString());

                //filter json for specific task
                if (!jsonFile.ToString().Contains("look_at_obj_in_light")) {
                    continue;
                }

                CObjectPool objectPool = GameObject.FindObjectOfType<CObjectPool>();


                CSceneBuilderTool.jsonPath = jsonFile.ToString();
                CSceneBuilderTool.LoadJsonInfo();


                objectPool.taskPath = CSceneBuilderTool.jsonPath;

                //reset
                CSceneBuilderTool.objectPlacingSuccessful = false;
                CSceneBuilderTool.objectPlacingFinish = false;
                CSceneBuilderTool.furniturePlacingSuccessful = false;
                CSceneBuilderTool.furniturePlacingFinish = false;

                //set furniture
                CFurniturePlacerTool furniturePlacerTool = GameObject.Find("FurniturePlacer").GetComponent<CFurniturePlacerTool>();
                //furniturePlacerTool.randomSeed = (int)UnityEngine.Random.Range(0f, 10000f);
                CFurniturePool furnturePool = furniturePlacerTool.gameObject.GetComponent<CFurniturePool>();
                furnturePool.randomSeed = (int)UnityEngine.Random.Range(0f, 10000f);
                furnturePool.roomType = CSceneBuilderTool.samplingRoomType;

                furniturePlacerTool.SetFloorAndScaleDoor();
                if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen || CSceneBuilderTool.samplingRoomType == CRoomType.Bathroom) {
                    //additional process for Kitchen
                    CKitchenPlacerTool kitchenPlacerTool = GameObject.Find("KitchenObjSet").GetComponent<CKitchenPlacerTool>();
                    kitchenPlacerTool.ReNameKitchenSceneObjs();
                }
                furniturePlacerTool.LoadFurniturePrefab();

                furniturePlacerTool.CustomPlaceFurniture();
                while (!CSceneBuilderTool.furniturePlacingFinish) {
                    yield return new EditorWaitForSeconds(0.2f);
                }

                Debug.Log("CSceneBuilderTool.furniturePlacingFinish" + CSceneBuilderTool.furniturePlacingSuccessful);

                if (CSceneBuilderTool.furniturePlacingSuccessful) {
                    Debug.Log("Place Furniture Successful!");

                    //rename cabinet,drawer,shelf
                    CKitchenPlacerTool kitchenPlacerTool = GameObject.Find("KitchenObjSet").GetComponent<CKitchenPlacerTool>();
                    kitchenPlacerTool.ReNameKitchenSceneObjs();

                    CObjectPlacerTool objectPlacerTool = GameObject.Find("ObjectPlacer").GetComponent<CObjectPlacerTool>();
                    objectPlacerTool.randomSeed = (int)UnityEngine.Random.Range(0f, 1000f);

                    CObjectPool qobjectPool = objectPlacerTool.gameObject.GetComponent<CObjectPool>();
                    qobjectPool.randomSeed = (int)UnityEngine.Random.Range(0f, 1000f);
         
                    qobjectPool.roomType = CSceneBuilderTool.samplingRoomType;
                    objectPlacerTool.LoadObjectPrefab();
                    objectPlacerTool.CustomPlaceObject();
                    while (!CSceneBuilderTool.objectPlacingFinish) {
                        yield return new EditorWaitForSeconds(1f);
                    }

                    //Set celling
                    StructureObject[] structObjs = Resources.FindObjectsOfTypeAll<StructureObject>();
                    foreach (StructureObject structObj in structObjs) {
                        if (structObj.WhatIsMyStructureObjectTag == StructureObjectTag.Ceiling) {
                            structObj.gameObject.SetActive(true);
                        }
                    }

                    //generate alfred training data
                    AlfredAgent.taskFinish = false;
                    AlfredAgent.hasGraph = false;
                    AlfredAgent.taskJsonPath = CSceneBuilderTool.jsonPath;

                    if (CSceneBuilderTool.objectPlacingSuccessful) {
                        AlfredAgent alfredAgent = GameObject.FindObjectOfType<AlfredAgent>();

                        alfredAgent.OneJsonForCurrentScene();
                        while (!AlfredAgent.taskFinish) {
                            yield return new EditorWaitForSeconds(2);
                        }
                    }
                }

                if (!(CSceneBuilderTool.objectPlacingSuccessful && CSceneBuilderTool.furniturePlacingSuccessful)) {
                    Debug.LogWarning("Wrong json/Wrong scene/Wrong solution: " + AlfredAgent.taskJsonPath);
                }

                EditorSceneManager.OpenScene(currentScenePath);
            }

            oneSceneLock = false;
            yield return null;
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(GenerateTrainingForOneTaskIE());
    }
}
#endif