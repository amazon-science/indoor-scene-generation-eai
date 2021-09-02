using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;
using UnityStandardAssets.Characters.FirstPerson;
#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
#endif

[SerializeField]
public class CustomReceptacleSpawnPoint : ReceptacleSpawnPoint {
    /*
     Object to hold comstraint for recpetable spawn point
     */
    public float rotationRad;
    public float sampleScore;

    public CustomReceptacleSpawnPoint() : base(Vector3.zero, null, null, null) { }
    public CustomReceptacleSpawnPoint(ReceptacleSpawnPoint receptacleSpawnPoint, float rootation, float diistance) : base(Vector3.zero, null, null, null) {
        ReceptacleBox = receptacleSpawnPoint.ReceptacleBox;
        Point = receptacleSpawnPoint.Point;
        Script = receptacleSpawnPoint.Script;
        ParentSimObjPhys = receptacleSpawnPoint.ParentSimObjPhys;

        rotationRad = rootation;
        sampleScore = diistance;
    }

    public static void ShuffleFrom(List<CustomReceptacleSpawnPoint> crsp_list, string envRule = "border", int seed = 123, bool jitter = true) {
        if (crsp_list.Count == 0) {
#if UNITY_EDITOR
            Debug.LogError("Cannot sample crsp from empty list");
#endif
        }
        if (envRule == "border" || envRule == "corner") {
            //Sample the points close to boarder first
            System.Random rng = new System.Random(seed);
            crsp_list.Shuffle_(rng);
            crsp_list.Sort(delegate (CustomReceptacleSpawnPoint cp1, CustomReceptacleSpawnPoint cp2) {
                return cp1.sampleScore.CompareTo(cp2.sampleScore);
            });
        } else if (envRule == "center") {
            //Sample the points close to center first
            System.Random rng = new System.Random(seed);
            crsp_list.Shuffle_(rng);
            crsp_list.Sort(delegate (CustomReceptacleSpawnPoint cp2, CustomReceptacleSpawnPoint cp1) {
                return cp1.sampleScore.CompareTo(cp2.sampleScore);
            });
        } else {
            //Random rule
            System.Random rng = new System.Random(seed);
            crsp_list.Shuffle_(rng);
        }


    }
}

public class CFurniturePlacerTool : MonoBehaviour {
    //[SerializeField]
    //public int randomSeed;
    //public List<GameObject> furnutureList = new List<GameObject>();
    // Start is called before the first frame update
    public PhysicsSceneManager physicsSceneManager;
    //public InstantiatePrefabTest spawner;

    public CFurniturePool furniturePool;

    public float yoffset = 0.01f;
    public int randomSeed = 0;

    public Dictionary<GameObject, Vector3> door2BoxSize = new Dictionary<GameObject, Vector3>();


    private void SetSpawner() {
        physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
        //spawner = physicsSceneManager.gameObject.GetComponent<InstantiatePrefabTest>();
        furniturePool = this.GetComponent<CFurniturePool>();
        randomSeed = furniturePool.randomSeed; //(int)UnityEngine.Random.Range(0f, 10000f);//
    }

    public void PlaceFurniture() {
        //        SetSpawner();
        //        Debug.Log("CFurniturePlacerTool: " + physicsSceneManager.GatherAllReceptaclesInScene().Count);

        //        InstantiatePrefabTest spawner = physicsSceneManager.gameObject.GetComponent<InstantiatePrefabTest>();
        //        CFurnturePool furnturePoolScript = this.gameObject.GetComponent<CFurnturePool>();
        //        SimObjPhysics receptacleSop = furnturePoolScript.floorPhysics;
        //        List<ReceptacleSpawnPoint> targetReceptacleSpawnPoints = receptacleSop.ReturnMySpawnPoints(false);
        //        Debug.Log("PlaceFurniture: " + targetReceptacleSpawnPoints.Count.ToString());

        //        GameObject _road_sign;
        //        //GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));

        //        //foreach (var point in targetReceptacleSpawnPoints)
        //        //{
        //        //    var go = Instantiate(_road_sign, point.Point, Quaternion.identity);
        //        //    go.transform.parent = this.gameObject.transform;
        //        //}

        //        //System.Random rng = new System.Random(2);

        //        for (int i = 0; i < 1; ++i)
        //        {
        //            int index = UnityEngine.Random.Range(0, furnturePoolScript.furnutureList.Count);
        //            GameObject testFurni = GameObject.Instantiate(furnturePoolScript.furnutureList[index]);

        //            targetReceptacleSpawnPoints.Shuffle_();
        //            SimObjPhysics sopToPlaceInReceptacle = testFurni.GetComponent<SimObjPhysics>();
        //            bool successful = spawner.PlaceObjectReceptacle(
        //                            targetReceptacleSpawnPoints,
        //                            sopToPlaceInReceptacle,
        //                            true,
        //                            100,
        //                            90,
        //                            true
        //                        );
        //            Debug.Log(successful);

        //            if (!successful)
        //            {
        //#if UNITY_EDITOR
        //                DestroyImmediate(testFurni);
        //#endif
        //            }
        //        }
    }


