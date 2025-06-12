using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Among_Us_ModManeger_Updater
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
                logBox.AppendText("バージョン情報を取得中...\r\n");

                using WebClient client = new WebClient();
                string versionText = await client.DownloadStringTaskAsync("https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/version.txt");
                string version = versionText.Trim();

                logBox.AppendText($"最新バージョン: {version}\r\n");

                string zipUrl = $"https://github.com/Tabasco1410/AmongUsModManeger/releases/download/{version}/Among.Us_ModManeger{version}.zip";
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_{version}.zip");
                string extractPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_Extracted_{version}");

                // 以前の展開フォルダを削除
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                Directory.CreateDirectory(extractPath); // ← ここが重要！

                logBox.AppendText("アップデートファイルをダウンロード中...\r\n");
                progressBar1.Value = 10;
                await client.DownloadFileTaskAsync(new Uri(zipUrl), tempZipPath);
                logBox.AppendText("ダウンロード完了\r\n");

                progressBar1.Value = 50;
                logBox.AppendText("ファイルを展開中...\r\n");

                ZipFile.ExtractToDirectory(tempZipPath, extractPath);
                logBox.AppendText("展開完了\r\n");

                // Among Us_ModManeger.exe を含むフォルダを再帰的に探す
                string FindExeDirectory(string root)
                {
                    foreach (var file in Directory.GetFiles(root))
                    {
                        if (Path.GetFileName(file) == "Among Us_ModManeger.exe")
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
                    logBox.AppendText($"Among Us_ModManeger.exe を含むフォルダを発見: {exeDirectory}\r\n");
                    logBox.AppendText("ファイルを正しい位置に移動中...\r\n");

                    MoveDirectoryContentsFlat(exeDirectory, targetDirectory);
                }
                else
                {
                    logBox.AppendText("エラー: Among Us_ModManeger.exe が見つかりません（展開フォルダ）\r\n");
                    return;
                }

                // 不要な一時フォルダ削除
                try { Directory.Delete(extractPath, true); } catch { }

                // Bootstrapper.exe があればそれを起動して終了
                string bootstrapperPath = Path.Combine(targetDirectory, "Bootstrapper.exe");
                if (File.Exists(bootstrapperPath))
                {
                    logBox.AppendText("Bootstrapper を起動して、Updater の更新を完了します...\r\n");

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
                string exePath = Path.Combine(targetDirectory, "Among Us_ModManeger.exe");

                if (File.Exists(exePath))
                {
                    logBox.AppendText("Mod Manager を起動中...\r\n");
                    Process.Start(exePath);
                }
                else
                {
                    logBox.AppendText("エラー: Among Us_ModManeger.exe が見つかりません（最終確認）\r\n");
                }

                progressBar1.Value = 100;
                logBox.AppendText("アップデート完了\r\n");

                await Task.Delay(1000);
                Application.Exit();
            }
            catch (Exception ex)
            {
                logBox.AppendText($"エラー: {ex.Message}\r\n");
            }
        }

        // フォルダ内の内容をすべて移動（既存ファイルは削除して上書き）
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
                    logBox.AppendText($"移動: {relativePath}\r\n");
                }
                catch (Exception ex)
                {
                    logBox.AppendText($"移動失敗: {relativePath} → {ex.Message}\r\n");
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // 不使用イベント
        }
    }
}
