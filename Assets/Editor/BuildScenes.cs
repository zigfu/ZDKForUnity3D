using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Linq;

public class BuildScenes {
    static string scenesPath = "Assets/_Scenes/";
    static string outputPath = "Demos";
	
    [MenuItem("Build/Build Scene Executables")]
    static void Build()
    {   
		if (!Directory.Exists(outputPath)) {
			Directory.CreateDirectory(outputPath);
		}
		
		// standardize the format of the directories to *nix format
		string realDir = scenesPath.ToLower().Replace('\\', '/');
		
		// get all assets in one of the given directories
		Debug.Log("About to build scenes from " + realDir);
		string[] assetsToInclude = (from asset in AssetDatabase.GetAllAssetPaths()
                                    where asset.StartsWith(realDir) && asset.EndsWith(".unity")
                                    select asset).ToArray();
		
		
        //string[] scenes = {"Avatar2Players", "AvatarFrontFacing"};
		PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.HiddenByDefault;
		PlayerSettings.defaultIsFullScreen = true;
		PlayerSettings.defaultWebScreenWidth = 1280;
		PlayerSettings.defaultScreenHeight = 720;
		//PlayerSettings.defaultWebScreenWidth = 1024;
		//PlayerSettings.defaultScreenHeight = 768;
		
        foreach(string scene in assetsToInclude) {
			string sceneName = Path.GetFileNameWithoutExtension(scene);
			Debug.Log("About to build " + sceneName);
			PlayerSettings.companyName = "ZigFu";
			PlayerSettings.productName = "Sample " + sceneName;
            string res = BuildPipeline.BuildPlayer(new string[] { scene }, getOutputPath(sceneName), BuildTarget.StandaloneWindows, BuildOptions.None);
            Debug.Log("result: " + res);
        }
    }

    private static string getOutputPath(string scene)
    {
        return string.Format("{0}\\{1}\\{1}.exe", outputPath, scene);
    }
}
