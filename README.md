# Unity-Script
エディタ拡張置き場
- [Exporter](Exporter.cs): unitypackageとフォルダを吐くやつ
    - `.gitignore`を考慮
    - masterにいないと出力できず最新のtagをバージョンとして使用
    - tagが先頭commitに紐づいていないと警告
    - 雑なAI出力なので注意
- [UP Import](UP-Import.cs): 指定フォルダ内のunitypackageを一括インポートするやつ
    - [UP Export](https://github.com/4OF-fof/Eagle-UnityPackageExport)との併用を想定
