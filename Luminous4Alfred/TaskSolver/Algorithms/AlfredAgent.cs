using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using Algorithm;

using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.EditorCoroutines.Editor;
#endif


public class AlfredAgent : MonoBehaviour {
    //Scene property
    public AgentManager agentManager;
    public PhysicsSceneManager physicsSceneManager;
    public PhysicsRemoteFPSAgentController PhysicsController;

    //Algorithmss
    public static bool hasGraph = false;
    public static Graph navigationGraph;
    public List<LowLevelActionType> actionList = new List<LowLevelActionType>();
    public static bool agentForceAction = true;

    //rendering
    public int captureWidth = 300;
    public int captureHeight = 300;
    public int viewAngle = 0;

    //json
    public static string taskJsonFolder = "Assets/Custom/Json/Floorplans/FloorPlan301/tasks/";
    public static string taskJsonPath = "/Users/zhayizho/Desktop/ai2thor-3.3.1/unity/Assets/Custom/Json/Floorplans/FloorPlan301/tasks/pick_and_place_with_movable_recep-Pen-Bowl-Desk-301]trial_T20190908_160414_444492.json";
    public static string taskSuffix = "";
    public TrajJson trajJson = new TrajJson();
    public bool taskSuccess = true;
    public static bool taskFinish = true;
    public string objInHand;

    // private vars for screenshot
    private Rect rect;
    private RenderTexture renderTexture;
    private Texture2D screenShot;
    private int counter = 0; // image #

    //save
    public static string saveRoot = "/Users/zhayizho/Desktop/unitybuild/newrules/train/";

    //object placer
    public CObjectPool objectPool;

    //set init instrucitons
    private void ResetAgent() {
        objInHand = "";
        taskSuccess = true;
        viewAngle = 0;
        agentManager = GameObject.Find("PhysicsSceneManager").GetComponent<AgentManager>();

        //spawner = physicsSceneManager.gameObject.GetComponent<InstantiatePrefabTest>();
        PhysicsController = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();
        physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();

        // character controller parameters
        PhysicsController.m_CharacterController = PhysicsController.gameObject.GetComponent<CharacterController>();
        PhysicsController.Start();
        //PhysicsController.SetAgentMode(mode: "default");
        PhysicsController.m_CharacterController.center = new Vector3(0, 0, 0);
        PhysicsController.m_CharacterController.radius = 0.2f;
        PhysicsController.m_CharacterController.height = 1.8f;

        CapsuleCollider cc = PhysicsController.gameObject.GetComponent<CapsuleCollider>();
        cc.center = PhysicsController.m_CharacterController.center;
        cc.radius = PhysicsController.m_CharacterController.radius;
        cc.height = PhysicsController.m_CharacterController.height;

        //PhysicsController.m_Camera.GetComponent<PostProcessVolume>().enabled = false;
        //PhysicsController.m_Camera.GetComponent<PostProcessLayer>().enabled = false;

        //image sysn
        PhysicsController.imageSynthesis = null;

        // camera position
        PhysicsController.m_Camera.transform.localPosition = new Vector3(0, 0.675f, 0);

        // camera FOV
        PhysicsController.m_Camera.fieldOfView = 60f;


        //Generate object name
        physicsSceneManager.SetupScene();
        //build navigation graph
        Vector3[] reachablePositions = PhysicsController.getReachablePositions();

        //initalize graph
        if (!hasGraph) {
            navigationGraph = new Graph();
            //Debug.Log("reachablePositions: " + reachablePositions.Length);
            //#if UNITY_EDITOR
            //            GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));


            //            foreach (var point in reachablePositions) {
            //                var go = Instantiate(_road_sign, point, Quaternion.identity);
            //                go.transform.parent = this.gameObject.transform;
            //            }
            //#endif
            //build graph
            for (int i = 0; i < reachablePositions.Length; ++i) {
                //Generate node
                Node node = new Node(reachablePositions[i]);
                navigationGraph.nodes.Add(node);
                for (int j = 0; j < i; ++j) {
                    if (Vector3.Distance(reachablePositions[i], reachablePositions[j]) < 0.26f) {
                        navigationGraph.nodes[j].connections.Add(node);
                        node.connections.Add(navigationGraph.nodes[j]);
                    }
                }
            }
            hasGraph = true;
        }

        //set random character location
        int randomPosIdx = (int)UnityEngine.Random.Range(0f, reachablePositions.Length);
        PhysicsController.gameObject.transform.position = reachablePositions[randomPosIdx];

        int randomRotIdx = (int)UnityEngine.Random.Range(0f, 4f);
        PhysicsController.gameObject.transform.rotation = Quaternion.Euler(0, randomRotIdx * 90, 0);
    }

