using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;

public class BuildScenes {
    static string scenesPath = "Assets/OpenNI/Scenes/";
    static string outputPath = "Demos";


    [MenuItem("Build/Build Scene Executables")]
    static void Build()
    {   
		if (!Directory.Exists(outputPath)) {
			Directory.CreateDirectory(outputPath);
		}
		
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
