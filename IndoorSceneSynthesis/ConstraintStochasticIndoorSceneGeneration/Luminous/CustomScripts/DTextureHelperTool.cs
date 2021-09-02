# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Assertions;
using System.IO;
using System;

public class DTextureHelperTool : MonoBehaviour {
    public void GetSOPTexturesInOneScene() {
        SimObjPhysics[] simObjPhysics = GameObject.FindObjectsOfType<SimObjPhysics>();
        foreach(SimObjPhysics sop in simObjPhysics) {
            GameObject sopObject = sop.gameObject;
            Debug.Log("Object name: " + sopObject.name);

            List<MeshRenderer> objMeshRenderers = new List<MeshRenderer>();
            List<Material> objMaterials = new List<Material>();

            // Get all MeshRenderers under Objects and Structures groups
            objMeshRenderers.AddRange(sopObject.GetComponentsInChildren<MeshRenderer>());
            // Get all shared Materials from MeshRenderers
            foreach (MeshRenderer meshRenderer in objMeshRenderers) {
                objMaterials.AddRange(meshRenderer.sharedMaterials);
            }

            Debug.Log("objMaterials length: " + objMaterials.Count);

            if(objMaterials.Count == 0) {
                Debug.LogError("Error: Object must have materials!");
            }

            Debug.Log("Material color: " + objMaterials[0].GetColor("_Color"));
            //if you would like to have other color property:
            //visit: https://docs.unity3d.com/ScriptReference/Material.GetColor.html


            //Get texture information if you need
            Texture mainTexture = objMaterials[0].mainTexture;
            Texture2D mainTexture2d = mainTexture as Texture2D;

            

            //Debug.Log("Texture size: " + mainTexture.height + "x" + mainTexture.width);


        }
    }

    public void LoopOverScenesToGetAllObjectMaterials() {
        //copy kitchens from i2thor to our env
        string scenesAtPath = "Assets/Scenes/";
        DirectoryInfo scenesDir = new DirectoryInfo(scenesAtPath);
        FileInfo[] sceneFileList = scenesDir.GetFiles("*.unity");
        foreach (FileInfo sceneFile in sceneFileList) {

            int prefabRelativepathStart = sceneFile.ToString().IndexOf(scenesAtPath);
            string prefabPath = sceneFile.ToString().Substring(prefabRelativepathStart);

            if (prefabPath.Contains("_physics")) {
                Debug.Log("prefabPath: " + prefabPath);

                int digitStart = prefabPath.IndexOf("FloorPlan");
                int digitEnd = prefabPath.IndexOf("_physics");

                int digitString = int.Parse(prefabPath.Substring(digitStart + 9, digitEnd - digitStart - 9));
                Debug.Log("Floorplan: " + digitString);

                SceneAsset oneScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(prefabPath);
                string scenePath = AssetDatabase.GetAssetPath(oneScene);
                Debug.Log("Loading scenes: " + scenePath);

                EditorSceneManager.OpenScene(scenePath);
                GetSOPTexturesInOneScene();

                // if debug
                break;
            }

        }

    }
}

# endif