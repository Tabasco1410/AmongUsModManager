using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Among_Us_ModManeger // ルート名前空間に移動
{
    /// <summary>
    /// MODの情報を保持するデータ構造
    /// UIの更新を通知するためにINotifyPropertyChangedを実装
    /// </summary>
    public class ModInfo : INotifyPropertyChanged
    {
        private string _installedVersion = "未導入";
        private string _latestVersion = "取得中...";
        private bool _isUpdateAvailable;
        private DateTime? _updateDetectionTime;
        private bool _isAutoUpdateEnabled;
        private string _detectionStatus = "未確認";
        private string _currentDllDownloadUrl; // 実際にダウンロードするDLLのURL

        public string Name { get; set; } // MOD名
        public string GitHubUrl { get; set; } // GitHubリポジトリのURL
        // BepInExフォルダからの相対パス (例: "plugins/TownOfHost.dll", "nebula/Nebula.dll")
        public List<string> DllPaths { get; set; }
        // GitHubリリースのDLLを特定するためのファイル名リスト。通常はDllPathsから派生する
        public List<string> DllNamesForDownload { get; set; }

        // 検出されたAmong Usの各インスタンスでのこのMODの導入状況とバージョン
        public ObservableCollection<InstalledModInstance> InstalledInstances { get; set; }

        // 導入済みMODのバージョン (全体的な表示用)
        public string InstalledVersion
        {
            get => _installedVersion;
            set
            {
                if (_installedVersion != value)
                {
                    _installedVersion = value;
                    OnPropertyChanged();
                    CheckForUpdate(); // バージョンが更新されたらアップデートチェックを再実行
                }
            }
        }

        // 最新のMODバージョン
        public string LatestVersion
        {
            get => _latestVersion;
            set
            {
                if (_latestVersion != value)
                {
                    _latestVersion = value;
                    OnPropertyChanged();
                    CheckForUpdate(); // バージョンが更新されたらアップデートチェックを再実行
                }
            }
        }

        // アップデートがあるかどうか
        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set
            {
                if (_isUpdateAvailable != value)
                {
                    _isUpdateAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        // アップデートが検出された日時
        public DateTime? UpdateDetectionTime
        {
            get => _updateDetectionTime;
            set
            {
                if (_updateDetectionTime != value)
                {
                    _updateDetectionTime = value;
                    OnPropertyChanged();
                }
            }
        }

        // 自動アップデートが有効かどうか
        public bool IsAutoUpdateEnabled
        {
            get => _isAutoUpdateEnabled;
            set
            {
                if (_isAutoUpdateEnabled != value)
                {
                    _isAutoUpdateEnabled = value;
                    OnPropertyChanged();
                    // 将来的には設定の保存ロジックをここに追加
                }
            }
        }

        // MODの検出ステータス (例: "導入済み", "未導入", "未確認", "エラー")
        public string DetectionStatus
        {
            get => _detectionStatus;
            set
            {
                if (_detectionStatus != value)
                {
                    _detectionStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        // ダウンロードするDLLファイルのURL
        public string CurrentDllDownloadUrl
        {
            get => _currentDllDownloadUrl;
            set
            {
                if (_currentDllDownloadUrl != value)
                {
                    _currentDllDownloadUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ModInfoクラスのコンストラクタ
        /// </summary>
        /// <param name="name">MOD名</param>
        /// <param name="gitHubUrl">GitHubリポジリのURL</param>
        /// <param name="dllPaths">BepInExフォルダからの相対DLLパス</param>
        /// <param name="dllNamesForDownload">ダウンロード時にチェックするDLLファイル名（省略可能、デフォルトはDllPathsから抽出）</param>
        public ModInfo(string name, string gitHubUrl, List<string> dllPaths, List<string> dllNamesForDownload = null)
        {
            Name = name;
            GitHubUrl = gitHubUrl;
            DllPaths = dllPaths;
            // ダウンロード時のファイル名が指定されていなければ、DllPathsからファイル名を抽出
            DllNamesForDownload = dllNamesForDownload ?? dllPaths.Select(p => Path.GetFileName(p)).ToList();
            InstalledInstances = new ObservableCollection<InstalledModInstance>();
        }

        /// <summary>
        /// 導入済みバージョンと最新バージョンを比較し、アップデートの有無を判定
        /// </summary>
        private void CheckForUpdate()
        {
            // いずれかの情報が未取得の場合はアップデート確認不可
            if (InstalledVersion.Contains("未導入") || LatestVersion == "取得中..." || LatestVersion == "不明")
            {
                IsUpdateAvailable = false;
                UpdateDetectionTime = null;
                return;
            }

            // 導入済みインスタンスのいずれかが最新バージョンと異なる場合、アップデートがあると判断
            // ここでは簡易的に、どのインスタンスでもバージョンが一致しない場合はアップデートありとする
            bool foundOutdatedInstance = false;
            foreach (var instance in InstalledInstances)
            {
                if (instance.IsInstalled && instance.Version != LatestVersion)
                {
                    foundOutdatedInstance = true;
                    break;
                }
            }

            // バージョン文字列が異なる場合、または未導入で最新版がある場合、アップデートがあると判断
            if (foundOutdatedInstance || (InstalledVersion.Contains("未導入") && LatestVersion != "取得中..." && LatestVersion != "不明"))
            {
                IsUpdateAvailable = true;
                UpdateDetectionTime = DateTime.Now;
            }
            else
            {
                IsUpdateAvailable = false;
                UpdateDetectionTime = null;
            }
        }

        // PropertyChangedイベントの実装
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// 検出されたAmong Usの各インスタンスでのこのMODの導入状況を保持します。
        /// </summary>
        public class InstalledModInstance : INotifyPropertyChanged
        {
            private string _installationName;
            private string _version;
            private bool _isInstalled; // このインスタンスにこのModが導入されているか

            public string InstallationName
            {
                get => _installationName;
                set { if (_installationName != value) { _installationName = value; OnPropertyChanged(); } }
            }

            public string Version
            {
                get => _version;
                set { if (_version != value) { _version = value; OnPropertyChanged(); } }
            }
            public bool IsInstalled
            {
                get => _isInstalled;
                set { if (_isInstalled != value) { _isInstalled = value; OnPropertyChanged(); } }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
