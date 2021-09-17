//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//using System.Text;
//using System.Net.Sockets;
//using System.Net;
//using System;

//# if UNITY_EDITOR
//using Unity.EditorCoroutines.Editor;
//using UnityEditor;
//using UnityEditor.SceneManagement;

////[InitializeOnLoad]
//public class Startup {
//    public static EditorCoroutine editorCoroutine;

//    static string robosimsHost = "127.0.0.1";
//    static int robosimsPort = 8300;
//    static Socket sock = null;

//    static Startup() {
//        Debug.Log("Up and running: ");
//        editorCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(CustomEditorServer());

//    }

//    static IEnumerator CustomEditorServer() {
//        var waitForOneSecond = new EditorWaitForSeconds(1.0f);

//        while (true) {
//            if (sock == null) {
//                // Debug.Log("connecting to host: " + robosimsHost);
//                IPAddress host = IPAddress.Parse(robosimsHost);
//                IPEndPoint hostep = new IPEndPoint(host, robosimsPort);
//                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//                try {
//                    sock.Connect(hostep);
//                } catch (SocketException e) {
//                    Debug.Log("Socket exception: " + e.ToString());
//                }
//            }

//            yield return waitForOneSecond;
//            Debug.Log("Printing each second sock connected? " + (sock != null && sock.Connected).ToString());

//            if (sock != null && sock.Connected) {
//                sock.Send(Encoding.ASCII.GetBytes("CLIENT IS RUNNING"));
//                // waiting for a frame here keeps the Unity window in sync visually
//                // its not strictly necessary, but allows the interact() command to work properly
//                // and does not reduce the overall FPS
//                yield return new WaitForEndOfFrame();

//                byte[] commandBuffer = new byte[1024];

//                int received = sock.Receive(commandBuffer);

//                //if (received == 0)
//                //{
//                //    continue;
//                //}

//                //resolve message
//                string commandMSG = Encoding.UTF8.GetString(commandBuffer).TrimEnd('\0');
//                Debug.Log("received headerBuffer: " + commandMSG);

//                if (commandMSG.Contains("sample a new scene")) {
//                    Debug.Log("enter if" + commandMSG);
//                    SampleNewScene();
//                } else if (commandMSG.Contains("get building scenes")) {
//                    GetBuildingScenes();
//                } else if (commandMSG.Contains("build all scenes")) {
//                    BuildAllScenes();
//                } else if (commandMSG.Contains("change task file path:")) {
//                    ChangeJsonTaskPath(commandMSG);
//                } else if (commandMSG.Contains("change build path:")) {
//                    ChangeBuildPath(commandMSG);
//                } else if (commandMSG.Contains("change random seed:")) {
//                    ChangeRandomSeed(commandMSG);
//                } else if (commandMSG.Contains("clear building scenes")) {
//                    ClearBuildingScenes(commandMSG);
//                } else if (commandMSG.Contains("close client")) {
//                    sock.Send(Encoding.ASCII.GetBytes("Close Unity Client!!!!: " + CSceneBuilderTool.globalRandomSeed.ToString()));
//                    StopEditorClient();
//                    EditorApplication.Exit(0);
//                } else if (commandMSG.Contains("open scene")) {
//                    OpenScene(commandMSG);
//                } else if (commandMSG.Contains("toggleplay")) {
//                    PlayMode();
//                }

//            } else {
//                //force break connect if it is not connected
//                sock = null;
//            }
//        }
//    }

//    public static void ClearBuildingScenes(string msg) {
//        EditorBuildSettingsScene[] original = new EditorBuildSettingsScene[0];
//        EditorBuildSettings.scenes = original;
//        //Array.Clear(EditorBuildSettings.scenes, 0, EditorBuildSettings.scenes.Length);
//        sock.Send(Encoding.ASCII.GetBytes("Clear Building Scenes!!!!: "));
//    }

//    public static void ChangeRandomSeed(string msg) {
//        var msgSplit = msg.Split(':');
//        CSceneBuilderTool.globalRandomSeed = int.Parse(msgSplit[msgSplit.Length - 1]);

