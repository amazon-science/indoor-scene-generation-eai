
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

# if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
# endif

public class CKitchenPlacerTool : MonoBehaviour {

    public CFurniturePool furniturePool;


    //face x
    public static List<string> faceXFridges = new List<string>() {
            "Fridge_1","Fridge_16","Fridge_19","Fridge_20","Fridge_22","Fridge_25","Fridge_26",
        };

    //negative z
    public static List<string> faceNXFridges = new List<string>() {
            "Fridge_5", "Fridge_8","Fridge_10","Fridge_12","Fridge_13","Fridge_18","Fridge_21","Fridge_24","Fridge_28",
        };

    //z correct
    //List<string> faceZFridges = new List<string>() {

    //};

    //negative z
    public static List<string> faceNZFridges = new List<string>() {
           "Fridge_15",
        };

    public static string FindARandomSOPType(SimObjType objType) {
        //but put all countertop into candidates;
        int objTypeCount = 0;
        foreach (SimObjPhysics soop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
            if (soop.Type == objType) {
                objTypeCount++;
                //sopE.directionDesc.Add("on");
                //sopE.groupObjName.Add("CounterTop_" + counterTopCount.ToString());
            }
        }

        if (objTypeCount == 0) {
            Debug.LogError("NO OBJ TYPE FOR KITCHEN: " + objType);
        }

        //random pick a countertop for it not pick, 
        int pickCounterTop = (int)UnityEngine.Random.Range(0f, objTypeCount) + 1;
        Debug.Log("pickCounterTop random?: " + pickCounterTop);

        string randomObjName = objType.ToString() + "_" + pickCounterTop.ToString();

        return randomObjName;
    }

    public void ReNameKitchenSceneObjs() {

        void RenamebySOP(SimObjType objType) {
            //rename coutertops in the Kitchen
            List<SimObjPhysics> requestSOPs = new List<SimObjPhysics>();

            foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if (sop.Type == objType) {
                    requestSOPs.Add(sop);
                }
            }

            //shuffle coutertops
            requestSOPs.Shuffle_(new System.Random(CSceneBuilderTool.globalRandomSeed + 1));

            //rename them
            for (int i = 0; i < requestSOPs.Count; ++i) {
                requestSOPs[i].gameObject.name = objType.ToString() + "_" + (i + 1).ToString();
            }
        }

        if (CSceneBuilderTool.samplingRoomType == CRoomType.Kitchen) {
            RenamebySOP(SimObjType.CounterTop);
            RenamebySOP(SimObjType.SinkBasin);
            RenamebySOP(SimObjType.StoveBurner);
            RenamebySOP(SimObjType.Drawer);
            RenamebySOP(SimObjType.Cabinet);
            RenamebySOP(SimObjType.Faucet);

        }

        if (CSceneBuilderTool.samplingRoomType == CRoomType.Bathroom) {
            RenamebySOP(SimObjType.CounterTop);
            RenamebySOP(SimObjType.SinkBasin);
            RenamebySOP(SimObjType.Sink);
            RenamebySOP(SimObjType.Drawer);
            RenamebySOP(SimObjType.Cabinet);
            RenamebySOP(SimObjType.Bathtub);
            RenamebySOP(SimObjType.BathtubBasin);
            RenamebySOP(SimObjType.Faucet);
        }

        ////rename sinkbasins
        //List<SimObjPhysics> sinkBasins = new List<SimObjPhysics>();

        //foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
        //    if (sop.Type == SimObjType.SinkBasin) {
        //        sinkBasins.Add(sop);
        //    }
        //}

        ////shuffle 
        //sinkBasins.Shuffle_(new System.Random(CSceneBuilderTool.globalRandomSeed));

        ////rename them
        //for (int i = 0; i < sinkBasins.Count; ++i) {
        //    sinkBasins[i].gameObject.name = "SinkBasin_" + (i + 1).ToString();
        //}



        ////rename sinkbasins
        //List<SimObjPhysics> stoveBurners = new List<SimObjPhysics>();

        //foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
        //    if (sop.Type == SimObjType.StoveBurner) {
        //        stoveBurners.Add(sop);
        //    }
        //}

        ////shuffle 
        //stoveBurners.Shuffle_(new System.Random(CSceneBuilderTool.globalRandomSeed));

