using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO.Compression;

public class Utility {
    public static AssetDataList LoadAvatarData() {
        string avatarListPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/VAMF/VAMF.json";
        if (!File.Exists(avatarListPath)) {
            string directoryPath = Path.GetDirectoryName(avatarListPath);
            if (!Directory.Exists(directoryPath)) {  
                Directory.CreateDirectory(directoryPath);
            }
            File.Create(avatarListPath).Close();
            AssetDataList initData = new AssetDataList();
            string initJson = JsonUtility.ToJson(initData, true);
            File.WriteAllText(avatarListPath, initJson);
            return initData;
        }

        string json = File.ReadAllText(avatarListPath);
        return JsonUtility.FromJson<AssetDataList>(json);
    }

    public static List<AssetDataList.baseAvatarInfo> GetBaseAvatarList() {
        return LoadAvatarData().baseAvatarList;
    }
    
    public static List<AssetDataList.modifiedAvatarInfo> GetModifiedAvatarList() {
        return LoadAvatarData().modifiedAvatarList;
    }

    public static Texture2D LoadThumbnail(string path, Dictionary<string, Texture2D> thumbnailCache) {
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

    public static AssetDataList LoadAssetData() {
        string assetListPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/VAMF/VAMF.json";
        if (!File.Exists(assetListPath)) {
            string directoryPath = Path.GetDirectoryName(assetListPath);
            if (!Directory.Exists(directoryPath)) {  
                Directory.CreateDirectory(directoryPath);
            }
            File.Create(assetListPath).Close();
            AssetDataList initData = new AssetDataList();
            string initJson = JsonUtility.ToJson(initData, true);
            File.WriteAllText(assetListPath, initJson);
            return initData;
        }

        string json = File.ReadAllText(assetListPath);
        return JsonUtility.FromJson<AssetDataList>(json);
    }

    public static void LoadZipList() {
        string assetFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/VAMF/Assets";
        if (!Directory.Exists(assetFolderPath)) {
            Directory.CreateDirectory(assetFolderPath);
        }

        AssetDataList assetData = LoadAssetData();
        List<string> supportedExtensions = new List<string> { ".zip", ".unitypackage" };
        int addedCount = 0;

        string[] files = Directory.GetFiles(assetFolderPath, "*.*", SearchOption.AllDirectories)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()) && 
                          !file.Contains(Path.Combine(assetFolderPath, ".unzip")))
            .ToArray();

        foreach (string file in files) {
            string fileName = Path.GetFileName(file);
            string relativePath = "Assets/" + file.Replace(assetFolderPath + "\\", "").Replace("\\", "/");
            List<string> unityPackageFiles;

            bool exists = assetData.assetList.Any(asset => asset.zipPath == relativePath);
            
            if (!exists) {
                AssetDataList.assetInfo newAsset = new AssetDataList.assetInfo {
                    uid = Guid.NewGuid().ToString(),
                    assetName = Path.GetFileNameWithoutExtension(fileName),
                    zipPath = relativePath
                };

                if (Path.GetExtension(file).ToLower() == ".unitypackage") {
                    newAsset.filePath = relativePath;
                }else{
                    string unityPackageFile = SearchUnityPackage(file);
                    if (!string.IsNullOrEmpty(unityPackageFile)) {
                        newAsset.filePath = "Assets/.unzip/" + Path.GetFileName(unityPackageFile);
                    }
                }
                
                assetData.assetList.Add(newAsset);
                addedCount++;
            }
        }