    public List<SimObjPhysicsExtended> GetRequiredExtendObjPhysics(CGenerationType gtype = CGenerationType.Furniture) {
        Debug.Log("GetRequiredExtendObjPhysics");
        List<SimObjPhysicsExtended> sopEList = new List<SimObjPhysicsExtended>();
        CFurniturePool furnturePool = this.gameObject.GetComponent<CFurniturePool>();

        //Version 1（obsolate）
        //if kitchen, add kitchen table/countertop
        //if (furnturePool.roomType == CRoomType.Kitchen) {
        //    furnturePool.kitchenTableList.Shuffle();
        //    furnturePool.furnitureList.AddRange(furnturePool.kitchenTableList);
        //}

        foreach (SimObjPhysicsExtended sopE in CSceneBuilderTool.jsonRule.sopEList) {
            if (CRoomRestrictions.RoomObj2GenerationType[sopE.ObjType] == gtype || CRoomRestrictions.RoomObj2GenerationType[sopE.ObjType] == CGenerationType.NotSure) {
                //if sopE is an obj, it must "on" something
                bool isObj = false;
                foreach (string desc in sopE.directionDesc) {
                    if (desc == "on") {
                        isObj = true;
                    }
                }
                if (isObj) {
                    //is object instead of furniture
                    continue;
                }

                Debug.Log("Loading furniture " + sopE.ObjType);
                foreach (GameObject go in furnturePool.furnitureList) {
                    if (go.GetComponent<SimObjPhysics>().ObjType == sopE.ObjType) {
                        //GameObject prefabGameObject = PrefabUtility.GetCorrespondingObjectFromSource(go);
                        Debug.Log("now loading prefab with name: " + go.name);
                        sopE.prefabName = go.name;
                        GameObject initGo = Instantiate(go, new Vector3(-100, -100, -100), Quaternion.identity);
                        sopE.simObjPhysics = initGo.GetComponent<SimObjPhysics>();
                        initGo.name = sopE.simObjName;
                        sopE.gameObject = initGo;
                        sopEList.Add(sopE);
                        break;
                    }
                }
            }
        }

        return sopEList;
    }

    public List<SimObjPhysics> GetRequiredObjPhysics() {
        List<SimObjPhysics> sopList = new List<SimObjPhysics>();
        CFurniturePool furnturePool = this.gameObject.GetComponent<CFurniturePool>();

        System.Random rng = new System.Random(randomSeed);
        furnturePool.furnitureList.Shuffle_(rng);

        foreach (CFurnitureItemRule cir in furnturePool.furnitureItemRules) {
            if (cir.numInRoom > 0 && cir.isFurniture) {
                foreach (GameObject go in furnturePool.furnitureList) {
                    if (go.GetComponent<SimObjPhysics>().ObjType == cir.objType) {
                        while (cir.numInRoom > 0) {
                            sopList.Add(Instantiate(go, new Vector3(-100, -100, -100), Quaternion.identity).GetComponent<SimObjPhysics>());
                            cir.numInRoom--;
                        }
                        break;
                    }
                }
            }
        }

        // shuffle object to place
        sopList.Shuffle_(rng);

        // sort sopList by priority
        sopList.Sort(delegate (SimObjPhysics s1, SimObjPhysics s2) {
            int p1 = furniturePool.furnitureItemRules[(int)s1.ObjType - 1].placePriority;
            int p2 = furniturePool.furnitureItemRules[(int)s2.ObjType - 1].placePriority;
            return p2.CompareTo(p1);
        });

        return sopList;
    }

    public List<SimObjPhysics> LoadSopListFromPool() {
        List<SimObjPhysics> sopList = new List<SimObjPhysics>();
        foreach (var go in furniturePool.furnitureList) {
            sopList.Add(Instantiate(go).GetComponent<SimObjPhysics>());
        }
        return sopList;
    }

    public bool CustomPlaceFurniture() {
        /*Main function to place furniture
         1.Get required furinture
         2.Sample their positions one by one
         */
        bool allSuccess = true;

        //Set global manager
        SetSpawner();
        //PhysicsSceneManager physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();

        //Debug.Log("CFurniturePlacerTool: " + physicsSceneManager.GatherAllReceptaclesInScene().Count);

        //Get recaptables from floor
        //InstantiatePrefabTest spawner = physicsSceneManager.gameObject.GetComponent<InstantiatePrefabTest>();
        CFurniturePool furnturePoolScript = this.gameObject.GetComponent<CFurniturePool>();
        SimObjPhysics receptacleSop = furnturePoolScript.floorPhysics;
        List<ReceptacleSpawnPoint> targetReceptacleSpawnPoints = receptacleSop.ReturnMySpawnPoints(false);
        Debug.Log("PlaceFurniture: " + targetReceptacleSpawnPoints.Count.ToString());

        //Get required furniture first

        //Get required furniture from Json
        List<SimObjPhysicsExtended> sopEList;
        if (furnturePoolScript.roomType == CRoomType.LivingRoom || furnturePoolScript.roomType == CRoomType.Kitchen ||
            furnturePoolScript.roomType == CRoomType.Bedroom || furnturePoolScript.roomType == CRoomType.Bathroom) {
            sopEList = GetRequiredExtendObjPhysics(); //living room is ok now
        } else {
            // for debug
            sopEList = new List<SimObjPhysicsExtended>();
            List<SimObjPhysics> sopList;
            if (furnturePoolScript.roomType == CRoomType.LivingRoom) {
                sopList = GetRequiredObjPhysics(); //living room is ok now
            } else {
                // for debug
                sopList = LoadSopListFromPool();
            }
            foreach (var sop in sopList) {
                var sopE = new SimObjPhysicsExtended(sop);
                sopE.gameObject = sop.gameObject;
                sopEList.Add(sopE);
            }
        }

        Debug.Log("sopEList: " + sopEList.Count.ToString());

        GameObject objPlaceParent = GameObject.Find("Objects");

#if UNITY_EDITOR
        IEnumerator SequentialSamplingFurniture() {
            foreach (SimObjPhysicsExtended sopToPlaceInReceptacle in sopEList) {
                bool successful = this.CustomPlaceFurnitureReceptacle(
                    targetReceptacleSpawnPoints,
                    sopToPlaceInReceptacle,
                    true,
                    3000,
                    90,
                    true
                );
                Debug.Log("place successful? " + successful);
                allSuccess = allSuccess && successful;
                if (!successful) {
                    DestroyImmediate(sopToPlaceInReceptacle.gameObject);

                } else {
                    // Set object parents
                    sopToPlaceInReceptacle.gameObject.transform.parent = objPlaceParent.transform;
                    // Put GameObject to corner
                    //if (sopToPlaceInReceptacle.ObjType == SimObjType.Sofa)
                    //{
                    //    //PushObjectToBorder(sopToPlaceInReceptacle);
                    //}
                }

                yield return new EditorWaitForSeconds(0.2f);
            }

            CSceneBuilderTool.furniturePlacingFinish = true;
            CSceneBuilderTool.furniturePlacingSuccessful = allSuccess;

            this.RestoreDoorSize();

        }
        EditorCoroutineUtility.StartCoroutineOwnerless(SequentialSamplingFurniture());
        return allSuccess;
#endif
        return allSuccess;
        //int randomIndex = UnityEngine.Random.Range(0, furnturePoolScript.furnutureList.Count);
        //GameObject testFurni = GameObject.Instantiate(furnturePoolScript.furnutureList[randomIndex]);
        //SimObjPhysics sopToPlaceInReceptacle = testFurni.GetComponent<SimObjPhysics>();




    }

