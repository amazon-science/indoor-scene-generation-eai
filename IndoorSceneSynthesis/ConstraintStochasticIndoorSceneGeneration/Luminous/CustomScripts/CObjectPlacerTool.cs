using System;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
#endif

public class CObjectPlacerTool : MonoBehaviour {
    //game
    public PhysicsSceneManager physicsSceneManager;
    public InstantiatePrefabTest spawner;

    //preperty
    public CObjectPool objectPool;
    public int randomSeed;

    //placing tool
    public CFurniturePlacerTool furniturePlacerTool;
    public float yoffset = 0.0005f;

    //room target
    public List<Vector2> cornerList;
    public List<Vector2> borderList;



    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void SetSpawner() {
        if (physicsSceneManager == null) {
            physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
            spawner = physicsSceneManager.gameObject.GetComponent<InstantiatePrefabTest>();
            objectPool = this.GetComponent<CObjectPool>();
            randomSeed = objectPool.randomSeed; //(int)UnityEngine.Random.Range(0f, 10000f);//

            furniturePlacerTool = this.gameObject.GetComponent<CFurniturePlacerTool>();
        }
    }

    public void PlaceObject(bool allowFloor = false) {
        PhysicsSceneManager physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
        CObjectPool objectPoolScript = this.gameObject.GetComponent<CObjectPool>();

        physicsSceneManager.SpawnedObjects.Clear();
        physicsSceneManager.RequiredObjects.Clear();
        foreach (GameObject go in objectPoolScript.objectList) {
            GameObject go_init = Instantiate(go, Vector3.zero, Quaternion.identity);
            go_init.transform.parent = this.transform;
            physicsSceneManager.SpawnedObjects.Add(go_init);
            physicsSceneManager.RequiredObjects.Add(go_init);
        }

        List<SimObjType> listOfExcludedReceptacleTypes = new List<SimObjType>();
        if (!allowFloor) {
            listOfExcludedReceptacleTypes.Add(SimObjType.Floor);
        }
        physicsSceneManager.RandomSpawnRequiredSceneObjects(
            seed: 0,
            spawnOnlyOutside: false,
            maxPlacementAttempts: 100,
            staticPlacement: true,
            excludedSimObjects: new HashSet<SimObjPhysics>(),
            numDuplicatesOfType: null,
            excludedReceptacleTypes: listOfExcludedReceptacleTypes
        );
    }

