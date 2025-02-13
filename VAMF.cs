using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[Serializable]
public class AvatarDataList {
    [Serializable]
    public class baseAvatarInfo {
        public string uid;
        public string avatarName;
        public string filePath;
        public string thumbnailPath;
    }
    [Serializable]
    public class modifiedAvatarInfo {
        public string uid;
        public string avatarName;
        public string filePath;
        public string thumbnailPath;
        public string description;
        public List<string> tags;
        public int baseAvatarId;
        public List<int> relationAssetsId;
    }

    public List<baseAvatarInfo> baseAvatarList = new List<baseAvatarInfo>();
    public List<modifiedAvatarInfo> modifiedAvatarList = new List<modifiedAvatarInfo>();
}

[InitializeOnLoad]
public class VMM : MonoBehaviour {
    [MenuItem("Window/VAMF")]
    public static void OpenWindow() {
        var window = EditorWindow.GetWindow<VAMHEditorWindow>("VRChat Avatar Modify Framework", typeof(SceneView));
    }
}

public class VAMHEditorWindow : EditorWindow {
    private AvatarDataList avatarData;
    private bool showDetailWindow = false;
    private AvatarDataList.baseAvatarInfo selectedBaseAvatar;
    private AvatarDataList.modifiedAvatarInfo selectedModifiedAvatar;
    private bool isBaseAvatar = true;
    private Dictionary<string, Texture2D> thumbnailCache = new Dictionary<string, Texture2D>();
    private Vector2 scrollPosition;

    void OnEnable() {
        LoadAvatarData();
    }

    private void LoadAvatarData() {
        avatarData = AssetsData.LoadAvatarData();
    }