    public bool CustomPlaceFurnitureReceptacle(
        List<ReceptacleSpawnPoint> rsps,
        SimObjPhysicsExtended sopE,
        bool PlaceStationary,
        int maxPlacementAttempts,
        int degreeIncrement,
        bool AlwaysPlaceUpright = true) {
        Debug.Log("CustomPlaceObjectReceptacle: " + sopE.ObjType + " Name: " + sopE.simObjName);
        List<CustomReceptacleSpawnPoint> customRsps = ProcessCustomReceptableSpawnPoint(rsps, sopE);//WallRuleReceptableSpawnPoint(rsps);

        //string shuffleRule = furniturePool.allItemRules[(int)sop.ObjType - 1].borderRule;
        //Debug.Log("CustomPlaceObjectReceptacle shuffleRule: " + shuffleRule + " " + sop.ObjType.ToString());

        //Debug.Log((int)sop.ObjType + " CustomPlaceObjectReceptacle OBJ: " + furniturePool.allItemRules[(int)sop.ObjType - 1].objType);

        //CustomReceptacleSpawnPoint.ShuffleFrom(customRsps, shuffleRule, randomSeed);

        //try a number of spawnpoints in this specific receptacle up to the maxPlacementAttempts
        int tries = 0;
        foreach (CustomReceptacleSpawnPoint p in customRsps) {
            if (CustomPlaceFurniture(sopE.simObjPhysics, p, PlaceStationary, degreeIncrement, AlwaysPlaceUpright)) {
                return true;
            }
            tries += 1;
            if (maxPlacementAttempts > 0 && tries > maxPlacementAttempts) {
                break;
            }
        }

        return false;
    }

    public List<CustomReceptacleSpawnPoint> ProcessCustomReceptableSpawnPoint(List<ReceptacleSpawnPoint> rrsps, SimObjPhysicsExtended sopE) {
        /*
         This function filter the ReceptableSpawnPoint according to different rules:
        1.room type: room shape
        2.attraction:
            2.1.border rule: whether the furniture should stay close to/far from border/corner/wall: e.g. sofa <-> wall
            2.2.grouping rule: what other furniture does the furniture want to group to chair -> desk
        3.repulsion:
            avoid crowded funiture groups. e.g dining table <-far way-> sofa
         */

        //separate room first
        List<ReceptacleSpawnPoint> rsps;
        if (this.furniturePool.roomLayoutType == CRoomLayoutType.Rectangle) {

            List<List<ReceptacleSpawnPoint>> rspsList = SeparateRoomReceptacleSpawnPoint(rrsps);
            int roomPart = furniturePool.furnitureItemRules[(int)sopE.ObjType - 1].roomSection;
            Debug.Log("ProcessCustomReceptableSpawnPoint enter room separation" + " " + roomPart);
            rsps = rspsList[roomPart];
        } else if (this.furniturePool.roomLayoutType == CRoomLayoutType.TwoRoom) {

            List<List<ReceptacleSpawnPoint>> rspsList = SeparateRoomReceptacleSpawnPoint(rrsps, proportion: 2.0f);
            int roomPart = furniturePool.furnitureItemRules[(int)sopE.ObjType - 1].roomSection;
            Debug.Log("ProcessCustomReceptableSpawnPoint enter room separation" + " " + roomPart);
            rsps = rspsList[roomPart];
        } else {
            rsps = rrsps;
        }

        //get rule
        List<CustomReceptacleSpawnPoint> crsps;
        string bRule = furniturePool.furnitureItemRules[(int)sopE.ObjType - 1].borderRule;
        if (bRule == "center" || bRule == "corner" || bRule == "border") {
            crsps = WallRuleReceptableSpawnPoint(rsps, bRule);
            CustomReceptacleSpawnPoint.ShuffleFrom(crsps, bRule, randomSeed++);
        } else if (bRule == "on the wall") {
            crsps = OnTheWallReceptableSpawnPoint(rsps);
            CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "random", randomSeed++);
        }

