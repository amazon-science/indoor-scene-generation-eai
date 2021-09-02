using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

[System.Serializable]
public class CObjPose {

    [System.Serializable]
    public class Orotation {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class Oposition {
        public float x;
        public float y;
        public float z;
    }


    public string objectName;
    public Orotation rotation = new Orotation();
    public Oposition position = new Oposition();
}

public class CObjectPool : MonoBehaviour
{
    public int randomSeed;
    public List<GameObject> objectList = new List<GameObject>();

    [Header("Rule")]
    public CRoomType roomType;
    public List<CObjectItemRule> objectItemRules;
    public bool beInResample = false;

    [Header("Generate Objects")]
    public List<GameObject> generatedObjects = new List<GameObject>();
    public List<GameObject> jsonMentionedObjects = new List<GameObject>();

    public void GetObjectPoses(bool write2json = true) {
        List<CObjPose> objPoses = new List<CObjPose>();
        foreach(var go in generatedObjects) {
            CObjPose goPose = new CObjPose();
            Debug.Log("CObjPool" + go.name);
            goPose.objectName = go.name;
            goPose.rotation.x = go.transform.eulerAngles.x;
            goPose.rotation.y = go.transform.eulerAngles.y;
            goPose.rotation.z = go.transform.eulerAngles.z;

            goPose.position.x = go.transform.position.x;
            goPose.position.y = go.transform.position.y;
            goPose.position.z = go.transform.position.z;

            objPoses.Add(goPose);
        }
        string jsonss = JsonConvert.SerializeObject(objPoses);
        Debug.Log("json:"+ jsonss);

        if (write2json) {
            string taskDesc = CSceneBuilderTool.jsonRule.taskDesc;
            Debug.Log("task desc " + taskDesc);

            taskDesc = taskDesc.Replace("/", "]");
            string writeFilePath = this.taskPath + "/objinfo/" + taskDesc + ".json";
            Debug.Log("writeFilePath: " + writeFilePath);
            StreamWriter writer = new StreamWriter(writeFilePath);
            writer.Write(jsonss);
            writer.Close();
        }

        //string objposes_str = JsonUtility.ToJson(objPoses);
        //return json;
    }


    [Header("Current json index")]
    public string taskPath = "Assets/Custom/Json/Floorplans/FloorPlan301";
    public int taskJsonIndex;
    public bool beInTask = false;

#if UNITY_EDITOR
    public void ClearGeneratedObjects() {
        foreach (var go in generatedObjects) {
            DestroyImmediate(go);
        }
        generatedObjects.Clear();
    }

    public void RestoreObjectName(string objName) {
        foreach(var jsonObj in jsonMentionedObjects) {
            if(jsonObj.name == objName) {
                jsonObj.SetActive(true);
                jsonMentionedObjects.Remove(jsonObj);
                break;
            }
        }
    }

    public void ClearObjInJson() {
        beInResample = true;

        SimObjPhysics[] goSOPs = GameObject.FindObjectsOfType<SimObjPhysics>();
        foreach(var sop in goSOPs) {

            //filter out furniture, leave object only
            if (CRoomRestrictions.RoomObj2GenerationType[sop.ObjType] == CGenerationType.Object || CRoomRestrictions.RoomObj2GenerationType[sop.ObjType] == CGenerationType.NotSure) {

                if (CRoomRestrictions.DonotMoveObjTypes.Contains(sop.ObjType)) {
                    continue;
                }

                bool inRule = false;
                foreach (SimObjPhysicsExtended sopE in CSceneBuilderTool.jsonRule.sopEList) {
                    if (sop.ObjType == sopE.ObjType && sop.gameObject.name == sopE.simObjName) {
                        if (sopE.directionDesc.Count > 0) { 
                            inRule = true;
                        }
                    }
                }

                if (inRule) {
                    jsonMentionedObjects.Add(sop.gameObject);
                    sop.gameObject.SetActive(false);
                    //DestroyImmediate(sop.gameObject);
                }
            }

        }

        generatedObjects.Clear();
    }

    public void LoadMergedJson() {
        /*
         Load merged cdf for scene generation
         */
        CSceneBuilderTool.jsonPath = this.taskPath + "/merged.json";
        CSceneBuilderTool.LoadJsonInfo();
    }


    public void LoadNextObjJson() {
        /*
         Load json for sample objects
         */
        string folder_path = this.taskPath + "/tasks";

        DirectoryInfo prefabDir = new DirectoryInfo(folder_path);
        FileInfo[] prefabInfoList = prefabDir.GetFiles("*.json");
        //System.Random rng = new System.Random();
        //Debug.Log("Total json file num: " + prefabInfoList.Length);

        this.taskJsonIndex += 1;
        FileInfo prefabFile = prefabInfoList[this.taskJsonIndex];

        int prefabRelativepathStart = prefabFile.ToString().IndexOf(folder_path);
        string prefabPath = prefabFile.ToString().Substring(prefabRelativepathStart);
        CSceneBuilderTool.jsonPath = prefabPath;

        Debug.Log(prefabPath);
        CSceneBuilderTool.LoadJsonInfo();


    }

    public void RestoreScene() {
        /*
         Restore scene
         */
        ClearGeneratedObjects();
        foreach(var go in jsonMentionedObjects) {
            go.SetActive(true);
        }
        jsonMentionedObjects.Clear();
    }

    public void LoopOverJsonFolderToGetObjPos() {
        /*
         Loop over task folder and generate object locations
         */
        
        IEnumerator LoopGenerate() {
            /*
            Load json for sample objects
            */
            yield return null;
            string folder_path = this.taskPath + "/tasks";

            DirectoryInfo prefabDir = new DirectoryInfo(folder_path);
            FileInfo[] prefabInfoList = prefabDir.GetFiles("*.json");
            this.taskJsonIndex = -1;

            for (int i = 0; i < prefabInfoList.Length; ++i) {
                LoadNextObjJson();
                ClearObjInJson();

                CObjectPlacerTool objectPlacerTool = this.gameObject.GetComponent<CObjectPlacerTool>();
                objectPlacerTool.CustomPlaceObject();
                CSceneBuilderTool.objectPlacingFinish = false;
                while (!CSceneBuilderTool.objectPlacingFinish) {
                    yield return new EditorWaitForSeconds(1);
                }

                GetObjectPoses(write2json: true);
                RestoreScene();
            }

            beInTask = false;
            yield return null;
        }

        EditorCoroutineUtility.StartCoroutineOwnerless(LoopGenerate());
    }
#endif
}
