using System;
using System.Collections.Generic;

[Serializable]
public class AssetDataList {
    [Serializable]
    public class assetInfo {
        public string uid;
        public string assetName;
        public string filePath;
        public string zipPath;
        public string url;
        public string thumbnailPath;
        public string description;
        public List<string> tags;
    }

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

    public List<assetInfo> assetList = new List<assetInfo>();
    public List<baseAvatarInfo> baseAvatarList = new List<baseAvatarInfo>();
    public List<modifiedAvatarInfo> modifiedAvatarList = new List<modifiedAvatarInfo>();
}