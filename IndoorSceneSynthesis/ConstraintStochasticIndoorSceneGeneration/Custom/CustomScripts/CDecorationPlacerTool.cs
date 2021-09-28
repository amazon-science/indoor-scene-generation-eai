using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CDecorationPlacerTool : MonoBehaviour
{
    public PhysicsSceneManager physicsSceneManager;
    public InstantiatePrefabTest spawner;

    public CFurniturePool furniturePool;

    public float yoffset = 0.05f;
    public int randomSeed = 0;


    public Dictionary<GameObject, Vector3> door2BoxSize = new Dictionary<GameObject, Vector3>();


    private void SetSpawner()
    {
        physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
        spawner = physicsSceneManager.gameObject.GetComponent<InstantiatePrefabTest>();
        furniturePool = this.GetComponent<CFurniturePool>();
        randomSeed = furniturePool.randomSeed; //(int)UnityEngine.Random.Range(0f, 10000f);//
    }

    public List<SimObjPhysicsExtended> GetRequiredExtendObjPhysics(CGenerationType gtype = CGenerationType.Decoration)
    {
        List<SimObjPhysicsExtended> sopEList = new List<SimObjPhysicsExtended>();
        CFurniturePool decoPool = this.gameObject.GetComponent<CFurniturePool>();

        foreach (SimObjPhysicsExtended sopE in CSceneBuilderTool.jsonRule.sopEList)
        {
            if (CRoomRestrictions.RoomObj2GenerationType[sopE.ObjType] == gtype)
            {
                Debug.Log("Loading Object " + sopE.ObjType);
                foreach (GameObject go in decoPool.furnitureList)
                {
                    Debug.Log("go.GetComponent<SimObjPhysics>().ObjType: " + go.GetComponent<SimObjPhysics>().ObjType);
                    if (go.GetComponent<SimObjPhysics>().ObjType == sopE.ObjType)
                    {
                        Debug.Log("go into");
                        GameObject initGo = Instantiate(go);
                        sopE.simObjPhysics = initGo.GetComponent<SimObjPhysics>();
                        sopE.gameObject = initGo;
                        sopEList.Add(sopE);
                        break;
                    }
                }

            }
        }
        return sopEList;
    }

    public List<SimObjPhysics> LoadSopListFromPool()
    {
        List<SimObjPhysics> sopList = new List<SimObjPhysics>();
        foreach (var go in furniturePool.furnitureList)
        {
            sopList.Add(Instantiate(go).GetComponent<SimObjPhysics>());
        }
        return sopList;
    }

    public bool CustomPlaceDecoration()
    {
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
        InstantiatePrefabTest spawner = physicsSceneManager.gameObject.GetComponent<InstantiatePrefabTest>();
        CFurniturePool furnturePoolScript = this.gameObject.GetComponent<CFurniturePool>();
        SimObjPhysics receptacleSop = furnturePoolScript.floorPhysics;
        List<ReceptacleSpawnPoint> targetReceptacleSpawnPoints = receptacleSop.ReturnMySpawnPoints(false);
        //Debug.Log("PlaceFurniture: " + targetReceptacleSpawnPoints.Count.ToString());

        List<SimObjPhysicsExtended> sopEList = GetRequiredExtendObjPhysics();
        ////Get required furniture first
        //var sopList = ;

        //LoadSopListFromPool();
        //foreach (var sop in sopList)
        //{
        //    var sopE = new SimObjPhysicsExtended(sop);
        //    sopE.gameObject = sop.gameObject;
        //    sopEList.Add(sopE);
        //}


        Debug.Log("sopEList: " + sopEList.Count.ToString());

        GameObject objPlaceParent = GameObject.Find("Objects");

        foreach (SimObjPhysicsExtended sopToPlaceInReceptacle in sopEList)
        {
            //Correct SOP
            ModifySimObjPhysics(sopToPlaceInReceptacle);

            bool successful = this.CustomPlaceDecorationReceptacle(
                targetReceptacleSpawnPoints,
                sopToPlaceInReceptacle,
                true,
                1200,
                90,
                true
            );
            Debug.Log("place successful? " + successful);
            allSuccess = allSuccess && successful;
            if (!successful)
            {
#if UNITY_EDITOR
                DestroyImmediate(sopToPlaceInReceptacle.gameObject);
#endif
            }
            else
            {
                sopToPlaceInReceptacle.gameObject.transform.parent = objPlaceParent.transform;

                //Set light switch
                if(sopToPlaceInReceptacle.ObjType == SimObjType.LightSwitch)
                {
                    SetLightSwitch(sopToPlaceInReceptacle);
                }
            }
        }
        return allSuccess;
    }

    public void ModifySimObjPhysics(SimObjPhysicsExtended sopE)
    {

        if (sopE.simObjPhysics.BoundingBox == null)
        {
            GameObject col = GameObject.Find(sopE.gameObject.name + "/Colliders/Col");
            if (col != null)
            {
                Debug.Log("ModifySimObjPhysics eeeeee" + sopE.gameObject.name);
                sopE.simObjPhysics.BoundingBox = col;
            }
        }
    }

    public bool CustomPlaceDecorationReceptacle(
        List<ReceptacleSpawnPoint> rsps,
        SimObjPhysicsExtended sopE,
        bool PlaceStationary,
        int maxPlacementAttempts,
        int degreeIncrement,
        bool AlwaysPlaceUpright = true)
    {
        Debug.Log("CustomPlaceObjectReceptacle: " + sopE.ObjType);
        List<CustomReceptacleSpawnPoint> customRsps = ProcessCustomReceptableSpawnPoint(rsps, sopE);//WallRuleReceptableSpawnPoint(rsps);


        Debug.Log("CustomPlaceFurnitureReceptacle+: " + customRsps.Count);
        int tries = 0;
        foreach (CustomReceptacleSpawnPoint p in customRsps)
        {
            if (CustomPlaceDecoration(sopE.simObjPhysics, p, PlaceStationary, degreeIncrement, AlwaysPlaceUpright))
            {
                return true;
            }
            tries += 1;
            if (maxPlacementAttempts > 0 && tries > maxPlacementAttempts)
            {
                break;
            }
        }
        return false;
    }

    public List<CustomReceptacleSpawnPoint> ProcessCustomReceptableSpawnPoint(List<ReceptacleSpawnPoint> rsps, SimObjPhysicsExtended sopE)
    {

        //get rule: only on the wall
        List<CustomReceptacleSpawnPoint> crsps = new List<CustomReceptacleSpawnPoint>();
        string bRule = furniturePool.furnitureItemRules[(int)sopE.ObjType - 1].borderRule;
        if (bRule == "on the wall")
        {
            crsps = OnTheWallReceptableSpawnPoint(rsps);
            CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "random", randomSeed++);
        }

        ModifyRuleReceptacleSpawnPoint(crsps, sopE);
        return crsps;
    }

    public void ModifyRuleReceptacleSpawnPoint(List<CustomReceptacleSpawnPoint> crsps, SimObjPhysicsExtended sopE)
    {
        if (sopE.ObjType == SimObjType.LightSwitch)
        {
            //Scale door box to avoid collision:
            var structureObj = GameObject.FindGameObjectsWithTag("Structure");
            List<Vector3> doorPositions = new List<Vector3>();

            for (var i = 0; i < structureObj.Length; i++)
            {
                if (structureObj[i].name.Contains("Door"))
                {
                    doorPositions.Add(structureObj[i].transform.position);
                }
            }

            foreach(CustomReceptacleSpawnPoint crsp in crsps)
            {
                crsp.Point.y = 1.0f;
                float min_dist = 100f;
                foreach(Vector3 doorPos in doorPositions)
                {
                    float dist = Vector3.Distance(crsp.Point, doorPos);
                    if(min_dist > dist)
                    {
                        min_dist = dist;
                    }
                }
                crsp.sampleScore = CLearningParams.PairType2WeightBesides[(SimObjType.LightSwitch, SimObjType.Undefined)]
                    * Mathf.Pow(min_dist,2);
            }

            CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "border", randomSeed++);
        }

        else if (sopE.ObjType == SimObjType.Painting)
        {
            //Scale door box to avoid collision:
            var simObjs = GameObject.FindGameObjectsWithTag("SimObjPhysics");
            List<Vector3> windowPositions = new List<Vector3>();

            for (var i = 0; i < simObjs.Length; i++)
            {
                SimObjPhysics groupSop = simObjs[i].GetComponent<SimObjPhysics>();
                if (groupSop != null && groupSop.ObjType == SimObjType.Window)
                {
                    windowPositions.Add(simObjs[i].transform.position);
                }
                    
            }

            foreach (CustomReceptacleSpawnPoint crsp in crsps)
            {
                crsp.Point.y = 1.5f;
                float min_dist = 100f;
                foreach (Vector3 doorPos in windowPositions)
                {
                    float dist = Vector3.Distance(crsp.Point, doorPos);
                    if (min_dist > dist)
                    {
                        min_dist = dist;
                    }
                }
                crsp.sampleScore = CLearningParams.PairType2WeightAway[(SimObjType.Painting, SimObjType.Window)]
                    * Mathf.Pow(1 / (min_dist + 0.01f), 2);
            }

            CustomReceptacleSpawnPoint.ShuffleFrom(crsps, "border", randomSeed++);
        }

    }



    public void FindCornerAndBorder(List<ReceptacleSpawnPoint> rsps)
    {
        //find the borders and corners among points
        List<Vector2> points2d = new List<Vector2>();
        foreach (ReceptacleSpawnPoint rsp in rsps)
        {
            Vector2 p = new Vector2(rsp.Point.x, rsp.Point.z);
            points2d.Add(p);
        }
        points2d = points2d.Distinct().ToList();
        IList<Vector2> cornerPoints = Geometry.ComputeConvexHull(points2d);

        furniturePool.cornrerList = new List<Vector2>();
        foreach (Vector2 corner in cornerPoints)
        {
            furniturePool.cornrerList.Add(new Vector2(corner.x, corner.y));
        }

        furniturePool.borderList = Geometry.BorderPoints(points2d, cornerPoints);
    }

    public List<CustomReceptacleSpawnPoint> OnTheWallReceptableSpawnPoint(List<ReceptacleSpawnPoint> rsps)
    {
        List<CustomReceptacleSpawnPoint> customPoints = new List<CustomReceptacleSpawnPoint>();
        if (furniturePool.cornrerList.Count == 0)
        {
            FindCornerAndBorder(rsps);
        }

        foreach (ReceptacleSpawnPoint rsp in rsps)
        {
            Vector2 p = new Vector2(rsp.Point.x, rsp.Point.z);
            bool isOnBorder = false;
            foreach (Vector2 borderPoint in furniturePool.borderList)
            {
                if (p.x == borderPoint.x && p.y == borderPoint.y)
                {
                    isOnBorder = true;
                    break;
                }
            }

            if (!isOnBorder)
            {
                float[] dr = Geometry.DistanceAndRotationFromBorder(p, furniturePool.borderList);
                //Debug.Log("point: " + rsp.Point.ToString() + dr[0] + " " + dr[1]);
                //#if UNITY_EDITOR
                //                GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
                //                GameObject go = Instantiate(_road_sign, new Vector3(p.x, 0, p.y), Quaternion.Euler(0, -dr[1] / Mathf.PI * 180, 0));
                //                go.transform.parent = this.transform;
                //#endif
                CustomReceptacleSpawnPoint crsp = new CustomReceptacleSpawnPoint(rsp, dr[1], dr[0]);
                customPoints.Add(crsp);
            }
        }

        //Debug.Log("On the wall!!!!!!!!!!!!!!!!!!!!!!!customPoints: " + customPoints.Count + " border " + this.borderList.Count);
        List<CustomReceptacleSpawnPoint> filteredPoints = new List<CustomReceptacleSpawnPoint>();
        foreach (ReceptacleSpawnPoint rsp in rsps)
        {
            Vector2 p = new Vector2(rsp.Point.x, rsp.Point.z);
            //Debug.Log("p: " + p.ToString());
            bool isOnBorder = false;
            foreach (Vector2 bp in furniturePool.borderList)
            {
                //Debug.Log("777 border poin:" + bp.ToString());
                if (p.Equals(bp))
                {
                    //Debug.Log("border point is ??? " + p.ToString());
                    isOnBorder = true;
                    break;
                }
            }

            if (isOnBorder)
            {
                float minDistance = 100f;
                CustomReceptacleSpawnPoint minDistCrsp = new CustomReceptacleSpawnPoint();
                foreach (var crsp in customPoints)
                {
                    Vector2 innerPoint = new Vector2(crsp.Point.x, crsp.Point.z);
                    float distance = Vector2.Distance(innerPoint, p);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minDistCrsp = crsp;
                    }
                }

                CustomReceptacleSpawnPoint borderCrsp = new CustomReceptacleSpawnPoint(rsp, minDistCrsp.rotationRad, minDistCrsp.sampleScore);
                filteredPoints.Add(borderCrsp);
                //#if UNITY_EDITOR
                //                GameObject _road_sign = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/Debug/road_sign.prefab", typeof(GameObject));
                //                GameObject go = Instantiate(_road_sign, new Vector3(p.x, 0, p.y), Quaternion.Euler(0, minDistCrsp.rotationRad / Mathf.PI * 180, 0));
                //                go.transform.parent = this.transform;
                //#endif
            }
        }
        //Debug.Log("On the wall!!!!!!!!!!!!!!!!!!!!!!!" + filteredPoints.Count);
        return filteredPoints;
    }

    public bool CustomPlaceDecoration(SimObjPhysics sop, CustomReceptacleSpawnPoint crsp, bool PlaceStationary, int degreeIncrement, bool alwaysPlaceUpright)
    {
        /*
         Main function to place furniture/objects: only check collision
         */
        if (crsp.ParentSimObjPhys == sop)
        {
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

        quat = new InstantiatePrefabTest.RotationAndDistanceValues(DistanceFromBoxBottomTosop,
            Quaternion.Euler(0, -crsp.rotationRad * Mathf.Rad2Deg + 90, 0));

        //Debug.Log("spawner: " + spawner.gameObject.name + " " + crsp.Point.ToString());
        //Debug.Log("quat: " + quat.distance + " " + quat.rotation + " -crsp.rotateDegree " + -crsp.rotateDegree);

        //if spawn area is clear, spawn it and return true that we spawned it
        bool noCollison = spawner.CheckSpawnArea(sop, crsp.Point + crsp.ParentSimObjPhys.transform.up * (quat.distance + this.yoffset), quat.rotation, false);

        //oabb.enabled = false;
        //Debug.Log("no collision??????? " + noCollison);
        if (noCollison)
        {
            //Debug.Log("Enter spawn if ");
            //translate position of the target sim object to the rsp.Point and offset in local y up
            sop.transform.position = crsp.Point + crsp.ReceptacleBox.transform.up * (quat.distance + yoffset);//rsp.Point + sop.transform.up * DistanceFromBottomOfBoxToTransform;
            sop.transform.rotation = quat.rotation;

            //set true if we want objects to be stationary when placed. (if placed on uneven surface, object remains stationary)
            //if false, once placed the object will resolve with physics (if placed on uneven surface object might slide or roll)
            if (PlaceStationary == true)
            {
                //make object being placed kinematic true
                sop.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                sop.GetComponent<Rigidbody>().isKinematic = true;

                //check if the parent sim object is one that moves like a drawer - and would require this to be parented
                //if(rsp.ParentSimObjPhys.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanOpen))
                sop.transform.SetParent(crsp.ParentSimObjPhys.transform);

                //if this object is a receptacle and it has other objects inside it, drop them all together
                if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle))
                {
                    PhysicsRemoteFPSAgentController agent = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();
                    agent.DropContainedObjectsStationary(sop); //use stationary version so that colliders are turned back on, but kinematics remain true
                }

                //if the target receptacle is a pickupable receptacle, set it to kinematic true as will sence we are placing stationary
                if (crsp.ParentSimObjPhys.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
                {
                    crsp.ParentSimObjPhys.GetComponent<Rigidbody>().isKinematic = true;
                }

            }

            //place stationary false, let physics drop everything too
            else
            {
                //if not placing stationary, put all objects under Objects game object
                GameObject topObject = GameObject.Find("Objects");
                //parent to the Objects transform
                sop.transform.SetParent(topObject.transform);

                Rigidbody rb = sop.GetComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                //if this object is a receptacle and it has other objects inside it, drop them all together
                if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle))
                {
                    PhysicsRemoteFPSAgentController agent = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();
                    agent.DropContainedObjects(target: sop, reparentContainedObjects: true, forceKinematic: false);
                }
            }
            sop.isInAgentHand = false;//set agent hand flag

            // #if UNITY_EDITOR
            // Debug.Log(sop.name + " succesfully spawned in " +rsp.ParentSimObjPhys.name + " at coordinate " + rsp.Point);
            // #endif

            return true;
        }

        return false;
    }

    public void SetFloor()
    {
        //Set floor
        GameObject structAttr = GameObject.Find("StructureAttr");
        if (structAttr != null)
        {
            foreach (Transform child in structAttr.transform)
            {
                SimObjPhysics simObjPhysics = child.GetComponent<SimObjPhysics>();
                if (simObjPhysics != null && simObjPhysics.ObjType == SimObjType.Floor)
                {
                    CFurniturePool furniturePool = this.gameObject.GetComponent<CFurniturePool>();
                    furniturePool.floorPhysics = simObjPhysics;

                    GameObject floor = simObjPhysics.gameObject;
                    foreach (Transform t in floor.transform)
                    {
                        Contains contains = t.GetComponent<Contains>();
                        if (contains != null)
                        {
                            contains.myParent = floor;
                            Debug.Log("CDecorationPlacer: Successfully placed set floor contains");
                        }
                    }
                    break;
                }
            }
        }
    }

    public void LoadRule()
    {
        CFurniturePool decorationPool = this.gameObject.GetComponent<CFurniturePool>();
        string csvPath = "Assets/Custom/Rules/BasicRule.csv";
        decorationPool.furnitureItemRules = CFurnitureItemRule.GetItemRule(csvPath);
    }

    public void SetLightSwitch(SimObjPhysicsExtended sopE)
    {
        /*
         Put all the environment light into light switch
         */
        CanToggleOnOff canToggleOnOff = sopE.gameObject.GetComponent<CanToggleOnOff>();
        GameObject lightObj = GameObject.Find("Lighting");
        List<GameObject> lightSources = new List<GameObject>();
        foreach(Transform child in lightObj.transform)
        {
            Light light = child.gameObject.GetComponent<Light>();
            if(light != null)
            {
                lightSources.Add(light.gameObject);
            }
        }


        canToggleOnOff.LightSources = lightSources.ToArray();
    }
}
