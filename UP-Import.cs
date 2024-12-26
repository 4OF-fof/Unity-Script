using UnityEngine;
using UnityEditor;
using System.IO;

public class BatchImportUnityPackages : EditorWindow {
    private string rootFolderPath = Path.Combine(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads"), "UP-Export");
    private Vector2 scrollPosition;
    private bool[] packageToggles;
    private string[] packagePaths;

    [MenuItem("Tools/UP Import")]
    private static void ShowWindow() {
        GetWindow<BatchImportUnityPackages>("UP Import");
    }

    private void OnEnable() {
        FindPackages();
    }

    private void OnGUI() {
        GUILayout.Label("UP Import", EditorStyles.boldLabel);
        rootFolderPath = EditorGUILayout.TextField("Folder Path", rootFolderPath);
        if(GUILayout.Button("Select Folder")) {
            rootFolderPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Packages", EditorStyles.label);
        if(GUILayout.Button("Refresh Package List")) {
            FindPackages();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for(int i = 0; i < packagePaths.Length; i++) {
            packageToggles[i] = true;
            packageToggles[i] = EditorGUILayout.ToggleLeft(Path.GetFileName(packagePaths[i]), packageToggles[i]);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Import")) {
            ImportSelectedPackages();
        }
    }


    private void FindPackages() {
        packagePaths = Directory.GetFiles(rootFolderPath, "*.unitypackage", SearchOption.AllDirectories);
        packageToggles = new bool[packagePaths.Length];
    }

    private void ImportSelectedPackages() {
        for(int i = 0; i < packagePaths.Length; i++) {
            if (packageToggles[i]) {
                AssetDatabase.ImportPackage(packagePaths[i], false);
            }
        }
    }
}