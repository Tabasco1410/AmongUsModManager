using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Among_Us_ModManeger
{
    /// <summary>
    /// 検出されたAmong Usのインストール情報を保持します。
    /// </summary>
    public class AmongUsInstallation : INotifyPropertyChanged
    {
        private string _name;
        private string _installPath;
        private string _bepInExPath; // BepInExフォルダのフルパス
        private string _exePath;     // Among Us.exe のフルパス

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string InstallPath
        {
            get => _installPath;
            set
            {
                if (_installPath != value)
                {
                    _installPath = value;
                    // InstallPathが設定されたときに派生プロパティも更新
                    _bepInExPath = Path.Combine(value, "BepInEx");
                    _exePath = Path.Combine(value, "Among Us.exe");
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BepInExPath));
                    OnPropertyChanged(nameof(ExePath));
                }
            }
        }

        public string BepInExPath => _bepInExPath;
        public string ExePath => _exePath;

        /// <summary>
        /// AmongUsInstallationの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="path">Among Usのインストールディレクトリのフルパス。</param>
        public AmongUsInstallation(string path)
        {
            InstallPath = path;
            // フォルダ名をインスタンス名とする（例: "Among Us", "Among Us_MODDED"）
            Name = Path.GetFileName(path);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