    //parse all taskJsons for current scene
    public void ParseAllTaskJsonsForCurrentScene() {
        //initailize shortest path memory
        //Graph.point2path = new Dictionary<string, Algorithm.Path>();
        hasGraph = false;

        List<string> taskJsons = LoadTaskJsonPaths(taskJsonFolder);
        int debug_idx = 0;
        foreach (string taskJson in taskJsons) {
            debug_idx++;
            //if (debug_idx < 33) {
            //    continue;
            //}
            //if (debug_idx > 40) {
            //    break;
            //}
            Debug.Log("task: " + taskJson);

            if (taskJson.Contains("Shelf") || taskJson.Contains("Cabinet") || taskJson.Contains("Drawer") || taskJson.Contains("Slice")) {
                continue;
            }

            if (AlfredTaskHelperTool.alfredTaskType != AlfredTaskType.all) {
                if (!taskJson.Contains(AlfredTaskHelperTool.alfredTaskType.ToString().ToLower())) {
                    continue;
                }
            }


            AlfredAgent.taskJsonPath = taskJson;

            var currentScene = EditorSceneManager.GetActiveScene();
            string currentScenePath = currentScene.path;

            //reset obj poses
            try {
                ResetObjLocations();


                //reset scene agent
                ResetAgent();

                //reset init conditions
                ResetSceneConditions();

                ParseTaskJson();
            } catch {
                Debug.LogError("Error in scene/task json: " + taskJson);
            }

            //break;

            EditorSceneManager.OpenScene(currentScenePath);


        }
    }
    //reset scene intial conditions
    private void ResetSceneConditions() {
        try {
            if (taskJsonPath.Contains("look_at_obj_in_light")) {
                foreach (var canToggleOnOff in GameObject.FindObjectsOfType<CanToggleOnOff>()) {
                    //Debug.Log("canToggleOnOff: " + canToggleOnOff.LightSources.Length);
                    //if (canToggleOnOff.LightSources.Length > 0) {
                    //    foreach (var lightResource in canToggleOnOff.LightSources) {
                    //        lightResource.SetActive(false);
                    //        canToggleOnOff.isOn = false;
                    //    }
                    //}
                    Debug.Log("Can toggle off object name:" + canToggleOnOff.gameObject.name);
                    SimObjPhysics canToggleSop = canToggleOnOff.gameObject.GetComponent<SimObjPhysics>();
                    if (CSceneBuilderTool.samplingRoomType == CRoomType.Bedroom && canToggleSop.ObjType == SimObjType.DeskLamp) {
                        canToggleOnOff.isOn = true; 
                        PhysicsController.ToggleObjectOff(canToggleSop.ObjectID, true);
                    }
                    if (CSceneBuilderTool.samplingRoomType == CRoomType.LivingRoom && canToggleSop.ObjType == SimObjType.FloorLamp) {
                        canToggleOnOff.isOn = true;
                        PhysicsController.ToggleObjectOff(canToggleSop.ObjectID, true);

                        Debug.Log("floorlamp off");
                    }

                }
                //ToggleObjectOff(getIdFromObjectType(SimObjType.DeskLamp));
            } else if (taskJsonPath.Contains("pick_clean_then_place_in_recep")) {
                foreach (var canBeDirty in GameObject.FindObjectsOfType<Dirty>()) {
                    canBeDirty.ToggleCleanOrDirty();
                }
            }
        } catch (Exception e){
            Debug.LogException(e, this);
        }
    }

    //Randomize object locations
    private void ResetObjLocations(bool loadFromFile=false){
        //find tool
        objectPool = GameObject.Find("ObjectPlacer").GetComponent<CObjectPool>();
        //read rules
        CSceneBuilderTool.jsonPath = taskJsonPath;
        Debug.Log("Loading task Json:" + taskJsonPath);
        CSceneBuilderTool.LoadJsonInfo();

        //string currentSceneName = EditorSceneManager.GetActiveScene().name;

        //load prefab
        objectPool.randomSeed = (int)UnityEngine.Random.Range(0f, 100000f);//int.Parse(currentSceneName);
        CObjectPlacerTool objectPlacerTool = objectPool.gameObject.GetComponent<CObjectPlacerTool>();
        objectPlacerTool.LoadObjectPrefab();

        //place object
        objectPool.ClearObjInJson();
        objectPlacerTool.CustomPlaceObjectParallel();
    }

