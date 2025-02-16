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
    private bool showFilterWindow = false;
    private string searchName = "";
    private string searchDescription = "";
    private string tempSearchName = "";
    private string tempSearchDescription = "";
    private Vector2 mainScrollPosition;
    private Vector2 detailScrollPosition;
    private Vector2 storedMainScrollPosition;
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
    private bool showImportDialog = false;

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
        var deletedAssets = new List<AssetDataList.assetInfo>();
        foreach (var asset in assetData.assetList) {
            string sourcePath = Path.GetFullPath(rootPath + asset.sourcePath);
            if (!File.Exists(sourcePath)) {
                deletedAssets.Add(asset);
            }
        }

        if (deletedAssets.Count > 0) {
            int deletedFileCount = 0;
            foreach (var asset in deletedAssets) {
                if (assetData.assetList.Remove(asset)) {
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
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
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
                storedMainScrollPosition = mainScrollPosition;
            }
            EditorGUILayout.Space(5);
            int assetCount = 0;

            // 検索ウィンドウが開いている場合はアセットボタンを無効化
            if (showFilterWindow) {
                GUI.enabled = false;
            }

            using (new EditorGUILayout.HorizontalScope()) {
                var filteredAssets = assetData.assetList.Where(asset => 
                    asset.assetType == selectedAssetType &&
                    (string.IsNullOrEmpty(searchName) || asset.assetName.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (string.IsNullOrEmpty(searchDescription) || 
                     (asset.description != null && asset.description.IndexOf(searchDescription, StringComparison.OrdinalIgnoreCase) >= 0))
                );
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

            // GUIの有効状態を元に戻す
            if (showFilterWindow) {
                GUI.enabled = true;
            }
        }

        if (showDetailWindow) {
            GUI.enabled = true;
            DetailWindow();
        }

        BeginWindows();
        var searchButtonRect = new Rect(position.width - 60, position.height - 60, 40, 40);
        var searchIconContent = EditorGUIUtility.IconContent("d_Search Icon");
        searchIconContent.tooltip = "Search & Filter";

        // フィルターが適用されているかどうかをチェック
        bool isFilterActive = !string.IsNullOrEmpty(searchName) || !string.IsNullOrEmpty(searchDescription);

        // 検索ボタンのクリック判定を最優先で処理
        if (Event.current.type == EventType.MouseDown && searchButtonRect.Contains(Event.current.mousePosition)) {
            showFilterWindow = !showFilterWindow;
            if (showFilterWindow) {
                tempSearchName = searchName;
                tempSearchDescription = searchDescription;
            }
            Event.current.Use();
            Repaint();
            EndWindows();
            return;
        }

        // 正方形の背景を描画
        var bgStyle = new GUIStyle();
        bgStyle.normal.background = EditorGUIUtility.whiteTexture;

        // 通常時とホバー時の背景色を設定
        Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        Color activeColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        // 輪郭線の色を設定
        Color borderColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
        if (searchButtonRect.Contains(Event.current.mousePosition)) {
            borderColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);
        }

        // マウスの状態に応じて背景色を変更
        if (Event.current.type == EventType.MouseDown && searchButtonRect.Contains(Event.current.mousePosition)) {
            GUI.backgroundColor = activeColor;
        } else if (searchButtonRect.Contains(Event.current.mousePosition)) {
            GUI.backgroundColor = hoverColor;
        } else {
            GUI.backgroundColor = normalColor;
        }
        
        // 背景を描画
        GUI.Box(searchButtonRect, "", bgStyle);
        GUI.backgroundColor = Color.white;

        // 輪郭線を描画
        var borderRects = new Rect[] {
            new Rect(searchButtonRect.x, searchButtonRect.y, searchButtonRect.width, 1),                    // 上
            new Rect(searchButtonRect.x, searchButtonRect.y + searchButtonRect.height - 1, searchButtonRect.width, 1),  // 下
            new Rect(searchButtonRect.x, searchButtonRect.y, 1, searchButtonRect.height),                    // 左
            new Rect(searchButtonRect.x + searchButtonRect.width - 1, searchButtonRect.y, 1, searchButtonRect.height)   // 右
        };

        foreach (var borderRect in borderRects) {
            EditorGUI.DrawRect(borderRect, borderColor);
        }

        // カスタムボタンスタイル
        var roundButtonStyle = new GUIStyle(EditorStyles.iconButton);
        roundButtonStyle.normal.background = null;
        roundButtonStyle.hover.background = null;
        roundButtonStyle.active.background = null;
        roundButtonStyle.fontSize = 20;
        roundButtonStyle.fixedWidth = 40;
        roundButtonStyle.fixedHeight = 40;
        roundButtonStyle.alignment = TextAnchor.MiddleCenter;

        // フィルターが適用されている場合はアイコンの色を変更
        Color originalColor = GUI.color;
        if (isFilterActive) {
            GUI.color = new Color(0.3f, 0.8f, 0.3f, 1.0f);
        }
        GUI.Button(searchButtonRect, searchIconContent, roundButtonStyle);
        GUI.color = originalColor;
        EndWindows();

        if (showFilterWindow) {
            FilterWindow();
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
        
        if (Event.current.type == EventType.MouseDown) {
            if (detailWindowRect.Contains(Event.current.mousePosition)) {
                if (isEditMode && editingAsset != null) {
                    Vector2 localMousePos = Event.current.mousePosition - new Vector2(x + 10, y + 10);
                    float selectAssetButtonY = 300;
                    Rect selectAssetButtonRect = new Rect(thumbnailSize, selectAssetButtonY, 100, 20);
                    if (!selectAssetButtonRect.Contains(localMousePos)) {
                        DependencyPopupWindow.CloseCurrentWindow();
                    }
                }
            }
        }
        
        if (Event.current.type == EventType.MouseDown && !detailWindowRect.Contains(Event.current.mousePosition)) {
            if (isEditMode) {
                isEditMode = false;
                editingAsset = null;
                if (!string.IsNullOrEmpty(selectedAsset.thumbnailPath)) {
                    if (thumbnailCache.ContainsKey(rootPath + selectedAsset.thumbnailPath)) {
                        thumbnailCache.Remove(rootPath + selectedAsset.thumbnailPath);
                    }
                }
                int index = assetData.assetList.FindIndex(a => a.uid == selectedAsset.uid);
                if (index != -1) {
                    selectedAsset = assetData.assetList[index].Clone();
                }
                DependencyPopupWindow.CloseCurrentWindow();
                GUI.FocusControl(null);
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

            if (GUILayout.Button("Get thumbnail from Booth")) {
                var targetAsset = isEditMode ? editingAsset : selectedAsset;
                if (!string.IsNullOrEmpty(targetAsset.url) && targetAsset.url.Contains("booth.pm")) {
                    EditorApplication.delayCall += async () => {
                        var tempAsset = targetAsset.Clone();
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
                    editingAsset = null;
                    if (!string.IsNullOrEmpty(selectedAsset.thumbnailPath)) {
                        if (thumbnailCache.ContainsKey(rootPath + selectedAsset.thumbnailPath)) {
                            thumbnailCache.Remove(rootPath + selectedAsset.thumbnailPath);
                        }
                    }
                    int index = assetData.assetList.FindIndex(a => a.uid == selectedAsset.uid);
                    if (index != -1) {
                        selectedAsset = assetData.assetList[index].Clone();
                    }
                    isEditMode = false;
                    DependencyPopupWindow.CloseCurrentWindow();
                    GUI.FocusControl(null);
                    Repaint();
                } else {
                    var newEditingAsset = new AssetDataList.assetInfo {
                        uid = selectedAsset.uid,
                        assetName = selectedAsset.assetName,
                        sourcePath = selectedAsset.sourcePath,
                        filePath = selectedAsset.filePath,
                        url = selectedAsset.url,
                        thumbnailPath = selectedAsset.thumbnailPath,
                        description = selectedAsset.description,
                        dependencies = selectedAsset.dependencies != null ? new List<string>(selectedAsset.dependencies) : null,
                        assetType = selectedAsset.assetType
                    };
                    editingAsset = newEditingAsset;
                    isEditMode = true;
                }
            }
            if (isEditMode && GUILayout.Button("Save", Style.detailEditInfoButton)) {
                SaveAssetChanges();
                isEditMode = false;
                DependencyPopupWindow.CloseCurrentWindow();
            }
            EditorGUILayout.EndHorizontal();

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

            if (!isEditMode) {
                using (new EditorGUILayout.HorizontalScope()) {
                    var toggleStyle = new GUIStyle(GUI.skin.toggle);
                    toggleStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                    toggleStyle.onNormal.textColor = new Color(0.8f, 0.8f, 0.8f);
                    showImportDialog = GUILayout.Toggle(showImportDialog, "Show import dialog", toggleStyle);
                }
                EditorGUILayout.Space(5);

                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("Import UnityPackage", GUILayout.Height(30))) {
                    ShowImportConfirmationDialog(selectedAsset);
                }
                GUI.backgroundColor = Color.white;
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private HashSet<string> processedAssets = new HashSet<string>();
    private List<AssetDataList.assetInfo> importList = new List<AssetDataList.assetInfo>();
    private Queue<AssetDataList.assetInfo> importQueue = new Queue<AssetDataList.assetInfo>();
    private bool isImporting = false;

    private void ShowImportConfirmationDialog(AssetDataList.assetInfo asset) {
        importList.Clear();
        processedAssets.Clear();
        CollectImportAssets(asset);
        processedAssets.Clear();

        // 依存元を追跡するための辞書を作成
        var requestedBy = new Dictionary<string, List<string>>();
        foreach (var importAsset in importList) {
            if (importAsset.dependencies != null) {
                foreach (var depUid in importAsset.dependencies) {
                    if (!requestedBy.ContainsKey(depUid)) {
                        requestedBy[depUid] = new List<string>();
                    }
                    requestedBy[depUid].Add(importAsset.uid);
                }
            }
        }

        string message = "The following assets will be imported:\n\n";

        // 選択されたアセットを最初に表示
        var selectedAssetInfo = importList.Find(a => a.uid == asset.uid);
        if (selectedAssetInfo != null) {
            message += $"{selectedAssetInfo.assetName} (Selected by User)\n\n";
        }

        // 残りのアセットを表示
        foreach (var currentAsset in importList) {
            if (currentAsset.uid == asset.uid) continue; // 選択されたアセットはスキップ

            message += currentAsset.assetName;
            
            if (requestedBy.ContainsKey(currentAsset.uid)) {
                var requesters = requestedBy[currentAsset.uid]
                    .Select(uid => assetData.assetList.Find(a => a.uid == uid))
                    .Where(a => a != null)
                    .Select(a => a.assetName);

                if (requesters.Any()) {
                    message += $" (Required by: {string.Join(", ", requesters)})";
                }
            }
            message += "\n\n";
        }

        if (EditorUtility.DisplayDialog("Import Confirmation", message, "Import", "Cancel")) {
            importQueue.Clear();
            foreach (var importAsset in importList) {
                importQueue.Enqueue(importAsset);
            }
            if (!isImporting) {
                isImporting = true;
                AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
                AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
                ImportNext();
            }
        }
    }

    private void OnImportPackageCompleted(string packageName) {
        EditorApplication.delayCall += () => {
            ImportNext();
        };
    }

    private void OnImportPackageCancelled(string packageName) {
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Import Cancelled", 
            "The import process has been cancelled. Some assets may not have been imported properly.", "OK");
        CleanupImport();
    }

    private void CleanupImport() {
        isImporting = false;
        importQueue.Clear();
        AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
        AssetDatabase.importPackageCancelled -= OnImportPackageCancelled;
    }

    private void ImportNext() {
        if (importQueue.Count == 0) {
            CleanupImport();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Import Completed", 
                "All assets have been successfully imported.", "OK");
            return;
        }

        var asset = importQueue.Dequeue();
        string packagePath = Path.GetFullPath(rootPath + asset.filePath);

        if (File.Exists(packagePath)) {
            float progress = 1f - (float)importQueue.Count / importList.Count;
            if (showImportDialog) {
                EditorUtility.DisplayProgressBar("Importing Assets", 
                    $"Waiting for import dialog response: {asset.assetName}...", progress);
            } else {
                EditorUtility.DisplayProgressBar("Importing Assets", 
                    $"Importing {asset.assetName}...", progress);
            }

            AssetDatabase.ImportPackage(packagePath, showImportDialog);
        } else {
            Debug.LogError($"UnityPackage file not found at: {packagePath}");
            ImportNext();
        }
    }

    private void OnDisable() {
        if (isImporting) {
            CleanupImport();
            EditorUtility.ClearProgressBar();
        }
    }

    private void CollectImportAssets(AssetDataList.assetInfo asset) {
        if (asset == null) return;
        if (processedAssets.Contains(asset.uid)) return;

        processedAssets.Add(asset.uid);

        // 依存関係を先に処理
        if (asset.dependencies != null && asset.dependencies.Count > 0) {
            foreach (string depUid in asset.dependencies) {
                var depAsset = assetData.assetList.Find(a => a.uid == depUid);
                if (depAsset != null) {
                    CollectImportAssets(depAsset);
                }
            }
        }

        // インポートリストに追加
        if (File.Exists(Path.GetFullPath(rootPath + asset.filePath))) {
            importList.Add(asset);
        }
    }

    private void SaveAssetChanges() {
        if (editingAsset != null && selectedAsset != null) {
            selectedAsset.assetName = editingAsset.assetName;
            selectedAsset.sourcePath = editingAsset.sourcePath;
            selectedAsset.url = editingAsset.url;
            selectedAsset.thumbnailPath = editingAsset.thumbnailPath;
            selectedAsset.description = editingAsset.description;
            selectedAsset.dependencies = editingAsset.dependencies != null ? new List<string>(editingAsset.dependencies) : null;
            selectedAsset.assetType = editingAsset.assetType;

            int index = assetData.assetList.FindIndex(a => a.uid == selectedAsset.uid);
            if (index != -1) {
                assetData.assetList[index] = selectedAsset.Clone();
            }

            Utility.SaveAssetData(assetData);
            editingAsset = null;
        }
    }

    private void FilterWindow() {
        float windowWidth = 250;
        float windowHeight = 200;
        float x = position.width - windowWidth - 70;
        float y = position.height - windowHeight - 70;

        var backgroundRect = new Rect(x, y, windowWidth, windowHeight);
        EditorGUI.DrawRect(backgroundRect, new Color(0.2f, 0.2f, 0.2f, 0.95f));

        // Enterキーの検出と処理
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return) {
            searchName = tempSearchName;
            searchDescription = tempSearchDescription;
            showFilterWindow = false;
            GUI.FocusControl(null);
            Event.current.Use();
            Repaint();
            return;
        }

        if (Event.current.type == EventType.MouseDown && !backgroundRect.Contains(Event.current.mousePosition)) {
            showFilterWindow = false;
            GUI.changed = true;
            Event.current.Use();
            return;
        }

        GUILayout.BeginArea(backgroundRect);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(windowWidth), GUILayout.Height(windowHeight));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Search & Filter", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Name", EditorStyles.boldLabel);
        tempSearchName = EditorGUILayout.TextField(tempSearchName, GUILayout.ExpandWidth(true));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
        tempSearchDescription = EditorGUILayout.TextField(tempSearchDescription, GUILayout.ExpandWidth(true));

        EditorGUILayout.Space(15);
        using (new EditorGUILayout.HorizontalScope()) {
            if (GUILayout.Button("Apply", GUILayout.Height(25))) {
                searchName = tempSearchName;
                searchDescription = tempSearchDescription;
                showFilterWindow = false;
                Repaint();
            }

            if (GUILayout.Button("Reset", GUILayout.Height(25))) {
                searchName = "";
                searchDescription = "";
                tempSearchName = "";
                tempSearchDescription = "";
                showFilterWindow = false;
                Repaint();
            }
        }

        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }
}