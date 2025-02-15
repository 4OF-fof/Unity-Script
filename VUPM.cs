using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

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
    private bool isInitialized = false;
    private bool isLoading = false;
    private AssetType selectedAssetType = AssetType.Unregistered;
    private bool isEditMode = false;
    private AssetDataList.assetInfo editingAsset = null;

    void OnEnable() {
        isInitialized = false;
        isLoading = true;
        EditorApplication.delayCall += Initialize;
    }

    private void Initialize() {
        EditorUtility.DisplayProgressBar("Loading", "Loading asset list...", 0.5f);
        try {
            Utility.LoadZipList();
            assetData = Utility.LoadAssetData();
            isInitialized = true;
        }
        finally {
            EditorUtility.ClearProgressBar();
            isLoading = false;
            Repaint();
        }
    }

    void OnGUI() {
        if (isLoading) {
            GUILayout.Label("Loading...", EditorStyles.boldLabel);
            return;
        }

        if (!isInitialized) {
            GUILayout.Label("Initializing...", EditorStyles.boldLabel);
            return;
        }

        if (showDetailWindow) {
            GUI.enabled = false;
        }

        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope()) {
            GUILayout.Label("VRChat Unity Package Manager", Style.title);
            if (GUILayout.Button("Sync Asset List", Style.button)) {
                isLoading = true;
                EditorApplication.delayCall += () => {
                    EditorUtility.DisplayProgressBar("Syncing", "Syncing asset list...", 0.5f);
                    try {
                        Utility.LoadZipList();
                        assetData = Utility.LoadAssetData();
                    }
                    finally {
                        EditorUtility.ClearProgressBar();
                        isLoading = false;
                        Repaint();
                    }
                };
            }
        }
        
        Color oldColor = GUI.color;
        GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
        GUILayout.Box("", Style.divLine);
        GUI.color = oldColor;

        GUILayout.BeginVertical();
        // AssetTypeタブの表示（2行）
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 1行目: Unregistered, Avatar, Hair, Cloth
        EditorGUILayout.BeginHorizontal();
        AssetType[] firstRow = { AssetType.Unregistered, AssetType.Avatar, AssetType.Hair, AssetType.Cloth };
        foreach (AssetType type in firstRow) {
            if (GUILayout.Toggle(selectedAssetType == type, type.ToString(), EditorStyles.toolbarButton)) {
                if (selectedAssetType != type) {
                    selectedAssetType = type;
                    showDetailWindow = false;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // 2行目: Accessory, Gimmick, Script, Other
        EditorGUILayout.BeginHorizontal();
        AssetType[] secondRow = { AssetType.Accessory, AssetType.Gimmick, AssetType.Script, AssetType.Other };
        foreach (AssetType type in secondRow) {
            if (GUILayout.Toggle(selectedAssetType == type, type.ToString(), EditorStyles.toolbarButton)) {
                if (selectedAssetType != type) {
                    selectedAssetType = type;
                    showDetailWindow = false;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        GUILayout.EndVertical();

        using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition)) {
            scrollPosition = scrollView.scrollPosition;
            EditorGUILayout.Space(5);
            int assetCount = 0;
            using (new EditorGUILayout.HorizontalScope()) {
                var filteredAssets = assetData.assetList.Where(asset => asset.assetType == selectedAssetType);
                foreach (var asset in filteredAssets) {
                    if (assetCount > 0 && assetCount % 4 == 0) {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(position.width / 4 - 10))) {
                        Texture2D thumbnail = null;
                        if (!string.IsNullOrEmpty(asset.thumbnailPath)) {
                            thumbnail = Utility.LoadThumbnail(rootPath + asset.thumbnailPath, thumbnailCache);
                        }
                        
                        if (thumbnail == null) {
                            string dummyPath = "Assets/Editor/Dummy.png";
                            thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(dummyPath);
                        }
                        GUILayout.Box(thumbnail, GUILayout.Width(position.width / 4 - 20), GUILayout.Height(position.width / 4 - 20));
                        
                        if (GUILayout.Button(asset.assetName, GUILayout.Height(30))) {
                            showDetailWindow = true;
                            selectedAsset = asset;
                        }
                    }
                    assetCount++;
                }
            }
        }

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
            if (isEditMode) {
                // 編集モードの場合は、編集をキャンセル
                isEditMode = false;
                editingAsset = null;
            }
            showDetailWindow = false;
            GUI.changed = true;
            Event.current.Use();
        }
        
        EditorGUI.DrawRect(new Rect(x, y, windowWidth, windowHeight), new Color(0.2f, 0.2f, 0.2f, 0.95f));
        
        GUILayout.BeginArea(new Rect(x + 10, y + 10, windowWidth - 20, windowHeight - 20));

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(thumbnailSize));
        if (selectedAsset != null) {
            GUILayout.FlexibleSpace(); // 上部に可変スペースを追加
            Texture2D thumbnail = null;
            if (!string.IsNullOrEmpty(selectedAsset.thumbnailPath)) {
                thumbnail = Utility.LoadThumbnail(rootPath + selectedAsset.thumbnailPath, thumbnailCache);
            }
            
            if (thumbnail == null) {
                string dummyPath = "Assets/Editor/Dummy.png";
                thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(dummyPath);
            }
            GUILayout.Box(thumbnail, GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize));
            GUILayout.FlexibleSpace(); // 下部に可変スペースを追加
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.Width(windowWidth - thumbnailSize - 40));
        if (selectedAsset != null) {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Asset Details", Style.detailTitle);
            if (GUILayout.Button(isEditMode ? "Cancel" : "Edit Asset Info", Style.detailEditInfoButton)) {
                isEditMode = !isEditMode;
                if (isEditMode) {
                    editingAsset = selectedAsset.Clone();
                } else {
                    editingAsset = null;
                }
            }
            if (isEditMode && GUILayout.Button("Save", Style.detailEditInfoButton)) {
                SaveAssetChanges();
                isEditMode = false;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Name", Style.detailContentName);
            if (isEditMode) {
                editingAsset.assetName = EditorGUILayout.TextField(editingAsset.assetName);
            } else {
                EditorGUILayout.LabelField(selectedAsset.assetName);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("File Path", Style.detailContentName);
            EditorGUILayout.LabelField(selectedAsset.sourcePath, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("URL", Style.detailContentName);
            if (isEditMode) {
                editingAsset.url = EditorGUILayout.TextField(editingAsset.url);
            } else {
                EditorGUILayout.LabelField(selectedAsset.url ?? "", EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Thumbnail Path", Style.detailContentName);
            if (isEditMode) {
                EditorGUILayout.BeginHorizontal();
                string thumbnailPath = EditorGUILayout.TextField(editingAsset.thumbnailPath);
                if (thumbnailPath != editingAsset.thumbnailPath) {
                    editingAsset.thumbnailPath = thumbnailPath;
                    Repaint();
                }
                if (GUILayout.Button("Select", GUILayout.Width(60))) {
                    string absolutePath = EditorUtility.OpenFilePanel("Select Thumbnail", "", "png,jpg,jpeg");
                    if (!string.IsNullOrEmpty(absolutePath)) {
                        // パスの区切り文字を正規化
                        string normalizedAbsolutePath = absolutePath.Replace("\\", "/");
                        string normalizedRootPath = rootPath.Replace("\\", "/");
                        
                        // 正規化したパスで比較
                        if (normalizedAbsolutePath.StartsWith(normalizedRootPath)) {
                            editingAsset.thumbnailPath = normalizedAbsolutePath.Substring(normalizedRootPath.Length).TrimStart('/');
                            Repaint();
                        } else {
                            EditorUtility.DisplayDialog("Invalid Path", "Please select a file within the VAMF directory.", "OK");
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            } else {
                EditorGUILayout.LabelField(selectedAsset.thumbnailPath ?? "", EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Description", Style.detailContentName);
            if (isEditMode) {
                editingAsset.description = EditorGUILayout.TextArea(editingAsset.description ?? "", GUILayout.Height(60));
            } else {
                EditorGUILayout.LabelField(selectedAsset.description ?? "", EditorStyles.wordWrappedLabel, GUILayout.Height(60));
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Tags", Style.detailContentName);
            if (isEditMode) {
                string tags = editingAsset.tags != null ? string.Join(",", editingAsset.tags) : "";
                string newTags = EditorGUILayout.TextField(tags);
                editingAsset.tags = newTags.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
            } else {
                string tags = selectedAsset.tags != null ? string.Join(", ", selectedAsset.tags) : "";
                EditorGUILayout.LabelField(tags, EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Asset Type", Style.detailContentName);
            if (isEditMode) {
                editingAsset.assetType = (AssetType)EditorGUILayout.EnumPopup(editingAsset.assetType);
            } else {
                EditorGUILayout.LabelField(selectedAsset.assetType.ToString());
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            // インポートボタンの追加（編集モード以外の時のみ表示）
            if (!isEditMode) {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("Import UnityPackage", GUILayout.Height(30))) {
                    string packagePath = Path.GetFullPath(rootPath + selectedAsset.filePath);
                    if (File.Exists(packagePath)) {
                        AssetDatabase.ImportPackage(packagePath, true);
                    } else {
                        EditorUtility.DisplayDialog("Import Error", "UnityPackage file not found at: " + packagePath, "OK");
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(5);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void SaveAssetChanges() {
        if (editingAsset != null && selectedAsset != null) {
            // 編集中のアセット情報を選択中のアセットに反映
            selectedAsset.assetName = editingAsset.assetName;
            selectedAsset.sourcePath = editingAsset.sourcePath;
            selectedAsset.url = editingAsset.url;
            selectedAsset.thumbnailPath = editingAsset.thumbnailPath;
            selectedAsset.description = editingAsset.description;
            selectedAsset.tags = editingAsset.tags;
            selectedAsset.assetType = editingAsset.assetType;

            // アセットリストを更新
            int index = assetData.assetList.FindIndex(a => a.uid == selectedAsset.uid);
            if (index != -1) {
                assetData.assetList[index] = selectedAsset;
            }

            // JSONファイルに保存
            Utility.SaveAssetData(assetData);
            editingAsset = null;
        }
    }
}