    public List<SimObjPhysicsExtended> GetRequiredExtendObjPhysics(CGenerationType gtype = CGenerationType.Object) {
        Debug.Log("GetRequiredExtendObjPhysics");
        List<SimObjPhysicsExtended> sopEList = new List<SimObjPhysicsExtended>();
        CObjectPool objectPool = this.gameObject.GetComponent<CObjectPool>();

        foreach (SimObjPhysicsExtended sopE in CSceneBuilderTool.jsonRule.sopEList) {
            Debug.Log("GetRequiredExtendObjPhysics " + sopE.simObjName + " " + sopE.ObjType + " " + gtype);
            if (CRoomRestrictions.RoomObj2GenerationType[sopE.ObjType] == gtype || CRoomRestrictions.RoomObj2GenerationType[sopE.ObjType] == CGenerationType.NotSure) {

                //if sopE is an obj, it must "on" something
                bool isObj = false;
                foreach (string desc in sopE.directionDesc) {
                    Debug.Log(sopE.simObjName + ": sopE.directionDesc: " + desc);
                    if (desc == "on") {
                        isObj = true;
                    }
                }
                Debug.Log("enter if " + isObj);
                if (!isObj) {
                    //not sure what the object is furniture or object


                    //if in kitchen, put it on a countertop
                    if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen) {

                        if (CRoomRestrictions.DonotMoveObjTypes.Contains(sopE.ObjType) && objectPool.beInResample) {                 
                            continue;
                        }

                            //but put all countertop into candidates;
                            int counterTopCount = 0;
                        foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                            if (sop.Type == SimObjType.CounterTop) {
                                counterTopCount++;
                                //sopE.directionDesc.Add("on");
                                //sopE.groupObjName.Add("CounterTop_" + counterTopCount.ToString());
                            }
                        }

                        if (counterTopCount == 0) {
                            Debug.LogError("NO COUNTERTOPS FOR KITCHEN");
                        }

                        //random pick a countertop for it not pick, 
                        int pickCounterTop = (int)UnityEngine.Random.Range(1f, counterTopCount + 1f);
                        sopE.directionDesc.Add("on");
                        sopE.groupObjName.Add("CounterTop_" + counterTopCount.ToString());

                    } else {
                        continue;
                    }
                } 

                Debug.Log("Loading Object " + sopE.ObjType);
                foreach (GameObject go in objectPool.objectList) {
                    if (go.GetComponent<SimObjPhysics>().ObjType == sopE.ObjType) {
                        sopE.simObjPhysics = go.GetComponent<SimObjPhysics>();
                        sopEList.Add(sopE);
                        break;
                    }
                }

            }
        }

        return sopEList;
    }

    public bool CustomPlaceObject() {
        bool allSuccess = true;

        SetSpawner();

        var allReceptacles = GameObject.FindObjectsOfType<SimObjPhysics>();

        List<SimObjPhysicsExtended> sopEList = GetRequiredExtendObjPhysics();

#if UNITY_EDITOR
        IEnumerator SequtialSamplingObjects() {
            foreach (SimObjPhysicsExtended sopE in sopEList) {
                //Debug.Log("Now place obj name:" + sopE.simObjName);
                SimObjType objType = sopE.ObjType;
                foreach (GameObject go in objectPool.objectList) {

                    if (go.GetComponent<SimObjPhysics>().ObjType == objType) {
                        SimObjPhysics sop = Instantiate(go, new Vector3(-100, -100, -100), Quaternion.identity).GetComponent<SimObjPhysics>();
                        sopE.simObjPhysics = sop;
                        sopE.gameObject = sop.gameObject;
                        sopE.gameObject.name = sopE.simObjName;

                        Debug.Log("Now place object (SOPE)" + sop.gameObject.name);

                        List<ReceptacleSpawnPoint> allRsps = new List<ReceptacleSpawnPoint>();
                        int receptacleIndex = 0;
                        for (int j = 0; j < sopE.directionDesc.Count; ++j) {
                            if (sopE.directionDesc[j] == "on") {
                                receptacleIndex = j;

                                string groupObjName = sopE.groupObjName[receptacleIndex];
                                //if kitchen randomly pick a countertop, cabinet, drawer
                                if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen) {
                                    if (groupObjName.Contains("CounterTop")) {
                                        groupObjName = CKitchenPlacerTool.FindARandomSOPType(SimObjType.CounterTop);
                                    } else if (groupObjName.Contains("Cabinet")) {
                                        groupObjName = CKitchenPlacerTool.FindARandomSOPType(SimObjType.Cabinet);
                                    } else if (groupObjName.Contains("Drawer")) {
                                        groupObjName = CKitchenPlacerTool.FindARandomSOPType(SimObjType.Drawer);
                                    } else if (groupObjName.Contains("StoveBurner")) {
                                        groupObjName = CKitchenPlacerTool.FindARandomSOPType(SimObjType.StoveBurner);
                                    }
                                }


                                GameObject receptacleObj = GameObject.Find(groupObjName);
                                if (receptacleObj == null) {
                                    Debug.LogWarning("No receptable found: " + groupObjName + " for obj: " + sopE.simObjName);
                                    allSuccess = false;
                                    break;
                                }
                                SimObjPhysics receptacleSop = receptacleObj.GetComponent<SimObjPhysics>();
                                if (receptacleSop != null) {
                                    Debug.Log("Find receptable (SOPE): " + receptacleSop.gameObject.name);

                                    //fix empty parent
                                    foreach (GameObject rtb in receptacleSop.ReceptacleTriggerBoxes) {
                                        Contains containsScript = rtb.GetComponent<Contains>();
                                        if (containsScript.myParent == null) {
                                            containsScript.myParent = receptacleSop.gameObject;
                                        }

                                    }

                                    List<ReceptacleSpawnPoint> targetReceptacleSpawnPoints = receptacleSop.ReturnMySpawnPoints(false);
                                    allRsps.AddRange(targetReceptacleSpawnPoints);
                                    //break;
                                }
                            }
                        }





                        //load general rule
                        CObjectItemRule objRule = objectPool.objectItemRules[(int)sopE.ObjType - 1];

                        //shuffle rsps
                        System.Random rng = new System.Random(randomSeed);
                        allRsps.Shuffle_(rng);

                        if (allRsps.Count > 0) {
                            bool successful = CustomPlaceObjectReceptacle(
                                allRsps,
                                sopE,
                                objRule.rho,
                                objRule.theta,
                                objRule.faceCenter,
                                true,
                                3000,
                                90,
                                true
                            );

                            Debug.Log("Place Successful? (SOPE) " + successful);
                            allSuccess = allSuccess && successful;
                        }

                        break;
                    }
                }
                yield return new EditorWaitForSeconds(0.2f);
            }

            CSceneBuilderTool.objectPlacingFinish = true;
            CSceneBuilderTool.objectPlacingSuccessful = allSuccess;
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(SequtialSamplingObjects());
#endif
        return allSuccess;
    }

    public bool CustomPlaceObjectParallel() {
        bool allSuccess = true;

        SetSpawner();

        var allReceptacles = GameObject.FindObjectsOfType<SimObjPhysics>();

        List<SimObjPhysicsExtended> sopEList = GetRequiredExtendObjPhysics();

        foreach (SimObjPhysicsExtended sopE in sopEList) {
            //Debug.Log("Now place obj name:" + sopE.simObjName);
            SimObjType objType = sopE.ObjType;
            foreach (GameObject go in objectPool.objectList) {

                if (go.GetComponent<SimObjPhysics>().ObjType == objType) {
                    SimObjPhysics sop = Instantiate(go, new Vector3(-100, -100, -100), Quaternion.identity).GetComponent<SimObjPhysics>();
                    sopE.simObjPhysics = sop;
                    sopE.gameObject = sop.gameObject;
                    sopE.gameObject.name = sopE.simObjName;

                    Debug.Log("Now place object (SOPE)" + sop.gameObject.name);

                    List<ReceptacleSpawnPoint> allRsps = new List<ReceptacleSpawnPoint>();
                    int receptacleIndex = 0;
                    for (int j = 0; j < sopE.directionDesc.Count; ++j) {
                        if (sopE.directionDesc[j] == "on") {
                            receptacleIndex = j;

                            string groupObjName = sopE.groupObjName[receptacleIndex];
                            //if kitchen randomly pick a countertop, cabinet, drawer
                            if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen) {
                                if (groupObjName.Contains("CounterTop")) {
                                    groupObjName = CKitchenPlacerTool.FindARandomSOPType(SimObjType.CounterTop);
                                } else if (groupObjName.Contains("Cabinet")) {
                                    groupObjName = CKitchenPlacerTool.FindARandomSOPType(SimObjType.Cabinet);
                                } else if (groupObjName.Contains("Drawer")) {
                                    groupObjName = CKitchenPlacerTool.FindARandomSOPType(SimObjType.Drawer);
                                } else if (groupObjName.Contains("StoveBurner")) {
                                    groupObjName = CKitchenPlacerTool.FindARandomSOPType(SimObjType.StoveBurner);
                                }
                            }


                            GameObject receptacleObj = GameObject.Find(groupObjName);
                            if (receptacleObj == null) {
                                Debug.LogWarning("No receptable found: " + groupObjName + " for obj: " + sopE.simObjName);
                                allSuccess = false;
                                break;
                            }
                            SimObjPhysics receptacleSop = receptacleObj.GetComponent<SimObjPhysics>();
                            if (receptacleSop != null) {
                                Debug.Log("Find receptable (SOPE): " + receptacleSop.gameObject.name);

                                //fix empty parent
                                foreach (GameObject rtb in receptacleSop.ReceptacleTriggerBoxes) {
                                    Contains containsScript = rtb.GetComponent<Contains>();
                                    if (containsScript.myParent == null) {
                                        containsScript.myParent = receptacleSop.gameObject;
                                    }

                                }

                                List<ReceptacleSpawnPoint> targetReceptacleSpawnPoints = receptacleSop.ReturnMySpawnPoints(false);
                                allRsps.AddRange(targetReceptacleSpawnPoints);
                                //break;
                            }
                        }
                    }

                    //load general rule
                    CObjectItemRule objRule = objectPool.objectItemRules[(int)sopE.ObjType - 1];

                    //shuffle rsps
                    System.Random rng = new System.Random(randomSeed);
                    allRsps.Shuffle_(rng);

                    if (allRsps.Count > 0) {
                        bool successful = CustomPlaceObjectReceptacle(
                            allRsps,
                            sopE,
                            objRule.rho,
                            objRule.theta,
                            objRule.faceCenter,
                            true,
                            3000,
                            90,
                            true
                        );

                        if (!successful && objectPool.beInResample) {
                            objectPool.RestoreObjectName(sopE.simObjName);
                            DestroyImmediate(sopE.simObjPhysics.gameObject);
                            
                        }

                        Debug.Log("Place Successful? (SOPE) " + successful);
                        allSuccess = allSuccess && successful;
                    }

                    break;
                }
            }
        }

        CSceneBuilderTool.objectPlacingFinish = true;
        CSceneBuilderTool.objectPlacingSuccessful = allSuccess;
        return allSuccess;
    }

    public bool CustomPlaceObjectReceptacle(
        List<ReceptacleSpawnPoint> rsps,
        SimObjPhysicsExtended sopE,
        float pho,
        float theta,
        int faceCenter,
        bool PlaceStationary,
        int maxPlacementAttempts,
        int degreeIncrement,
        bool AlwaysPlaceUpright = true) {

        List<CustomReceptacleSpawnPoint> customRsps = ProcessCustomReceptableSpawnPoint(rsps, sopE, pho, theta, faceCenter);//WallRuleReceptableSpawnPoint(rsps);

        Debug.Log("CustomPlaceObjectReceptacle: " + sopE.ObjType + " ProcessCustomReceptableSpawnPoint: " + customRsps.Count);

        //try a number of spawnpoints in this specific receptacle up to the maxPlacementAttempts
        int tries = 0;
        foreach (CustomReceptacleSpawnPoint p in customRsps) {

            //bool placeSuccessful = spawner.PlaceObject(sop: sopE.simObjPhysics, rsp: p, PlaceStationary: true, degreeIncrement: 90, AlwaysPlaceUpright: true);

            bool placeSuccessful = CustomPlaceObj(sopE.simObjPhysics, p, PlaceStationary, degreeIncrement, AlwaysPlaceUpright);
            if (placeSuccessful) {
                return true;
            }
            tries += 1;
            if (maxPlacementAttempts > 0 && tries > maxPlacementAttempts) {
                break;
            }
        }

        return false;
    }

    private List<CustomReceptacleSpawnPoint> ProcessCustomReceptableSpawnPoint(List<ReceptacleSpawnPoint> rsps, SimObjPhysicsExtended sopE, float pho, float theta, int faceCenter, bool finalRandomShuffle = true) {
        List<CustomReceptacleSpawnPoint> crsps = new List<CustomReceptacleSpawnPoint>();

        //find room border and corner
        if (this.cornerList.Count == 0) {
            GameObject furniturePlacer = GameObject.Find("FurniturePlacer");
            if (furniturePlacer != null) {
                CFurniturePool furniturePool = GameObject.Find("FurniturePlacer").GetComponent<CFurniturePool>();
                cornerList = furniturePool.cornrerList;
                borderList = furniturePool.borderList;
            }
        }

        //Debug.Log("cornerList borderList " + borderList.Count + " " + cornerList.Count);


        Vector2 roomCenter = Vector2.zero;
        foreach (var corner in this.cornerList) {
            roomCenter.x += corner.x;
            roomCenter.y += corner.y;
        }
        if (cornerList.Count > 0) {
            roomCenter.x /= cornerList.Count;
            roomCenter.y /= cornerList.Count;
        }


        //Debug.Log("Room center: " + roomCenter);

        //find the average score of d & R
        float maxD = -1000f;
        float minD = 1000f;
        float maxR = -1000f;
        float minR = 1000f;


        //set up rotation start to make sure all the rotations are in the PI range
        float rotationStone = -1f;
        foreach (ReceptacleSpawnPoint rsp in rsps) {
            Vector2 p = new Vector2(rsp.Point.x, rsp.Point.z);

            float[] dr = Geometry.DistanceAndRotationFromBorder(p, new List<Vector2>() { roomCenter });
            //Debug.Log("point: " + rsp.Point.ToString() + " " + dr[0] + " " + dr[1]);
            //dr[1] = -dr[1];
            if (rotationStone == -1f) {
                rotationStone = dr[1];
            } else {
                while (dr[1] - rotationStone > Mathf.PI) {
                    dr[1] -= 2 * Mathf.PI;
                }

                while (dr[1] - rotationStone < -Mathf.PI) {
                    dr[1] += 2 * Mathf.PI;
                }
            }

            //#if UNITY_EDITOR
            //            GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
            //            if (UnityEngine.Random.Range(0f, 1f) < 0.05f)
            //            {
            //                GameObject go1 = Instantiate(_road_sign, new Vector3(p.x, 1.0f, p.y), Quaternion.Euler(0, -dr[1] * Mathf.Rad2Deg, 0));
            //                go1.transform.parent = this.transform;
            //            }
            //#endif
            CustomReceptacleSpawnPoint crsp = new CustomReceptacleSpawnPoint(rsp, dr[1], dr[0]);
            crsps.Add(crsp);

            maxD = Mathf.Max(maxD, dr[0]);
            minD = Mathf.Min(minD, dr[0]);
            maxR = Mathf.Max(maxR, dr[1]);
            minR = Mathf.Min(minR, dr[1]);
        }

        float centerD = pho * (maxD - minD) + minD;
        float centerR = theta * (maxR - minR) + minR;

        //Debug.Log("mmmm" + maxD + " " + minD + " " + maxR + " " + minR);

        Vector2 sampleCenter = new Vector2(roomCenter.x + centerD * Mathf.Cos(centerR), roomCenter.y + centerD * Mathf.Sin(centerR));

        //#if UNITY_EDITOR
        //        GameObject go = Instantiate(_road_sign, new Vector3(roomCenter.x, 1.0f, roomCenter.y), Quaternion.identity);
        //        go.transform.parent = this.transform;
        //        GameObject go2 = Instantiate(_road_sign, new Vector3(sampleCenter.x, 1.0f, sampleCenter.y), Quaternion.Euler(0, centerR / Mathf.PI * 180, 0));
        //        go.transform.parent = this.transform;

        //#endif

        foreach (CustomReceptacleSpawnPoint crsp in crsps) {
            float newD = Vector2.Distance(new Vector2(crsp.Point.x, crsp.Point.z), sampleCenter);
            crsp.sampleScore = newD;

            //if face center rotate 180 degree
            if (faceCenter == 1) {
                crsp.rotationRad = (crsp.ParentSimObjPhys.transform.eulerAngles.y) * Mathf.Deg2Rad; //+= Mathf.PI;
            } else {
                crsp.rotationRad += UnityEngine.Random.Range(0f, 2 * Mathf.PI);
            }
            //round degree to the closed 90
            crsp.rotationRad = Mathf.Round(crsp.rotationRad * 2.0f / Mathf.PI) * Mathf.PI / 2.0f;
        }

        if (finalRandomShuffle) {
            CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "random", randomSeed++);
        } else {
            CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "border", randomSeed++);
        }


        JsonRuleReceptacleSpawnPoint(crsps, sopE);

        //Modify rotation rule
        ModifyRotationRuleReceptacleSpawnPoint(crsps, sopE);

        return crsps;
    }

    private void ModifyRotationRuleReceptacleSpawnPoint(List<CustomReceptacleSpawnPoint> crsps, SimObjPhysicsExtended sopE) {
        if (CRoomRestrictions.SOPRotationOffset.ContainsKey(sopE.ObjType)) {
            float offsetRad = CRoomRestrictions.SOPRotationOffset[sopE.ObjType];
            foreach (CustomReceptacleSpawnPoint crsp in crsps) {
                crsp.rotationRad += offsetRad;
            }
        }
    }

    public void JsonRuleReceptacleSpawnPoint(List<CustomReceptacleSpawnPoint> crsps, SimObjPhysicsExtended sopE) {
        /*
         Mix prior rule and json rule according to weight
         */
        if (sopE.groupObjName.Count == 0) {
            return;
        }
        for (int i = 0; i < sopE.groupObjName.Count; ++i) {
            string groupObjName = sopE.groupObjName[i];
            //Debug.Log(sopE.gameObject.name + "....enter json rule..........." + groupObjName);
            GameObject groupObj = GameObject.Find(groupObjName);

            if (groupObj == null) {
                Debug.LogWarning("JsonRuleReceptacleSpawnPoint can not find group obj: " + sopE.simObjName + " -> " + sopE.groupObjName);
                crsps.Clear();
                continue;
            }
            if (sopE.directionDesc[i] == "on") {
                continue; //already handble by receptacles
            }

            SimObjType groupObjType = groupObj.GetComponent<SimObjPhysics>().ObjType;

            Debug.Log("JsonRuleReceptacleSpawnPoint !find! group obj: " + sopE.simObjName + " -> " + sopE.groupObjName[i] + " : " + sopE.directionDesc[i]);

            if (sopE.directionDesc[i] == "besides") {

                foreach (CustomReceptacleSpawnPoint crsp in crsps) {
                    float dist = Vector3.Distance(crsp.Point, groupObj.transform.position);

                    (SimObjType, SimObjType) pairType = (sopE.ObjType, groupObjType);
                    float pairWeight = 1.0f;
                    if (CLearningParams.PairType2WeightBesides.ContainsKey(pairType)) {
                        pairWeight = CLearningParams.PairType2WeightBesides[pairType];
                    }
                    crsp.sampleScore = Mathf.Pow(crsp.sampleScore, 2) + pairWeight * Mathf.Pow(dist - 0.6f, 2); //offset, make a small gap between them

                }
                CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "border", randomSeed);
            } else if (sopE.directionDesc[i] == "face") {
                BoxCollider oabb = groupObj.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>();
                Vector3 oabbCenter = oabb.transform.TransformPoint(oabb.center);

                foreach (CustomReceptacleSpawnPoint crsp in crsps) {
                    //Vector3 closestOnCollider = transform.TransformPoint(oabb.ClosestPoint(crsp.Point));

                    float dist = Vector3.Distance(crsp.Point, oabbCenter);

                    float rotationRad = Mathf.Atan2(oabbCenter.z - crsp.Point.z, oabbCenter.x - crsp.Point.x);
                    crsp.rotationRad = rotationRad; //???????????????????????????????



                    (SimObjType, SimObjType) pairType = (sopE.ObjType, groupObjType);
                    float pairWeight = 1.0f;

                    if (CLearningParams.PairType2WeightBesides.ContainsKey(pairType)) {
                        pairWeight = CLearningParams.PairType2WeightBesides[pairType];
                    }
                    crsp.sampleScore = Mathf.Pow(crsp.sampleScore, 2) + pairWeight * Mathf.Pow(dist, 2);
                }
                CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "border", randomSeed);
            } else if (sopE.directionDesc[i] == "away") {
                BoxCollider oabb = groupObj.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>();
                Vector3 oabbCenter = oabb.transform.TransformPoint(oabb.center);

                foreach (CustomReceptacleSpawnPoint crsp in crsps) {
                    //Vector3 closestOnCollider = transform.TransformPoint(oabb.ClosestPoint(crsp.Point));

                    float dist = Vector3.Distance(crsp.Point, oabbCenter);

                    (SimObjType, SimObjType) pairType = (sopE.ObjType, groupObjType);
                    float pairWeight = 1.0f;

                    if (CLearningParams.PairType2WeightAway.ContainsKey(pairType)) {
                        pairWeight = CLearningParams.PairType2WeightAway[pairType];
                    }
                    crsp.sampleScore = Mathf.Pow(crsp.sampleScore, 2) + pairWeight * Mathf.Pow(1 / (dist + 0.01f), 2);
                }
                CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "border", randomSeed);
            }

        }
    }

    public bool CustomPlaceObj(SimObjPhysics sop, CustomReceptacleSpawnPoint crsp, bool PlaceStationary, int degreeIncrement, bool alwaysPlaceUpright) {
        if (crsp.ParentSimObjPhys == sop) {
#if UNITY_EDITOR
            Debug.Log("Can't place object inside itself!");
#endif
            return false;
        }
        //remember the original rotation of the sim object if we need to reset it
        //Quaternion originalRot = sop.transform.rotation;
        Vector3 originalPos = sop.transform.position;
        Quaternion originalRot = sop.transform.rotation;

        //get the bounding box of the sim object we are trying to place
        BoxCollider oabb = sop.BoundingBox.GetComponent<BoxCollider>();
        oabb.enabled = true;

        //zero out rotation and velocity/angular velocity, then match the target receptacle's rotation
        //?????????
        sop.transform.rotation = crsp.ReceptacleBox.transform.rotation;
        Rigidbody sopRB = sop.GetComponent<Rigidbody>();
        sopRB.velocity = Vector3.zero;
        sopRB.angularVelocity = Vector3.zero;

        Plane BoxBottom;
        float DistanceFromBoxBottomTosop;
        InstantiatePrefabTest.RotationAndDistanceValues quat;

        //Vector3 Offset = oabb.ClosestPoint(oabb.transform.TransformPoint(oabb.center) + -crsp.ReceptacleBox.transform.up * 5); //was using rsp.point
        Vector3 target_location = oabb.transform.TransformPoint(oabb.center) + -crsp.ReceptacleBox.transform.up * 5;
        Vector3 Offset = oabb.ClosestPoint(target_location);

        //Debug.Log(oabb.center + " box: " + oabb.bounds.extents.ToString() + " oabb 2 global: " + oabb.transform.TransformPoint(oabb.center) + "offset: " + Offset.ToString());

        BoxBottom = new Plane(crsp.ReceptacleBox.transform.up, Offset);
        DistanceFromBoxBottomTosop = BoxBottom.GetDistanceToPoint(sop.transform.position);

        //quat = new InstantiatePrefabTest.RotationAndDistanceValues(DistanceFromBoxBottomTosop,
        //    Quaternion.Euler(0, -crsp.rotationRad * Mathf.Rad2Deg + 90, 0));

        quat = new InstantiatePrefabTest.RotationAndDistanceValues(DistanceFromBoxBottomTosop,
            Quaternion.Euler(0, crsp.rotationRad * Mathf.Rad2Deg, 0));

        Vector3 newPosition = crsp.Point + crsp.ParentSimObjPhys.transform.up * (quat.distance + this.yoffset);
        //Debug.Log("spawner: " + spawner.gameObject.name + " " + crsp.Point.ToString());
        //Debug.Log("quat: " + quat.distance + " " + quat.rotation + " -crsp.rotateDegree " + -crsp.rotateDegree);

        //if spawn area is clear, spawn it and return true that we spawned it
        //bool noCollison = spawner.CheckSpawnArea(sop, newPosition, quat.rotation, false);

        //track colliders
        List<Collider> colsToDisable = new List<Collider>();
        foreach (Collider g in sop.MyColliders) {
            //only track this collider if it's enabled by default
            //some objects, like houseplants, might have colliders in their simObj.MyColliders that are disabled
            if (g.enabled) {
                colsToDisable.Add(g);
            }
        }

        //GameObject parentObject = crsp.ParentSimObjPhys.gameObject;
        //foreach (Collider c in parentObject.GetComponentsInChildren<Collider>()) {
        //    colsToDisable.Add(c);
        //}


        //disable collision before moving to check the spawn area
        foreach (Collider c in colsToDisable) {
            c.enabled = false;
        }


        //move it into place so the bouding box is in the right spot to generate the overlap box later
        sop.transform.position = newPosition;
        sop.transform.rotation = quat.rotation;

        //now let's get the BoundingBox of the simObj as reference cause we need it to create the overlapbox
        GameObject bb = sop.BoundingBox.transform.gameObject;
        //Debug.Log("sop: " + sop.Type + " layer: " + bb.layer);
        //bb.layer = 8;
        BoxCollider bbcol = bb.GetComponent<BoxCollider>();
        Vector3 bbCenter = bbcol.center;
        Vector3 bbCenterTransformPoint = bb.transform.TransformPoint(bbCenter);

        //move sim object back to it's original spot back so the overlap box doesn't hit it
        sop.transform.position = originalPos;
        sop.transform.rotation = originalRot;


        //re-enable the collision after returning in place
        foreach (Collider c in colsToDisable) {
            c.enabled = true;
        }

        //spawn overlap box
        Collider[] hitColliders = Physics.OverlapBox(
            bbCenterTransformPoint,
            bbcol.size / 2.0f,
            quat.rotation,
            1 << 8 | 1 << 10, //simObjVisible, agent
            QueryTriggerInteraction.Collide
        );

        bool noCollison = hitColliders.Length == 0;

        //Debug.Log("spawner: " + spawner.gameObject.name + " " + crsp.Point.ToString());
        //Debug.Log("quat: " + quat.distance + " " + quat.rotation + " -crsp.rotateDegree " + -crsp.rotateDegree);

        //if spawn area is clear, spawn it and return true that we spawned it
        //bool noCollison = spawner.CheckSpawnArea(sop, crsp.Point + crsp.ParentSimObjPhys.transform.up * (quat.distance + this.yoffset), quat.rotation, false);

        //oabb.enabled = false;
        //Debug.Log(sop.Type + " no collision??????? " + noCollison + " pos " + bbCenterTransformPoint + " size " + bbcol.size / 2.0f);
        if (noCollison) {

            //translate position of the target sim object to the rsp.Point and offset in local y up
            sop.transform.position = crsp.Point + crsp.ReceptacleBox.transform.up * (quat.distance + yoffset);//rsp.Point + sop.transform.up * DistanceFromBottomOfBoxToTransform;
            sop.transform.rotation = quat.rotation;
            //Check the ReceptacleBox's Sim Object component to see what Type it is. Then check to
            //see if the type is the kind where the Object placed must be completely contained or just the bottom 4 corners contained
            int HowManyCornersToCheck = 0;
            if (ReceptacleRestrictions.OnReceptacles.Contains(crsp.ParentSimObjPhys.ObjType)) {
                //check that only the bottom 4 corners are in bounds
                HowManyCornersToCheck = 4;
            }

            if (ReceptacleRestrictions.InReceptacles.Contains(crsp.ParentSimObjPhys.ObjType)) {
                //check that all 8 corners are within bounds
                HowManyCornersToCheck = 8;
            }

            if (ReceptacleRestrictions.InReceptaclesThatOnlyCheckBottomFourCorners.Contains(crsp.ParentSimObjPhys.ObjType)) {
                //only check bottom 4 corners even though the action is PlaceIn
                HowManyCornersToCheck = 4;
            }

            //Debug.Log("HowManyCornersToCheck: " + HowManyCornersToCheck);

            //Special cases
            if (crsp.ParentSimObjPhys.ObjType == SimObjType.StoveBurner) {
                HowManyCornersToCheck = 0;
            }

            int CornerCount = 0;

            List<Vector3> spawnCorners = GetSimObjCorners(sop);

            spawnCorners.Sort(delegate (Vector3 p1, Vector3 p2) {
                return Vector3.Distance(p1, crsp.Point).CompareTo(Vector3.Distance(p2, crsp.Point));
            });

            for (int i = 0; i < 8; i++) {
                if (crsp.Script.CheckIfPointIsInsideReceptacleTriggerBox(spawnCorners[i])) {
                    CornerCount++;
                }
            }

            //if not enough corners are inside the receptacle, abort
            if (CornerCount < HowManyCornersToCheck) {
                sop.transform.rotation = originalRot;
                sop.transform.position = originalPos;
                return false;
            }

            //set true if we want objects to be stationary when placed. (if placed on uneven surface, object remains stationary)
            //if false, once placed the object will resolve with physics (if placed on uneven surface object might slide or roll)
            if (PlaceStationary == true) {
                //make object being placed kinematic true
                sop.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                sop.GetComponent<Rigidbody>().isKinematic = true;

                //check if the parent sim object is one that moves like a drawer - and would require this to be parented
                //if(rsp.ParentSimObjPhys.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanOpen))
                sop.transform.SetParent(crsp.ParentSimObjPhys.transform);

                //GameObject topObject = GameObject.Find("Objects");
                //parent to the Objects transform
                //sop.transform.SetParent(topObject.transform);

                //if this object is a receptacle and it has other objects inside it, drop them all together
                if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                    PhysicsRemoteFPSAgentController agent = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();
                    agent.DropContainedObjectsStationary(sop); //use stationary version so that colliders are turned back on, but kinematics remain true
                }

                //if the target receptacle is a pickupable receptacle, set it to kinematic true as will sence we are placing stationary
                if (crsp.ParentSimObjPhys.PrimaryProperty == SimObjPrimaryProperty.CanPickup) {
                    crsp.ParentSimObjPhys.GetComponent<Rigidbody>().isKinematic = true;
                }

            }

            //place stationary false, let physics drop everything too
            else {
                //if not placing stationary, put all objects under Objects game object
                GameObject topObject = GameObject.Find("Objects");
                //parent to the Objects transform
                sop.transform.SetParent(topObject.transform);

                Rigidbody rb = sop.GetComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                //if this object is a receptacle and it has other objects inside it, drop them all together
                if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                    PhysicsRemoteFPSAgentController agent = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();
                    agent.DropContainedObjects(target: sop, reparentContainedObjects: true, forceKinematic: false);
                }
            }
            sop.isInAgentHand = false;//set agent hand flag

            objectPool.generatedObjects.Add(sop.gameObject);


            // #if UNITY_EDITOR
            // Debug.Log(sop.name + " succesfully spawned in " +rsp.ParentSimObjPhys.name + " at coordinate " + rsp.Point);
            // #endif

            return true;
        }

        return false;
    }

    public List<Vector3> GetSimObjCorners(SimObjPhysics simObj) {
        //now let's get the BoundingBox of the simObj as reference cause we need it to create the overlapbox
        GameObject bb = simObj.BoundingBox.transform.gameObject;
        BoxCollider bbcol = bb.GetComponent<BoxCollider>();
        Vector3 bbCenter = bbcol.center;
        //Vector3 bbCenterTransformPoint = bb.transform.TransformPoint(bbCenter);
        // keep track of all 8 corners of the OverlapBox
        List<Vector3> corners = new List<Vector3>();
        // bottom forward right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, -bbcol.size.y, bbcol.size.z) * 0.5f));
        // bottom forward left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, -bbcol.size.y, bbcol.size.z) * 0.5f));
        // bottom back left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, -bbcol.size.y, -bbcol.size.z) * 0.5f));
        // bottom back right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, -bbcol.size.y, -bbcol.size.z) * 0.5f));

        // top forward right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, bbcol.size.y, bbcol.size.z) * 0.5f));
        // top forward left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, bbcol.size.y, bbcol.size.z) * 0.5f));
        // top back left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));
        // top back right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));

        return corners;
    }

