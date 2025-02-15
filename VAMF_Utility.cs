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
                    if (unityPackageFile == "MULTIPLE_UNITYPACKAGES_FOUND") {
                        newAsset.filePath = "MULTIPLE_UNITYPACKAGES_FOUND";
                    } else if (!string.IsNullOrEmpty(unityPackageFile)) {
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
            if (!Directory.Exists(tempPath)) {
                Directory.CreateDirectory(tempPath);
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tempPath);

            string[] files = Directory.GetFiles(tempPath, "*.unitypackage", SearchOption.AllDirectories);
            unityPackageFiles.AddRange(files);

            if (unityPackageFiles.Count == 1) {
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
                return "MULTIPLE_UNITYPACKAGES_FOUND";
            }

            return string.Empty;
        }
        catch (Exception e) {
            Debug.LogError($"Error extracting zip file: {e.Message}");
            return string.Empty;
        }
        finally {
            if (Directory.Exists(tempPath)) {
                Directory.Delete(tempPath, true);
            }
        }
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