    private Texture2D LoadThumbnail(string path) {
        if (string.IsNullOrEmpty(path)) return null;
        if (thumbnailCache.ContainsKey(path)) return thumbnailCache[path];

        if (File.Exists(path)) {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData)) {
                thumbnailCache[path] = texture;
                return texture;
            }
        }
        return null;
    }

    void OnGUI() {
        if (showDetailWindow) {
            GUI.enabled = false;
        }

        string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/VAMF/";

        GUIStyle title = new GUIStyle(EditorStyles.boldLabel);
        title.fontSize = 20;
        title.alignment = TextAnchor.UpperCenter;
        title.margin = new RectOffset(170, 0, 10, 10);

        GUIStyle subTitle = new GUIStyle(EditorStyles.boldLabel);
        subTitle.fontSize = 15;
        subTitle.alignment = TextAnchor.UpperCenter;
        subTitle.margin = new RectOffset(0, 0, 10, 10);

        GUIStyle divLine = new GUIStyle();
        divLine.normal.background = EditorGUIUtility.whiteTexture;
        divLine.margin = new RectOffset(20, 20, 0, 0);
        divLine.fixedHeight = 2;

        GUIStyle button = new GUIStyle(EditorStyles.miniButton);
        button.fontSize = 15;
        button.fixedWidth = 130;
        button.fixedHeight = 20;
        button.margin = new RectOffset(0, 40, 10, 10);

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("VRChat Avatar Modify Framework", title);
        if (GUILayout.Button("Sync Avatar List", button)) {
            LoadAvatarData();
        }
        EditorGUILayout.EndHorizontal();
        
        Color oldColor = GUI.color;
        GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
        GUILayout.Box("", divLine);
        GUI.color = oldColor;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.Label("Base Avatar", subTitle);
        int baseCount = 0;
        EditorGUILayout.BeginHorizontal();
        foreach (var baseAvatar in avatarData.baseAvatarList) {
            if (baseCount > 0 && baseCount % 4 == 0) {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 4 - 10));
            
            if (!string.IsNullOrEmpty(baseAvatar.thumbnailPath)) {

                Texture2D thumbnail = LoadThumbnail(rootPath + baseAvatar.thumbnailPath);
                if (thumbnail != null) {
                    GUILayout.Box(thumbnail, GUILayout.Width(position.width / 4 - 20), GUILayout.Height(position.width / 4 - 20));
                }else{
                    GUILayout.Box("", GUILayout.Width(position.width / 4 - 20), GUILayout.Height(position.width / 4 - 20));
                }
            }
            
            if (GUILayout.Button(baseAvatar.avatarName, GUILayout.Height(30))) {
                showDetailWindow = true;
                selectedBaseAvatar = baseAvatar;
                isBaseAvatar = true;
            }
            
            EditorGUILayout.EndVertical();
            baseCount++;
        }
        if (baseCount > 0 && baseCount % 4 == 0) {
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
        }
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 4 - 10));
        if (GUILayout.Button("Create Base Avatar", GUILayout.Height(50))) {
            Debug.Log("Create Base Avatar");
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);

        GUILayout.Label("Modified Avatar", subTitle);
        int modifiedCount = 0;
        EditorGUILayout.BeginHorizontal();
        foreach (var modifiedAvatar in avatarData.modifiedAvatarList) {
            if (modifiedCount > 0 && modifiedCount % 4 == 0) {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 4 - 10));
            
            if (!string.IsNullOrEmpty(modifiedAvatar.thumbnailPath)) {
                Texture2D thumbnail = LoadThumbnail(rootPath + modifiedAvatar.thumbnailPath);
                if (thumbnail != null) {
                    GUILayout.Box(thumbnail, GUILayout.Width(position.width / 4 - 20), GUILayout.Height(position.width / 4 - 20));
                }else{
                    GUILayout.Box("", GUILayout.Width(position.width / 4 - 20), GUILayout.Height(position.width / 4 - 20));
                }   
            }
            
            if (GUILayout.Button(modifiedAvatar.avatarName, GUILayout.Height(30))) {
                showDetailWindow = true;
                selectedModifiedAvatar = modifiedAvatar;
                isBaseAvatar = false;
            }
            
            EditorGUILayout.LabelField("UID: " + modifiedAvatar.uid);
            EditorGUILayout.LabelField("File: " + modifiedAvatar.filePath);
            EditorGUILayout.EndVertical();
            modifiedCount++;
        }
        if (modifiedCount > 0 && modifiedCount % 4 == 0) {
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
        }
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 4 - 10));
        if (GUILayout.Button("Create Modified Avatar", GUILayout.Height(50))) {
            Debug.Log("Create Modified Avatar");
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
        
        EditorGUILayout.EndScrollView();

        if (showDetailWindow) {
            GUI.enabled = true;
            ShowDetailWindow();
        }
    }

    private void ShowDetailWindow() {
        float windowWidth = 500;
        float windowHeight = 400;
        float x = (position.width - windowWidth) / 2;
        float y = (position.height - windowHeight) / 2;

        Rect backgroundRect = new Rect(0, 0, position.width, position.height);
        EditorGUI.DrawRect(backgroundRect, new Color(0, 0, 0, 0.5f));
        
        if (Event.current.type == EventType.MouseDown && !new Rect(x, y, windowWidth, windowHeight).Contains(Event.current.mousePosition)) {
            showDetailWindow = false;
            GUI.changed = true;
            Event.current.Use();
        }
        
        EditorGUI.DrawRect(new Rect(x, y, windowWidth, windowHeight), new Color(0.2f, 0.2f, 0.2f, 0.95f));
        
        GUILayout.BeginArea(new Rect(x + 10, y + 10, windowWidth - 20, windowHeight - 20));

        if (isBaseAvatar && selectedBaseAvatar != null) {
            EditorGUILayout.LabelField("Base Avatar Details", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Name: " + selectedBaseAvatar.avatarName);
            EditorGUILayout.LabelField("UID: " + selectedBaseAvatar.uid);
            EditorGUILayout.LabelField("File Path: " + selectedBaseAvatar.filePath);
            if (!string.IsNullOrEmpty(selectedBaseAvatar.thumbnailPath)) {
                Texture2D thumbnail = LoadThumbnail(selectedBaseAvatar.thumbnailPath);
                if (thumbnail != null) {
                    GUILayout.Box(thumbnail, GUILayout.Width(200), GUILayout.Height(200));
                }
                EditorGUILayout.LabelField("Thumbnail Path: " + selectedBaseAvatar.thumbnailPath);
            }
        }
        else if (!isBaseAvatar && selectedModifiedAvatar != null) {
            EditorGUILayout.LabelField("Modified Avatar Details", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Name: " + selectedModifiedAvatar.avatarName);
            EditorGUILayout.LabelField("UID: " + selectedModifiedAvatar.uid);
            EditorGUILayout.LabelField("File Path: " + selectedModifiedAvatar.filePath);
            if (!string.IsNullOrEmpty(selectedModifiedAvatar.thumbnailPath)) {
                Texture2D thumbnail = LoadThumbnail(selectedModifiedAvatar.thumbnailPath);
                if (thumbnail != null) {
                    GUILayout.Box(thumbnail, GUILayout.Width(200), GUILayout.Height(200));
                }
                EditorGUILayout.LabelField("Thumbnail Path: " + selectedModifiedAvatar.thumbnailPath);
            }
            EditorGUILayout.LabelField("Description: " + selectedModifiedAvatar.description);
            EditorGUILayout.LabelField("Base Avatar ID: " + selectedModifiedAvatar.baseAvatarId);
            
            EditorGUILayout.LabelField("Tags:", EditorStyles.boldLabel);
            if (selectedModifiedAvatar.tags != null && selectedModifiedAvatar.tags.Count > 0) {
                EditorGUI.indentLevel++;
                foreach (var tag in selectedModifiedAvatar.tags) {
                    EditorGUILayout.LabelField("- " + tag);
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space(20);
        if (GUILayout.Button("Close", GUILayout.Width(100))) {
            showDetailWindow = false;
        }

        GUILayout.EndArea();
    }
}

public class AssetsData {
    public static AvatarDataList LoadAvatarData() {
        string avatarListPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/VAMF/VAMF_Avatar.json";
        if (!File.Exists(avatarListPath)) {
            string directoryPath = Path.GetDirectoryName(avatarListPath);
            if (!Directory.Exists(directoryPath)) {  
                Directory.CreateDirectory(directoryPath);
            }
            File.Create(avatarListPath).Close();
            AvatarDataList initData = new AvatarDataList();
            string initJson = JsonUtility.ToJson(initData, true);
            File.WriteAllText(avatarListPath, initJson);
            return initData;
        }

        string json = File.ReadAllText(avatarListPath);
        return JsonUtility.FromJson<AvatarDataList>(json);
    }

    public static List<AvatarDataList.baseAvatarInfo> GetBaseAvatarList() {
        return LoadAvatarData().baseAvatarList;
    }
    
    public static List<AvatarDataList.modifiedAvatarInfo> GetModifiedAvatarList() {
        return LoadAvatarData().modifiedAvatarList;
    }
}