        ////rename them
        //for (int i = 0; i < stoveBurners.Count; ++i) {
        //    stoveBurners[i].gameObject.name = "StoveBurner_" + (i + 1).ToString();
        //}
    }

    //----------------the same as furniture placer

    public static float GetFridgeRotateOffset(SimObjPhysicsExtended sopE) {
        /*
         The correct rotate of an object should face front, however, refridgerator models in ai2thor are problematic, some of them
        face X direction, some of their directions are reversed.
         */
        //print("Now placing fridege: " + fridgeGO.name + " pos " + fridgeGO.gameObject.transform.position);


        if (faceXFridges.Contains(sopE.prefabName)) {
            return 0.5f * Mathf.PI;
        } else if (faceNXFridges.Contains(sopE.prefabName)) {
            return 1.5f * Mathf.PI;
        } else if (faceNZFridges.Contains(sopE.prefabName)) {
            return Mathf.PI;
        }

        return 0;
    }

#if UNITY_EDITOR
    public CModifiedSimObjPhysics LoadACounterTop() {
        GameObject ct = Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/KitchenCountertops/NewStove.prefab", typeof(GameObject)));

        ct.transform.parent = this.transform;
        return ct.GetComponent<CModifiedSimObjPhysics>();
    }
    //-------Version 2--------------- countertop placing tool
    public bool CustomPlaceACounterTop() {
        CModifiedSimObjPhysics msop = LoadACounterTop();

        Debug.Log("loading countertop");

        //set receptacle spawn points
        this.furniturePool = GameObject.Find("FurniturePlacer").GetComponent<CFurniturePool>();
        SimObjPhysics receptacleSop = this.furniturePool.floorPhysics;
        List<ReceptacleSpawnPoint> targetReceptacleSpawnPoints = receptacleSop.ReturnMySpawnPoints(false);

        List<CustomReceptacleSpawnPoint> crsps = WallRuleReceptableSpawnPoint(targetReceptacleSpawnPoints);
        Debug.Log("PlaceFurniture Kitchen Countertop: " + crsps.Count.ToString());

        //        GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
        //        foreach (var rsp in crsps) {
        //            var p = rsp.Point;
        //#if UNITY_EDITOR
        //            GameObject go = Instantiate(_road_sign, p, Quaternion.identity);
        //            go.transform.parent = this.transform;
        //#endif
        //        }

        CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "random", furniturePool.randomSeed++);
        //try a number of spawnpoints in this specific receptacle up to the maxPlacementAttempts
        int tries = 0;
        foreach (CustomReceptacleSpawnPoint p in crsps) {
            if (CustomPlaceMSOP(msop, p, true, 90, true)) {
                return true;
            }
            tries += 1;
            if (tries > 3000) {
                break;
            }
        }

        return false;
    }

    public bool CustomPlaceMSOP(CModifiedSimObjPhysics msop, CustomReceptacleSpawnPoint crsp, bool PlaceStationary, int degreeIncrement, bool alwaysPlaceUpright) {
        /*
         Main function to place furniture/objects: only check collision
         */
        if (crsp.ParentSimObjPhys == msop) {
            Debug.Log("Can't place object inside itself!");
            return false;
        }
        //remember the original rotation of the sim object if we need to reset it
        //Quaternion originalRot = sop.transform.rotation;
        Vector3 originalPos = msop.gameObject.transform.position;
        Quaternion originalRot = msop.transform.rotation;

        //get the bounding box of the sim object we are trying to place
        BoxCollider oabb = msop.BoundingBox.GetComponent<BoxCollider>();
        oabb.enabled = true;

        //zero out rotation and velocity/angular velocity, then match the target receptacle's rotation
        //?????????
        msop.transform.rotation = crsp.ReceptacleBox.transform.rotation;
        Rigidbody sopRB = msop.GetComponent<Rigidbody>();
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
        DistanceFromBoxBottomTosop = BoxBottom.GetDistanceToPoint(msop.transform.position);

        quat = new InstantiatePrefabTest.RotationAndDistanceValues(DistanceFromBoxBottomTosop,
            Quaternion.Euler(0, -crsp.rotationRad * Mathf.Rad2Deg + 90, 0));

        Vector3 newPosition = crsp.Point + crsp.ParentSimObjPhys.transform.up * (quat.distance + 0.01f);
        //Debug.Log("spawner: " + spawner.gameObject.name + " " + crsp.Point.ToString());
        //Debug.Log("quat: " + quat.distance + " " + quat.rotation + " -crsp.rotateDegree " + -crsp.rotateDegree);

        //if spawn area is clear, spawn it and return true that we spawned it
        //bool noCollison = spawner.CheckSpawnArea(sop, newPosition, quat.rotation, false);

        //track colliders
        List<Collider> colsToDisable = new List<Collider>();
        //foreach (Collider g in msop.MyColliders) {
        //    //only track this collider if it's enabled by default
        //    //some objects, like houseplants, might have colliders in their simObj.MyColliders that are disabled
        //    if (g.enabled) {
        //        colsToDisable.Add(g);
        //    }
        //}

        foreach (Collider g in GetComponentsInChildren<Collider>(false)) {
            //only track this collider if it's enabled by default
            //some objects, like houseplants, might have colliders in their simObj.MyColliders that are disabled
            if (g.enabled) {
                colsToDisable.Add(g);
            }
        }


        //Debug.Log("colliders counts: " + colsToDisable.Count);

        //disable collision before moving to check the spawn area
        foreach (Collider c in colsToDisable) {
            c.enabled = false;
        }


        //move it into place so the bouding box is in the right spot to generate the overlap box later
        msop.transform.position = newPosition;
        msop.transform.localRotation = quat.rotation;

        //now let's get the BoundingBox of the simObj as reference cause we need it to create the overlapbox
        GameObject bb = msop.BoundingBox.transform.gameObject;
        //Debug.Log("sop: " + sop.Type + " layer: " + bb.layer);
        //bb.layer = 8;
        BoxCollider bbcol = bb.GetComponent<BoxCollider>();
        Vector3 bbCenter = bbcol.center;
        Vector3 bbCenterTransformPoint = bb.transform.TransformPoint(bbCenter);

        //move sim object back to it's original spot back so the overlap box doesn't hit it
        msop.transform.position = originalPos;
        msop.transform.rotation = originalRot;


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
        //Debug.Log(msop.gameObject.name + " no collision??????? " + hitColliders.Length + " pos " + bbCenterTransformPoint + " size " + bbcol.size / 2.0f);

        //if (hitColliders.Length == 1) {
        //    Debug.Log("only one collider: " + hitColliders[0].gameObject.name);
        //    GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
        //    Instantiate(_road_sign, hitColliders[0].gameObject.transform.position, Quaternion.identity);
        //}


        if (noCollison) {
            //Debug.Log("Enter spawn if ");
            //translate position of the target sim object to the rsp.Point and offset in local y up
            msop.transform.position = crsp.Point + crsp.ReceptacleBox.transform.up * (quat.distance + 0.01f);//rsp.Point + sop.transform.up * DistanceFromBottomOfBoxToTransform;
            msop.transform.rotation = quat.rotation;

            //set true if we want objects to be stationary when placed. (if placed on uneven surface, object remains stationary)
            //if false, once placed the object will resolve with physics (if placed on uneven surface object might slide or roll)
            if (PlaceStationary == true) {
                //make object being placed kinematic true
                msop.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                msop.GetComponent<Rigidbody>().isKinematic = true;

                //check if the parent sim object is one that moves like a drawer - and would require this to be parented
                //if(rsp.ParentSimObjPhys.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanOpen))
                msop.transform.SetParent(crsp.ParentSimObjPhys.transform);

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
                msop.transform.SetParent(topObject.transform);

                Rigidbody rb = msop.GetComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                ////if this object is a receptacle and it has other objects inside it, drop them all together
                //if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                //    PhysicsRemoteFPSAgentController agent = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();
                //    agent.DropContainedObjects(target: sop, reparentContainedObjects: true, forceKinematic: false);
                //}
            }
            //msop.isInAgentHand = false;//set agent hand flag

            // #if UNITY_EDITOR
            // Debug.Log(sop.name + " succesfully spawned in " +rsp.ParentSimObjPhys.name + " at coordinate " + rsp.Point);
            // #endif

            return true;
        }

        return false;
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

    public List<CustomReceptacleSpawnPoint> WallRuleReceptableSpawnPoint(List<ReceptacleSpawnPoint> rsps, string subRule = "border", bool global = true) {
        /*
         Rules to get attraction and replusion from the wall.
        subrule:push to corner of border
        global: calculate border globally or from given rsps
         */
        //GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));

        //rsps.Shuffle_();
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

                //GameObject go = Instantiate(_road_sign, new Vector3(p.x, 0, p.y), Quaternion.Euler(0, -dr[1] / Mathf.PI * 180,0));
                //go.transform.parent = this.transform;
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
                //GameObject go = Instantiate(_road_sign, new Vector3(p.x, 0, p.y), Quaternion.Euler(0, -dr[1] / Mathf.PI * 180,0));
                //go.transform.parent = this.transform;
                if (subRule == "center") {
                    //Debug.Log("WallRuleReceptableSpawnPoint: is center");
                    //randomSeed
                    UnityEngine.Random.InitState(furniturePool.randomSeed++);
                    dr[0] = (int)UnityEngine.Random.Range(0f, 4f) * Mathf.PI;
                }
                CustomReceptacleSpawnPoint crsp = new CustomReceptacleSpawnPoint(rsp, dr[1], dr[0]);
                customPoints.Add(crsp);
            }
        }

        return customPoints;
    }
#endif
}