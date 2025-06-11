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

                // バージョン取得
                using WebClient client = new WebClient();
                string versionText = await client.DownloadStringTaskAsync("https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/version.txt");
                string version = versionText.Trim();

                logBox.AppendText($"最新バージョン: {version}\r\n");

                // ダウンロードURLを作成
                string zipUrl = $"https://github.com/Tabasco1410/AmongUsModManeger/releases/download/{version}/Among.Us_ModManeger{version}.zip";
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_{version}.zip");

                logBox.AppendText("アップデートファイルをダウンロード中...\r\n");

                // ダウンロード開始
                progressBar1.Value = 10;
                await client.DownloadFileTaskAsync(new Uri(zipUrl), tempZipPath);
                logBox.AppendText("ダウンロード完了\r\n");

                // 解凍
                progressBar1.Value = 50;
                logBox.AppendText("ファイルを展開中...\r\n");

                string extractPath = AppDomain.CurrentDomain.BaseDirectory;
                ZipFile.ExtractToDirectory(tempZipPath, extractPath, true); // 上書き有効
                logBox.AppendText("展開完了\r\n");

                // 中に1フォルダだけあるなら中身を移動
                string[] directories = Directory.GetDirectories(extractPath);
                if (directories.Length == 1)
                {
                    string innerDir = directories[0];
                    logBox.AppendText($"サブフォルダ {Path.GetFileName(innerDir)} の中身を移動中...\r\n");

                    foreach (var file in Directory.GetFiles(innerDir))
                    {
                        string destFile = Path.Combine(extractPath, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                    }

                    foreach (var dir in Directory.GetDirectories(innerDir))
                    {
                        string destDir = Path.Combine(extractPath, Path.GetFileName(dir));
                        if (Directory.Exists(destDir))
                            Directory.Delete(destDir, true);
                        Directory.Move(dir, destDir);
                    }

                    Directory.Delete(innerDir, true);
                }

                // 起動
                progressBar1.Value = 80;
                string exePath = Path.Combine(extractPath, "Among Us_ModManeger.exe");

                if (File.Exists(exePath))
                {
                    logBox.AppendText("Mod Manager を起動中...\r\n");
                    Process.Start(exePath);
                }
                else
                {
                    logBox.AppendText("エラー: Among Us_ModManeger.exe が見つかりません\r\n");
                }

                progressBar1.Value = 100;
                logBox.AppendText("アップデート完了\r\n");

                // アップデーター自身を終了
                await Task.Delay(1000);
                Application.Exit();
            }
            catch (Exception ex)
            {
                logBox.AppendText($"エラー: {ex.Message}\r\n");
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // 使わないなら空でもOK
        }
    }
}
