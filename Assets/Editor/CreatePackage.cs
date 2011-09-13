using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CreatePackage {
    //TODO: move to external config file
    static string[] DirsToInclude = {
                               "Assets/_Scenes",
                               "Assets/HandpointControls",
                               "Assets/OpenNI",
							   "Assets/SlideViewer",
                           };
    static string PackageName = "UnityOpenNIBindings.unitypackage";

    static bool StartsWithOneOf(string str, IEnumerable<string> starts)
    {
        foreach(string beginning in starts) {
            if (str.StartsWith(beginning)) {
                return true;
            }
        }
        return false;
    }

    [MenuItem("Build/Create OpenNI Package")]
    static void CreateUnityPackage()
    {
        // standardize the format of the directories to *nix format
        var RealDirs = from dir in DirsToInclude
                       select dir.ToLower().Replace('\\', '/');

        // get all assets in one of the given directories
        string[] assetsToInclude = (from asset in AssetDatabase.GetAllAssetPaths()
                                    where StartsWithOneOf(asset, RealDirs)
                                    select asset).ToArray();

        // build the package, including those assets' dependencies too
        AssetDatabase.ExportPackage((from asset in AssetDatabase.GetAllAssetPaths()
                                     where StartsWithOneOf(asset, RealDirs)
                                     select asset).ToArray(),
                                    PackageName,
                                    ExportPackageOptions.IncludeDependencies);
    }
}
