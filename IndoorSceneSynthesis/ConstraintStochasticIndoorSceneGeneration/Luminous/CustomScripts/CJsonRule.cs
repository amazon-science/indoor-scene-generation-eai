using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;


[System.Serializable]
public class CJsonRule
{
    public JObject jsonRule;
    public string taskDesc;
    public JToken simbotInit;
    public List<CRoomType> roomTypeOptions = new List<CRoomType>();

    public List<SimObjPhysicsExtended> sopEList = new List<SimObjPhysicsExtended>();

    public CJsonRule() { }

    public void LoadJsonRuleFromFile(string jsonFile)
    {
        roomTypeOptions.Clear();
        sopEList.Clear();
        jsonRule = JObject.Parse(File.ReadAllText(jsonFile));
        ParseJsonRule();
    }

    public void ParseJsonRule()
    {
        taskDesc = (string)jsonRule["task_desc"];
        Debug.Log("task desc " + taskDesc);

        JToken sceneRule = jsonRule["scene"];
        simbotInit = sceneRule["simbot_init"];


        foreach (var s in sceneRule["scene_type"])
        {
            var roomtt = (CRoomType)System.Enum.Parse(typeof(CRoomType), s.ToString());
            roomTypeOptions.Add(roomtt);
            //Debug.Log("ParseJsonRule: " + roomtt);
        }
        foreach (JToken itemInfo in sceneRule["required_objects"])
        {
            //Debug.Log(itemInfo);
            sopEList.Add(new SimObjPhysicsExtended(itemInfo));
        }

    }
}
