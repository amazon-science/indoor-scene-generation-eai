
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using Unity.EditorCoroutines.Editor;
#endif

public class CSceneBuilderTool : MonoBehaviour
{
    //task CDF json file
    public static string jsonPath = "Assets/Custom/Json/task_1 (1).json";//"/Users/zhayizho/Downloads/task_1 (1).json";
    public static string buildPath = "/Users/zhayizho/Desktop/unitybuild/";
    public static int globalRandomSeed;
    public static int currentJsonIndex = 0;

    public static string jsonSceneTitle;
    public static CJsonRule jsonRule = new CJsonRule();
    public static bool loadDrawerShelfCabinet = false;

    //room layout .unity
    public static string roomLayoutFolder = "Assets/Custom/LivingRooms";

    //max trial for scene generation
    public static int maxTrials = 2;
    public static int currentTrial = 0;

    GameObject _furniture_placer;
    GameObject _object_placer;
    GameObject _decoration_placer;
    GameObject _scene_builder;

    //global flag
    public static CRoomType samplingRoomType = CRoomType.LivingRoom;
    public static bool isSampling = false;
    public static bool oneJsonLock = false;

    public static bool furniturePlacingFinish = false;
    public static bool objectPlacingFinish = false;
    public static bool decorationPlacingFinish = false;

    public static bool furniturePlacingSuccessful = false;
    public static bool objectPlacingSuccessful = false;
    public static bool decorationPlacingSuccessful = false;

    public static bool last_scene_successful = false;


    //log
    public static string logFilePath = "Assets/Custom/Log/kitchen_log.txt";

    public static void LoadJsonInfo() {
        Debug.Log("Loading json from file: " + jsonPath);
        var pathSplit = jsonPath.Split('/');
        jsonSceneTitle = pathSplit[pathSplit.Length - 1];
        int endIndex = jsonSceneTitle.LastIndexOf('.');
        jsonSceneTitle = jsonSceneTitle.Substring(0, endIndex);

        CSceneBuilderTool.jsonRule = new CJsonRule();
        CSceneBuilderTool.jsonRule.LoadJsonRuleFromFile(jsonPath);
    }

#if UNITY_EDITOR
    public void GenerateScene()
    {
       
        currentTrial = 0;

        _furniture_placer = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/FurniturePlacer.prefab", typeof(GameObject));
        _object_placer = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/ObjectPlacer.prefab", typeof(GameObject));
        _decoration_placer = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/DecorationPlacer.prefab", typeof(GameObject));
        _scene_builder = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/SceneBuilder.prefab", typeof(GameObject));

        GenerateSceneFromScratch();
    }

    public void LoadNextJson() {
        string folder_path = "Assets/Custom/Json/alfred_livingrooms/"; //living room as default
        if (CSceneBuilderTool.samplingRoomType == CRoomType.Bedroom) {
            folder_path = "Assets/Custom/Json/alfred_bedrooms/";
        } else if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen) {
            folder_path = "Assets/Custom/Json/alfred_kitchens/";
        } else if (CSceneBuilderTool.samplingRoomType == CRoomType.LivingRoom) {
            folder_path = "Assets/Custom/Json/alfred_livingrooms/";
        } 
        DirectoryInfo prefabDir = new DirectoryInfo(folder_path);
        FileInfo[] prefabInfoList = prefabDir.GetFiles("*.json");
        //System.Random rng = new System.Random();
        //Debug.Log("Total json file num: " + prefabInfoList.Length);