    //scene object record to traj
    public void RecordObjLocationsAndPDDL() {
        //record necessary obj locations
        List<SimObjPhysicsExtended> sopEList = CSceneBuilderTool.jsonRule.sopEList;
        foreach(var sopE in sopEList) {
            string sopEName = sopE.simObjName;
            GameObject go = GameObject.Find(sopEName);
            if(go != null) {
                OObjectPose goPose = new OObjectPose(sopEName);
                trajJson.scene.object_poses.Add(goPose);
            }
        }
        //record pddl
        trajJson.pddl_params.mrecep_target = CSceneBuilderTool.jsonRule.jsonRule["pddl_params"]["mrecep_target"].ToString();
        trajJson.pddl_params.object_sliced = CSceneBuilderTool.jsonRule.jsonRule["pddl_params"]["object_sliced"].ToObject<bool>();
        trajJson.pddl_params.object_target = CSceneBuilderTool.jsonRule.jsonRule["pddl_params"]["object_target"].ToString();
        trajJson.pddl_params.parent_target = CSceneBuilderTool.jsonRule.jsonRule["pddl_params"]["parent_target"].ToString();
        trajJson.pddl_params.toggle_target = CSceneBuilderTool.jsonRule.jsonRule["pddl_params"]["toggle_target"].ToString();

        //record task/scene type
        trajJson.task_desc = CSceneBuilderTool.jsonRule.jsonRule["task_desc"].ToString();
        trajJson.scene.floor_plan = CSceneBuilderTool.samplingRoomType.ToString();
    }

    //parse task json into traj json
    public void ParseTaskJson(bool deleteUnsucceeful = true) {

        //initialize traj json
        actionList.Clear();
        trajJson = new TrajJson();

        //scene object record
        RecordObjLocationsAndPDDL();

        //load json
        JObject jsonRule = JObject.Parse(File.ReadAllText(taskJsonPath));


        //set folder
        string taskDesc = (string)jsonRule["task_desc"];
        string taskFolder = taskDesc.Split('/')[0] + taskSuffix;
        string trialFolder = taskDesc.Split('/')[1];

        if (!System.IO.Directory.Exists(System.IO.Path.Combine(saveRoot, taskFolder))) {
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(saveRoot, taskFolder));
        }

        string trajFolder = System.IO.Path.Combine(saveRoot, taskFolder, trialFolder);
        if (!Directory.Exists(trajFolder)) {
            System.IO.Directory.CreateDirectory(trajFolder);
        }

        string rawImageFolder = System.IO.Path.Combine(trajFolder, "raw_images");
        if (!Directory.Exists(rawImageFolder)) {
            System.IO.Directory.CreateDirectory(rawImageFolder);
        }

