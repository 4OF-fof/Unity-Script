using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace _4OF.devTools {
    public class Exporter : EditorWindow {
        private string _exportPath = "";
        private string _gitignoreContent = "";
        private Vector2 _gitignoreScrollPosition;
        private string _ignoreFiles = "";
        private string _outputDirectory = "";
        private Vector2 _scrollPosition;
        private bool _useGitignore = true;
        private string _version = "1.0.0";

        private void OnEnable() {
            // デフォルトの出力先をDownloadsフォルダーに設定
            _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            LoadGitignoreContent();
        }

        private void OnGUI() {
            using var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition);
            _scrollPosition = scrollView.scrollPosition;

            GUILayout.Label("エクスポート設定", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // エクスポートパス
            EditorGUILayout.LabelField("エクスポートパス (Assetsフォルダ相対):");
            using (new EditorGUILayout.HorizontalScope()) {
                var newExportPath = EditorGUILayout.TextField(_exportPath);
                if (newExportPath != _exportPath) {
                    _exportPath = newExportPath;
                    LoadGitignoreContent();
                }

                if (GUILayout.Button("フォルダ選択", GUILayout.Width(100))) {
                    var selectedPath = EditorUtility.OpenFolderPanel("エクスポートフォルダを選択", "Assets", "");
                    if (!string.IsNullOrEmpty(selectedPath)) {
                        if (selectedPath.StartsWith(Application.dataPath)) {
                            // gitブランチチェック（選択したフォルダ直下の.gitを確認）
                            var currentBranch = GetCurrentGitBranch(selectedPath);
                            if (currentBranch != "master" && currentBranch != "main") {
                                EditorUtility.DisplayDialog("エラー", 
                                    $"エクスポートはmasterブランチでのみ実行可能です。\n現在のブランチ: {currentBranch}", "OK");
                                return;
                            }
                                
                            _exportPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                            LoadGitignoreContent();
                                
                            // 最新のtagをバージョンとして取得（選択したフォルダ直下の.gitから）
                            var latestTag = GetLatestGitTag(selectedPath);
                            if (!string.IsNullOrEmpty(latestTag)) {
                                _version = latestTag;
                                
                                // tagが紐づいているコミットとmasterの先頭コミットを比較
                                if (!IsTagOnLatestMasterCommit(selectedPath, latestTag)) {
                                    EditorUtility.DisplayDialog("警告", 
                                        $"最新のtag '{latestTag}' はmasterブランチの先頭コミットに紐づいていません。\nエクスポートを続ける前に確認してください。", "OK");
                                }
                            }
                        }
                        else {
                            EditorUtility.DisplayDialog("エラー", "Assetsディレクトリ内のフォルダを選択してください。", "OK");
                        }
                    }
                }
            }
            EditorGUILayout.Space();

            // 出力先ディレクトリ
            EditorGUILayout.LabelField("出力先ディレクトリ:");
            using (new EditorGUILayout.HorizontalScope()) {
                _outputDirectory = EditorGUILayout.TextField(_outputDirectory);
                if (GUILayout.Button("参照", GUILayout.Width(100))) {
                    var selectedPath = EditorUtility.OpenFolderPanel("出力先ディレクトリを選択", _outputDirectory, "");
                    if (!string.IsNullOrEmpty(selectedPath)) _outputDirectory = selectedPath;
                }
            }
            EditorGUILayout.Space();

            // バージョン
            EditorGUILayout.LabelField("バージョン:");
            _version = EditorGUILayout.TextField(_version);
            EditorGUILayout.Space();

            // gitignore参照トグル
            using (new EditorGUILayout.HorizontalScope()) {
                _useGitignore = EditorGUILayout.Toggle(".gitignoreを使用", _useGitignore);
                if (GUILayout.Button("再読み込み", GUILayout.Width(100))) LoadGitignoreContent();
            }

            switch (_useGitignore) {
                // gitignore内容を表示
                case true when !string.IsNullOrEmpty(_gitignoreContent): {
                    EditorGUILayout.LabelField(".gitignoreの内容:", EditorStyles.boldLabel);
                    using var gitignoreScrollView = new EditorGUILayout.ScrollViewScope(_gitignoreScrollPosition, GUILayout.Height(100));
                    _gitignoreScrollPosition = gitignoreScrollView.scrollPosition;
                    EditorGUILayout.SelectableLabel(_gitignoreContent, GUILayout.ExpandHeight(true));
                    break;
                }
                case true when string.IsNullOrEmpty(_gitignoreContent):
                    EditorGUILayout.HelpBox(".gitignoreファイルが見つかりません。", MessageType.Warning);
                    break;
            }

            EditorGUILayout.Space();

            // Ignore Files
            EditorGUILayout.LabelField("除外ファイル (1行に1つ、ワイルドカード使用可):");
            _ignoreFiles = EditorGUILayout.TextArea(_ignoreFiles, GUILayout.Height(100));
            EditorGUILayout.Space();

            if (GUILayout.Button("エクスポート", GUILayout.Height(30))) {
                ExportUnityPackage();
                ExportRawData();
            }
        }

        [MenuItem("devTools/パッケージエクスポート")]
        public static void Open() {
            var window = GetWindow<Exporter>();
            window.titleContent = new GUIContent("パッケージエクスポート");
            window.Show();
        }

        private void ExportUnityPackage() {
            if (string.IsNullOrEmpty(_exportPath)) {
                EditorUtility.DisplayDialog("エラー", "エクスポートパスを指定してください。", "OK");
                return;
            }

            if (!Directory.Exists(_exportPath) && !File.Exists(_exportPath)) {
                EditorUtility.DisplayDialog("エラー", "指定されたエクスポートパスが存在しません。", "OK");
                return;
            }

            if (!Directory.Exists(_outputDirectory)) {
                EditorUtility.DisplayDialog("エラー", "出力先ディレクトリが存在しません。", "OK");
                return;
            }

            var packageName = Path.GetFileName(_exportPath.TrimEnd('/', '\\'));
            var exportFolderName = $"exporter_{packageName}_{_version}";
            var exportFolder = Path.Combine(_outputDirectory, exportFolderName);
            var outputPath = Path.Combine(exportFolder, packageName +  "_" + _version + ".unitypackage");

            // 既存フォルダの確認
            if (Directory.Exists(exportFolder)) {
                if (!EditorUtility.DisplayDialog("確認", 
                    $"出力先が既に存在します:\n{exportFolder}\n\n上書きしますか？", 
                    "上書き", "キャンセル")) {
                    return;
                }
            }

            Directory.CreateDirectory(exportFolder);

            try {
                var assetPaths = GetFilteredAssetPaths(_exportPath);

                if (assetPaths.Length == 0) {
                    EditorUtility.DisplayDialog("警告", "エクスポートするファイルが見つかりません。", "OK");
                    return;
                }

                AssetDatabase.ExportPackage(assetPaths, outputPath, ExportPackageOptions.Recurse);
                EditorUtility.DisplayDialog("成功",
                    $"UnityPackageをエクスポートしました:\n{outputPath}\n\nエクスポートされたファイル数: {assetPaths.Length}", "OK");
            }
            catch (Exception e) {
                EditorUtility.DisplayDialog("エラー", $"UnityPackageのエクスポートに失敗しました:\n{e.Message}", "OK");
            }
        }

        private void ExportRawData() {
            if (string.IsNullOrEmpty(_exportPath)) {
                EditorUtility.DisplayDialog("エラー", "エクスポートパスを指定してください。", "OK");
                return;
            }

            if (!Directory.Exists(_outputDirectory)) {
                EditorUtility.DisplayDialog("エラー", "出力先ディレクトリが存在しません。", "OK");
                return;
            }

            var sourcePath = _exportPath;
            if (!Path.IsPathRooted(sourcePath))
                sourcePath = Path.Combine(Application.dataPath.Replace("Assets", ""), _exportPath);

            if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath)) {
                EditorUtility.DisplayDialog("エラー", "指定されたエクスポートパスが存在しません。", "OK");
                return;
            }

            var targetName = Path.GetFileName(_exportPath.TrimEnd('/', '\\'));
            var exportFolderName = $"exporter_{targetName}_{_version}";
            var exportFolder = Path.Combine(_outputDirectory, exportFolderName);
            var targetPath = Path.Combine(exportFolder, targetName);

            Directory.CreateDirectory(exportFolder);

            try {
                if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true);

                CopyDirectory(sourcePath, targetPath);
                EditorUtility.RevealInFinder(targetPath);
            }
            catch (Exception e) {
                EditorUtility.DisplayDialog("エラー", $"生データのエクスポートに失敗しました:\n{e.Message}", "OK");
            }
        }

        private string[] GetFilteredAssetPaths(string rootPath) {
            var allPaths = new List<string>();

            if (File.Exists(rootPath)) {
                allPaths.Add(rootPath);
            }
            else if (Directory.Exists(rootPath)) {
                var guids = AssetDatabase.FindAssets("", new[] { rootPath });
                foreach (var guid in guids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    allPaths.Add(path);
                }
            }

            var ignorePatterns = GetIgnorePatterns();
            var filteredPaths = allPaths.Where(path => !ShouldIgnore(path, ignorePatterns)).ToArray();

            return filteredPaths;
        }

        private List<string> GetIgnorePatterns() {
            var patterns = new List<string> {
                // デフォルトで除外するパターン
                ".git",
                ".gitignore"
            };

            // ユーザー指定の無視パターン
            if (!string.IsNullOrEmpty(_ignoreFiles)) {
                var lines = _ignoreFiles.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                patterns.AddRange(lines.Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)));
            }

            // gitignoreを使用する場合（エクスポートパス直下を探索）
            if (_useGitignore && !string.IsNullOrEmpty(_exportPath)) {
                var gitignorePath = GetGitignorePath();
                if (File.Exists(gitignorePath)) {
                    var gitignoreLines = File.ReadAllLines(gitignorePath);
                    patterns.AddRange(gitignoreLines
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#")));
                }
            }

            return patterns;
        }

        private bool ShouldIgnore(string path, List<string> patterns) {
            foreach (var pattern in patterns)
                if (MatchesPattern(path, pattern))
                    return true;

            return false;
        }

        private bool MatchesPattern(string path, string pattern) {
            // 簡易的なパターンマッチング
            pattern = pattern.Replace("\\", "/");
            path = path.Replace("\\", "/");

            // ディレクトリパターン
            if (pattern.EndsWith("/"))
                return path.Contains("/" + pattern.TrimEnd('/') + "/") || path.EndsWith("/" + pattern.TrimEnd('/'));

            // ワイルドカードパターン
            if (pattern.Contains("*")) {
                var regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*\\*/", ".*")
                    .Replace("\\*", "[^/]*")
                    .Replace("\\?", ".") + "$";
                return Regex.IsMatch(path, regexPattern);
            }

            // 完全一致または終端一致
            return path.EndsWith(pattern) || path.Contains("/" + pattern);
        }

        private void CopyDirectory(string sourceDir, string targetDir) {
            var ignorePatterns = GetIgnorePatterns();

            if (File.Exists(sourceDir)) {
                // 単一ファイルの場合
                var targetFile = Path.Combine(Path.GetDirectoryName(targetDir) ?? string.Empty, Path.GetFileName(sourceDir));
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? string.Empty);
                File.Copy(sourceDir, targetFile, true);
                return;
            }

            Directory.CreateDirectory(targetDir);

            // ファイルをコピー
            foreach (var file in Directory.GetFiles(sourceDir)) {
                var relativePath = GetRelativePath(Application.dataPath.Replace("Assets", ""), file);
                if (!ShouldIgnore(relativePath, ignorePatterns)) {
                    var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                    File.Copy(file, targetFile, true);
                }
            }

            // サブディレクトリを再帰的にコピー
            foreach (var directory in Directory.GetDirectories(sourceDir)) {
                var relativePath = GetRelativePath(Application.dataPath.Replace("Assets", ""), directory);
                if (!ShouldIgnore(relativePath, ignorePatterns)) {
                    var targetSubDir = Path.Combine(targetDir, Path.GetFileName(directory));
                    CopyDirectory(directory, targetSubDir);
                }
            }
        }

        private string GetRelativePath(string fromPath, string toPath) {
            var fromUri = new Uri(fromPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            var toUri = new Uri(toPath);
            var relativeUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private void LoadGitignoreContent() {
            if (string.IsNullOrEmpty(_exportPath)) {
                _gitignoreContent = "";
                return;
            }

            var gitignorePath = GetGitignorePath();
            if (File.Exists(gitignorePath))
                try {
                    _gitignoreContent = File.ReadAllText(gitignorePath);
                }
                catch (Exception e) {
                    _gitignoreContent = "";
                    Debug.LogWarning($".gitignoreの読み込みに失敗しました: {e.Message}");
                }
            else
                _gitignoreContent = "";
        }

        private string GetGitignorePath() {
            if (string.IsNullOrEmpty(_exportPath)) return "";

            var sourcePath = _exportPath;
            if (!Path.IsPathRooted(sourcePath))
                sourcePath = Path.Combine(Application.dataPath.Replace("Assets", ""), _exportPath);

            // ファイルの場合はそのディレクトリを使用
            if (File.Exists(sourcePath)) sourcePath = Path.GetDirectoryName(sourcePath);

            return sourcePath != null ? Path.Combine(sourcePath, ".gitignore") : "";
        }

        private string GetCurrentGitBranch(string gitRepoPath = null) {
            try {
                var workingDirectory = gitRepoPath ?? Application.dataPath.Replace("Assets", "");
                var processStartInfo = new ProcessStartInfo {
                    FileName = "git",
                    Arguments = "rev-parse --abbrev-ref HEAD",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null) return "unknown";
                    
                var output = process.StandardOutput.ReadToEnd().Trim();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output)) {
                    return output;
                }

                if (!string.IsNullOrEmpty(error)) {
                    Debug.LogWarning($"Git branch check error: {error}");
                }
            }
            catch (Exception e) {
                Debug.LogWarning($"Failed to get git branch: {e.Message}");
            }

            return "unknown";
        }

        private string GetLatestGitTag(string gitRepoPath = null) {
            try {
                var workingDirectory = gitRepoPath ?? Application.dataPath.Replace("Assets", "");
                var processStartInfo = new ProcessStartInfo {
                    FileName = "git",
                    Arguments = "describe --tags --abbrev=0",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null) return "";
                    
                var output = process.StandardOutput.ReadToEnd().Trim();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output)) {
                    return output;
                }

                if (!string.IsNullOrEmpty(error)) {
                    Debug.LogWarning($"Git tag fetch error: {error}");
                }
            }
            catch (Exception e) {
                Debug.LogWarning($"Failed to get latest git tag: {e.Message}");
            }

            return "";
        }

        private bool IsTagOnLatestMasterCommit(string gitRepoPath, string tag) {
            try {
                var workingDirectory = gitRepoPath ?? Application.dataPath.Replace("Assets", "");
                
                // tagが紐づいているコミットハッシュを取得
                var tagCommit = GetCommitHash(workingDirectory, tag);
                if (string.IsNullOrEmpty(tagCommit)) return false;
                
                // masterブランチの先頭コミットハッシュを取得
                var masterCommit = GetCommitHash(workingDirectory, "master");
                if (string.IsNullOrEmpty(masterCommit)) {
                    // masterがない場合はmainを確認
                    masterCommit = GetCommitHash(workingDirectory, "main");
                }
                
                return tagCommit == masterCommit;
            }
            catch (Exception e) {
                Debug.LogWarning($"Failed to check tag commit: {e.Message}");
                return true; // エラー時は警告を表示しない
            }
        }

        private string GetCommitHash(string workingDirectory, string reference) {
            try {
                var processStartInfo = new ProcessStartInfo {
                    FileName = "git",
                    Arguments = $"rev-parse {reference}",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null) return "";
                    
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output)) {
                    return output;
                }
            }
            catch (Exception e) {
                Debug.LogWarning($"Failed to get commit hash for {reference}: {e.Message}");
            }

            return "";
        }
    }
}