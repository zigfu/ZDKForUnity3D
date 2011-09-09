using UnityEditor;
using UnityEngine;
using System.Collections;

public class BuildScenes {
    static string scenesPath = "Assets/OpenNI/Scenes/";
    static string outputPath = "d:\\work\\test";


    [MenuItem("Build/BuildSceneExecutables")]
    static void Build()
    {   
        string[] scenes = {"Avatar2Players", "AvatarFrontFacing"};
        foreach(string scene in scenes) {
            string res = BuildPipeline.BuildPlayer(new string[] { scenesPath + scene + ".unity" }, getOutputPath(scene), BuildTarget.StandaloneWindows, BuildOptions.None);
            Debug.Log("result: " + res);
        }
    }

    private static string getOutputPath(string scene)
    {
        return string.Format("{0}\\{1}\\{1}.exe", outputPath, scene);
    }
}
