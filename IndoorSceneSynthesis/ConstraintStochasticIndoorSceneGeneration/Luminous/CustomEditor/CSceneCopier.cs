# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Assertions;
using System.IO;
using System;

using Unity.EditorCoroutines.Editor;

[CustomEditor(typeof(CSceneCopierTool))]
public class CSceneCopier : Editor {
    public CSceneCopierTool targetScript;
    public List<SceneAsset> m_SceneAssets = new List<SceneAsset>();
    public List<string> m_SceneNames = new List<string>();


    private void OnEnable() {
        targetScript = (CSceneCopierTool)target;
    }
    public override void OnInspectorGUI() {
        GUILayout.Label("Scenes to copy", EditorStyles.boldLabel);
        //fromScene = (SceneAsset)EditorGUILayout.ObjectField(fromScene, typeof(SceneAsset), false);
        for (int i = 0; i < m_SceneAssets.Count; ++i) {
            m_SceneAssets[i] = (SceneAsset)EditorGUILayout.ObjectField(m_SceneAssets[i], typeof(SceneAsset), false);
        }

        if (GUILayout.Button("Add")) {
            m_SceneAssets.Add(null);
            m_SceneNames.Add("");
        }

        GUILayout.Space(8);

        GUILayout.Label("New Scene Names", EditorStyles.boldLabel);
        //newSceneName = (string)EditorGUILayout.TextField(newSceneName);
        for (int i = 0; i < m_SceneAssets.Count; ++i) {
            m_SceneNames[i] = (string)EditorGUILayout.TextField(m_SceneNames[i]);
        }

        GUILayout.Label("\n Copy iThor Scene Tools");
        if (GUILayout.Button("Add Structure by Room Type")) {
            CopyRoomFromTypes(CSceneBuilderTool.samplingRoomType);
            //CopyStructure2NewScene();
        }

        if (GUILayout.Button("Copy Structure !")) {
            //CopyRoomFromTypes();
            CopyStructure2NewScene();
        }

        //if (GUILayout.Button("Add Structure to Kitchens"))
        //{
        //    AddGameObjectToScene();
        //}
        GUILayout.Label("\n Screenshot Tools");
        //if (GUILayout.Button("Take screenshots"))
        //{
        //    GetAllScreenShots();
        //}

        if (GUILayout.Button("Take screenshot for current scene")) {
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            TakeScreenShot(new FileInfo(@currentScenePath));
        }

        if (GUILayout.Button("Take Screenshots by one click")) {
            TakeScreenShotbyOneClick();
        }

        GUILayout.Label("\n Scene generation debug Tools");
        if (GUILayout.Button("Generate scene by hand")) {
            GenerateSceneByHand();
        }



        if (GUILayout.Button("Generate scene one click")) {
            GenerateSceneAt();
        }

        if (GUILayout.Button("Add buildscenes to build settings")) {

            EditorBuildSettings.scenes = null;
            List<EditorBuildSettingsScene> currentBuildingScenes = new List<EditorBuildSettingsScene>();
            void AddScenePath(string scenesAtPath) {
                DirectoryInfo scenesDir = new DirectoryInfo(scenesAtPath);
                FileInfo[] sceneFileList = scenesDir.GetFiles("*.unity");
                foreach (FileInfo sceneFile in sceneFileList) {
                    //Debug.Log(sceneFile.Name);
                    currentBuildingScenes.Add(new EditorBuildSettingsScene(scenesAtPath + sceneFile.Name, true));
                }
            }
            AddScenePath("Assets/Custom/BuildTrainingKitchens/");
            AddScenePath("Assets/Custom/BuildTrainingLivingrooms/");
            AddScenePath("Assets/Custom/BuildTrainingBedrooms/");
            AddScenePath("Assets/Custom/BuildTrainingBathrooms/");


            //foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes) {
            //    currentBuildingScenes.Add(buildScene);
            //}

            EditorBuildSettings.scenes = currentBuildingScenes.ToArray();
        }

        if (GUILayout.Button("Post Process Scene")) {
            IEnumerator PostProcessScene() {
                //post
                int debug_index = 0;
                DirectoryInfo scenesDir = new DirectoryInfo("Assets/Custom/BuildTrainingKitchens/");
                FileInfo[] sceneFileList = scenesDir.GetFiles("*.unity");
                foreach (FileInfo sceneFile in sceneFileList) {
                    EditorSceneManager.OpenScene(sceneFile.ToString());

                    //delete helpertool
                    GameObject helpertool = GameObject.Find("HelperTool");
                    if (helpertool != null) {
                        DestroyImmediate(helpertool);
                    }

                    GameObject dalibao = GameObject.Find("Dalibao");
                    if (dalibao != null) {
                        DestroyImmediate(dalibao);
                    }

                    GameObject furniturePlacer = GameObject.Find("FurniturePlacer");
                    if (furniturePlacer != null) {
                        DestroyImmediate(furniturePlacer);
                    }

                    GameObject sceneBuilder = GameObject.Find("SceneBuilder");
                    if (sceneBuilder != null) {
                        DestroyImmediate(sceneBuilder);
                    }

                    GameObject objectPlacer = GameObject.Find("ObjectPlacer");
                    if (objectPlacer != null) {
                        DestroyImmediate(objectPlacer);
                    }

                    GameObject decorationPlacer = GameObject.Find("DecorationPlacer");
                    if (decorationPlacer != null) {
                        DestroyImmediate(decorationPlacer);
                    }

                    //Set celling!!!
                    StructureObject[] structObjs = Resources.FindObjectsOfTypeAll<StructureObject>();
                    foreach (StructureObject structObj in structObjs) {
                        if (structObj.WhatIsMyStructureObjectTag == StructureObjectTag.Ceiling) {
                            structObj.gameObject.SetActive(true);
                        }
                    }

                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    //break;
                    //yield return new EditorWaitForSeconds(1);

                    yield return null;
                    //if(debug_index++ > 10) {
                    //    break;
                    //}

                }

            }

            EditorCoroutineUtility.StartCoroutineOwnerless(PostProcessScene());

            //TakeScreenShot(sceneFile);
            //SceneAsset oneScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneFile.ToString());
            //m_SceneAssets.Add(oneScene);

        }

        if (GUILayout.Button("Debug 1")) {
            string scenesAtPath = "Assets/Custom/Livingrooms/";
            DirectoryInfo scenesDir = new DirectoryInfo(scenesAtPath);
            FileInfo[] sceneFileList = scenesDir.GetFiles("*.unity");
            foreach (FileInfo sceneFile in sceneFileList) {
                EditorSceneManager.OpenScene(sceneFile.ToString());
                string currentSceneName = EditorSceneManager.GetActiveScene().name.Substring(10); // an integer
                var curScene = EditorSceneManager.GetActiveScene();
                //curScene.name = currentSceneName;
                Debug.Log(scenesAtPath + currentSceneName + ".unity" + " //currentSceneName: " + currentSceneName);
                AssetDatabase.RenameAsset(scenesAtPath + "Livingroom" + currentSceneName + ".unity", currentSceneName + ".unity");

                //EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
        }

        GUILayout.Label("\n Add Helpertool to scenes");
        if (GUILayout.Button("Add Helpertool to scenes")) {
            AddHelperTools();
        }
        if (GUILayout.Button("Delete Helpertool to scenes")) {
            DeleteHelperTools();
        }
        if (GUILayout.Button("Loop over build scene to get object positions")) {
            LoopOverBuildSceneToGetObjPos();
        }

        if (GUILayout.Button("Rename faucets")) {
            RenameFaucets();
        }

    }

    private void DeleteHelperTools() {
        DirectoryInfo sceneDir = new DirectoryInfo("Assets/Custom/BuildTrainingBathrooms");
        FileInfo[] sceneList = sceneDir.GetFiles("*.unity");
        foreach (FileInfo sceneFile in sceneList) {

            //prefabPathList.Add(prefabPath);
            EditorSceneManager.OpenScene(sceneFile.ToString());

            GameObject dalibao = GameObject.Find("HelperTool");
            if (dalibao != null) {
                DestroyImmediate(dalibao);
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }

    private void RenameFaucets() {
        DirectoryInfo sceneDir = new DirectoryInfo("Assets/Custom/BuildTrainingKitchens");
        FileInfo[] sceneList = sceneDir.GetFiles("*.unity");
        foreach (FileInfo sceneFile in sceneList) {

            //prefabPathList.Add(prefabPath);
            EditorSceneManager.OpenScene(sceneFile.ToString());

            string sceneIndexStr = sceneFile.ToString().Substring(sceneFile.ToString().Length - 3);

            Debug.Log("sceneIndexStr: " + sceneIndexStr);

            GameObject dalibao = GameObject.Find("HelperTool");
            if (dalibao == null) {
                dalibao = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/Dalibao.prefab", typeof(GameObject)));
                dalibao.name = "HelperTool";
                CObjectPool objectPool = GameObject.Find("ObjectPlacer").GetComponent<CObjectPool>();
                objectPool.taskPath = "Assets/Custom/Json/Floorplans/FloorPlan" + sceneIndexStr;

                //diable ceiling
                StructureObject[] structObjs = GameObject.FindObjectsOfType<StructureObject>();
                foreach (StructureObject structObj in structObjs) {
                    if (structObj.WhatIsMyStructureObjectTag == StructureObjectTag.Ceiling) {
                        structObj.gameObject.SetActive(false);
                    }
                }
            }

            CKitchenPlacerTool kitchenPlacerTool = GameObject.FindObjectOfType<CKitchenPlacerTool>();
            kitchenPlacerTool.ReNameKitchenSceneObjs();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }

    public static void PerformOperation(string operation) {
        EditorApplication.ExecuteMenuItem("Edit/" + operation);
    }

    public void TakeScreenShotbyOneClick(string scenesAtPath = "Assets/Custom/BuildTrainingBedrooms/") {
        //destroy objs
        void Destroybyname(string objName) {
            //delete helpertool
            GameObject helpertool = GameObject.Find(objName);
            if (helpertool) {
                DestroyImmediate(helpertool);
            }
        }

        IEnumerator TakeAllSceneShots() {
            DirectoryInfo scenesDir = new DirectoryInfo(scenesAtPath);
            FileInfo[] sceneFileList = scenesDir.GetFiles("*.unity");
            foreach (FileInfo sceneFile in sceneFileList) {
                EditorSceneManager.OpenScene(sceneFile.ToString());
                yield return new EditorWaitForSeconds(2);

                Light[] allLights = GameObject.FindObjectsOfType<Light>();
                foreach (var light in allLights) {
                    if (light.type == LightType.Point) {
                        light.gameObject.SetActive(false);
                    }
                }

                StructureObject[] structObjs = GameObject.FindObjectsOfType<StructureObject>();
                GameObject ceiling = null;
                foreach (StructureObject structObj in structObjs) {
                    if (structObj.WhatIsMyStructureObjectTag == StructureObjectTag.Ceiling) {
                        ceiling = structObj.gameObject;
                        ceiling.SetActive(false);
                    }
                }

                TakeScreenShot(sceneFile);

                if (ceiling != null) {
                    ceiling.SetActive(true);
                }


                //SceneAsset oneScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneFile.ToString());
                //m_SceneAssets.Add(oneScene);


                Destroybyname("HelperTool");

            }
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(TakeAllSceneShots());
    }

    IEnumerator GenerateSceneFromList() {
        for (int i = 0; i < m_SceneAssets.Count; ++i) {
            SceneAsset fromScene = m_SceneAssets[i];
            string scenePath = AssetDatabase.GetAssetPath(fromScene);
            Debug.Log("CDF Scene from scene path: " + scenePath);

            EditorSceneManager.OpenScene(scenePath);
            Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/Dalibao.prefab", typeof(GameObject)));

            CSceneBuilderTool.LoadJsonInfo();
            //CSceneBuilderTool.jsonRule.LoadJsonRuleFromFile(CSceneBuilderTool.jsonPath);

            CSceneBuilderTool.globalRandomSeed = (int)UnityEngine.Random.Range(0f, 10000f);
            CSceneBuilderTool scene_builder = GameObject.Find("SceneBuilder").GetComponent<CSceneBuilderTool>();

            CSceneBuilderTool.isSampling = true; //lock it for sure
            EditorCoroutineUtility.StartCoroutineOwnerless(scene_builder.SetUpNewSceneFromSelf(false));

            while (CSceneBuilderTool.isSampling) {
                yield return new EditorWaitForSeconds(1);
            }

            ////destrop scene generating tool
            //DestroyImmediate(GameObject.Find("Dalibao"));

        }
        yield return null;
    }

    public void GenerateSceneByHand() {
        EditorCoroutineUtility.StartCoroutineOwnerless(GenerateSceneFromList());
    }

    public void GenerateSceneAt() {
        string scenesAtPath = CSceneBuilderTool.roomLayoutFolder;
        DirectoryInfo scenesDir = new DirectoryInfo(scenesAtPath);
        FileInfo[] sceneFileList = scenesDir.GetFiles("*.unity");
        foreach (FileInfo sceneFile in sceneFileList) {
            if (sceneFile.ToString().Contains("204")) {
                //wrong scene layout
                continue;
            }
            Debug.Log("GenerateSceneAt:" + sceneFile.ToString());

            int prefabRelativepathStart = sceneFile.ToString().IndexOf(scenesAtPath);
            string prefabPath = sceneFile.ToString().Substring(prefabRelativepathStart);
            SceneAsset oneScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(prefabPath);
            m_SceneAssets.Add(oneScene);
        }

        GenerateSceneByHand();
    }

    public void CopyRoomFromTypes(CRoomType roomType = CRoomType.LivingRoom) {
        //copy kitchens from i2thor to our env
        string scenesAtPath = "Assets/Scenes/";
        DirectoryInfo scenesDir = new DirectoryInfo(scenesAtPath);
        FileInfo[] sceneFileList = scenesDir.GetFiles("*.unity");
        foreach (FileInfo sceneFile in sceneFileList) {
            Debug.Log("Copy:" + sceneFile.ToString());

            int prefabRelativepathStart = sceneFile.ToString().IndexOf(scenesAtPath);
            string prefabPath = sceneFile.ToString().Substring(prefabRelativepathStart);

            if (prefabPath.Contains("_physics")) {
                Debug.Log("prefabPath: " + prefabPath);

                int digitStart = prefabPath.IndexOf("FloorPlan");
                int digitEnd = prefabPath.IndexOf("_physics");

                int digitString = int.Parse(prefabPath.Substring(digitStart + 9, digitEnd - digitStart - 9));
                Debug.Log("digitString: " + digitString);

                if (digitString < 31 && roomType == CRoomType.Kitchen) {

                    SceneAsset oneScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(prefabPath);
                    m_SceneAssets.Add(oneScene);
                    m_SceneNames.Add("NKitchen" + digitString.ToString());
                }


                if (digitString < 431 && digitString > 400 && roomType == CRoomType.Bathroom) {

                    SceneAsset oneScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(prefabPath);
                    m_SceneAssets.Add(oneScene);
                    m_SceneNames.Add("NBathroom" + digitString.ToString());
                }

                if (digitString < 231 && digitString > 200 && roomType == CRoomType.LivingRoom) {

                    SceneAsset oneScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(prefabPath);
                    m_SceneAssets.Add(oneScene);
                    m_SceneNames.Add("Livingroom" + digitString.ToString());
                }


            }

        }

    }

    public void CopyStructure2NewScene() {
        Assert.IsTrue(m_SceneAssets.Count == m_SceneNames.Count);

        IEnumerator SequentialCopyScenes() {
            for (int i = 0; i < m_SceneAssets.Count; ++i) {
                SceneAsset fromScene = m_SceneAssets[i];
                string newSceneName = m_SceneNames[i];

                string scenePath = AssetDatabase.GetAssetPath(fromScene);
                Debug.Log("Copy Scene from scene path: " + scenePath + " \n Scene name: " + newSceneName.ToString());

                EditorSceneManager.OpenScene(scenePath);

                //new scene
                string saveSceneFolder = "Assets/Custom/";
                string saveScenePath = saveSceneFolder + newSceneName + ".unity";

                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), saveScenePath, true);
                EditorSceneManager.OpenScene(saveScenePath);

                //EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Paste"));

                StructureObject[] structObjs = GameObject.FindObjectsOfType<StructureObject>();
                foreach (StructureObject structObj in structObjs) {
                    if (structObj.WhatIsMyStructureObjectTag == StructureObjectTag.Ceiling) {
                        structObj.gameObject.SetActive(false);
                    }
                }


                GameObject structureAttr = new GameObject();
                structureAttr.name = "StructureAttr";

                GameObject structure = GameObject.Find("Structure");
                structureAttr.transform.parent = structure.transform;

                //Copy floor/window/curtain
                //Set floor
                GameObject obbjects = GameObject.Find("Objects");
                List<GameObject> structureInObjects = new List<GameObject>();
                if (obbjects != null) {
                    if (CSceneBuilderTool.samplingRoomType == CRoomType.Bathroom) {
                        foreach (Transform child in obbjects.transform) {
                            string childName = child.gameObject.name;
                            if (childName.Contains("Floor_") || childName.Contains("Window_") || childName.Contains("Curtains_") ||
                                childName.Contains("Bathtub") || childName.Contains("ShowerCurtain_") || childName.Contains("ShowerHead_") ||
                                childName.Contains("ShowerGlass_") || childName.Contains("ShowerDoor_") ||
                                childName.Contains("CounterTop_") || childName.Contains("Sink_") || childName.Contains("Cabinet_") ||
                                childName.Contains("Drawer_") || childName.Contains("Mirror") || childName.Contains("CabinetMesh") ||
                                childName.Contains("Faucet_") || childName.Contains("BathroomSink") || childName.Contains("CabinetBody") ||
                                childName.Contains("FP424:polySurface389") //special cases
                                ) {
                                structureInObjects.Add(child.gameObject);
                            }
                        }
                    }

                    if (CSceneBuilderTool.samplingRoomType == CRoomType.LivingRoom) {
                        foreach (Transform child in obbjects.transform) {
                            string childName = child.gameObject.name;
                            if (childName.Contains("Floor_") || childName.Contains("Window_") || childName.Contains("Curtains_")) {
                                structureInObjects.Add(child.gameObject);
                            }
                        }
                    }

                    if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen) {
                        foreach (Transform child in obbjects.transform) {
                            string childName = child.gameObject.name;
                            if (childName.Contains("Burner") ||
                                childName.Contains("CounterTop_") || childName.Contains("Sink_") || childName.Contains("Cabinet_") ||
                                childName.Contains("Drawer_") || childName.Contains("Burner_") || childName.Contains("StoveKnob_") ||
                                childName.Contains("Faucet_") || childName.Contains("OvenTop")
                                ) {
                                structureInObjects.Add(child.gameObject);
                            }
                        }
                    }

                }

                foreach (GameObject go in structureInObjects) {
                    go.transform.parent = structureAttr.transform;
                }

                //Delete furniture and objects
                DestroyImmediate(obbjects);

                //Filter out unneccessary structure
                GameObject roomStruct = GameObject.Find("Structure");
                List<GameObject> objsUnnecessary = new List<GameObject>();
                if (roomStruct != null) {

                    if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen) {
                        foreach (Transform child in roomStruct.transform) {
                            string childName = child.gameObject.name;
                            if (childName.Contains("FloorPlan")) {
                                foreach (Transform childchild in child.transform) {
                                    if (childchild.gameObject.name.Contains("Ladle") || childchild.gameObject.name.Contains("Kettle") ||
                                       childchild.gameObject.name.Contains("Vase") || childchild.gameObject.name.Contains("PlateStack") ||
                                       childchild.gameObject.name.Contains("Spatula") || childchild.gameObject.name.Contains("PaperClutter") ||
                                       childchild.gameObject.name.Contains("Pitcher") || childchild.gameObject.name.Contains("Pot") ||
                                       childchild.gameObject.name.Contains("Bowl") || childchild.gameObject.name.Contains("WineGlasses") ||
                                        childchild.gameObject.name.Contains("Jar") || childchild.gameObject.name.Contains("Decals") ||
                                        childchild.gameObject.name.Contains("TurkeyPan") || childchild.gameObject.name.Contains("WineGlass") ||
                                        childchild.gameObject.name.Contains("Mug") || childchild.gameObject.name.Contains("SaltShaker") ||
                                        childchild.gameObject.name.Contains("Ladel") || childchild.gameObject.name.Contains("Bottle") ||
                                        childchild.gameObject.name.Contains("Bucket") || childchild.gameObject.name.Contains("CuttingBoard") ||
                                        childchild.gameObject.name.Contains("Cup") //|| childchild.gameObject.name.Contains("CuttingBoard")

                                        ) {
                                        //childchild.gameObject.SetActive(false);
                                        objsUnnecessary.Add(childchild.gameObject);
                                    }
                                }
                            }
                            if (childName.Contains("PaperClutter")) {
                                objsUnnecessary.Add(child.gameObject);
                            }
                        }
                    }
                }

                //Delete objs
                foreach (var obj in objsUnnecessary) {
                    DestroyImmediate(obj);
                }

                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

                yield return new EditorWaitForSeconds(2);
            }
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(SequentialCopyScenes());
    }

    public void AddGameObjectToScene() {
        /*Add structure obj to scene*/
        DirectoryInfo sceneDir = new DirectoryInfo("Assets/Custom/Kitchens");
        FileInfo[] sceneList = sceneDir.GetFiles("*.unity");
        foreach (FileInfo sceneFile in sceneList) {
            Debug.Log("AddGameObjectToScene: " + sceneFile.ToString());
            //int prefabRelativepathStart = prefabFile.ToString().IndexOf(dirPath);
            //string prefabPath = prefabFile.ToString().Substring(prefabRelativepathStart);
            ////Debug.Log("LoadFurniturePrefab: " + prefabPath);

            //prefabPathList.Add(prefabPath);
            EditorSceneManager.OpenScene(sceneFile.ToString());
            GameObject go = new GameObject();
            go.name = "Structure";
            GameObject structureAttr = GameObject.Find("StructureAttr");
            structureAttr.transform.parent = go.transform;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }

    /*
     * 
     * This part get all the scene in ai2thor and take screenshots  
     * 
     */
    public int captureWidth = 1920;
    public int captureHeight = 1080;


    // folder to write output (defaults to data path)
    public string folder;

    // private vars for screenshot
    private Rect rect;
    private RenderTexture renderTexture;
    private Texture2D screenShot;
    private int counter = 0; // image #

    // commands
    private bool captureScreenshot = false;
    private bool captureVideo = false;

    public void GetAllScreenShots() {
        /*
         Get all the screenshots from 45 degree top view for ai2thor scene
         */
        DirectoryInfo sceneDir = new DirectoryInfo("Assets/Scenes");
        FileInfo[] sceneList = sceneDir.GetFiles("*.unity");

        foreach (FileInfo sceneFile in sceneList) {
            if (sceneFile.Name.Contains("physics")) {
                if (sceneFile.Name.Contains("FloorPlan50") || sceneFile.Name.Contains("FloorPlan51") ||
                    sceneFile.Name.Contains("FloorPlan52")) {
                    continue;
                }
                EditorSceneManager.OpenScene(sceneFile.ToString());
                TakeScreenShot(sceneFile);
            }
        }

    }
    public void TakeScreenShot(FileInfo sceneFile, float height = 3.8f) {
        string sceneName = sceneFile.Name;
        var sceneNameSplit = sceneName.Split('.');
        Debug.Log("Take screenshot for scene: " + sceneName);
        string saveFileName = "Assets/Custom/Screenshots/" + sceneNameSplit[0] + ".png";

        //remove robot
        GameObject robot = GameObject.Find("FPSController");
        if (robot != null) {
            robot.SetActive(false);
        }

        //get scene bounds
        var rnds = FindObjectsOfType<Renderer>();
        if (rnds.Length == 0)
            return; // nothing to see here, go on

        //find scene bound
        var b = rnds[0].bounds;
        for (int i = 1; i < rnds.Length; i++)
            b.Encapsulate(rnds[i].bounds);

        Vector3 cameraPosition = 0.5f * b.max + 0.5f * b.min + height * Vector3.up;

        //hide ceiling
        GameObject[] gameObjects = FindObjectsOfType<GameObject>();

        for (var i = 0; i < gameObjects.Length; i++) {
            if (gameObjects[i].name.Contains("Ceiling")) {
                gameObjects[i].SetActive(false);
                break;
            }
        }

        //set camera
        GameObject go = new GameObject();
        go.AddComponent<Camera>();
        go.transform.position = cameraPosition;
        go.transform.LookAt(b.center - Vector3.up * 0.5f);

        // create screenshot objects if needed
        if (renderTexture == null) {
            // creates off-screen render texture that can rendered into
            rect = new Rect(0, 0, captureWidth, captureHeight);
            renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
            screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        }

        // get main camera and manually render scene into rt
        Camera camera = go.GetComponent<Camera>(); // NOTE: added because there was no reference to camera in original script; must add this script to Camera
        camera.targetTexture = renderTexture;
        camera.Render();

        // read pixels will read from the currently active render texture so make our offscreen 
        // render texture active and then read the pixels
        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(rect, 0, 0);


        byte[] fileData = screenShot.EncodeToPNG();


        Debug.Log("take screen shot: " + camera.gameObject.name);
        var f = System.IO.File.Create(saveFileName);
        f.Write(fileData, 0, fileData.Length);
        f.Close();

        //reset robot
        if (robot != null) {
            robot.SetActive(true);
        }
    }

    private void AddHelperTools() {
        DirectoryInfo sceneDir = new DirectoryInfo("Assets/Custom/Bedrooms");
        FileInfo[] sceneList = sceneDir.GetFiles("*.unity");
        foreach (FileInfo sceneFile in sceneList) {
            Debug.Log("AddGameObjectToScene: " + sceneFile.ToString());
            //int prefabRelativepathStart = prefabFile.ToString().IndexOf(dirPath);
            //string prefabPath = prefabFile.ToString().Substring(prefabRelativepathStart);
            ////Debug.Log("LoadFurniturePrefab: " + prefabPath);

            //prefabPathList.Add(prefabPath);
            EditorSceneManager.OpenScene(sceneFile.ToString());

            string sceneIndexStr = sceneFile.ToString().Substring(sceneFile.ToString().Length - 3);

            Debug.Log("sceneIndexStr: " + sceneIndexStr);

            GameObject dalibao = GameObject.Find("Dalibao");
            if (dalibao == null) {
                dalibao = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/Dalibao.prefab", typeof(GameObject)));
                dalibao.name = "HelperTool";
                CObjectPool objectPool = GameObject.Find("ObjectPlacer").GetComponent<CObjectPool>();
                objectPool.taskPath = "Assets/Custom/Json/Floorplans/FloorPlan" + sceneIndexStr;

                //diable ceiling
                StructureObject[] structObjs = GameObject.FindObjectsOfType<StructureObject>();
                foreach (StructureObject structObj in structObjs) {
                    if (structObj.WhatIsMyStructureObjectTag == StructureObjectTag.Ceiling) {
                        structObj.gameObject.SetActive(false);
                    }
                }
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }

    private void LoopOverBuildSceneToGetObjPos() {
        IEnumerator IELoopOverBuildSceneToGetObjPos() {
            DirectoryInfo sceneDir = new DirectoryInfo("Assets/Custom/BuildTrainingKitchens");
            CSceneBuilderTool.samplingRoomType = CRoomType.Kitchen;
            List<string> vaidationSceneNames = new List<string>() { "9", "10", "29", "215", "219", "226", "308",
                        "315", "325", "404", "424", "425"};

            FileInfo[] sceneList = sceneDir.GetFiles("*.unity");
            foreach (FileInfo sceneFile in sceneList) {
                Debug.Log("LoopOverBuildSceneToGetObjPos: " + sceneFile.ToString());
                EditorSceneManager.OpenScene(sceneFile.ToString());

                string currentSceneName = EditorSceneManager.GetActiveScene().name; // an integer

                if (vaidationSceneNames.Contains(currentSceneName))
                        continue;

                //????
                CSceneBuilderTool.samplingRoomType = CRoomType.Kitchen;

                GameObject dalibao = GameObject.Find("Dalibao");
                GameObject helperTool = GameObject.Find("HelperTool");
                if (dalibao == null && helperTool == null) {
                    dalibao = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/Dalibao.prefab", typeof(GameObject)));
                    dalibao.name = "HelperTool";
                }

                CObjectPool objectPool = GameObject.Find("ObjectPlacer").GetComponent<CObjectPool>();
                objectPool.taskPath = "Assets/Custom/Json/Floorplans/FloorPlan" + currentSceneName;

                objectPool.randomSeed = int.Parse(currentSceneName);
                CObjectPlacerTool objectPlacerTool = objectPool.gameObject.GetComponent<CObjectPlacerTool>();
                objectPlacerTool.LoadObjectPrefab();

                objectPool.beInTask = true;
                objectPool.LoopOverJsonFolderToGetObjPos();
                while (objectPool.beInTask) { 
                    yield return new EditorWaitForSeconds(1);
                }
                //don't save 
                //EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
            yield return null;
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(IELoopOverBuildSceneToGetObjPos());
    }
}
#endif
