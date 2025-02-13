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

    void OnEnable() {
        LoadAvatarData();
    }

    private void LoadAvatarData() {
        avatarData = AssetsData.LoadAvatarData();
    }

    void OnGUI() {
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

        GUILayout.Label("Base Avatar", subTitle);
        int baseCount = 0;
        EditorGUILayout.BeginHorizontal();
        foreach (var baseAvatar in avatarData.baseAvatarList) {
            if (baseCount > 0 && baseCount % 4 == 0) {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width / 4 - 10));
            EditorGUILayout.LabelField("Name: " + baseAvatar.avatarName);
            EditorGUILayout.LabelField("UID: " + baseAvatar.uid);
            EditorGUILayout.LabelField("File: " + baseAvatar.filePath);
            if (!string.IsNullOrEmpty(baseAvatar.thumbnailPath)) {
                EditorGUILayout.LabelField("Thumbnail: " + baseAvatar.thumbnailPath);
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
            EditorGUILayout.LabelField("Name: " + modifiedAvatar.avatarName);
            EditorGUILayout.LabelField("UID: " + modifiedAvatar.uid);
            EditorGUILayout.LabelField("File: " + modifiedAvatar.filePath);
            if (!string.IsNullOrEmpty(modifiedAvatar.thumbnailPath)) {
                EditorGUILayout.LabelField("Thumbnail: " + modifiedAvatar.thumbnailPath);
            }
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
