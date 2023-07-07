/*
 * @Author: delevin.ying 
 * @Date: 2023-06-25 16:19:32 
 * @Last Modified by: delevin.ying
 * @Last Modified time: 2023-06-25 16:31:42
 */


using UnityEditor;
using UnityEngine;
using System;
//using YooAsset;
using Yoo;
using YooAsset.Editor;

public static class BuildApp
{

    private const string c_RelativeDirPrefix = "../Release/";
    private const string c_InitScenePath = "Assets/Demo/Space Shooter/Boot.unity";


    [MenuItem("QuickFolder/Open Bundles")]
    public static void OpenBundles()
    {
        Application.OpenURL("file://"+Application.dataPath+"/../Bundles/");
    }

    [MenuItem("QuickFolder/Open Root Project")]
    public static void OpenRoot()
    {
        Application.OpenURL("file://"+Application.dataPath+"/../../");
    }

    [MenuItem("QuickFolder/Open Tools")]
    public static void OpenTools()
    {
        Application.OpenURL("file://"+Application.dataPath+"/../../Tools/");
    }

    [MenuItem("QuickFolder/Open Cache Bundles")]
    public static void OpenCacheBundles()
    {
        Application.OpenURL("file://"+Application.dataPath+"/../Sandbox/");
    }

    [MenuItem("Debug Tools/TestLastVersion")]
    public static void DebugLastVersion()
    {
        var manifest = Resources.Load<BuildinFileManifest>("BuildinFileManifest");
        var currVersion = manifest.NextPackageVersion;
        LogMaster.Log("curr Version " + currVersion);

        var lastVersion = _GetLastVersion(currVersion);

        PlayerSettings.bundleVersion = lastVersion;

        LogMaster.Log("set PlayerSettings. Version " + lastVersion);
    }


    [MenuItem("Debug Tools/RevertVersion")]
    public static void RevertVersion()
    {
        var manifest = Resources.Load<BuildinFileManifest>("BuildinFileManifest");
        var currVersion = manifest.NextPackageVersion;

        if(string.IsNullOrEmpty(currVersion)){
            // throw new Exception("版本号异常");
            //! 纠正异常
            currVersion = "0.0.1";
        }

        //LogMaster.Log("curr Version " + currVersion);

        //var lastVersion = _GetLastVersion(currVersion);

        PlayerSettings.bundleVersion = currVersion;

        LogMaster.Log("revert PlayerSettings. Version " + currVersion);
    }



    [MenuItem("Build Tools/BuildApp")]
    public static void Build()
    {
        var manifest = Resources.Load<BuildinFileManifest>("BuildinFileManifest");
        var packageVersion = manifest.NextPackageVersion;// AssetBundleBuilderSettingData.Setting.packageVersion;
        if(string.IsNullOrEmpty(packageVersion))
        {
            packageVersion = "0.0.1";
            UnityEngine.Debug.Log("[PackageVersion]  init version");
        }

        PlayerSettings.bundleVersion = packageVersion;
        //UnityEngine.Debug.Log("[PlayerSettings.bundleVersion]  "+ packageVersion);

        var outputPath =
             $"{c_RelativeDirPrefix}/ProjectS_EXE"; //EditorUtility.SaveFolderPanel("Choose Location of the Built Game", "", "");
        if (outputPath.Length == 0)
            return;

        #region 将Unity的BuildInScene设置为仅包含Init，因为我们为了支持在编辑器模式下的测试而必须将所有Scene放到Unity的BuildInSetting里

        var backScenes = EditorBuildSettings.scenes;
        var scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(c_InitScenePath, true) };
        EditorBuildSettings.scenes = scenes;

        #endregion

        // 如果执行打包，就强行替换为非本地调试模式，进行AB加载
        Boot updater = UnityEngine.Object.FindObjectOfType<Boot>();
        YooAsset.EPlayMode backPlayMode = updater.PlayMode;
        updater.PlayMode = YooAsset.EPlayMode.HostPlayMode;

        var targetName = GetBuildTargetName(EditorUserBuildSettings.activeBuildTarget);
        if (targetName == null)
            return;

        var buildPlayerOptions = new BuildPlayerOptions
        {
            locationPathName = outputPath + targetName,
            target = EditorUserBuildSettings.activeBuildTarget,
            options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None
        };
        var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);

        UnityEngine.Debug.Log("[PlayerSettings.bundleVersion] packageVersion: " + packageVersion);

        updater.PlayMode = backPlayMode;
        EditorBuildSettings.scenes = backScenes;

        if(buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded){
            LogMaster.Log("build Success  outputPath："+ outputPath + targetName);



            /*AssetBundleBuilderSettingData.Setting.packageVersion*/
            manifest.NextPackageVersion = SetNextVersion(packageVersion);

            UnityEditor.AssetDatabase.SaveAssets();

            Application.OpenURL("file://"+outputPath);
        }

		//if (AssetBundleBuilderSettingData.IsDirty){
  //          AssetBundleBuilderSettingData.SaveFile();
  //      }
				
        

        Debug.Log("output : " + outputPath);
    }

    public static string SetNextVersion(string str)
    {
        var codeStr = string.Copy(str);
        codeStr = codeStr.Replace(".", "");
        var version = int.Parse(codeStr);
        version++;
        if (version >= 9999999)
        {
            LogMaster.E("版本超过限定------");
            return string.Format("0.0.1");
        }

        if (version < 100)
        {
            return string.Format($"0.0.{version}");
        }
        else if (version < 10000)
        {
            return string.Format($"0.{version / 100}.{version % 100}");
        }
        else
        {
            var high = version / 10000;
            var low = version % 10000;
            return string.Format($"{high}.{low / 100}.{low % 100}");
        }
    }

    private static string _GetLastVersion(string str)
    {
        var codeStr = string.Copy(str);
        codeStr = codeStr.Replace(".", "");
        var version = int.Parse(codeStr);
        version--;
        if (version <= 0)
        {
            LogMaster.E("版本超过限定------");
            return string.Format("0.0.1");
        }

        if (version < 100)
        {
            return string.Format($"0.0.{version}");
        }
        else if (version < 10000)
        {
            return string.Format($"0.{version / 100}.{version % 100}");
        }
        else
        {
            var high = version / 10000;
            var low = version % 10000;
            return string.Format($"{high}.{low / 100}.{low % 100}");
        }
    }

    private static string GetBuildTargetName(BuildTarget target)
    {
        var time = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var name = PlayerSettings.productName + "-v" + PlayerSettings.bundleVersion + ".";
        switch (target)
        {
            case BuildTarget.Android:
                return string.Format("/{0}{1}-{2}.apk", name, 1, time);

            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return string.Format("/{0}{1}-{2}.exe", name, 1, time);

#if UNITY_2017_3_OR_NEWER
            case BuildTarget.StandaloneOSX:
                return "/" + name + ".app";

#else
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
                case BuildTarget.StandaloneOSXUniversal:
                    return "/" + path + ".app";

#endif

            case BuildTarget.WebGL:
            case BuildTarget.iOS:
                return "";
            // Add more build targets for your own.
            default:
                Debug.Log("Target not implemented.");
                return null;
        }
    }
}