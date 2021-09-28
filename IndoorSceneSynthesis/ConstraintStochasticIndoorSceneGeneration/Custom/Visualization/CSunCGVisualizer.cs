using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CSunCGVisualizer : MonoBehaviour
{
    [Header("All furniture and objects")]
    public CFurniturePool furniturePool;

    [Header("json file")]
    public static string sampleJsonPath = "Assets/Custom/Visualization/3d_front_example.json";
    public static int currentJsonIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

#if UNITY_EDITOR
    public void LoadObjectPrefab(bool loadCommonObject = true, bool canDuplicate = false)
    {
        //load pool
        Debug.Log("LoadFurniturePrefab");
        CFurniturePool objPool = furniturePool;
        objPool.furnitureList.Clear();

        List<string> prefabPathList = new List<string>();
        List<SimObjType> nonDuplicatedObjTypeList = new List<SimObjType>();

        void LoadPrefabPaths(string dirPath)
        {
            //DirectoryInfo dir = new DirectoryInfo(dirPath);
            string[] fileInfoList = Directory.GetDirectories(dirPath);
            foreach (string file in fileInfoList)
            {
                string prefabDirPath = file.ToString() + "/Prefabs";
                DirectoryInfo prefabDir = new DirectoryInfo(prefabDirPath);
                try
                {
                    FileInfo[] prefabInfoList = prefabDir.GetFiles("*.prefab");
                    foreach (FileInfo prefabFile in prefabInfoList)
                    {
                        int prefabRelativepathStart = prefabFile.ToString().IndexOf(dirPath);
                        string prefabPath = prefabFile.ToString().Substring(prefabRelativepathStart);
                        Debug.Log("LoadFurniturePrefab: " + prefabPath);

                        prefabPathList.Add(prefabPath);
                    }
                }
                catch
                {
                    Debug.LogWarning("the prefab path is wrong: " + prefabDirPath);
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

        //addtional prefabs
        string dirPath_ad = "Assets/Custom/Visualization/Prefabs";
        DirectoryInfo additionalPrefabDir = new DirectoryInfo(dirPath_ad);
        FileInfo[] ad_prefabInfoList = additionalPrefabDir.GetFiles("*.prefab");
        foreach (FileInfo prefabFile in ad_prefabInfoList)
        {
            int ad_prefabRelativepathStart = prefabFile.ToString().IndexOf(dirPath_ad);
            string ad_prefabPath = prefabFile.ToString().Substring(ad_prefabRelativepathStart);
            Debug.Log("LoadFurniturePrefab: " + ad_prefabPath);

            prefabPathList.Add(ad_prefabPath);
        }
        
        //LoadPrefabPaths(dirPath_ad);

        if (loadCommonObject)
        {
            string dirPath5 = "Assets/Physics/SimObjsPhysics/Common Objects";
            LoadPrefabPaths(dirPath5);

            //string dirPath2 = "Assets/Physics/SimObjsPhysics/Custom Project Objects";
            //LoadPrefabPaths(dirPath2);

            string dirPath6 = "Assets/Physics/SimObjsPhysics/Miscellaneous Objects";
            LoadPrefabPaths(dirPath6);
        }

        //shuffle list
        System.Random rng = new System.Random(objPool.randomSeed++);
        prefabPathList.Shuffle_(rng);

        //put prefab into furniture pool

        foreach (string prefabPath in prefabPathList)
        {
            GameObject objPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));

            if (!canDuplicate)
            {
                //string[] pathParts = prefabPath.Split('/');
                //string prefabName = pathParts[pathParts.Length - 1].Split('.')[0].Split('_')[0];
                //Debug.Log("prefab name " + " " + prefabPath + " " + prefabName);
                //GameObject objPrefabGo = Instantiate(objPrefab);
                SimObjPhysics sop = objPrefab.GetComponent<SimObjPhysics>();
                StructureObject sto = objPrefab.GetComponent<StructureObject>();


                if (sop != null)
                {
                    //not a good object, continue
                    //Debug.Log("loading prefab from path: " + prefabPath + " --- " + objPrefab.name);
                    if (nonDuplicatedObjTypeList.Contains(sop.ObjType))
                    {
                        continue;
                    }
                    else
                    {
                        nonDuplicatedObjTypeList.Add(sop.ObjType);
                        objPool.furnitureList.Add(objPrefab);

                    }
                }
                else if (sto != null)
                {
                    objPool.furnitureList.Add(objPrefab);
                }


            }
            else
            {
                objPool.furnitureList.Add(objPrefab);
            }
        }

        objPool.furnitureList.Sort((x, y) => string.Compare(x.name, y.name));

        //DirectoryInfo dir = new DirectoryInfo("Assets/Physics/SimObjsPhysics");
        //FileInfo[] fileInfo = dir.GetFiles();
        //foreach (var file in fileInfo)
        //    Debug.Log(file);
    }
#endif

    public void LoadJsonRuleForSceneFormer(string jsonFile = "Assets/Custom/Visualization/sf_example.json")
    {
        JArray roomObject = JArray.Parse(File.ReadAllText(jsonFile));
        foreach(var valid_object in roomObject.Children<JObject>())
        {
            //obj type
            Debug.Log("CSUNCG: " + valid_object["type"].ToString());
            SimObjType targetType = CVisualizationParams.SceneFormer2Sop[valid_object["type"].ToString()];

            //position
            float x = float.Parse(valid_object["transform"][12].ToString());
            float y = float.Parse(valid_object["transform"][13].ToString());
            float z = float.Parse(valid_object["transform"][14].ToString());

            Vector3 center = new Vector3(x, y, z);

            //scale
            float sx = float.Parse(valid_object["width"].ToString());
            float sy = float.Parse(valid_object["height"].ToString()); 
            float sz = float.Parse(valid_object["length"].ToString());

            Vector3 scale = new Vector3(sx, sy, sz);

            //rotation
            float m00 = float.Parse(valid_object["transform"][0].ToString());
            float m01 = float.Parse(valid_object["transform"][1].ToString());
            float m02 = float.Parse(valid_object["transform"][2].ToString());
            float m10 = float.Parse(valid_object["transform"][4].ToString());
            float m11 = float.Parse(valid_object["transform"][5].ToString());
            float m12 = float.Parse(valid_object["transform"][6].ToString());
            float m20 = float.Parse(valid_object["transform"][8].ToString());
            float m21 = float.Parse(valid_object["transform"][9].ToString());
            float m22 = float.Parse(valid_object["transform"][10].ToString());

            Vector3 forward;
            forward.x = m02;
            forward.y = m12;
            forward.z = m22;

            Vector3 upwards;
            upwards.x = m01;
            upwards.y = m11;
            upwards.z = m21;

            float qw = Mathf.Sqrt(1 + m00 + m11 + m22) / 2.0f;
            float qx = -(m21 - m12) / (qw * 4);
            float qy = -(m02 - m20) / (qw * 4);
            float qz = -(m10 - m01) / (qw * 4);

            Quaternion rot = Quaternion.LookRotation(forward, upwards);  //new Quaternion(qx, qy, qz, qw);

            // find object sop
            if (targetType != SimObjType.Undefined)
            {

                Debug.Log("CSUNCG: " + targetType);
                PlaceOneObjectFromJsonInfo(targetType, center, rot, scale, false, -30);
            }

        }
    }

    public void LoadJsonRulefor3DSLN(string jsonFile, string dataSource = "3DFRONT")
    {

        JObject roomObject = JObject.Parse(File.ReadAllText(jsonFile));
        foreach(var valid_object in roomObject["valid_objects"])
        {
            //obj type
            Debug.Log(dataSource + ": " + valid_object["type"]);
            try {
                
                SimObjType targetType;
                string boxString = "new_bbox";
                if (dataSource == "SUNCG") {
                    targetType = CVisualizationParams.SLN3D2Sop[valid_object["type"].ToString()];
                } else {
                    targetType = CVisualizationParams.FRONT3D2Sop[valid_object["type"].ToString()];
                    boxString = "new_bbox";
                }

                //position
                float x1 = float.Parse(valid_object[boxString][0][0].ToString());
                float y1 = float.Parse(valid_object[boxString][0][1].ToString());
                float z1 = float.Parse(valid_object[boxString][0][2].ToString());
                float x2 = float.Parse(valid_object[boxString][1][0].ToString());
                float y2 = float.Parse(valid_object[boxString][1][1].ToString());
                float z2 = float.Parse(valid_object[boxString][1][2].ToString());

                Vector3 b1 = new Vector3(x1, y1, z1);
                Vector3 b2 = new Vector3(x2, y2, z2);

                Vector3 center = 0.5f * (b1 + b2);

                //scale
                Vector3 size = b2 - b1;

                //Debug.Log("CSUNCG: " + x1 + ";" + center.x);

                //rotation
                int rotation = int.Parse(valid_object["rotation"].ToString());

                bool change_x_z_scale = false;

                if (rotation % 12 >= 4 && rotation % 12 < 10) {
                    change_x_z_scale = true;
                }

                foreach (var entry in CVisualizationParams.RotationOffset2SuncgSimObjType) {
                    if (entry.Value.Contains(targetType)) {
                        rotation += entry.Key / 15;
                    }
                }


                Quaternion rot = Quaternion.Euler(0, -rotation * 15, 0);

                // find object sop
                if (targetType != SimObjType.Undefined) {

                    Debug.Log("CSUNCG: " + targetType);
                    PlaceOneObjectFromJsonInfo(targetType, center, rot, size, change_x_z_scale);
                }

            } catch {
                Debug.Log("no such prefab");
            }

        }
  
    }

    public void PlaceOneObjectFromJsonInfo(SimObjType targetType, Vector3 pos, Quaternion rot, Vector3 size, bool change_x_z_scale,
        int rotation_offset = 0)
    {
        Debug.Log("Loading furniture " + targetType);
        foreach (GameObject go in furniturePool.furnitureList)
        {
            SimObjPhysics sop = go.GetComponent<SimObjPhysics>();
            StructureObject sto = go.GetComponent<StructureObject>();

            if (sop != null)
            {
                if (go.GetComponent<SimObjPhysics>().ObjType == targetType)
                {
                    GameObject initGo = Instantiate(go, pos, rot);
                    BoxCollider oabb = initGo.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>();
                    oabb.enabled = true;
                    Vector3 bbox_size = oabb.bounds.size;

                    Vector3 new_scale = new Vector3(size.x / bbox_size.x, size.y / bbox_size.y, size.z / bbox_size.z);

                    if (change_x_z_scale)
                    {
                        float temp = new_scale.x;
                        new_scale.x = new_scale.z;
                        new_scale.z = temp;
                    }


                    Debug.Log("ori size: " + bbox_size + " ; new size: " + new_scale);
                    initGo.transform.localScale = new_scale;

                    if (rotation_offset != 0)
                    {
                        initGo.transform.Rotate(0f, rotation_offset, 0.0f, Space.World);
                    }

                    break;
                }
            }
        }
    }

}
