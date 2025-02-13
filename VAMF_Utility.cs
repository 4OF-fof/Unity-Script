using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Utility {
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