        try {
            //resolve initial image
            LookDown();
            LookDown();
            RecordFrame(0, rawImageFolder);
            ImageJson imageJsonInit = new ImageJson();
            imageJsonInit.low_idx = 0;
            imageJsonInit.high_idx = 0;
            imageJsonInit.image_name = 0.ToString("D9") + ".png";
            trajJson.images.Add(imageJsonInit);

            //Get agent init poses
            AgentInitInfo agentMeta = trajJson.scene.init_action;
            agentMeta.rotation = (int)PhysicsController.transform.eulerAngles.y;
            float cameraX = PhysicsController.m_Camera.transform.rotation.eulerAngles.x;
            agentMeta.horizon = (int)(cameraX > 180 ? cameraX - 360 : cameraX);
            agentMeta.x = PhysicsController.transform.position.x;
            agentMeta.y = PhysicsController.transform.position.y;
            agentMeta.z = PhysicsController.transform.position.z;


            //initial look down event
            LookDown();
            RecordFrame(1, rawImageFolder);
            ImageJson imageJsonLookDown = new ImageJson();
            imageJsonLookDown.low_idx = 1;
            imageJsonLookDown.high_idx = 0;
            imageJsonLookDown.image_name = 1.ToString("D9") + ".png";
            trajJson.images.Add(imageJsonLookDown);


            LowAction low_action_look_down = new LowAction();
            low_action_look_down.api_action.action = LowLevelActionType.LookDown.ToString();
            low_action_look_down.discrete_action.action = "LookDown_15";
            low_action_look_down.high_idx = 0;

            trajJson.plan.low_actions.Add(low_action_look_down);

            int high_idx = 0;
            int low_idx = 2;
            foreach (var s in jsonRule["script"]) {

                //resolve high level plan
                HighPddl high_pddl = new HighPddl();
                string actionH = (string)s["action"];

                //modify toggle object high description
                string MactionH = actionH == "ToggleObjectOn" ? "ToggleObject" : actionH;

                high_pddl.discrete_action.action = MactionH;
                high_pddl.planner_action.action = MactionH;
                high_pddl.high_idx = high_idx;

                //this arg for navigation
                string args1 = (string)s["name"];
                Debug.Log("actionH: " + actionH + "; object: " + args1);
                string objectId = GameObject.Find((string)s["name"]).GetComponent<SimObjPhysics>().objectID;

                //this arg for high pddl language description
                string args2 = (string)jsonRule["high_pddl"][high_idx]["name"];
                args2 = args2.Split('_')[0].ToLower();

                //args1 = args1.Split('_')[0].ToLower();
                high_pddl.discrete_action.args.Add(args2);



                trajJson.plan.high_pddl.Add(high_pddl);

                actionList.Clear();
                List<string> actioInteractionIds = new List<string>();

                //resolve low level plan
                if (actionH == "GotoLocation") {
                    Vector3 targetLoc = GameObject.Find((string)s["name"]).transform.position;
                    if (objectId.Contains("Fridge")) {
                        //Debug.Log("?????????????Fridge" + targetLoc);
                        targetLoc = ModifyLastLocation(targetLoc);
                        //Debug.Log("?????????????Fridge after" + targetLoc);
                    }

                    bool final_look_up = false;
                    if (args2.Contains("floorlamp")) {
                        final_look_up = true;
                    }

                    GoToLocation(targetLoc, low_idx, rawImageFolder, final_look_up);
                } else if (actionH == "PickupObject") {

                    PickUpObject(objectId);
                    RecordFrame(low_idx, rawImageFolder);
                    objInHand = args2;
                    actionList.Add(LowLevelActionType.PickupObject);
                    actioInteractionIds.Add(objectId);

                } else if (actionH == "PutObject") {
                    if (AlfredDefinitions.NeedOpenObjs.Contains(args2)) {
                        OpenObject(objectId);
                        RecordFrame(low_idx, rawImageFolder);
                        actionList.Add(LowLevelActionType.OpenObject);
                        actioInteractionIds.Add(objectId);

                        PutObject(objectId);
                        RecordFrame(low_idx + 1, rawImageFolder);
                        high_pddl.discrete_action.args.Insert(0, objInHand);
                        actionList.Add(LowLevelActionType.PutObject);
                        actioInteractionIds.Add(objectId);

                    } else {
                        PutObject(objectId);
                        RecordFrame(low_idx, rawImageFolder);
                        high_pddl.discrete_action.args.Insert(0, objInHand);
                        actionList.Add(LowLevelActionType.PutObject);
                        actioInteractionIds.Add(objectId);
                    }

                } else if (actionH == "ToggleObjectOff") {
                    ToggleObjectOff(objectId);
                    RecordFrame(low_idx, rawImageFolder);
                    actioInteractionIds.Add(objectId);
                    actionList.Add(LowLevelActionType.ToggleObjectOff);
                } else if (actionH == "ToggleObjectOn") {
                    ToggleObjectOn(objectId);
                    RecordFrame(low_idx, rawImageFolder);
                    actioInteractionIds.Add(objectId);
                    actionList.Add(LowLevelActionType.ToggleObjectOn);
                } else if (actionH == "CleanObject") {
                    string sinkId = GameObject.Find("SinkBasin_1").GetComponent<SimObjPhysics>().objectID;
                    PutObject(sinkId);
                    RecordFrame(low_idx, rawImageFolder);
                    actionList.Add(LowLevelActionType.PutObject);
                    actioInteractionIds.Add(sinkId);

                    string faucetId = GameObject.Find("Faucet_1").GetComponent<SimObjPhysics>().objectID;
                    ToggleObjectOn(faucetId);
                    RecordFrame(low_idx + 1, rawImageFolder);
                    actionList.Add(LowLevelActionType.ToggleObjectOn);
                    actioInteractionIds.Add(faucetId);

                    //set clean
                    SimObjPhysics objSop = getSimObjectFromId(objectId);
                    Dirty objDirty = objSop.gameObject.GetComponent<Dirty>();
                    if (objDirty != null) {
                        objDirty.ToggleCleanOrDirty();
                    }

                    ToggleObjectOff(faucetId);
                    RecordFrame(low_idx + 2, rawImageFolder);
                    actionList.Add(LowLevelActionType.ToggleObjectOff);
                    actioInteractionIds.Add(faucetId);

                    PickUpObject(objectId);
                    RecordFrame(low_idx + 3, rawImageFolder);
                    objInHand = args2;
                    actionList.Add(LowLevelActionType.PickupObject);
                    actioInteractionIds.Add(objectId);
                } else if (actionH == "CoolObject") {
                    string fridgeId = GameObject.Find("Fridge_1").GetComponent<SimObjPhysics>().objectID;
                    OpenObject(fridgeId);
                    RecordFrame(low_idx, rawImageFolder);
                    actionList.Add(LowLevelActionType.OpenObject);
                    actioInteractionIds.Add(fridgeId);

                    PutObject(fridgeId);
                    RecordFrame(low_idx + 1, rawImageFolder);
                    actionList.Add(LowLevelActionType.PutObject);
                    actioInteractionIds.Add(fridgeId);

                    CloseObject(fridgeId);
                    RecordFrame(low_idx + 2, rawImageFolder);
                    actionList.Add(LowLevelActionType.CloseObject);
                    actioInteractionIds.Add(fridgeId);

                    OpenObject(fridgeId);
                    RecordFrame(low_idx + 3, rawImageFolder);
                    actionList.Add(LowLevelActionType.OpenObject);
                    actioInteractionIds.Add(fridgeId);

                    PickUpObject(objectId);
                    RecordFrame(low_idx + 4, rawImageFolder);
                    objInHand = args2;
                    actionList.Add(LowLevelActionType.PickupObject);
                    actioInteractionIds.Add(objectId);

                    CloseObject(fridgeId);
                    RecordFrame(low_idx + 5, rawImageFolder);
                    actionList.Add(LowLevelActionType.CloseObject);
                    actioInteractionIds.Add(fridgeId);

                } else if (actionH == "HeatObject") {
                    string microwaveId = GameObject.Find("Microwave_1").GetComponent<SimObjPhysics>().objectID;
                    OpenObject(microwaveId);
                    RecordFrame(low_idx, rawImageFolder);
                    actionList.Add(LowLevelActionType.OpenObject);
                    actioInteractionIds.Add(microwaveId);

                    PutObject(microwaveId);
                    RecordFrame(low_idx + 1, rawImageFolder);
                    actionList.Add(LowLevelActionType.PutObject);
                    actioInteractionIds.Add(microwaveId);

                    CloseObject(microwaveId);
                    RecordFrame(low_idx + 2, rawImageFolder);
                    actionList.Add(LowLevelActionType.CloseObject);
                    actioInteractionIds.Add(microwaveId);

                    ToggleObjectOn(microwaveId);
                    RecordFrame(low_idx + 3, rawImageFolder);
                    actionList.Add(LowLevelActionType.ToggleObjectOn);
                    actioInteractionIds.Add(microwaveId);

                    ToggleObjectOff(microwaveId);
                    RecordFrame(low_idx + 4, rawImageFolder);
                    actionList.Add(LowLevelActionType.ToggleObjectOff);
                    actioInteractionIds.Add(microwaveId);

                    OpenObject(microwaveId);
                    RecordFrame(low_idx + 5, rawImageFolder);
                    actionList.Add(LowLevelActionType.OpenObject);
                    actioInteractionIds.Add(microwaveId);

                    PickUpObject(objectId);
                    RecordFrame(low_idx + 6, rawImageFolder);
                    objInHand = args2;
                    actionList.Add(LowLevelActionType.PickupObject);
                    actioInteractionIds.Add(objectId);

                    CloseObject(microwaveId);
                    RecordFrame(low_idx + 7, rawImageFolder);
                    actionList.Add(LowLevelActionType.CloseObject);
                    actioInteractionIds.Add(microwaveId);

                } else if (actionH == "SliceObject") {
                    //sliceobj
                    string changeToObjId = getSimObjectFromId(objectId).gameObject.GetComponent<SliceObject>().ObjectToChangeTo.name;

                    SliceObject(objectId);
                    RecordFrame(low_idx, rawImageFolder);
                    actionList.Add(LowLevelActionType.SliceObject);
                    actioInteractionIds.Add(objectId);

                    //after slice rename

                } else {
                    throw new ArgumentException("No such action: " + actionH);
                }

                for (int j = 0; j < actionList.Count; ++j) {
                    LowLevelActionType actionL = actionList[j];
                    LowAction low_action = new LowAction();
                    low_action.api_action.action = actionL.ToString();

                    if (AlfredDefinitions.ApiActionName2DiscreteActionName.ContainsKey(actionL.ToString())) {
                        low_action.discrete_action.action = AlfredDefinitions.ApiActionName2DiscreteActionName[actionL.ToString()];
                    } else {
                        low_action.discrete_action.action = low_action.api_action.action;
                    }

                    if (AlfredDefinitions.InteractiveActionType.Contains(actionL)) {
                        low_action.api_action.objectId = actioInteractionIds[j];
                    }

                    low_action.high_idx = high_idx;

                    trajJson.plan.low_actions.Add(low_action);

                    //resolve image
                    ImageJson imageJson = new ImageJson();
                    imageJson.low_idx = low_idx;
                    imageJson.high_idx = j == actionList.Count - 1 ? high_idx + 1 : high_idx;
                    imageJson.image_name = low_idx.ToString("D9") + ".png";

                    trajJson.images.Add(imageJson);

                    low_idx++;

                    if (taskSuccess == false) {
                        break;
                    }
                }

                //update high idx
                high_idx++;
            }

            //save json
            using (StreamWriter saveTrajJsonFile = File.CreateText(trajFolder + "/traj_data.json")) {
                //remove that last image in traj_json: this is because of the ET format
                trajJson.images.RemoveAt(trajJson.images.Count - 1);

                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(saveTrajJsonFile, trajJson);
            }
        } catch (Exception e) {
            Debug.LogException(e, this);
            taskSuccess = false;
        }
        //task file: delete data
        if (taskSuccess == false && deleteUnsucceeful) {
            ClearFolder(trajFolder);
            Directory.Delete(trajFolder);
        }