          //grouping rule
          else if (furniturePool.furnitureItemRules[(int)sopE.ObjType - 1].groupToObjType != SimObjType.Undefined) {
            SimObjType gp = furniturePool.furnitureItemRules[(int)sopE.ObjType - 1].groupToObjType;
            SimObjPhysics found_gp_sop = null;
            foreach (SimObjPhysics everySop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if (everySop.ObjType == gp && everySop.BoundingBox != null) {
                    found_gp_sop = everySop;
                    break;
                }
            }
            if (found_gp_sop != null) {
                Debug.Log("found group 2 obj: ");
                crsps = GroupRuleReceptableSpawnPoint(rsps, found_gp_sop);
                CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "border", randomSeed);
            } else {
                crsps = new List<CustomReceptacleSpawnPoint>();
                foreach (ReceptacleSpawnPoint rsp in rsps) {
                    float rotationRad = UnityEngine.Random.Range(0f, 2 * Mathf.PI);
                    crsps.Add(new CustomReceptacleSpawnPoint(rsp, rotationRad, 0));
                    CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "random", randomSeed);
                }
            }
        } else {
            crsps = new List<CustomReceptacleSpawnPoint>();
            foreach (ReceptacleSpawnPoint rsp in rsps) {
                float rotationRad = UnityEngine.Random.Range(0f, 2 * Mathf.PI);
                crsps.Add(new CustomReceptacleSpawnPoint(rsp, rotationRad, 0));
                CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "random", randomSeed);
            }
        }

        Debug.Log("ProcessCustomReceptableSpawnPoint before filter crsps count: " + crsps.Count);

        //json rule
        JsonRuleReceptacleSpawnPoint(crsps, sopE);
        //RepulsionRuleReceptacleSpawnPoint(crsps, sopE);

        //Modify rotation rule
        ModifyRotationRuleReceptacleSpawnPoint(crsps, sopE);

        return crsps;
    }

    public void ModifyRotationRuleReceptacleSpawnPoint(List<CustomReceptacleSpawnPoint> crsps, SimObjPhysicsExtended sopE) {
        /*
         Modify the rotation of a furniture to the right direction
         */
        if (CRoomRestrictions.SOPRotationOffset.ContainsKey(sopE.ObjType)) {
            float offsetRad = CRoomRestrictions.SOPRotationOffset[sopE.ObjType];
            foreach (CustomReceptacleSpawnPoint crsp in crsps) {
                crsp.rotationRad += offsetRad;
            }
        } else {
            //modify fridge
            if (sopE.ObjType == SimObjType.Fridge) {
                float offsetRad = CKitchenPlacerTool.GetFridgeRotateOffset(sopE);
                foreach (CustomReceptacleSpawnPoint crsp in crsps) {
                    crsp.rotationRad += offsetRad;
                }
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
        string groupObjName = sopE.groupObjName[0];
        if (!String.IsNullOrEmpty(groupObjName)) {

            GameObject groupObj = GameObject.Find(groupObjName);

            if (groupObj == null) {
                Debug.LogWarning("JsonRuleReceptacleSpawnPoint can not find group obj: " + sopE.simObjName + " -> " + sopE.groupObjName);
                crsps.Clear();
                return;
            }

            SimObjType groupObjType = groupObj.GetComponent<SimObjPhysics>().ObjType;

            Debug.Log("JsonRuleReceptacleSpawnPoint !find! group obj: " + sopE.simObjName + " -> " + sopE.groupObjName + " : " + sopE.directionDesc);

            if (sopE.directionDesc[0] == "besides") {

                foreach (CustomReceptacleSpawnPoint crsp in crsps) {
                    float dist = Vector3.Distance(crsp.Point, groupObj.transform.position);

                    (SimObjType, SimObjType) pairType = (sopE.ObjType, groupObjType);
                    float pairWeight = 1.0f;
                    if (CLearningParams.PairType2WeightBesides.ContainsKey(pairType)) {
                        pairWeight = CLearningParams.PairType2WeightBesides[pairType];
                    }
                    crsp.sampleScore = Mathf.Pow(crsp.sampleScore, 2) + pairWeight * Mathf.Pow(dist, 2);

                }
                CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "border", randomSeed);
            } else if (sopE.directionDesc[0] == "face") {
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
            } else if (sopE.directionDesc[0] == "away") {
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

    public List<CustomReceptacleSpawnPoint> RepulsionRuleReceptacleSpawnPoint(List<CustomReceptacleSpawnPoint> crsps, SimObjPhysicsExtended sopE) {
        //avoid crowded e.g. sofa/dining table
        List<CustomReceptacleSpawnPoint> filteredCrsps = crsps;

        //if contain subject type
        if (CRoomRestrictions.FurnitureAvoidFurnitures.ContainsKey(sopE.ObjType)) {
            foreach (SimObjType awayFromType in CRoomRestrictions.FurnitureAvoidFurnitures[sopE.ObjType]) {
                SimObjPhysics[] allObjs = GameObject.FindObjectsOfType<SimObjPhysics>();
                foreach (SimObjPhysics objPhysics in allObjs) {
                    if (objPhysics.ObjType == awayFromType) {
                        List<CustomReceptacleSpawnPoint> tempCrsps = new List<CustomReceptacleSpawnPoint>();

                        BoxCollider oabb = objPhysics.BoundingBox.GetComponent<BoxCollider>();
                        oabb.enabled = true;

                        //filter out closest points
                        foreach (CustomReceptacleSpawnPoint crsp in filteredCrsps) {
                            Vector3 p = crsp.Point;
                            Vector3 closePointOnBox = oabb.ClosestPoint(p);

                            //if for enough put it in
                            if (Vector3.Distance(p, closePointOnBox) > 1.6f) {
                                tempCrsps.Add(crsp);
                            }
                        }

                        filteredCrsps = tempCrsps;
                    }
                }
            }
        }


        return filteredCrsps;
    }

    public List<CustomReceptacleSpawnPoint> GroupRuleReceptableSpawnPoint(List<ReceptacleSpawnPoint> rsps, SimObjPhysics sop) {
#if UNITY_EDITOR
        //GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
#endif
        List<CustomReceptacleSpawnPoint> customPoints = new List<CustomReceptacleSpawnPoint>();
        Debug.Log("GroupRuleReceptableSpawnPoint: group 2 obj " + sop.gameObject.name);
        BoxCollider oabb = sop.BoundingBox.GetComponent<BoxCollider>();
        Vector3 oabbCenter = oabb.transform.TransformPoint(oabb.center);

        foreach (ReceptacleSpawnPoint rsp in rsps) {
            float distance = Vector3.Distance(rsp.Point, oabbCenter);
            float rotationRad = Mathf.Atan2(oabbCenter.z - rsp.Point.z, oabbCenter.x - rsp.Point.x);
            var crsp = new CustomReceptacleSpawnPoint(rsp, rotationRad, distance);
            customPoints.Add(crsp);
            //Debug.Log("GroupRuleReceptableSpawnPoint: " + oabbCenter.ToString() + " " + rotationRad);
#if UNITY_EDITOR
            //GameObject go = Instantiate(_road_sign, crsp.Point, Quaternion.Euler(0, crsp.rotationRad * Mathf.Rad2Deg, 0));
            //go.transform.parent = this.transform;
#endif
        }

        return customPoints;
    }

    public List<List<ReceptacleSpawnPoint>> SeparateRoomReceptacleSpawnPoint(List<ReceptacleSpawnPoint> rsps, float proportion = 3f) {
        //separate receptacle spanws points into two parts
        if (furniturePool.cornrerList.Count == 0) {
            FindCornerAndBorder(rsps);
        }
        float length = Vector2.Distance(furniturePool.cornrerList[0], furniturePool.cornrerList[1]);
        float width = Vector2.Distance(furniturePool.cornrerList[1], furniturePool.cornrerList[2]);

        List<ReceptacleSpawnPoint> rsps1 = new List<ReceptacleSpawnPoint>();
        List<ReceptacleSpawnPoint> rsps2 = new List<ReceptacleSpawnPoint>();
        Vector2 cutPoint;
        Vector2 corner1; // corner2;

        float proportionA = proportion - 1.0f;
        float proportionB = 1.0f;

        UnityEngine.Random.InitState(randomSeed++);
        //swap room proportion
        if (UnityEngine.Random.Range(0f, 1f) < 0.5f) {
            float temp = proportionB;
            proportionB = proportionA;
            proportionA = temp;
        }

        if (length > width) {

            cutPoint = 1.0f / proportion * (proportionA * furniturePool.cornrerList[0] + proportionB * furniturePool.cornrerList[1]);
            corner1 = furniturePool.cornrerList[0];
            //corner2 = cornrerList[1];
        } else {
            cutPoint = 1.0f / proportion * (proportionA * furniturePool.cornrerList[1] + proportionB * furniturePool.cornrerList[2]);
            corner1 = furniturePool.cornrerList[1];
            //corner2 = cornrerList[2];
        }
        if (cutPoint.x == corner1.x) {
            //seprate on y
            foreach (var rsp in rsps) {
                if (rsp.Point.z > cutPoint.y) {
                    rsps1.Add(rsp);
                } else {
                    rsps2.Add(rsp);
                }
            }
        } else // (cutPoint.y == cornrerList[0].y)
          {
            //seprate on x
            foreach (var rsp in rsps) {
                if (rsp.Point.x > cutPoint.x) {
                    rsps1.Add(rsp);
                } else {
                    rsps2.Add(rsp);
                }
            }
        }

        //Debug.Log("targetReceptacleSpawnPoints Separated: " + rsps1.Count + " " + rsps2.Count);
        //GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));

        //        foreach (var rsp in rsps1)
        //        {
        //            var p = rsp.Point;
        //#if UNITY_EDITOR
        //            GameObject go = Instantiate(_road_sign, p, Quaternion.identity);
        //            go.transform.parent = this.transform;
        //#endif
        //        }

        //make the larger area as default
        if (rsps1.Count > rsps2.Count)
            return new List<List<ReceptacleSpawnPoint>>() { rsps1, rsps2 };
        else
            return new List<List<ReceptacleSpawnPoint>>() { rsps2, rsps1 };
    }

    public void FindCornerAndBorder(List<ReceptacleSpawnPoint> rsps) {
        //find the borders and corners among points
        List<Vector2> points2d = new List<Vector2>();
        foreach (ReceptacleSpawnPoint rsp in rsps) {
            Vector2 p = new Vector2(rsp.Point.x, rsp.Point.z);
            points2d.Add(p);
        }
        points2d = points2d.Distinct().ToList();
        IList<Vector2> cornerPoints = Geometry.ComputeConvexHull(points2d);

        furniturePool.cornrerList = new List<Vector2>();
        foreach (Vector2 corner in cornerPoints) {
            furniturePool.cornrerList.Add(new Vector2(corner.x, corner.y));
        }

        furniturePool.borderList = Geometry.BorderPoints(points2d, cornerPoints);
    }

    public List<CustomReceptacleSpawnPoint> OnTheWallReceptableSpawnPoint(List<ReceptacleSpawnPoint> rsps) {
        List<CustomReceptacleSpawnPoint> customPoints = new List<CustomReceptacleSpawnPoint>();
        if (furniturePool.cornrerList.Count == 0) {
            FindCornerAndBorder(rsps);
        }

        foreach (ReceptacleSpawnPoint rsp in rsps) {
            Vector2 p = new Vector2(rsp.Point.x, rsp.Point.z);
            bool isOnBorder = false;
            foreach (Vector2 borderPoint in furniturePool.borderList) {
                if (p.x == borderPoint.x && p.y == borderPoint.y) {
                    isOnBorder = true;
                    break;
                }
            }

            if (!isOnBorder) {
                float[] dr = Geometry.DistanceAndRotationFromBorder(p, furniturePool.borderList);
                //Debug.Log("point: " + rsp.Point.ToString() + dr[0] + " " + dr[1]);
#if UNITY_EDITOR
                //GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
                //GameObject go = Instantiate(_road_sign, new Vector3(p.x, 0, p.y), Quaternion.Euler(0, -dr[1] / Mathf.PI * 180, 0));
                //go.transform.parent = this.transform;
#endif
                CustomReceptacleSpawnPoint crsp = new CustomReceptacleSpawnPoint(rsp, dr[1], dr[0]);
                customPoints.Add(crsp);
            }
        }

        //Debug.Log("On the wall!!!!!!!!!!!!!!!!!!!!!!!customPoints: " + customPoints.Count + " border " + this.borderList.Count);
        List<CustomReceptacleSpawnPoint> filteredPoints = new List<CustomReceptacleSpawnPoint>();
        foreach (ReceptacleSpawnPoint rsp in rsps) {
            Vector2 p = new Vector2(rsp.Point.x, rsp.Point.z);
            //Debug.Log("p: " + p.ToString());
            bool isOnBorder = false;
            foreach (Vector2 bp in furniturePool.borderList) {
                //Debug.Log("777 border poin:" + bp.ToString());
                if (p.Equals(bp)) {
                    //Debug.Log("border point is ??? " + p.ToString());
                    isOnBorder = true;
                    break;
                }
            }

            if (isOnBorder) {
                float minDistance = 100f;
                CustomReceptacleSpawnPoint minDistCrsp = new CustomReceptacleSpawnPoint();
                foreach (var crsp in customPoints) {
                    Vector2 innerPoint = new Vector2(crsp.Point.x, crsp.Point.z);
                    float distance = Vector2.Distance(innerPoint, p);
                    if (distance < minDistance) {
                        minDistance = distance;
                        minDistCrsp = crsp;
                    }
                }

                rsp.Point.y = 1.5f; // give a height
                CustomReceptacleSpawnPoint borderCrsp = new CustomReceptacleSpawnPoint(rsp, minDistCrsp.rotationRad, minDistCrsp.sampleScore);
                filteredPoints.Add(borderCrsp);
#if UNITY_EDITOR
                //GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
                //GameObject go = Instantiate(_road_sign, new Vector3(p.x, 0, p.y), Quaternion.Euler(0, minDistCrsp.rotationRad / Mathf.PI * 180, 0));
                //go.transform.parent = this.transform;
#endif
            }
        }
        //Debug.Log("On the wall!!!!!!!!!!!!!!!!!!!!!!!" + filteredPoints.Count);
        return filteredPoints;
    }

    public List<CustomReceptacleSpawnPoint> WallRuleReceptableSpawnPoint(List<ReceptacleSpawnPoint> rsps, string subRule = "border", bool global = true) {
        /*
         Rules to get attraction and replusion from the wall.
        subrule:push to corner of border
        global: calculate border globally or from given rsps
         */
#if UNITY_EDITOR
        //GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
#endif

        List<CustomReceptacleSpawnPoint> customPoints = new List<CustomReceptacleSpawnPoint>();

        List<Vector2> localCornerList;
        List<Vector2> localBorderList;
        if (global) {
            if (furniturePool.cornrerList.Count == 0) {
                FindCornerAndBorder(rsps);
            }
            localCornerList = furniturePool.cornrerList;
            localBorderList = furniturePool.borderList;
        } else {
            //find rsps corners and borders from given rsps
            List<Vector2> points2d = new List<Vector2>();
            foreach (ReceptacleSpawnPoint rsp in rsps) {
                Vector2 p = new Vector2(rsp.Point.x, rsp.Point.z);
                points2d.Add(p);
            }
            points2d = points2d.Distinct().ToList();
            IList<Vector2> cornerPoints = Geometry.ComputeConvexHull(points2d);

            localCornerList = new List<Vector2>();
            foreach (Vector2 corner in cornerPoints) {
                furniturePool.cornrerList.Add(new Vector2(corner.x, corner.y));
            }

            localBorderList = Geometry.BorderPoints(points2d, cornerPoints);
        }
        if (subRule == "corner") {
            //Debug.Log("----------corner rule-------" + localCornerList.Count);
            foreach (ReceptacleSpawnPoint rsp in rsps) {
                Vector2 p = new Vector2(rsp.Point.x, rsp.Point.z);

                float[] dr = Geometry.DistanceAndRotationFromBorder(p, localCornerList);
                //Debug.Log("point: " + rsp.Point.ToString() + " " + dr[0] + " " + dr[1]);
#if UNITY_EDITOR
                //GameObject go = Instantiate(_road_sign, new Vector3(p.x, 0, p.y), Quaternion.Euler(0, -dr[1] / Mathf.PI * 180,0));
                //go.transform.parent = this.transform;
#endif
                CustomReceptacleSpawnPoint crsp = new CustomReceptacleSpawnPoint(rsp, dr[1], dr[0]);
                customPoints.Add(crsp);
            }
            //Debug.LogError("corner stop");
        } else {
            //Debug.Log("----------border or center-------" + localBorderList.Count);
            //        foreach (var p in borderPoints)
            //        {
            //            //Debug.Log("GetBorder " + p);
            //#if UNITY_EDITOR
            //            //GameObject go = Instantiate(_road_sign, new Vector3(p.x, 0, p.y), Quaternion.identity);
            //            //go.transform.parent = this.transform;
            //#endif
            //        }

            foreach (ReceptacleSpawnPoint rsp in rsps) {
                Vector2 p = new Vector2(rsp.Point.x, rsp.Point.z);
                float[] dr = Geometry.DistanceAndRotationFromBorder(p, localBorderList);
                //Debug.Log("point: " + rsp.Point.ToString() + dr[0] + " " + dr[1]);
#if UNITY_EDITOR
                //GameObject go = Instantiate(_road_sign, new Vector3(p.x, 0, p.y), Quaternion.Euler(0, -dr[1] / Mathf.PI * 180,0));
                //go.transform.parent = this.transform;
#endif
                if (subRule == "center") {
                    //Debug.Log("WallRuleReceptableSpawnPoint: is center");
                    //randomSeed
                    UnityEngine.Random.InitState(randomSeed++);
                    dr[0] = (int)UnityEngine.Random.Range(0f, 4f) * Mathf.PI;
                }
                CustomReceptacleSpawnPoint crsp = new CustomReceptacleSpawnPoint(rsp, dr[1], dr[0]);
                customPoints.Add(crsp);
            }
        }
        return customPoints;
    }


    public bool CustomPlaceFurniture(SimObjPhysics sop, CustomReceptacleSpawnPoint crsp, bool PlaceStationary, int degreeIncrement, bool alwaysPlaceUpright) {
        /*
         Main function to place furniture/objects: only check collision
         */
        if (crsp.ParentSimObjPhys == sop) {
#if UNITY_EDITOR
            Debug.Log("Can't place object inside itself!");
#endif
            return false;
        }

        //if (sop == null) {
        //    Debug.Log("Can't access to the object because it has been destroyed");
        //    return false;
        //}

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

        quat = new InstantiatePrefabTest.RotationAndDistanceValues(DistanceFromBoxBottomTosop,
            Quaternion.Euler(0, -crsp.rotationRad * Mathf.Rad2Deg + 90, 0));

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
            1 << 8 | 1 << 10 //simObjVisible, agent
                             //QueryTriggerInteraction.Collide
        );

        bool noCollison = hitColliders.Length == 0;
        //oabb.enabled = false;
        //Debug.Log(sop.Type + " no collision??????? " + noCollison + " pos " + bbCenterTransformPoint + " size " + bbcol.size / 2.0f);

        if (noCollison) {
            //Debug.Log("Enter spawn if ");
            //translate position of the target sim object to the rsp.Point and offset in local y up
            sop.transform.position = crsp.Point + crsp.ReceptacleBox.transform.up * (quat.distance + yoffset);//rsp.Point + sop.transform.up * DistanceFromBottomOfBoxToTransform;
            sop.transform.rotation = quat.rotation;

            //set true if we want objects to be stationary when placed. (if placed on uneven surface, object remains stationary)
            //if false, once placed the object will resolve with physics (if placed on uneven surface object might slide or roll)
            if (PlaceStationary == true) {
                //make object being placed kinematic true
                sop.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                sop.GetComponent<Rigidbody>().isKinematic = true;

                //check if the parent sim object is one that moves like a drawer - and would require this to be parented
                //if(rsp.ParentSimObjPhys.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanOpen))
                sop.transform.SetParent(crsp.ParentSimObjPhys.transform);

                ////if this object is a receptacle and it has other objects inside it, drop them all together
                //if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                //    PhysicsRemoteFPSAgentController agent = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();
                //    agent.DropContainedObjectsStationary(sop); //use stationary version so that colliders are turned back on, but kinematics remain true
                //}

                ////if the target receptacle is a pickupable receptacle, set it to kinematic true as will sence we are placing stationary
                //if (crsp.ParentSimObjPhys.PrimaryProperty == SimObjPrimaryProperty.CanPickup) {
                //    crsp.ParentSimObjPhys.GetComponent<Rigidbody>().isKinematic = true;
                //}

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
                ////if this object is a receptacle and it has other objects inside it, drop them all together
                //if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                //    PhysicsRemoteFPSAgentController agent = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();
                //    agent.DropContainedObjects(target: sop, reparentContainedObjects: true, forceKinematic: false);
                //}
            }
            sop.isInAgentHand = false;//set agent hand flag

            // #if UNITY_EDITOR
            // Debug.Log(sop.name + " succesfully spawned in " +rsp.ParentSimObjPhys.name + " at coordinate " + rsp.Point);
            // #endif

            return true;
        }

        return false;
    }


    public void RegisterRigidBody() {
        physicsSceneManager.rbsInScene = new List<Rigidbody>(FindObjectsOfType<Rigidbody>());
    }

    //  furniture to the border if necessary
    public void PushObjectToBorder(SimObjPhysics sop) {
        Quaternion rotation = sop.gameObject.transform.rotation;
        Vector3 testDir = Vector3.right;//sop.gameObject.transform.parent.InverseTransformDirection(-sop.gameObject.transform.forward);
        Vector3 testDir2 = Vector3.back;//sop.gameObject.transform.parent.InverseTransformDirection(sop.gameObject.transform.right);
        //Vector3 testDir3 = sop.gameObject.transform.parent.InverseTransformDirection(sop.gameObject.transform.forward);

        Vector3 testDirR = rotation * testDir;
        Vector3 testDirR2 = rotation * testDir2;

        Debug.Log("Test direction: " + testDirR + " ---- " + testDirR2);

        Vector3 pushDirection = testDirR;
        BoxCollider oabb = sop.BoundingBox.GetComponent<BoxCollider>();
        oabb.enabled = true;
        Vector3 aim = oabb.transform.TransformPoint(oabb.center) + 10 * pushDirection;
        Vector3 closePointOnBox = oabb.ClosestPoint(aim);
#if UNITY_EDITOR
        GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
        Instantiate(_road_sign, closePointOnBox, Quaternion.identity);
        Instantiate(_road_sign, aim, Quaternion.identity);

        Vector3 rayOrigin = closePointOnBox + 0.01f * pushDirection;
        Ray ray = new Ray(rayOrigin, pushDirection);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            Vector3 targetPos = hit.point;
            Instantiate(_road_sign, targetPos, Quaternion.identity);
            Debug.Log("Raycast hit " + hit.collider.gameObject.name + " position " + targetPos);
            sop.transform.gameObject.transform.position += (targetPos - rayOrigin);
        }
#endif
        oabb.enabled = false;
    }

    public void SetFloorAndScaleDoor(bool largerScale = false, float structureScale = 1.5f) {
        /*
         scale room size and keep door size
         */
        //Set floor
        GameObject structAttr = GameObject.Find("StructureAttr");
        if (structAttr != null) {
            //
            foreach (Transform child in structAttr.transform) {
                SimObjPhysics simObjPhysics = child.GetComponent<SimObjPhysics>();
                if (simObjPhysics != null && simObjPhysics.ObjType == SimObjType.Floor) {
                    CFurniturePool furniturePool = this.gameObject.GetComponent<CFurniturePool>();
                    furniturePool.floorPhysics = simObjPhysics;

                    GameObject floor = simObjPhysics.gameObject;
                    foreach (Transform t in floor.transform) {
                        Contains contains = t.GetComponent<Contains>();
                        if (contains != null) {
                            contains.myParent = floor;
                            //Debug.Log("CFurniturePlacer: Successfully placed set floor contains");
                        }
                    }
                    break;
                }
            }
        }

        //Scale door box to avoid collision:
        var gameObjects = GameObject.FindGameObjectsWithTag("Structure");

        for (var i = 0; i < gameObjects.Length; i++) {
            if (gameObjects[i].name.Contains("Door")) {

                BoxCollider bc = gameObjects[i].GetComponent<BoxCollider>();
                if (bc != null) {
                    door2BoxSize.Add(gameObjects[i], bc.size);
                    if (bc.size.x < bc.size.z) {
                        bc.size = new Vector3(bc.size.x + 1.0f, bc.size.y, bc.size.z); //don't block the door
                    } else {
                        bc.size = new Vector3(bc.size.x, bc.size.y, bc.size.z + 1.0f); //don't block the door
                    }
                    Debug.Log("Door found and changed size " + gameObjects[i].name);
                }
            }
        }

        //larger room
        if (largerScale) {
            Material wallMaterial = null;
            for (var i = 0; i < gameObjects.Length; i++) {
                if (gameObjects[i].name.Contains("Wall")) {
                    wallMaterial = gameObjects[i].GetComponent<Renderer>().sharedMaterial;
                }
            }

            GameObject structure = GameObject.Find("Structure");
            foreach (KeyValuePair<GameObject, Vector3> kvp in door2BoxSize) {
                GameObject door = kvp.Key;
                //door.GetComponent<BoxCollider>().size = kvp.Value;

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = door.transform.position + Vector3.up * kvp.Value.y * 0.5f;
                //cube.transform.localScale = kvp.Value;
                cube.transform.parent = structure.transform;

                if (wallMaterial != null) {
                    cube.GetComponent<Renderer>().material = wallMaterial;
                }

                door.transform.localScale = new Vector3(1f / structureScale, 1f, 1f / structureScale);

                if (kvp.Value.x > kvp.Value.z) {
                    cube.transform.localScale = new Vector3(kvp.Value.x, kvp.Value.y, 0.01f);
                } else {
                    cube.transform.localScale = new Vector3(0.01f, kvp.Value.y, kvp.Value.z);
                }
            }


            structure.transform.localScale = new Vector3(structureScale, 1f, structureScale);



        }
    }

    public void RestoreDoorSize() {
        foreach (KeyValuePair<GameObject, Vector3> kvp in door2BoxSize) {
            GameObject door = kvp.Key;
            door.GetComponent<BoxCollider>().size = kvp.Value;
        }
    }

#if UNITY_EDITOR
    public void LoadFurniturePrefab(bool loadCommonObject = true, bool canDuplicate = false) {
        //load pool
        Debug.Log("LoadFurniturePrefab");
        CFurniturePool furnturePool = this.gameObject.GetComponent<CFurniturePool>();
        furnturePool.furnitureList.Clear();
        CRoomType roomType = furnturePool.roomType;

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

        if (loadCommonObject) {
            string dirPathC1 = "Assets/Physics/SimObjsPhysics/Common Objects";
            LoadPrefabPaths(dirPathC1);

            //string dirPath2 = "Assets/Physics/SimObjsPhysics/Custom Project Objects";
            //LoadPrefabPaths(dirPath2);

            string dirPathC2 = "Assets/Physics/SimObjsPhysics/Miscellaneous Objects";
            LoadPrefabPaths(dirPathC2);

        }


        //if (roomType == CRoomType.LivingRoom || roomType == CRoomType.Bedroom) {
            string dirPath = "Assets/Physics/SimObjsPhysics/Living Room Objects";
            LoadPrefabPaths(dirPath);

            string dirPath2 = "Assets/Physics/SimObjsPhysics/Living Room and Bedroom Objects";
            LoadPrefabPaths(dirPath2);

            string dirPath3 = "Assets/Physics/SimObjsPhysics/Bedroom Objects";
            LoadPrefabPaths(dirPath3);


        //} else if (roomType == CRoomType.Kitchen) {
            string dirPath4 = "Assets/Physics/SimObjsPhysics/Kitchen Objects";
            LoadPrefabPaths(dirPath4);
        //} else {
            //bathroom
            string dirPath5 = "Assets/Physics/SimObjsPhysics/Bathroom Objects";
            LoadPrefabPaths(dirPath5);
        //}

        //load rule
        string csvPath = "Assets/Custom/Rules/FurnitureObjectRule.csv";
        furnturePool.furnitureItemRules = CFurnitureItemRule.GetItemRule(csvPath);

        //shuffle list
        System.Random rng = new System.Random(furnturePool.randomSeed++);
        prefabPathList.Shuffle_(rng);

        //put prefab into furniture pool

        foreach (string prefabPath in prefabPathList) {
            GameObject furniPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));

            if (!canDuplicate) {

                SimObjPhysics sop = furniPrefab.GetComponent<SimObjPhysics>();
                if (sop == null) {
                    string[] pathParts = prefabPath.Split('/');
                    string prefabName = pathParts[pathParts.Length - 1].Split('.')[0].Split('_')[0];
                    Debug.LogWarning("bad prefab name " + " " + prefabPath + " " + prefabName);
                } else {
                    if (nonDuplicatedObjTypeList.Contains(sop.ObjType)) {
                        continue;
                    } else {
                        nonDuplicatedObjTypeList.Add(sop.ObjType);
                        furnturePool.furnitureList.Add(furniPrefab);

                    }
                }
            } else {
                furnturePool.furnitureList.Add(furniPrefab);
            }
        }

        //sort
        furnturePool.furnitureList.Sort((x, y) => string.Compare(x.name, y.name));

        //DirectoryInfo dir = new DirectoryInfo("Assets/Physics/SimObjsPhysics");
        //FileInfo[] fileInfo = dir.GetFiles();
        //foreach (var file in fileInfo)
        //    Debug.Log(file);
    }
#endif
}
