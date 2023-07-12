using UnityEditor;
using UnityEngine;

public class MeshAssetChecker : EditorWindow
{
    [MenuItem("Window/Mesh Asset Checker")]
    public static void ShowWindow()
    {
        GetWindow<MeshAssetChecker>("Mesh Asset Checker");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Check Mesh Assets"))
        {
            string[] guids = AssetDatabase.FindAssets("t:Mesh");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log("Mesh Asset: " + path);
            }
        }
    }
}



// public class MeshAssetChecker : AssetPostprocessor
// {
//     private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
//     {
//         foreach (string importedAsset in importedAssets)
//         {
//             if (importedAsset.EndsWith(".fbx") || importedAsset.EndsWith(".obj"))
//             {
//                 ModelImporter modelImporter = AssetImporter.GetAtPath(importedAsset) as ModelImporter;
//                 if (modelImporter != null)
//                 {
//                     ModelImporterMeshCompression compression = modelImporter.meshCompression;
//                     if (compression != ModelImporterMeshCompression.Off)
//                     {
//                         Debug.LogWarning("Mesh Asset: " + importedAsset + " is using mesh compression.");
//                     }
//                 }
//             }
//         }
//     }
// }
