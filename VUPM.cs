using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class VUPM : MonoBehaviour {
    [MenuItem("Window/VUPM")]
    public static void OpenWindow() {
        var window = EditorWindow.GetWindow<VUPMEditorWindow>("VRChat Unity Package Manager", typeof(SceneView));
    }
}

public class VUPMEditorWindow : EditorWindow {

    private string rootPath = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/VAMF/");

    private AssetDataList assetData;
    private AssetDataList.assetInfo selectedAsset;
    private bool showDetailWindow = false;
    private Vector2 scrollPosition;
    private Dictionary<string, Texture2D> thumbnailCache = new Dictionary<string, Texture2D>();

    void OnEnable() {
        Utility.LoadZipList();
        assetData = Utility.LoadAssetData();
    }

    void OnGUI() {
        if (showDetailWindow) {
            GUI.enabled = false;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("VRChat Unity Package Manager", Style.title);
        if (GUILayout.Button("Sync Asset List", Style.button)) {
            Utility.LoadZipList();
            assetData = Utility.LoadAssetData();
        }
        EditorGUILayout.EndHorizontal();
        
        Color oldColor = GUI.color;
        GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
        GUILayout.Box("", Style.divLine);
        GUI.color = oldColor;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.Space(10);
        int assetCount = 0;
        EditorGUILayout.BeginHorizontal();
        foreach (var asset in assetData.assetList) {
            if (assetCount > 0 && assetCount % 4 == 0) {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 4 - 10));
            
            if (!string.IsNullOrEmpty(asset.thumbnailPath)) {
                Texture2D thumbnail = Utility.LoadThumbnail(rootPath + asset.thumbnailPath, thumbnailCache);
                if (thumbnail != null) {
                    GUILayout.Box(thumbnail, GUILayout.Width(position.width / 4 - 20), GUILayout.Height(position.width / 4 - 20));
                }else{
                    GUILayout.Box("", GUILayout.Width(position.width / 4 - 20), GUILayout.Height(position.width / 4 - 20));
                }
            }else {
                string dummyPath = "Assets/Editor/Dummy.png";
                Texture2D dummyThumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(dummyPath);
                GUILayout.Box(dummyThumbnail, GUILayout.Width(position.width / 4 - 20), GUILayout.Height(position.width / 4 - 20));
            }
            
            if (GUILayout.Button(asset.assetName, GUILayout.Height(30))) {
                showDetailWindow = true;
                selectedAsset = asset;
            }
            
            EditorGUILayout.EndVertical();
            assetCount++;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        if (showDetailWindow) {
            GUI.enabled = true;
            DetailWindow();
        }
    }

    private void DetailWindow() {
        float windowWidth = 800;
        float windowHeight = 500;
        float x = (position.width - windowWidth) / 2;
        float y = (position.height - windowHeight) / 2;
        float thumbnailSize = 250;

        Rect backgroundRect = new Rect(0, 0, position.width, position.height);
        EditorGUI.DrawRect(backgroundRect, new Color(0, 0, 0, 0.5f));
        
        if (Event.current.type == EventType.MouseDown && !new Rect(x, y, windowWidth, windowHeight).Contains(Event.current.mousePosition)) {
            showDetailWindow = false;
            GUI.changed = true;
            Event.current.Use();
        }
        
        EditorGUI.DrawRect(new Rect(x, y, windowWidth, windowHeight), new Color(0.2f, 0.2f, 0.2f, 0.95f));
        
        GUILayout.BeginArea(new Rect(x + 10, y + 10, windowWidth - 20, windowHeight - 20));

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(thumbnailSize));
        if (selectedAsset != null) {
            if (!string.IsNullOrEmpty(selectedAsset.thumbnailPath)) {
                Texture2D thumbnail = Utility.LoadThumbnail(rootPath + selectedAsset.thumbnailPath, thumbnailCache);
                if (thumbnail != null) {
                    GUILayout.Box(thumbnail, GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize));
                }
            }else {
                string dummyPath = "Assets/Editor/Dummy.png";
                Texture2D dummyThumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(dummyPath);
                GUILayout.Box(dummyThumbnail, GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize));
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.Width(windowWidth - thumbnailSize - 40));
        if (selectedAsset != null) {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Asset Details", Style.detailTitle);
            if (GUILayout.Button("Edit Asset Info", Style.detailEditInfoButton)) {
                Debug.Log("Edit Avatar Info");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Name", Style.detailContentName);
            EditorGUILayout.LabelField(selectedAsset.assetName);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("File Path", Style.detailContentName);
            EditorGUILayout.LabelField(selectedAsset.filePath, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Thumbnail Path", Style.detailContentName);
            EditorGUILayout.LabelField(selectedAsset.thumbnailPath, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
