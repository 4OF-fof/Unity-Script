using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class BatchImportUnityPackages : EditorWindow {
    private string rootFolderPath = Path.Combine(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads"), "UP-Export");
    private Vector2 scrollPosition;
    private bool[] packageToggles;
    private List<string> packagePaths = new List<string>();

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

        for(int i = 0; i < packagePaths.Count; i++) {
            packageToggles[i] = EditorGUILayout.ToggleLeft(Path.GetFileName(packagePaths[i]), packageToggles[i]);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Import")) {
            ImportSelectedPackages();
        }
    }


    private void FindPackages() {
        packagePaths.Clear();
        for(int i = 5; i >= 1; i--) {
            string tmpPath = Path.Combine(rootFolderPath, i.ToString());
            if(Directory.Exists(tmpPath)) packagePaths.AddRange(Directory.GetFiles(tmpPath, "*.unitypackage", SearchOption.TopDirectoryOnly));
        }
        packagePaths.AddRange(Directory.GetFiles(rootFolderPath, "*.unitypackage", SearchOption.TopDirectoryOnly));
        packageToggles = new bool[packagePaths.Count];
        for (int i = 0; i < packageToggles.Length; i++) packageToggles[i] = true;
    }

    private void ImportSelectedPackages() {
        for(int i = 0; i < packagePaths.Count; i++) {
            if (packageToggles[i]) {
                AssetDatabase.ImportPackage(packagePaths[i], false);
            }
        }
    }
}