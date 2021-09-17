#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.AI;


using UnityStandardAssets.Characters.FirstPerson;

using UnityEditor.SceneManagement;
using UnityEditor;

using System.Linq;

[CustomEditor(typeof(CFurniturePlacerTool))]
public class CFurniturePlacer : Editor
{
    public CFurniturePlacerTool targetScript;
    SerializedProperty Spawner;

    private void OnEnable()
    {
        targetScript = (CFurniturePlacerTool)target;
        Spawner = serializedObject.FindProperty("spawner");
    }
    //Set up GUI on Unity Editor
    public override void OnInspectorGUI()
    {

        if (GUILayout.Button("Set Floor"))
        {
            targetScript.SetFloorAndScaleDoor(false);
        }

        //if (GUILayout.Button("Place Furniture Test"))
        //{
        //    targetScript.PlaceFurniture();
        //}

        if (GUILayout.Button("Load Furniture Prefab"))
        {
            targetScript.LoadFurniturePrefab();
        }

        if (GUILayout.Button("Custom Place Furniture"))
        {    
            targetScript.CustomPlaceFurniture();
            //targetScript.RegisterRigidBody();
        }



        if (GUILayout.Button("Load Basic Rule"))
        {
            //Debug.Log(Mathf.Atan2(0.0f, 1.0f));
            //Debug.Log(Mathf.Atan2(0.0f, -1.0f));

            //int value;

            //SimObjType parsed_enum = (SimObjType)System.Enum.Parse(typeof(SimObjType), "Apple");
            ////SimObjType sot = type;
            //Debug.Log(parsed_enum);
            //string csvPath = "Assets/Custom/FurnitureObjectRule.csv";
            //CItemRule.GetItemRule(csvPath);
            //CFurnturePool furniturePool = targetScript.GetComponent<CFurnturePool>();
            //string shuffleRule = furniturePool.allItemRules[26].borderRule;
            //Debug.Log("CustomPlaceObjectReceptacle shuffleRule: " + shuffleRule + " " + furniturePool.allItemRules[26].objType.ToString());
            //load rule

            CFurniturePlacerTool decorationPlacertool = GameObject.Find("DecorationPlacer").GetComponent<CFurniturePlacerTool>();
            CFurniturePool decorationPool = decorationPlacertool.gameObject.GetComponent<CFurniturePool>();
            string csvPath = "Assets/Custom/Rules/BasicRule.csv";
            decorationPool.furnitureItemRules = CFurnitureItemRule.GetItemRule(csvPath);

        }

        if (GUILayout.Button("NAV Debug"))
        {
            //SetNavMeshNotWalkable(GameObject.Find("Objects"));
            //SetNavMeshNotWalkable(GameObject.Find("Structure"));


            //GameObject floor = GameObject.Find("StructureAttr").transform.FirstChildOrDefault(x => x.name.Contains("Floor")).gameObject;
            //Debug.Log("Debug Editor: floor name: " + floor);
            //SetNavMeshWalkable(floor);

            var agentController = FindObjectOfType<PhysicsRemoteFPSAgentController>();
            //var capsuleCollider = agentController.GetComponent<CapsuleCollider>();
            var navmeshAgent = agentController.GetComponent<NavMeshAgent>();
            navmeshAgent.enabled = true;
            // The Editor bake interface does not take with parameters and could not be modified as of 2018.3
            //var buildSettings = 
            new NavMeshBuildSettings()
            {
                agentTypeID = navmeshAgent.agentTypeID,
                agentRadius = 0.2f,
                agentHeight = 1.8f,
                agentSlope = 10,
                agentClimb = 0.5f,
                minRegionArea = 0.05f,
                overrideVoxelSize = false,
                overrideTileSize = false
            };

            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }

    public static void SetNavMeshNotWalkable(GameObject hirerarchy)
    {

        var objectHierarchy = GameObject.Find("Objects");
        if (objectHierarchy == null)
        {
            objectHierarchy = GameObject.Find("Object");
        }
        for (int i = 0; i < objectHierarchy.transform.childCount; i++)
        {
            var child = objectHierarchy.transform.GetChild(i);
            child.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(meshRenderer =>
            {
                Debug.Log("Mesh Renderer not walk" + meshRenderer.gameObject.name + " layer ");
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(meshRenderer.gameObject, UnityEditor.StaticEditorFlags.NavigationStatic);
                UnityEditor.GameObjectUtility.SetNavMeshArea(meshRenderer.gameObject, NavMesh.GetAreaFromName("Not Walkable"));
            });
            Debug.Log("Setting flag for " + child.gameObject.name + " layer " + NavMesh.GetAreaFromName("Not Walkable"));
        }
    }

    public static void SetNavMeshWalkable(GameObject hirerarchy)
    {

        //  var objectHierarchy = hirerarchy.transform.FirstChildOrDefault(x => x.name.Contains("Floor"));
        hirerarchy.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(meshRenderer =>
        {
            Debug.Log("Mesh Renderer walk" + meshRenderer.gameObject.name + " layer ");
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(meshRenderer.gameObject, UnityEditor.StaticEditorFlags.NavigationStatic);
            UnityEditor.GameObjectUtility.SetNavMeshArea(meshRenderer.gameObject, NavMesh.GetAreaFromName("Walkable"));
        });
    }

   

   
}
#endif