        if (addedCount > 0) {
            SaveAssetData(assetData);
        }
    }

    public static string SearchUnityPackage(string zipPath) {
        List<string> unityPackageFiles = new List<string>();
        string tempPath = Path.Combine(Path.GetTempPath(), "VAMF_Temp");
        
        try {
            EditorUtility.DisplayProgressBar("Extracting", $"Extracting {Path.GetFileName(zipPath)}...", 0.3f);
            
            if (!Directory.Exists(tempPath)) {
                Directory.CreateDirectory(tempPath);
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tempPath);

            EditorUtility.DisplayProgressBar("Searching", "Searching for UnityPackage files...", 0.6f);
            string[] files = Directory.GetFiles(tempPath, "*.unitypackage", SearchOption.AllDirectories);
            unityPackageFiles.AddRange(files);

            if (unityPackageFiles.Count == 1) {
                EditorUtility.DisplayProgressBar("Copying", "Copying UnityPackage file...", 0.9f);
                string targetDir = Path.GetDirectoryName(zipPath) + "/.unzip";
                if (!Directory.Exists(targetDir)) {
                    Directory.CreateDirectory(targetDir);
                }
                string fileName = Path.GetFileName(unityPackageFiles[0]);
                string targetPath = Path.Combine(targetDir, fileName);
                
                if (File.Exists(targetPath)) {
                    File.Delete(targetPath);
                }
                File.Copy(unityPackageFiles[0], targetPath);
                
                return targetPath;
            } else if (unityPackageFiles.Count > 1) {
                EditorUtility.ClearProgressBar();
                string selectedFile = UnityPackageSelect(unityPackageFiles, zipPath);
                
                if (!string.IsNullOrEmpty(selectedFile)) {
                    EditorUtility.DisplayProgressBar("Copying", "Copying selected UnityPackage file...", 0.9f);
                    string targetDir = Path.GetDirectoryName(zipPath) + "/.unzip";
                    if (!Directory.Exists(targetDir)) {
                        Directory.CreateDirectory(targetDir);
                    }
                    string fileName = Path.GetFileName(selectedFile);
                    string targetPath = Path.Combine(targetDir, fileName);
                    
                    if (File.Exists(targetPath)) {
                        File.Delete(targetPath);
                    }
                    File.Copy(selectedFile, targetPath);
                    
                    return targetPath;
                }
                return string.Empty;
            }

            return string.Empty;
        }
        catch (Exception e) {
            Debug.LogError($"Error extracting zip file: {e.Message}");
            return string.Empty;
        }
        finally {
            EditorUtility.ClearProgressBar();
            if (Directory.Exists(tempPath)) {
                Directory.Delete(tempPath, true);
            }
        }
    }

    private class UnityPackageSelectWindow : EditorWindow {
        private List<string> unityPackageFiles;
        private string selectedFile = "";
        private Vector2 scrollPosition = Vector2.zero;
        private string zipPath;

        public static string ShowWindow(List<string> files, string sourceZipPath) {
            var window = GetWindow<UnityPackageSelectWindow>("Select UnityPackage");
            window.unityPackageFiles = files;
            window.zipPath = sourceZipPath;
            window.minSize = new Vector2(600, 400);
            window.maxSize = new Vector2(600, 400);
            window.ShowModal();
            return window.selectedFile;
        }

        void OnGUI() {
            EditorGUILayout.LabelField("Multiple UnityPackage files found", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField("Source ZIP:", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField(Path.GetFileName(zipPath), EditorStyles.wordWrappedLabel);
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Select UnityPackage:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (unityPackageFiles != null) {
                foreach (var file in unityPackageFiles) {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    using (new EditorGUILayout.HorizontalScope()) {
                        EditorGUILayout.LabelField(Path.GetFileName(file), EditorStyles.boldLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(100))) {
                            selectedFile = file;
                            Close();
                        }
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }
            
            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && 
                !new Rect(0, 0, position.width, position.height).Contains(Event.current.mousePosition)) {
                Close();
            }
        }
    }

    public static string UnityPackageSelect(List<string> unityPackageFiles, string zipPath) {
        return UnityPackageSelectWindow.ShowWindow(unityPackageFiles, zipPath);
    }

    public static List<AssetDataList.assetInfo> GetAssetList() {
        return LoadAssetData().assetList;
    }

    public static void SaveAssetData(AssetDataList assetData) {
        string assetListPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/VAMF/VAMF.json";
        string json = JsonUtility.ToJson(assetData, true);
        File.WriteAllText(assetListPath, json);
    }
}