        CSceneBuilderTool.currentJsonIndex = (CSceneBuilderTool.currentJsonIndex + 1) % prefabInfoList.Length;
        FileInfo prefabFile = prefabInfoList[CSceneBuilderTool.currentJsonIndex]; //skip every 3: 3417 / 3 kitchens
        int prefabRelativepathStart = prefabFile.ToString().IndexOf(folder_path);
        string prefabPath = prefabFile.ToString().Substring(prefabRelativepathStart);
        CSceneBuilderTool.jsonPath = prefabPath;
    }

    IEnumerator ReadJsonAndGenerateScene(bool go_back_to_click_me = false) {
        UnityEngine.Random.InitState(globalRandomSeed++);

        //A new trial for sampling scenes
        currentTrial += 1;

        //load json
        LoadJsonInfo();

        //record current scene
        var originalScene = EditorSceneManager.GetActiveScene();
        string originalScenePath = originalScene.path;

        //select scene
        int roomTypeIndex = (int)UnityEngine.Random.Range(0f, jsonRule.roomTypeOptions.Count);
        CRoomType roomt = jsonRule.roomTypeOptions[roomTypeIndex];
        CSceneBuilderTool.samplingRoomType = roomt;
        string roomtt = roomt.ToString();

        string sceneRoot = "Assets/Custom/" + roomtt + "s/";
        int sceneIndex = (int)UnityEngine.Random.Range(1f, 31f);
        int startIndex = roomt == CRoomType.LivingRoom ? 200 : roomt == CRoomType.Bedroom ? 300 : 0;

        if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen || CSceneBuilderTool.samplingRoomType == CRoomType.Bathroom) {
            //modifed room version
            roomtt = "N" + roomtt;
        }

        string sceneName = roomtt + (startIndex + sceneIndex).ToString();

        string scenePath = sceneRoot + sceneName + ".unity";

        //Debug.Log("scenePath: " + scenePath);

        ////skip bad scene models
        //if (sceneName == "LivingRoom204") {
        //    //bad scene because lack of floor receptacle
        //    CSceneBuilderTool.last_scene_successful = false;
        //    //GenerateSceneFromScratch();
        //    //return;
        //} else {
        //Debug.Log("generating scene from room: " + scenePath);

        EditorSceneManager.OpenScene(scenePath);
        GameObject helperTool = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/Dalibao.prefab", typeof(GameObject)));
        helperTool.gameObject.name = "HelperTool";

        CSceneBuilderTool.isSampling = true;
        EditorCoroutineUtility.StartCoroutineOwnerless(SetUpNewSceneFromSelf(false));

        while (CSceneBuilderTool.isSampling) {
            yield return new EditorWaitForSeconds(2);
        }

        Debug.Log("Generate one scene successful? " + CSceneBuilderTool.last_scene_successful);

        if (CSceneBuilderTool.last_scene_successful || currentTrial > maxTrials) {
            //if successful and reach max trials
            //reset and return

            using (StreamWriter sw = File.AppendText(logFilePath)) {
                sw.WriteLine(CSceneBuilderTool.currentJsonIndex.ToString() + "\t" + CSceneBuilderTool.jsonPath + "\t" + CSceneBuilderTool.last_scene_successful.ToString());
            }

            CSceneBuilderTool.last_scene_successful = false;
            CSceneBuilderTool.oneJsonLock = false;

            if (go_back_to_click_me) {
                string clickMeScenePath = "Assets/Custom/ClickMe.unity";
                EditorSceneManager.OpenScene(clickMeScenePath);
            }

            yield return null;
        } else {
            //start again
            EditorCoroutineUtility.StartCoroutineOwnerless(ReadJsonAndGenerateScene(go_back_to_click_me));
        }

        yield return null;


        
    }

    public void GenerateSceneOneByOne() {

        IEnumerator onebyone(int howMany = 1000) {
            for(int i = 0; i < howMany; ++i) {
                CSceneBuilderTool.oneJsonLock = true;
                LoadNextJson();
                Debug.Log(i + " now generate json: " + jsonPath);
                GenerateSceneFromScratch();

                while (CSceneBuilderTool.oneJsonLock) {
                    yield return new EditorWaitForSeconds(2);
                }
            }


            //    string dirPath = "Assets/Custom/Json/alfred_livingrooms/";

            //    //string[] fileInfoList = Directory.GetDirectories(dirPath);
            //    DirectoryInfo prefabDir = new DirectoryInfo(dirPath);
            //    FileInfo[] prefabInfoList = prefabDir.GetFiles("*.json");
            //    System.Random rng = new System.Random();

            //    //prefabInfoList.OrderBy(x => rng.Next()).ToArray()
            //    foreach (FileInfo prefabFile in prefabInfoList) {
            //        scene_count++;
            //        CSceneBuilderTool.isSampling = true;
            //        //if (scene_count < 41) {
            //        //    continue;
            //        //}

            //        int prefabRelativepathStart = prefabFile.ToString().IndexOf(dirPath);
            //        string prefabPath = prefabFile.ToString().Substring(prefabRelativepathStart);
            //        Debug.Log("LoadFurniturePrefab: " + prefabPath + "\n" + "scene_count: " + scene_count);

            //        ////no supported 
            //        //if (prefabPath.Contains("Statue") || prefabPath.Contains("trial_T20190907_023343_425395") ) {
            //        //    continue;
            //        //    //422868 782702
            //        //}

            //        try {
            //            EditorCoroutineUtility.StartCoroutineOwnerless(ReadJsonAndGenerateScene());

            //            jsonPath = prefabPath;
            //        } catch {
            //            Debug.LogWarning("wrong scene json: " + prefabPath);
            //            CSceneBuilderTool.isSampling = false;
            //        }


            //        while (CSceneBuilderTool.isSampling) {
            //            yield return new EditorWaitForSeconds(2);
            //        }

            //        if (scene_count > 5) {
            //            break;
            //        }
            //    }

            //    yield return null;
            //}
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(onebyone());

    }


    public void GenerateSceneFromScratch()
    {
        
        //if (currentTrial > maxTrials) {
        //    Debug.LogError("CSceneBuilderTool: generate scene fails!");
        //    return;
        //}

        //reset trials
        currentTrial = 0;

        if (!File.Exists(logFilePath)) {
            // Create a file to write to.
            File.CreateText(logFilePath);
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(ReadJsonAndGenerateScene(true));

    }

    public IEnumerator SetUpNewSceneFromSelf(bool setRandomSeed = false, int objectRandomSeed = -1, bool deleteUnsuccessful = true)
    {
        //lock
        CSceneBuilderTool.isSampling = true;

        // var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var originalScene = EditorSceneManager.GetActiveScene();
        string originalScenePath = originalScene.path;
        string saveSceneFolder = "Assets/Custom/BuildScenes/";

        int sceneIndex = globalRandomSeed; //(int)UnityEngine.Random.Range(0f, 100000f);
        if (setRandomSeed)
        {
            sceneIndex = (int)UnityEngine.Random.Range(0f, 100000f);
        }

        string saveScenePath = saveSceneFolder + jsonSceneTitle + "_" + sceneIndex.ToString() + ".unity";

        if (objectRandomSeed > 0) {
            saveScenePath = saveSceneFolder + objectRandomSeed.ToString() + ".unity";
        }

        EditorSceneManager.SaveScene(originalScene, saveScenePath, true);
        EditorSceneManager.OpenScene(saveScenePath);

        //GameObject dalibao = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/Dalibao.prefab", typeof(GameObject)));

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

        if (CSceneBuilderTool.furniturePlacingSuccessful)
        {
            Debug.Log("Place Furniture Successful!");

            //rename cabinet,drawer,shelf
            CKitchenPlacerTool kitchenPlacerTool = GameObject.Find("KitchenObjSet").GetComponent<CKitchenPlacerTool>();
            kitchenPlacerTool.ReNameKitchenSceneObjs();


            CObjectPlacerTool objectPlacerTool = GameObject.Find("ObjectPlacer").GetComponent<CObjectPlacerTool>();
            objectPlacerTool.randomSeed = (int)UnityEngine.Random.Range(0f, 1000f);

            CObjectPool objectPool = objectPlacerTool.gameObject.GetComponent<CObjectPool>();
            objectPool.randomSeed = (int)UnityEngine.Random.Range(0f, 1000f);
            if (objectRandomSeed >= 0) {
                objectPool.randomSeed = objectRandomSeed;
            }
            objectPool.roomType = CSceneBuilderTool.samplingRoomType;

            objectPlacerTool.LoadObjectPrefab();

            objectPlacerTool.CustomPlaceObject();
            while (!CSceneBuilderTool.objectPlacingFinish) {
                yield return new EditorWaitForSeconds(0.2f);
            }

            //yield return new EditorWaitForSeconds(10);

            if (CSceneBuilderTool.objectPlacingSuccessful)
            {
                Debug.Log("Place Objects Successful!");

                Debug.Log("Debuging scene tools");
                //return false;

                //Set decoration
                //CDecorationPlacerTool decorationPlacertool = GameObject.Find("DecorationPlacer").GetComponent<CDecorationPlacerTool>();
                //decorationPlacertool.GetComponent<CFurniturePool>().randomSeed = (int)UnityEngine.Random.Range(0f, 1000f);

                //decorationPlacertool.SetFloor();
                //decorationPlacertool.LoadRule();
                //decorationPlacertool.CustomPlaceDecoration();
                

                //Debug.Log("Place Decoration Successful!");

                //Restore door size
                furniturePlacerTool.RestoreDoorSize();

                //Set celling!!!
                StructureObject[] structObjs = Resources.FindObjectsOfTypeAll<StructureObject>();
                foreach (StructureObject structObj in structObjs) {
                    if (structObj.WhatIsMyStructureObjectTag == StructureObjectTag.Ceiling) {
                        structObj.gameObject.SetActive(false);
                    }
                }
                //

                //start game
                //EditorApplication.ExecuteMenuItem("Edit/Play");

                //EditorApplication.ExecuteMenuItem("Edit/Play");

                //add streaming tool
                //AddFMEStreamToCurrentScene();

                //set nav
                SetNavMeshNotWalkable();

                var agentController = FindObjectOfType<PhysicsRemoteFPSAgentController>();
                //var capsuleCollider = agentController.GetComponent<CapsuleCollider>();
                var navmeshAgent = agentController.GetComponent<NavMeshAgent>();
                navmeshAgent.enabled = true;
                // The Editor bake interface does not take with parameters and could not be modified as of 2018.3
                //var buildSettings = 
                new NavMeshBuildSettings()
                {
                    agentTypeID = navmeshAgent.agentTypeID,
                    agentRadius = 0.2f,
                    agentHeight = 1.8f,
                    agentSlope = 10,
                    agentClimb = 0.5f,
                    minRegionArea = 0.05f,
                    overrideVoxelSize = false,
                    overrideTileSize = false
                };

                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

                //save
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

                //save and register
                //var currentScene = EditorSceneManager.GetActiveScene();
                //EditorSceneManager.SaveScene(currentScene, saveScenePath);

                List<EditorBuildSettingsScene> currentBuildingScenes = new List<EditorBuildSettingsScene>();
                foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
                {
                    currentBuildingScenes.Add(buildScene);
                }
                currentBuildingScenes.Add(new EditorBuildSettingsScene(saveScenePath, true));
                EditorBuildSettings.scenes = currentBuildingScenes.ToArray();

                //successful = true;

            }
        }

        //destrop scene generating tool
        //DestroyImmediate(dalibao);

        if (!deleteUnsuccessful) {
            //save
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        Debug.Log("go back to original scene: " + originalScenePath);
        EditorSceneManager.OpenScene(originalScenePath);

        CSceneBuilderTool.last_scene_successful = objectPlacingSuccessful && furniturePlacingSuccessful;

        if (deleteUnsuccessful) {
            if ((!CSceneBuilderTool.objectPlacingSuccessful) || (!CSceneBuilderTool.furniturePlacingSuccessful)) {
                Debug.Log("now deleting scene: " + Application.dataPath + saveScenePath.Substring(6));
                //File.Delete(Application.dataPath + saveScenePath.Substring(6));
                AssetDatabase.DeleteAsset(saveScenePath);
            }
        }
        Debug.Log("Successful one scene: " + objectPlacingSuccessful + " " + furniturePlacingSuccessful);


        //reset
        CSceneBuilderTool.objectPlacingSuccessful = false;
        CSceneBuilderTool.objectPlacingFinish = false;
        CSceneBuilderTool.furniturePlacingSuccessful = false;
        CSceneBuilderTool.furniturePlacingFinish = false;
        CSceneBuilderTool.isSampling = false; 



        //return successful;
    }

    public static void SetNavMeshNotWalkable()
    {

        var objectHierarchy = GameObject.Find("Objects");
        if (objectHierarchy == null)
        {
            objectHierarchy = GameObject.Find("Object");
        }
        for (int i = 0; i < objectHierarchy.transform.childCount; i++)
        {
            var child = objectHierarchy.transform.GetChild(i);
            child.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(meshRenderer =>
            {
                Debug.Log("Mesh Renderer not walk" + meshRenderer.gameObject.name + " layer ");
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(meshRenderer.gameObject, UnityEditor.StaticEditorFlags.NavigationStatic);
                UnityEditor.GameObjectUtility.SetNavMeshArea(meshRenderer.gameObject, NavMesh.GetAreaFromName("Not Walkable"));
            });
            Debug.Log("Setting flag for " + child.gameObject.name + " layer " + NavMesh.GetAreaFromName("Not Walkable"));
        }
    }

    public static void SetNavMeshWalkable(GameObject hirerarchy)
    {

        //  var objectHierarchy = hirerarchy.transform.FirstChildOrDefault(x => x.name.Contains("Floor"));
        hirerarchy.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(meshRenderer =>
        {
            Debug.Log("Mesh Renderer walk" + meshRenderer.gameObject.name + " layer ");
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(meshRenderer.gameObject, UnityEditor.StaticEditorFlags.NavigationStatic);
            UnityEditor.GameObjectUtility.SetNavMeshArea(meshRenderer.gameObject, NavMesh.GetAreaFromName("Walkable"));
        });
    }

    public void BuildScenes()
    {
        
        //EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        Debug.Log("path: " + buildPath);
        //string[] levels = new string[]
        List<string> scenePaths = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            scenePaths.Add(scene.path);
        }

        // Build player.
        BuildPipeline.BuildPlayer(scenePaths.ToArray(), buildPath + "/BuiltGame", BuildTarget.StandaloneOSX, BuildOptions.None);

        //// Run the game (Process class from System.Diagnostics).
        //System.Diagnostics.Process proc = new System.Diagnostics.Process();
        //proc.StartInfo.FileName = buildPath + "/BuiltGame";
        //proc.Start();
    }

    //Set FMEStream to current scene
    private UnityAction<byte[]> methodDelegate;
    public void AddFMEStreamToCurrentScene()
    {
        ////string sceneName = "FloorPlan" + i.ToString() + "_physics.unity";

        //Debug.Log("Adding FMEStream to current scene: ");
        //if (GameObject.Find("FMNetworkManager") != null)
        //{
        //    //it exists
        //    return;
        //}

        ////Add FM Network Manager
        //GameObject fmSocketManager = new GameObject("FMNetworkManager");
        //fmSocketManager.SetActive(true);
        //fmSocketManager.AddComponent<FMSocketIOManager>();
        //FMSocketIOManager mangerScript = fmSocketManager.GetComponent<FMSocketIOManager>();
        //mangerScript.NetworkType = FMSocketIONetworkType.Server;

        ////Add Game View Encoder
        //GameObject gameViewEncoder = new GameObject("GameViewEncoder");
        //gameViewEncoder.AddComponent<GameViewEncoder>();
        //GameViewEncoder encoderScript = gameViewEncoder.GetComponent<GameViewEncoder>();
        //encoderScript.CaptureMode = GameViewCaptureMode.RenderCam;
        //encoderScript.Resolution.x = 1256;
        //encoderScript.Resolution.y = 960;
        //encoderScript.Quality = 50;
        //encoderScript.StreamFPS = 30;
        //encoderScript.MatchScreenAspect = false;

        ////Add RenderCam
        //GameObject renderCam = new GameObject("RenderCam");
        //renderCam.AddComponent<Camera>();
        ////renderCam.transform.parent = gameViewEncoder.transform;
        //encoderScript.RenderCam = renderCam.GetComponent<Camera>();

        //renderCam.GetComponent<Camera>().enabled = false;

        ////Add Event Listener
        //UnityEvent unityEvent = new UnityEvent();
        ////m_MyFirstAction = new UnityAction();
        //methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<byte[]>), mangerScript, "SendToOthers") as UnityAction<byte[]>;
        //encoderScript.OnDataByteReadyEvent = new UnityEventByteArray();

        ////Debug.Log(encoderScript.OnDataByteReadyEvent);
        ////encoderScript.OnDataByteReadyEvent.AddListener(Hello);

        //UnityEditor.Events.UnityEventTools.AddPersistentListener(encoderScript.OnDataByteReadyEvent, methodDelegate);
        ////Debug.Log(encoderScript.OnDataByteReadyEvent.GetPersistentEventCount());


        ////GameObject fmNetworkManager = new GameObject("FMNetworkManager");
        ////fmNetworkManager.SetActive(true);


        ////Save Scene
        ////EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

#endif
}
