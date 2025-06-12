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
                logBox.AppendText("�o�[�W���������擾��...\r\n");

                using WebClient client = new WebClient();
                string versionText = await client.DownloadStringTaskAsync("https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/version.txt");
                string version = versionText.Trim();

                logBox.AppendText($"�ŐV�o�[�W����: {version}\r\n");

                string zipUrl = $"https://github.com/Tabasco1410/AmongUsModManeger/releases/download/{version}/Among.Us_ModManeger{version}.zip";
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_{version}.zip");
                string extractPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_Extracted_{version}");

                // �ȑO�̓W�J�t�H���_���폜
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                Directory.CreateDirectory(extractPath); // �� �������d�v�I

                logBox.AppendText("�A�b�v�f�[�g�t�@�C�����_�E�����[�h��...\r\n");
                progressBar1.Value = 10;
                await client.DownloadFileTaskAsync(new Uri(zipUrl), tempZipPath);
                logBox.AppendText("�_�E�����[�h����\r\n");

                progressBar1.Value = 50;
                logBox.AppendText("�t�@�C����W�J��...\r\n");

                ZipFile.ExtractToDirectory(tempZipPath, extractPath);
                logBox.AppendText("�W�J����\r\n");

                // Among Us_ModManeger.exe ���܂ރt�H���_���ċA�I�ɒT��
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
                    logBox.AppendText($"Among Us_ModManeger.exe ���܂ރt�H���_�𔭌�: {exeDirectory}\r\n");
                    logBox.AppendText("�t�@�C���𐳂����ʒu�Ɉړ���...\r\n");

                    MoveDirectoryContentsFlat(exeDirectory, targetDirectory);
                }
                else
                {
                    logBox.AppendText("�G���[: Among Us_ModManeger.exe ��������܂���i�W�J�t�H���_�j\r\n");
                    return;
                }

                // �s�v�Ȉꎞ�t�H���_�폜
                try { Directory.Delete(extractPath, true); } catch { }

                // Bootstrapper.exe ������΂�����N�����ďI��
                string bootstrapperPath = Path.Combine(targetDirectory, "Bootstrapper.exe");
                if (File.Exists(bootstrapperPath))
                {
                    logBox.AppendText("Bootstrapper ���N�����āAUpdater �̍X�V���������܂�...\r\n");

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
                    logBox.AppendText("Mod Manager ���N����...\r\n");
                    Process.Start(exePath);
                }
                else
                {
                    logBox.AppendText("�G���[: Among Us_ModManeger.exe ��������܂���i�ŏI�m�F�j\r\n");
                }

                progressBar1.Value = 100;
                logBox.AppendText("�A�b�v�f�[�g����\r\n");

                await Task.Delay(1000);
                Application.Exit();
            }
            catch (Exception ex)
            {
                logBox.AppendText($"�G���[: {ex.Message}\r\n");
            }
        }

        // �t�H���_���̓��e�����ׂĈړ��i�����t�@�C���͍폜���ď㏑���j
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
                    logBox.AppendText($"�ړ�: {relativePath}\r\n");
                }
                catch (Exception ex)
                {
                    logBox.AppendText($"�ړ����s: {relativePath} �� {ex.Message}\r\n");
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // �s�g�p�C�x���g
        }
    }
}