//        sock.Send(Encoding.ASCII.GetBytes("Change Random Seed!!!!: " + CSceneBuilderTool.globalRandomSeed.ToString()));
//    }

//    public static void ChangeBuildPath(string msg) {
//        //CSceneBuilderTool sceneBuilderTool = GameObject.FindObjectOfType<CSceneBuilderTool>();
//        var msgSplit = msg.Split(':');
//        CSceneBuilderTool.buildPath = msgSplit[msgSplit.Length - 1];

//        sock.Send(Encoding.ASCII.GetBytes("Change Build Path!!!!: " + CSceneBuilderTool.buildPath));
//    }

//    public static void ChangeJsonTaskPath(string msg) {
//        //CSceneBuilderTool sceneBuilderTool = GameObject.FindObjectOfType<CSceneBuilderTool>();
//        var msgSplit = msg.Split(':');
//        CSceneBuilderTool.jsonPath = msgSplit[msgSplit.Length - 1];

//        sock.Send(Encoding.ASCII.GetBytes("Change Json Path!!!!: " + CSceneBuilderTool.jsonPath));
//    }

//    public static void BuildAllScenes() {
//        CSceneBuilderTool sceneBuilderTool = GameObject.FindObjectOfType<CSceneBuilderTool>();
//        Debug.Log("Building all scenes: " + sceneBuilderTool.gameObject.name);
//        sceneBuilderTool.BuildScenes();

//        GetBuildingScenes();
//    }

//    public static void GetBuildingScenes() {
//        string allScenePaths = "";
//        foreach (var scene in EditorBuildSettings.scenes) {
//            allScenePaths += scene.path + ";";
//        }

//        sock.Send(Encoding.ASCII.GetBytes("Building scenes!!!!!!" + allScenePaths));
//    }


//    public static void SampleNewScene() {
//        CSceneBuilderTool sceneBuilderTool = GameObject.FindObjectOfType<CSceneBuilderTool>();
//        Debug.Log("SampleNewScene: " + sceneBuilderTool.gameObject.name);
//        sceneBuilderTool.GenerateScene();

//        sock.Send(Encoding.ASCII.GetBytes("Sample a new scene!!!!!!"));
//    }

//    public static void StopEditorClient() {
//        sock.Close();
//        sock = null;
//        EditorCoroutineUtility.StopCoroutine(editorCoroutine);
//    }

//    public static void OpenScene(string msg) {
//        var msgSplit = msg.Split(':');
//        string scenePath = msgSplit[msgSplit.Length - 1];

//        EditorSceneManager.OpenScene(scenePath);
//        sock.Send(Encoding.ASCII.GetBytes("Open scene!!!!!!"));
//    }

//    public static void PlayMode() {
//        EditorApplication.ExecuteMenuItem("Edit/Play");
//        sock.Send(Encoding.ASCII.GetBytes("Play scene!!!!!!"));
//        //UnityEditor.EditorApplication.isPlaying
//    }

//    public static void SampleSceneFromCurrentPrefab(string jsonPath) {

//        IEnumerator sampleSceneIE() {
//            GameObject helperTool = GameObject.Instantiate((GameObject)AssetDatabase.LoadAssetAtPath("Assets/Custom/SceneRandomizer/Dalibao.prefab", typeof(GameObject)));
//            helperTool.gameObject.name = "HelperTool";

//            CSceneBuilderTool sceneBuilderTool = GameObject.Find("SceneBuilder").GetComponent<CSceneBuilderTool>();
//            CSceneBuilderTool.jsonPath = jsonPath;

//            EditorCoroutineUtility.StartCoroutineOwnerless(sceneBuilderTool.SetUpNewSceneFromSelf(true));
//            sock.Send(Encoding.ASCII.GetBytes("SetUpNewSceneFromSelf!!!!!!"));

//            yield return null;
//        }

//        EditorCoroutineUtility.StartCoroutineOwnerless(sampleSceneIE());
//    }
//}
////#endif