        taskFinish = true;
    }

    public void OneJsonForCurrentScene() {
        //reset obj poses
        ResetObjLocations();

        //reset scene agent
        ResetAgent();

        //reset init conditions
        ResetSceneConditions();

        ParseTaskJson(deleteUnsucceeful: true);
    }

    public void BuildGraph() {

        ParseAllTaskJsonsForCurrentScene();
    }

    //Find object by Id
    private SimObjPhysics getSimObjectFromId(string objectId) {
        SimObjPhysics[] sops = GameObject.FindObjectsOfType<SimObjPhysics>();
        foreach (var sop in sops) {
            if (sop.ObjectID == objectId) {
                return sop;
            }
        }
        return null;
    }

    //Find object go  by Id
    private GameObject getGoFromId(string objectId) {
        SimObjPhysics[] sops = GameObject.FindObjectsOfType<SimObjPhysics>();
        foreach (var sop in sops) {
            if (sop.ObjectID == objectId) {
                return sop.gameObject;
            }
        }
        return null;
    }


    //Find objectId by objectType
    private string getIdFromObjectType(SimObjType sopType) {
        SimObjPhysics[] sops = GameObject.FindObjectsOfType<SimObjPhysics>();
        foreach (var sop in sops) {
            if (sop.ObjType == sopType) {
                return sop.ObjectID;
            }
        }
        return null;
    }

    //Render frame
    public void RecordFrame(int frame = 0, string saveImageFolder = "Assets/Custom/Screenshots/") {
        //sceenshot
        //PhysicsController.m_Camera.Render();

        string sceneName = frame.ToString("D9");
        string saveFileName = saveImageFolder + "/" + sceneName + ".png";

        // create screenshot objects if needed
        if (renderTexture == null) {
            // creates off-screen render texture that can rendered into
            rect = new Rect(0, 0, captureWidth, captureHeight);
            renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
            screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        }

        Camera camera = PhysicsController.m_Camera;
        //camera.fieldOfView = 90f;
        camera.targetTexture = renderTexture;
        camera.Render();

        // read pixels will read from the currently active render texture so make our offscreen 
        // render texture active and then read the pixels
        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(rect, 0, 0);


        byte[] fileData = screenShot.EncodeToPNG();


        //Debug.Log("take screen shot: " + camera.gameObject.name);
        var f = System.IO.File.Create(saveFileName);
        f.Write(fileData, 0, fileData.Length);
        f.Close();
    }

    //Distable or enable collision
    public void SetSopCollision(SimObjPhysics sop, bool beEnable = false) {
        foreach (Collider col in sop.MyColliders) {
            col.enabled = beEnable;
        }

        BoxCollider box = sop.BoundingBox.GetComponent<BoxCollider>();
        box.enabled = beEnable;

        foreach (Collider c in sop.gameObject.GetComponents<Collider>()) {
            c.enabled = beEnable;
        }
    }

    //Get Agent rotation int
    public int GetControllerRotation() {
        float cameraY = PhysicsController.m_Camera.transform.rotation.eulerAngles.y;
        cameraY = cameraY > 180 ? cameraY - 360 : cameraY;

        int rotationPrefix = (int)cameraY / 90;
        //Debug.Log("cameraY: " + cameraY + " " + rotationPrefix);

        return rotationPrefix;
    }

    //modify last position to let the fridge door openable
    public Vector3 ModifyLastLocation(Vector3 targetLocation) {
        Vector3 endLoaction = navigationGraph.GetNearestNode(targetLocation).node_position;
        float diffX = endLoaction.x - targetLocation.x;
        float diffZ = endLoaction.z - targetLocation.z;

        if (Mathf.Abs(diffX) > Mathf.Abs(diffZ)) {
            if (diffX > 0)
                return targetLocation + new Vector3(0.5f, 0f, 0f);
            else
                return targetLocation + new Vector3(-0.5f, 0f, 0f);
        } else {
            if (diffZ > 0)
                return targetLocation + new Vector3(0, 0f, 0.5f);
            else
                return targetLocation + new Vector3(0, 0f, -0.5f);
        }
    }

    //high-level action
    public void GoToLocation(Vector3 targetLocation, int frameStart = 0, string saveImageFolder = "Assets/Custom/Screenshots/", bool finalLookUp = false) {
        //actionList.Clear();

        while (viewAngle < 0) {
            viewAngle++;
            actionList.Add(LowLevelActionType.LookUp);
        }

        //get shortest path
        Vector3 currentLocation = PhysicsController.gameObject.transform.position;
        Node startNode = navigationGraph.GetNearestNode(currentLocation);
        Node endNode = navigationGraph.GetNearestNode(targetLocation);

        navigationGraph.GetShortestPath(startNode, endNode);
        Algorithm.Path path = navigationGraph.m_Paths[0];
        //        foreach (Node node in path.nodes) {
        ////#if UNITY_EDITOR
        ////            GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
        ////            var go = Instantiate(_road_sign, node.node_position, Quaternion.identity);
        ////#endif
        //        }

        //Follow path
        int agentRotation = GetControllerRotation();
        for (int i = 0; i < path.nodes.Count - 1; ++i) {
            Vector3 currentPos = path.nodes[i].node_position;
            Vector3 nextPos = path.nodes[i + 1].node_position;
            {
                int targetRotation = path.GetPathDirectionAtIndex(i);
                int rotationIdxV = ((targetRotation - agentRotation) % 4 + 4) % 4;
                //Debug.Log(i + "agentrotation targetrot: " + agentRotation + " " + targetRotation + " " + rotationIdxV);
                if (rotationIdxV % 4 == 3) {
                    actionList.Add(LowLevelActionType.RotateLeft);
                } else if (rotationIdxV == 2) {
                    if (UnityEngine.Random.Range(0f, 1f) < 0.5f) {
                        actionList.Add(LowLevelActionType.RotateRight);
                        actionList.Add(LowLevelActionType.RotateRight);
                    } else {
                        actionList.Add(LowLevelActionType.RotateLeft);
                        actionList.Add(LowLevelActionType.RotateLeft);
                    }
                } else if (rotationIdxV % 4 == 1) {
                    actionList.Add(LowLevelActionType.RotateRight);
                }

                agentRotation = targetRotation;
                actionList.Add(LowLevelActionType.MoveAhead);
            }
        }

        //look at target
        Vector3 agentLocation = path.nodes[path.nodes.Count - 1].node_position;
        float diffX = targetLocation.x - agentLocation.x;
        float diffZ = targetLocation.z - agentLocation.z;

        int targetRotationF = 0;
        if (Mathf.Abs(diffX) > Mathf.Abs(diffZ)) {
            if (diffX > 0)
                targetRotationF = 1;
            else
                targetRotationF = -1;
        } else {
            if (diffZ > 0)
                targetRotationF = 0;
            else
                targetRotationF = 2;
        }

        int rotationIdx = ((targetRotationF - agentRotation) % 4 + 4) % 4;
        if (rotationIdx % 4 == 3) {
            actionList.Add(LowLevelActionType.RotateLeft);
        } else if (rotationIdx == 2) {
            if (UnityEngine.Random.Range(0f, 1f) < 0.5f) {
                actionList.Add(LowLevelActionType.RotateRight);
                actionList.Add(LowLevelActionType.RotateRight);
            } else {
                actionList.Add(LowLevelActionType.RotateLeft);
                actionList.Add(LowLevelActionType.RotateLeft);
            }
        } else if (rotationIdx % 4 == 1) {
            actionList.Add(LowLevelActionType.RotateRight);
        }

        agentRotation = targetRotationF;

        //TODO: horizon adjust
        //if (targetLocation.y < 0.9f) {
        //    actionList.Add(LowLevelActionType.LookDown);
        //    viewAngle--;
        //}
        if (targetLocation.y < 0.6f && !finalLookUp) {
            actionList.Add(LowLevelActionType.LookDown);
            viewAngle--;
        }

        if (finalLookUp) {
            actionList.Add(LowLevelActionType.LookUp);
            actionList.Add(LowLevelActionType.LookUp);
            viewAngle += 2 ;
        }


        // execute action
        foreach (LowLevelActionType actionL in actionList) {
            //Debug.Log("---------------LowLevelAction----------------" + actionL.ToString());
            switch (actionL) {
                case LowLevelActionType.MoveAhead:
                    MoveAhead();
                    break;
                case LowLevelActionType.RotateLeft:
                    RotateLeft();
                    break;
                case LowLevelActionType.RotateRight:
                    RotateRight();
                    break;
                case LowLevelActionType.LookUp:
                    LookUp();
                    break;
                case LowLevelActionType.LookDown:
                    LookDown();
                    break;
                default:
                    Debug.LogError("Unknow navigation action");
                    break;
            }

            if (frameStart > 0) {
                //render frame
                RecordFrame(frameStart++, saveImageFolder);
            }
        }
    }

    //Agent Action Part
    private void MoveAhead() {
        Dictionary<string, object> action = new Dictionary<string, object>();
        action["action"] = "MoveAhead";
        action["moveMagnitude"] = 0.25f;
        action["forceAction"] = true;
        PhysicsController.ProcessControlCommand(action);

        if (PhysicsController.lastActionSuccess == false) {
            taskSuccess = false;
        }
    }

    private void RotateLeft() {
        Dictionary<string, object> action = new Dictionary<string, object>();
        action["action"] = "RotateLeft";
        action["forceAction"] = true;
        PhysicsController.ProcessControlCommand(action);

        if (PhysicsController.lastActionSuccess == false) {
            taskSuccess = false;
        }
    }

    private void RotateRight() {
        Dictionary<string, object> action = new Dictionary<string, object>();
        action["action"] = "RotateRight";
        action["forceAction"] = true;
        PhysicsController.ProcessControlCommand(action);

        if (PhysicsController.lastActionSuccess == false) {
            taskSuccess = false;
        }
    }

    private void LookUp() {
        PhysicsController.m_Camera.transform.Rotate(-15, 0, 0);
    }

    private void LookDown() {
        PhysicsController.m_Camera.transform.Rotate(15, 0, 0);
    }

    private void OpenObject(string objectId) {
        CanOpen_Object coo = getSimObjectFromId(objectId).gameObject.GetComponent<CanOpen_Object>();
        foreach (var movingPart in coo.MovingParts) {
            movingPart.SetActive(false);
        }
        coo.isOpen = true;

        foreach (Collider col in coo.gameObject.GetComponentsInChildren<Collider>()) {
            col.enabled = false;
        }
    }

    private void CloseObject(string objectId) {
        CanOpen_Object coo = getSimObjectFromId(objectId).gameObject.GetComponent<CanOpen_Object>();
        foreach (var movingPart in coo.MovingParts) {
            movingPart.SetActive(true);
        }
        coo.isOpen = false;
    }

    private void ToggleObjectOn(string objectId) {
        //PhysicsController.agentState = AgentState.Processing;
        PhysicsController.ToggleObjectOn(objectId: objectId, forceAction: agentForceAction);
    }

    private void ToggleObjectOff(string objectId) {
        //PhysicsController.agentState = AgentState.Processing;
        PhysicsController.ToggleObjectOff(objectId: objectId, forceAction: agentForceAction);
    }

    private void PickUpObject(string objectId) {
        //PhysicsController.agentState = AgentState.Processing;
        PhysicsController.PickupObject(objectId: objectId, forceAction: agentForceAction);

        if (PhysicsController.lastActionSuccess == false) {
            taskSuccess = false;
        }
    }

    private void PutObject(string objectId) {
        PhysicsController.agentState = AgentState.Processing;
        PhysicsController.PutObject(objectId: objectId, forceAction: agentForceAction);

        if (PhysicsController.lastActionSuccess == false) {
            taskSuccess = false;
        }
    }

    private void SliceObject(string objectId) {
        SliceObject sli = getSimObjectFromId(objectId).gameObject.GetComponent<SliceObject>();

        GameObject resultObject = Instantiate(sli.ObjectToChangeTo, sli.transform.position, sli.transform.rotation);
        sli.gameObject.SetActive(false);

        int childIndex = 1;
        foreach(Transform childTransform in resultObject.transform) {
            SimObjPhysics childSop = childTransform.gameObject.GetComponent<SimObjPhysics>();
            childSop.gameObject.name = childSop.ObjType.ToString() + "_" + childIndex.ToString();
            childSop.objectID = objectId + "|" + childSop.gameObject.name;

            physicsSceneManager.ObjectIdToSimObjPhysics.Add(childSop.objectID, childSop);
            childIndex++;
        }

        
    }

    //utilities
    public List<string> LoadTaskJsonPaths(string dirr) {
        DirectoryInfo prefabDir = new DirectoryInfo(dirr);
        FileInfo[] taskJsonFileList = prefabDir.GetFiles("*.json");

        List<string> taskJsonList = new List<string>();
        foreach (var file in taskJsonFileList) {
            taskJsonList.Add(file.ToString());
        }

        return taskJsonList;
    }

    //clean folder
    private void ClearFolder(string folderName) {
        DirectoryInfo dir = new DirectoryInfo(folderName);

        foreach (FileInfo fi in dir.GetFiles()) {
            fi.Delete();
        }

        foreach (DirectoryInfo di in dir.GetDirectories()) {
            ClearFolder(di.FullName);
            di.Delete();
        }
    }
}
