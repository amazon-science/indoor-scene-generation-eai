//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.EditorCoroutines.Editor;
//using UnityEditor;
//using System.Text;
//using System.Net.Sockets;
//using System.Net;
//using System;

//public class CEditorWindow : EditorWindow
//{
//    public EditorCoroutine editorCoroutine;

//    protected string robosimsHost = "127.0.0.1";
//    protected int robosimsPort = 8200;
//    private Socket sock = null;


//    [MenuItem("Window/Custom")]
//    public static void ShowWindow()
//    {
//        EditorWindow.GetWindow<CEditorWindow>("Custom");
//    }

//    void OnGUI()
//    {
//        GUILayout.Label("Color the selected objects!", EditorStyles.boldLabel);

//        ///color = EditorGUILayout.ColorField("Color", color);

//        if (GUILayout.Button("Start!"))
//        {
//            editorCoroutine = EditorCoroutineUtility.StartCoroutine(CustomEditorServer(), this);
//        }

//        if (GUILayout.Button("Stop!"))
//        {
//            StopEditorServer();
//        }

//    }

//    IEnumerator CustomEditorServer()
//    {
//        var waitForOneSecond = new EditorWaitForSeconds(1.0f);

//        while (true)
//        {
//            if (this.sock == null)
//            {
//                // Debug.Log("connecting to host: " + robosimsHost);
//                IPAddress host = IPAddress.Parse(robosimsHost);
//                IPEndPoint hostep = new IPEndPoint(host, robosimsPort);
//                this.sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//                try
//                {
//                    this.sock.Connect(hostep);
//                }
//                catch (SocketException e)
//                {
//                    Debug.Log("Socket exception: " + e.ToString());
//                }
//            }

//            yield return waitForOneSecond;
//            Debug.Log("Printing each second");

//            if (this.sock != null && this.sock.Connected)
//            {
//                this.sock.Send(Encoding.ASCII.GetBytes("CLIENT IS RUNNING"));
//                // waiting for a frame here keeps the Unity window in sync visually
//                // its not strictly necessary, but allows the interact() command to work properly
//                // and does not reduce the overall FPS
//                yield return new WaitForEndOfFrame();

//                byte[] commandBuffer = new byte[1024];

//                int received = this.sock.Receive(commandBuffer);

//                //if (received == 0)
//                //{
//                //    continue;
//                //}

//                //resolve message
//                string commandMSG = Encoding.UTF8.GetString(commandBuffer).TrimEnd('\0');
//                Debug.Log("received headerBuffer: " + commandMSG);

//                if (commandMSG.Contains("sample a new scene"))
//                {
//                    Debug.Log("enter if" + commandMSG);
//                    SampleNewScene();
//                }
//                else if(commandMSG.Contains("get building scenes"))
//                {
//                    GetBuildingScenes();
//                }
//                else if (commandMSG.Contains("build all scenes"))
//                {
//                    BuildAllScenes();
//                }
//                else if (commandMSG.Contains("change task file path:"))
//                {
//                    ChangeJsonTaskPath(commandMSG);
//                }
//                else if (commandMSG.Contains("change build path:"))
//                {
//                    ChangeBuildPath(commandMSG);
//                }
//                else if (commandMSG.Contains("change random seed:"))
//                {
//                    ChangeRandomSeed(commandMSG);
//                }
//            }
//        }
//    }

//    public void ChangeRandomSeed(string msg)
//    {
//        var msgSplit = msg.Split(':');
//        CSceneBuilderTool.globalRandomSeed = int.Parse(msgSplit[msgSplit.Length - 1]);

//        this.sock.Send(Encoding.ASCII.GetBytes("Change Random Seed!!!!: " + CSceneBuilderTool.globalRandomSeed.ToString()));
//    }

//    public void ChangeBuildPath(string msg)
//    {
//        //CSceneBuilderTool sceneBuilderTool = GameObject.FindObjectOfType<CSceneBuilderTool>();
//        var msgSplit = msg.Split(':');
//        CSceneBuilderTool.buildPath = msgSplit[msgSplit.Length - 1];

//        this.sock.Send(Encoding.ASCII.GetBytes("Change Build Path!!!!: " + CSceneBuilderTool.buildPath));
//    }

//    public void ChangeJsonTaskPath(string msg)
//    {
//        //CSceneBuilderTool sceneBuilderTool = GameObject.FindObjectOfType<CSceneBuilderTool>();
//        var msgSplit = msg.Split(':');
//        CSceneBuilderTool.jsonPath = msgSplit[msgSplit.Length - 1];

//        this.sock.Send(Encoding.ASCII.GetBytes("Change Json Path!!!!: " + CSceneBuilderTool.jsonPath));
//    }

//    public void BuildAllScenes()
//    {
//        CSceneBuilderTool sceneBuilderTool = GameObject.FindObjectOfType<CSceneBuilderTool>();
//        Debug.Log("Building all scenes: " + sceneBuilderTool.gameObject.name);
//        sceneBuilderTool.BuildScenes();

//        GetBuildingScenes();
//    }

//    public void GetBuildingScenes()
//    {
//        string allScenePaths = "";
//        foreach(var scene in EditorBuildSettings.scenes)
//        {
//            allScenePaths += scene.path + ";";
//        }

//        this.sock.Send(Encoding.ASCII.GetBytes("Building scenes!!!!!!" + allScenePaths));
//    }


//    public void SampleNewScene()
//    {
//        CSceneBuilderTool sceneBuilderTool = GameObject.FindObjectOfType<CSceneBuilderTool>();
//        Debug.Log("SampleNewScene: " + sceneBuilderTool.gameObject.name);
//        sceneBuilderTool.GenerateScene();

//        this.sock.Send(Encoding.ASCII.GetBytes("Sample a new scene!!!!!!"));
//    }

//    public void StopEditorServer()
//    {
//        this.sock.Close();
//        this.sock = null;
//        EditorCoroutineUtility.StopCoroutine(editorCoroutine);
//    }
//}

