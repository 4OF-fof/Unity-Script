using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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
    private Stack<AssetDataList.assetInfo> assetHistory = new Stack<AssetDataList.assetInfo>();
    private bool showDetailWindow = false;
    private Vector2 mainScrollPosition; // アセット一覧用のスクロール位置
    private Vector2 detailScrollPosition; // 詳細ウィンドウ用のスクロール位置
    private Vector2 storedMainScrollPosition; // 詳細ウィンドウ表示時のメインスクロール位置を保存
    private Vector2 descriptionScrollPosition;
    private Dictionary<string, Texture2D> thumbnailCache = new Dictionary<string, Texture2D>();
    private bool isInitialized = false;
    private bool isLoading = false;
    private AssetType selectedAssetType = AssetType.Unregistered;
    private bool isEditMode = false;
    private AssetDataList.assetInfo editingAsset = null;
    private Vector2 dependencyPopupScrollPosition;
    private bool showDependencyPopup = false;
    private Rect dependencyPopupRect;

    private class DependencyPopupWindow : EditorWindow {
        private VUPMEditorWindow parentWindow;
        private Vector2 scrollPosition;
        private AssetDataList assetData;
        private AssetDataList.assetInfo editingAsset;
        private GUIStyle hoverButtonStyle;
        private static DependencyPopupWindow currentWindow;

        public static void ShowWindow(VUPMEditorWindow parent, AssetDataList assetData, AssetDataList.assetInfo editingAsset, Vector2 position) {
            if (currentWindow != null) {
                currentWindow.Close();
            }
            var window = CreateInstance<DependencyPopupWindow>();
            window.titleContent = new GUIContent("Select Dependency");
            window.position = new Rect(position.x, position.y, 250, 300);
            window.parentWindow = parent;
            window.assetData = assetData;
            window.editingAsset = editingAsset;
            window.ShowPopup();
            currentWindow = window;
        }

        public static void CloseCurrentWindow() {
            if (currentWindow != null) {
                currentWindow.Close();
                currentWindow = null;
            }
        }

        void OnGUI() {
            if (hoverButtonStyle == null) {
                hoverButtonStyle = new GUIStyle(EditorStyles.label);
                hoverButtonStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                hoverButtonStyle.hover.textColor = new Color(0.4f, 0.7f, 1.0f);
                hoverButtonStyle.active.textColor = new Color(0.3f, 0.6f, 0.9f);
                hoverButtonStyle.padding = new RectOffset(5, 5, 2, 2);
                hoverButtonStyle.margin = new RectOffset(0, 0, 1, 1);
            }

            // ウィンドウ外クリックの判定
            var windowRect = new Rect(Vector2.zero, position.size);
            if (Event.current.type == EventType.MouseDown && !windowRect.Contains(Event.current.mousePosition)) {
                currentWindow = null;
                Close();
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var asset in assetData.assetList) {
                if (asset.uid != editingAsset.uid) {
                    bool isAlreadyDependent = editingAsset.dependencies != null && 
                                            editingAsset.dependencies.Contains(asset.uid);
                    
                    if (isAlreadyDependent) {
                        GUI.enabled = false;
                        GUILayout.Label(asset.assetName, hoverButtonStyle);
                        GUI.enabled = true;
                    } else {
                        Rect buttonRect = GUILayoutUtility.GetRect(GUIContent.none, hoverButtonStyle, GUILayout.ExpandWidth(true));
                        bool isHover = buttonRect.Contains(Event.current.mousePosition);
                        
                        if (isHover) {
                            EditorGUI.DrawRect(buttonRect, new Color(0.4f, 0.4f, 0.4f, 0.2f));
                        }

                        if (GUI.Button(buttonRect, asset.assetName, hoverButtonStyle)) {
                            if (editingAsset.dependencies == null) {
                                editingAsset.dependencies = new List<string>();
                            }
                            editingAsset.dependencies.Add(asset.uid);
                            parentWindow.Repaint();
                            Close();
                        }

                        if (isHover) {
                            Repaint();
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void OnDestroy() {
            currentWindow = null;
        }
    }

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
            CleanupMissingAssets();
            isInitialized = true;
        }
        finally {
            EditorUtility.ClearProgressBar();
            isLoading = false;
            Repaint();
        }
    }

    private void CleanupMissingAssets() {
        // sourcePathのファイルが存在しないアセットを削除
        var deletedAssets = new List<AssetDataList.assetInfo>();
        foreach (var asset in assetData.assetList) {
            string sourcePath = Path.GetFullPath(rootPath + asset.sourcePath);
            if (!File.Exists(sourcePath)) {
                deletedAssets.Add(asset);
            }
        }

        if (deletedAssets.Count > 0) {
            int deletedFileCount = 0;
            // まずアセットデータから削除
            foreach (var asset in deletedAssets) {
                if (assetData.assetList.Remove(asset)) {
                    // .unzip以下のファイルが存在する場合は削除
                    string filePath = Path.GetFullPath(rootPath + asset.filePath);
                    if (filePath.Contains(".unzip") && File.Exists(filePath)) {
                        try {
                            File.Delete(filePath);
                            deletedFileCount++;
                        }
                        catch (Exception ex) {
                            Debug.LogError($"Failed to delete file: {filePath}, Error: {ex.Message}");
                        }
                    }
                }
            }

            // 変更を保存
            Utility.SaveAssetData(assetData);
            Debug.Log($"Removed {deletedAssets.Count} assets from data. Deleted {deletedFileCount} .unzip files.");
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
                        CleanupMissingAssets();
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

        using (var scrollView = new EditorGUILayout.ScrollViewScope(showDetailWindow ? storedMainScrollPosition : mainScrollPosition)) {
            if (!showDetailWindow) {
                mainScrollPosition = scrollView.scrollPosition;
            } else if (Event.current.type == EventType.Layout) {
                // 詳細ウィンドウ表示時は現在のスクロール位置を保存
                storedMainScrollPosition = mainScrollPosition;
            }
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
        
        Rect detailWindowRect = new Rect(x, y, windowWidth, windowHeight);
        
        // 詳細ウィンドウ内のクリックを検出
        if (Event.current.type == EventType.MouseDown) {
            if (detailWindowRect.Contains(Event.current.mousePosition)) {
                // Select Assetボタンの領域を除外
                if (isEditMode && editingAsset != null) {
                    Vector2 localMousePos = Event.current.mousePosition - new Vector2(x + 10, y + 10);
                    float selectAssetButtonY = 300; // おおよその位置
                    Rect selectAssetButtonRect = new Rect(thumbnailSize, selectAssetButtonY, 100, 20);
                    if (!selectAssetButtonRect.Contains(localMousePos)) {
                        DependencyPopupWindow.CloseCurrentWindow();
                    }
                }
            }
        }
        
        if (Event.current.type == EventType.MouseDown && !detailWindowRect.Contains(Event.current.mousePosition)) {
            if (isEditMode) {
                // 編集モードの場合は、編集をキャンセル
                isEditMode = false;
                editingAsset = null;
                DependencyPopupWindow.CloseCurrentWindow();
            }
            showDetailWindow = false;
            assetHistory.Clear();
            GUI.changed = true;
            Event.current.Use();
        }
        
        EditorGUI.DrawRect(detailWindowRect, new Color(0.2f, 0.2f, 0.2f, 0.95f));
        
        GUILayout.BeginArea(new Rect(x + 10, y + 10, windowWidth - 20, windowHeight - 20));

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(thumbnailSize));
        if (selectedAsset != null) {
            // Backボタンをここに移動
            if (!isEditMode && assetHistory.Count > 0) {
                if (GUILayout.Button("← Back", GUILayout.Width(80), GUILayout.Height(25))) {
                    selectedAsset = assetHistory.Pop();
                    isEditMode = false;
                    editingAsset = null;
                    Repaint();
                }
                EditorGUILayout.Space(5);
            }

            GUILayout.FlexibleSpace();
            Texture2D thumbnail = null;
            var displayAsset = isEditMode ? editingAsset : selectedAsset;
            if (!string.IsNullOrEmpty(displayAsset.thumbnailPath)) {
                thumbnail = Utility.LoadThumbnail(rootPath + displayAsset.thumbnailPath, thumbnailCache);
            }
            
            if (thumbnail == null) {
                string dummyPath = "Assets/Editor/Dummy.png";
                thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(dummyPath);
            }
            GUILayout.Box(thumbnail, GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize));

            // Add Get thumbnail from Booth button
            if (GUILayout.Button("Get thumbnail from Booth")) {
                var targetAsset = isEditMode ? editingAsset : selectedAsset;
                if (!string.IsNullOrEmpty(targetAsset.url) && targetAsset.url.Contains("booth.pm")) {
                    EditorApplication.delayCall += async () => {
                        var tempAsset = targetAsset.Clone();
                        // 編集モード中は一時的な変更として扱う
                        if (isEditMode) {
                            await Utility.GetBoothThumbnail(tempAsset, assetData, rootPath, thumbnailCache, false);
                            editingAsset = tempAsset;
                        } else {
                            await Utility.GetBoothThumbnail(tempAsset, assetData, rootPath, thumbnailCache, true);
                            selectedAsset = assetData.assetList.Find(a => a.uid == targetAsset.uid);
                        }
                        Repaint();
                    };
                } else {
                    EditorUtility.DisplayDialog("Error", "This asset does not have a valid Booth URL.", "OK");
                }
            }
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.Width(windowWidth - thumbnailSize - 40));
        if (selectedAsset != null) {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Asset Details", Style.detailTitle);
            if (GUILayout.Button(isEditMode ? "Cancel" : "Edit Asset Info", Style.detailEditInfoButton)) {
                if (isEditMode) {
                    // キャンセル時の処理
                    editingAsset = null;
                    // サムネイルキャッシュをクリアして再読み込み
                    if (!string.IsNullOrEmpty(selectedAsset.thumbnailPath)) {
                        if (thumbnailCache.ContainsKey(rootPath + selectedAsset.thumbnailPath)) {
                            thumbnailCache.Remove(rootPath + selectedAsset.thumbnailPath);
                        }
                    }
                    // アセットリストから最新の状態を取得して更新
                    int index = assetData.assetList.FindIndex(a => a.uid == selectedAsset.uid);
                    if (index != -1) {
                        selectedAsset = assetData.assetList[index].Clone();
                    }
                    isEditMode = false;
                    DependencyPopupWindow.CloseCurrentWindow();
                    GUI.FocusControl(null);
                    Repaint();
                } else {
                    // 編集モードに入る時の処理
                    int index = assetData.assetList.FindIndex(a => a.uid == selectedAsset.uid);
                    if (index != -1) {
                        selectedAsset = assetData.assetList[index].Clone();
                    }
                    editingAsset = selectedAsset.Clone();
                    isEditMode = true;
                }
            }
            if (isEditMode && GUILayout.Button("Save", Style.detailEditInfoButton)) {
                SaveAssetChanges();
                isEditMode = false;
                DependencyPopupWindow.CloseCurrentWindow();
            }
            EditorGUILayout.EndHorizontal();

            // プロパティ表示部分をスクロール可能な領域に
            using (var scrollView = new EditorGUILayout.ScrollViewScope(detailScrollPosition, GUILayout.Height(windowHeight - 140))) {
                detailScrollPosition = scrollView.scrollPosition;
                
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
                            string normalizedAbsolutePath = absolutePath.Replace("\\", "/");
                            string normalizedRootPath = rootPath.Replace("\\", "/");
                            
                            if (normalizedAbsolutePath.StartsWith(normalizedRootPath + "Thumbnail")) {
                                editingAsset.thumbnailPath = normalizedAbsolutePath.Substring(normalizedRootPath.Length).TrimStart('/');
                                Repaint();
                            } else {
                                EditorUtility.DisplayDialog("Invalid Path", "Please select a file within the Thumbnail directory.", "OK");
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                } else {
                    EditorGUILayout.LabelField(selectedAsset.thumbnailPath ?? "", EditorStyles.wordWrappedLabel);
                }

                EditorGUILayout.Space(5);
                if (isEditMode || (selectedAsset.dependencies != null && selectedAsset.dependencies.Count > 0)) {
                    EditorGUILayout.LabelField("Dependencies", Style.detailContentName);
                    if (isEditMode) {
                        if (editingAsset.dependencies != null) {
                            List<string> dependenciesToRemove = new List<string>();
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            foreach (string depUid in editingAsset.dependencies) {
                                var depAsset = assetData.assetList.Find(a => a.uid == depUid);
                                if (depAsset != null) {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField(depAsset.assetName);
                                    if (GUILayout.Button("×", GUILayout.Width(20))) {
                                        dependenciesToRemove.Add(depUid);
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                            }
                            EditorGUILayout.EndVertical();
                            
                            foreach (string uidToRemove in dependenciesToRemove) {
                                editingAsset.dependencies.Remove(uidToRemove);
                            }
                        }

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Add Dependency", GUILayout.Width(100));
                        if (GUILayout.Button("Select Asset")) {
                            Vector2 popupPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                            DependencyPopupWindow.ShowWindow(this, assetData, editingAsset, popupPosition);
                        }
                        EditorGUILayout.EndHorizontal();
                    } else {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        foreach (string depUid in selectedAsset.dependencies) {
                            var depAsset = assetData.assetList.Find(a => a.uid == depUid);
                            if (depAsset != null) {
                                EditorGUILayout.BeginHorizontal();
                                var linkStyle = new GUIStyle(EditorStyles.label);
                                linkStyle.normal.textColor = new Color(0.4f, 0.7f, 1.0f);
                                if (GUILayout.Button(depAsset.assetName, linkStyle)) {
                                    assetHistory.Push(selectedAsset);
                                    selectedAsset = depAsset;
                                    isEditMode = false;
                                    editingAsset = null;
                                    Repaint();
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.Space(5);
                }

                EditorGUILayout.LabelField("Asset Type", Style.detailContentName);
                if (isEditMode) {
                    editingAsset.assetType = (AssetType)EditorGUILayout.EnumPopup(editingAsset.assetType);
                } else {
                    EditorGUILayout.LabelField(selectedAsset.assetType.ToString());
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Description", Style.detailContentName);
                if (isEditMode) {
                    editingAsset.description = EditorGUILayout.TextArea(editingAsset.description ?? "", GUILayout.Height(60));
                } else {
                    using (var descriptionScrollView = new EditorGUILayout.ScrollViewScope(
                        descriptionScrollPosition,
                        GUILayout.Height(60)))
                    {
                        descriptionScrollPosition = descriptionScrollView.scrollPosition;
                        EditorGUILayout.LabelField(selectedAsset.description ?? "", EditorStyles.wordWrappedLabel);
                    }
                }

                EditorGUILayout.Space(5);

                EditorGUILayout.EndVertical();
            }

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
            selectedAsset.dependencies = editingAsset.dependencies;
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