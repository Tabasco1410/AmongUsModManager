using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Among_Us_ModManager_Updater
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                WriteLog("バージョン情報を取得中...");

                using WebClient client = new WebClient();
                string versionText = await client.DownloadStringTaskAsync("https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/version.txt");
                string version = versionText.Trim();

                WriteLog($"最新バージョン: {version}");

                string zipUrl = $"https://github.com/Tabasco1410/AmongUsModManager/releases/download/{version}/Among.Us_ModManager{version}.zip";
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_{version}.zip");
                string extractPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_Extracted_{version}");

                WriteLog($"ダウンロードURL: {zipUrl}");

                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                Directory.CreateDirectory(extractPath);

                WriteLog("アップデートファイルをダウンロード中...");
                progressBar1.Value = 10;
                await client.DownloadFileTaskAsync(new Uri(zipUrl), tempZipPath);
                WriteLog("ダウンロード完了");

                progressBar1.Value = 50;
                WriteLog("ファイルを展開中...");
                ZipFile.ExtractToDirectory(tempZipPath, extractPath);
                WriteLog("展開完了");

                string FindExeDirectory(string root)
                {
                    foreach (var file in Directory.GetFiles(root))
                    {
                        if (Path.GetFileName(file) == "Among Us_ModManager.exe")
                            return root;
                    }

                    foreach (var dir in Directory.GetDirectories(root))
                    {
                        string found = FindExeDirectory(dir);
                        if (found != null)
                            return found;
                    }

                    return null;
                }

                string exeDirectory = FindExeDirectory(extractPath);
                string targetDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (exeDirectory != null)
                {
                    WriteLog($"Among Us_ModManager.exe を含むフォルダを発見: {exeDirectory}");
                    WriteLog("ファイルを正しい位置に移動中...");

                    MoveDirectoryContentsFlat(exeDirectory, targetDirectory);
                }
                else
                {
                    WriteLog("エラー: Among Us_ModManager.exe が見つかりません（展開フォルダ）");
                    return;
                }

                try { Directory.Delete(extractPath, true); } catch { }

                string bootstrapperPath = Path.Combine(targetDirectory, "Bootstrapper.exe");
                if (File.Exists(bootstrapperPath))
                {
                    WriteLog("Bootstrapper を起動して、Updater の更新を完了します...");

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = bootstrapperPath,
                        Arguments = $"\"{Application.ExecutablePath}\"",
                        UseShellExecute = true
                    });

                    await Task.Delay(500);
                    Application.Exit();
                    return;
                }

                progressBar1.Value = 80;
                string exePath = Path.Combine(targetDirectory, "Among Us_ModManager.exe");

                if (File.Exists(exePath))
                {
                    WriteLog("Mod Manager を起動中...");
                    Process.Start(exePath);
                }
                else
                {
                    WriteLog("エラー: Among Us_ModManager.exe が見つかりません（最終確認）");
                }

                progressBar1.Value = 100;
                WriteLog("アップデート完了");

                await Task.Delay(1000);
                Application.Exit();
            }
            catch (Exception ex)
            {
                WriteLog($"エラー: {ex.Message}");
            }
        }

        private void WriteLog(string text)
        {
            logBox.AppendText(text + "\r\n");
            LogOutput.Log(text);
        }

        void MoveDirectoryContentsFlat(string sourceDir, string destDir)
        {
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceDir, file);
                string destFile = Path.Combine(destDir, Path.GetFileName(file));

                try
                {
                    if (File.Exists(destFile))
                        File.Delete(destFile);

                    File.Copy(file, destFile, true);
                    WriteLog($"移動: {relativePath}");
                }
                catch (Exception ex)
                {
                    WriteLog($"移動失敗: {relativePath} → {ex.Message}");
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // 不使用イベント
        }
    }
}
