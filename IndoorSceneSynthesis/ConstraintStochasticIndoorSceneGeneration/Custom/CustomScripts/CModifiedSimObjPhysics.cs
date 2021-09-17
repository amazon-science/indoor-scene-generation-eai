using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json.Linq;

[System.Serializable]
public class SimObjPhysicsExtended {

    public SimObjPhysics simObjPhysics;
    public CModifiedSimObjPhysics modifiedSimObjPhysics; //for kitchen and bathroom objects e.g. coutertop basin

    public SimObjType ObjType;
    public string simObjName;
    public GameObject gameObject;
    public string prefabName;

    public List<string> groupObjName = new List<string>();
    public List<string> directionDesc = new List<string>();


    public SimObjPhysicsExtended() { }

    public SimObjPhysicsExtended(SimObjPhysics sop) {
        simObjPhysics = sop;
        this.ObjType = sop.ObjType;
    }

    public SimObjPhysicsExtended(CModifiedSimObjPhysics msop) {
        modifiedSimObjPhysics = msop;
        this.ObjType = msop.Type;
    }

    public SimObjPhysicsExtended(string sopName) {
        simObjName = sopName;
        ObjType = (SimObjType)System.Enum.Parse(typeof(SimObjType), sopName.Split('_')[0]);
    }

    public SimObjPhysicsExtended(JToken itemToken) {
        string sopName = (string)itemToken["name"];
        simObjName = sopName;
        ObjType = (SimObjType)System.Enum.Parse(typeof(SimObjType), sopName.Split('_')[0]);
        //Debug.Log(sopName + " ObjType???" + ObjType);

        if (((JObject)itemToken).ContainsKey("location")) {
            foreach (var x in itemToken["location"]) {
                //JObject ox = (JObject)x;
                Dictionary<string, string> dx = x.ToObject<Dictionary<string, string>>();
                foreach (KeyValuePair<string, string> ele in dx) {
                    groupObjName.Add(ele.Key);
                    directionDesc.Add(ele.Value);

                    //Debug.Log("groupObjName: " + groupObjName + " --  " + directionDesc);

                }
            }
        }
    }

    public GameObject GetGameObject() {
        if (simObjPhysics != null) {
            return simObjPhysics.gameObject;
        } else {
            return modifiedSimObjPhysics.gameObject;
        }
    }
}


public class CModifiedSimObjPhysics : MonoBehaviour
{
    [Header("Unique String ID of this Object")]
    [SerializeField]
    public string objectID = string.Empty;

    [Header("Object Type")]
    [SerializeField]
    public SimObjType Type = SimObjType.Undefined;

    [Header("Primary Property (Must Have only 1)")]
    [SerializeField]
    public SimObjPrimaryProperty PrimaryProperty;

    [Header("Additional Properties (Can Have Multiple)")]
    [SerializeField]
    public SimObjSecondaryProperty[] SecondaryProperties;

    [Header("non Axis-Aligned Box enclosing all colliders of this object")]
    // This can be used to get the "bounds" of the object, but needs to be created manually
    // we should look into a programatic way to figure this out so we don't have to set it up for EVERY object
    // for now, only CanPickup objects have a BoundingBox, although maybe every sim object needs one for
    // spawning eventually? For now make sure the Box Collider component is disabled on this, because it's literally
    // just holding values for the center and size of the box.
    public GameObject BoundingBox = null;

    [Header("Raycast to these points to determine Visible/Interactable")]
    [SerializeField]
    public Transform[] VisibilityPoints = null;

    [Header("If this object is a Receptacle, put all trigger boxes here")]
    [SerializeField]
    public GameObject[] ReceptacleTriggerBoxes = null;

    //[Header("State information Bools here")]
    [Header("Non - Trigger Colliders of this object")]
    public Collider[] MyColliders = null;
}