#if UNITY_EDITOR
    public void LoadObjectPrefab(bool loadCommonObject = true, bool canDuplicate = false) {
        //load pool
        Debug.Log("LoadFurniturePrefab");
        CObjectPool objPool = this.gameObject.GetComponent<CObjectPool>();
        objPool.objectList.Clear();
        CRoomType roomType = objPool.roomType;

        List<string> prefabPathList = new List<string>();
        List<SimObjType> nonDuplicatedObjTypeList = new List<SimObjType>();

        void LoadPrefabPaths(string dirr) {
            //DirectoryInfo dir = new DirectoryInfo(dirPath);
            string[] fileInfoList = Directory.GetDirectories(dirr);
            foreach (string file in fileInfoList) {
                string prefabDirPath = file.ToString() + "/Prefabs";
                prefabDirPath = prefabDirPath.Replace('\\', '/');

                DirectoryInfo prefabDir = new DirectoryInfo(prefabDirPath);
                try {
                    FileInfo[] prefabInfoList = prefabDir.GetFiles("*.prefab");
                    foreach (FileInfo prefabFile in prefabInfoList) {
                        int prefabRelativepathStart = prefabFile.ToString().Replace('\\', '/').IndexOf(dirr);
                        string prefabPath = prefabFile.ToString().Replace('\\', '/').Substring(prefabRelativepathStart);

                        //Debug.Log("LoadFurniturePrefab: " + prefabPath);

                        prefabPathList.Add(prefabPath);
                    }
                } catch {
                    //Debug.Log("the prefab path is wrong: " + prefabDirPath);
                }
                prefabDirPath = file.ToString() + "/Prefab";
                prefabDir = new DirectoryInfo(prefabDirPath);
                prefabDirPath = prefabDirPath.Replace('\\', '/');

                try {
                    FileInfo[] prefabInfoList = prefabDir.GetFiles("*.prefab");
                    foreach (FileInfo prefabFile in prefabInfoList) {
                        int prefabRelativepathStart = prefabFile.ToString().Replace('\\', '/').IndexOf(dirr);
                        string prefabPath = prefabFile.ToString().Replace('\\', '/').Substring(prefabRelativepathStart);
                        //Debug.Log("LoadFurniturePrefab: " + prefabPath);

                        prefabPathList.Add(prefabPath);
                    }
                } catch {
                    //Debug.Log("the prefab path is wrong: " + prefabDirPath);
                }
            }
        }


        string dirPath1 = "Assets/Physics/SimObjsPhysics/Living Room Objects";
        LoadPrefabPaths(dirPath1);

        string dirPath2 = "Assets/Physics/SimObjsPhysics/Living Room and Bedroom Objects";
        LoadPrefabPaths(dirPath2);

        string dirPath3 = "Assets/Physics/SimObjsPhysics/Bedroom Objects";
        LoadPrefabPaths(dirPath3);

        string dirPath4 = "Assets/Physics/SimObjsPhysics/Kitchen Objects";
        LoadPrefabPaths(dirPath4);

        //bathroom
        string dirPath7 = "Assets/Physics/SimObjsPhysics/Bathroom Objects";
        LoadPrefabPaths(dirPath7);

        if (loadCommonObject) {
            string dirPath5 = "Assets/Physics/SimObjsPhysics/Common Objects";
            LoadPrefabPaths(dirPath5);

            //string dirPath2 = "Assets/Physics/SimObjsPhysics/Custom Project Objects";
            //LoadPrefabPaths(dirPath2);

            string dirPath6 = "Assets/Physics/SimObjsPhysics/Miscellaneous Objects";
            LoadPrefabPaths(dirPath6);
        }

        //load object rules
        string csvPath = "Assets/Custom/Rules/ObjectRule.csv";
        objPool.objectItemRules = CObjectItemRule.GetItemRule(csvPath);

        //shuffle list
        System.Random rng = new System.Random(objPool.randomSeed++);
        prefabPathList.Shuffle_(rng);

        //put prefab into furniture pool

        foreach (string prefabPath in prefabPathList) {
            GameObject objPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));

            if (!canDuplicate) {
                //string[] pathParts = prefabPath.Split('/');
                //string prefabName = pathParts[pathParts.Length - 1].Split('.')[0].Split('_')[0];
                //Debug.Log("prefab name " + " " + prefabPath + " " + prefabName);
                //GameObject objPrefabGo = Instantiate(objPrefab);
                SimObjPhysics sop = objPrefab.GetComponent<SimObjPhysics>();


                if (sop == null) {
                    //not a good object, continue
                    continue;
                }

                if (CRoomRestrictions.RoomObj2GenerationType[sop.ObjType] == CGenerationType.Object || CRoomRestrictions.RoomObj2GenerationType[sop.ObjType] == CGenerationType.NotSure) {
                    //Debug.Log("loading prefab from path: " + prefabPath + " --- " + objPrefab.name);
                    if (nonDuplicatedObjTypeList.Contains(sop.ObjType)) {
                        continue;
                    } else {
                        nonDuplicatedObjTypeList.Add(sop.ObjType);
                        objPool.objectList.Add(objPrefab);

                    }
                }

            } else {
                objPool.objectList.Add(objPrefab);
            }
        }

        //sort
        objPool.objectList.Sort((x, y) => string.Compare(x.name, y.name));


        //DirectoryInfo dir = new DirectoryInfo("Assets/Physics/SimObjsPhysics");
        //FileInfo[] fileInfo = dir.GetFiles();
        //foreach (var file in fileInfo)
        //    Debug.Log(file);
    }
#endif
}
