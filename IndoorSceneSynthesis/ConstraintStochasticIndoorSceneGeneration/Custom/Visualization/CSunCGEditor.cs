#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(CSunCGVisualizer))]
public class CSunCGEditor : Editor
{
    public CSunCGVisualizer targetScript;

    private void OnEnable()
    {
        targetScript = (CSunCGVisualizer)target;
    }

    //Set up GUI on Unity Editor
    public override void OnInspectorGUI()
    {
        CSunCGVisualizer.sampleJsonPath = (string)EditorGUILayout.TextField("json path", CSunCGVisualizer.sampleJsonPath);


        if (GUILayout.Button("Load Prefabs"))
        {
            targetScript.LoadObjectPrefab();
        }

        if (GUILayout.Button("Load Room Json 3DSLN"))
        {
            targetScript.LoadJsonRulefor3DSLN(CSunCGVisualizer.sampleJsonPath, dataSource:"3D_FRONT");
        }

        if (GUILayout.Button("Load Room Json SceneFormer"))
        {
            targetScript.LoadJsonRuleForSceneFormer();
        }

        if (GUILayout.Button("Load Next Json Sample")) {
            // var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string originalScenePath = "Assets/Custom/Visualization/v3D.unity";
            string saveSceneFolder = "Assets/Custom/BuildScenes/";

            for (int i = 0; i < 100; ++i) {
                string saveScenePath = saveSceneFolder + "q" + i.ToString() + ".unity";

                //EditorSceneManager.OpenScene(EditorSceneManager.GetActiveScene().path);

                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), saveScenePath, true);
                EditorSceneManager.OpenScene(saveScenePath);

                string folder_path = "Assets/Custom/Visualization/deepsynth2/"; //living room as default
                DirectoryInfo prefabDir = new DirectoryInfo(folder_path);
                FileInfo[] prefabInfoList = prefabDir.GetFiles("*.json");
                //System.Random rng = new System.Random();

                //CSunCGVisualizer.currentJsonIndex = (CSunCGVisualizer.currentJsonIndex + 1) % prefabInfoList.Length;
                FileInfo prefabFile = prefabInfoList[i];
                int prefabRelativepathStart = prefabFile.ToString().IndexOf(folder_path);
                string prefabPath = prefabFile.ToString().Substring(prefabRelativepathStart);
                CSunCGVisualizer.sampleJsonPath = prefabPath;

                targetScript.furniturePool.randomSeed = i;
                targetScript.LoadObjectPrefab();
                targetScript.LoadJsonRulefor3DSLN(CSunCGVisualizer.sampleJsonPath, "SUNCG");

                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

                EditorSceneManager.OpenScene(originalScenePath);
            }
        }
    }



}
# endif