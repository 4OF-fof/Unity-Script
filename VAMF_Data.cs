using System;
using System.Collections.Generic;

[Serializable]
public enum AssetType {
    Unregistered,
    Avatar,
    Hair,
    Cloth,
    Accessory,
    Gimmick,
    Script,
    Other
}

[Serializable]
public class AssetDataList {
    [Serializable]
    public class assetInfo {
        public string uid;
        public string assetName;
        public string filePath;
        public string sourcePath;
        public string url;
        public string thumbnailPath;
        public string description;
        public List<string> dependencies;  // 依存アセットのUIDリスト
        public AssetType assetType;

        public assetInfo Clone() {
            return new assetInfo {
                uid = this.uid,
                assetName = this.assetName,
                filePath = this.filePath,
                sourcePath = this.sourcePath,
                url = this.url,
                thumbnailPath = this.thumbnailPath,
                description = this.description,
                dependencies = new List<string>(this.dependencies ?? new List<string>()),
                assetType = this.assetType
            };
        }
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