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
                WriteLog("�o�[�W���������擾��...");

                using WebClient client = new WebClient();
                string versionText = await client.DownloadStringTaskAsync("https://raw.githubusercontent.com/Tabasco1410/AmongUsModManager/main/version.txt");
                string version = versionText.Trim();

                WriteLog($"�ŐV�o�[�W����: {version}");

                string zipUrl = $"https://github.com/Tabasco1410/AmongUsModManager/releases/download/{version}/Among.Us_ModManager{version}.zip";
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_{version}.zip");
                string extractPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_Extracted_{version}");

                WriteLog($"�_�E�����[�hURL: {zipUrl}");

                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                Directory.CreateDirectory(extractPath);

                WriteLog("�A�b�v�f�[�g�t�@�C�����_�E�����[�h��...");
                progressBar1.Value = 10;
                await client.DownloadFileTaskAsync(new Uri(zipUrl), tempZipPath);
                WriteLog("�_�E�����[�h����");

                progressBar1.Value = 50;
                WriteLog("�t�@�C����W�J��...");
                ZipFile.ExtractToDirectory(tempZipPath, extractPath);
                WriteLog("�W�J����");

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
                    WriteLog($"Among Us_ModManager.exe ���܂ރt�H���_�𔭌�: {exeDirectory}");
                    WriteLog("�t�@�C���𐳂����ʒu�Ɉړ���...");

                    MoveDirectoryContentsFlat(exeDirectory, targetDirectory);
                }
                else
                {
                    WriteLog("�G���[: Among Us_ModManager.exe ��������܂���i�W�J�t�H���_�j");
                    return;
                }

                try { Directory.Delete(extractPath, true); } catch { }

                string bootstrapperPath = Path.Combine(targetDirectory, "Bootstrapper.exe");
                if (File.Exists(bootstrapperPath))
                {
                    WriteLog("Bootstrapper ���N�����āAUpdater �̍X�V���������܂�...");

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
                    WriteLog("Mod Manager ���N����...");
                    Process.Start(exePath);
                }
                else
                {
                    WriteLog("�G���[: Among Us_ModManager.exe ��������܂���i�ŏI�m�F�j");
                }

                progressBar1.Value = 100;
                WriteLog("�A�b�v�f�[�g����");

                await Task.Delay(1000);
                Application.Exit();
            }
            catch (Exception ex)
            {
                WriteLog($"�G���[: {ex.Message}");
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
                    WriteLog($"�ړ�: {relativePath}");
                }
                catch (Exception ex)
                {
                    WriteLog($"�ړ����s: {relativePath} �� {ex.Message}");
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // �s�g�p�C�x���g
        }
    }
}
