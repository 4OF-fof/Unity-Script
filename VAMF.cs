using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
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
        public List<string> childAvatarIdList;
    }
    [Serializable]
    public class modifiedAvatarInfo {
        public string uid;
        public string avatarName;
        public string filePath;
        public string thumbnailPath;
        public string description;
        public int baseAvatarId;
        public List<int> parentAvatarIdList;
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

    private string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/VAMF/";

    private AvatarDataList avatarData;
    private AvatarDataList.baseAvatarInfo selectedBaseAvatar;
    private AvatarDataList.modifiedAvatarInfo selectedModifiedAvatar;
    private bool isBaseAvatar = true;
    private bool showDetailWindow = false;
    private Dictionary<string, Texture2D> thumbnailCache = new Dictionary<string, Texture2D>();
    private Vector2 scrollPosition;

    void OnEnable() {
        LoadAvatarData();
    }

    private void LoadAvatarData() {
        avatarData = Utility.LoadAvatarData();
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

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("VRChat Avatar Modify Framework", Style.title);
        if (GUILayout.Button("Sync Avatar List", Style.button)) {
            LoadAvatarData();
        }
        EditorGUILayout.EndHorizontal();
        
        Color oldColor = GUI.color;
        GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
        GUILayout.Box("", Style.divLine);
        GUI.color = oldColor;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.Label("Base Avatar", Style.subTitle);
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

        GUILayout.Label("Modified Avatar", Style.subTitle);
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
        if (isBaseAvatar && selectedBaseAvatar != null) {
            if (!string.IsNullOrEmpty(selectedBaseAvatar.thumbnailPath)) {
                Texture2D thumbnail = LoadThumbnail(rootPath + selectedBaseAvatar.thumbnailPath);
                if (thumbnail != null) {
                    GUILayout.Box(thumbnail, GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize));
                }
            }
        } else if (!isBaseAvatar && selectedModifiedAvatar != null) {
            if (!string.IsNullOrEmpty(selectedModifiedAvatar.thumbnailPath)) {
                Texture2D thumbnail = LoadThumbnail(rootPath + selectedModifiedAvatar.thumbnailPath);
                if (thumbnail != null) {
                    GUILayout.Box(thumbnail, GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize));
                }
            }
        }
        EditorGUILayout.EndVertical();

        // 情報表示部分
        EditorGUILayout.BeginVertical(GUILayout.Width(windowWidth - thumbnailSize - 40));
        if (isBaseAvatar && selectedBaseAvatar != null) {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Base Avatar Details", Style.detailTitle);
            if (GUILayout.Button("Edit Avatar Info", Style.detailEditInfoButton)) {
                Debug.Log("Edit Avatar Info");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Name", Style.detailContentName);
            EditorGUILayout.LabelField(selectedBaseAvatar.avatarName);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("File Path", Style.detailContentName);
            EditorGUILayout.LabelField(selectedBaseAvatar.filePath, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Thumbnail Path", Style.detailContentName);
            EditorGUILayout.LabelField(selectedBaseAvatar.thumbnailPath, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
        }else if (!isBaseAvatar && selectedModifiedAvatar != null) {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Modified Avatar Details", Style.detailTitle);
            if (GUILayout.Button("Edit Avatar Info", Style.detailEditInfoButton)) {
                Debug.Log("Edit Avatar Info");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Name", Style.detailContentName);
            EditorGUILayout.LabelField(selectedModifiedAvatar.avatarName);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Description", Style.detailContentName);
            EditorGUILayout.LabelField(selectedModifiedAvatar.description, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("File Path", Style.detailContentName);
            EditorGUILayout.LabelField(selectedModifiedAvatar.filePath, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Thumbnail Path", Style.detailContentName);
            EditorGUILayout.LabelField(selectedModifiedAvatar.thumbnailPath, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
