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

                // �o�[�W�����擾
                using WebClient client = new WebClient();
                string versionText = await client.DownloadStringTaskAsync("https://raw.githubusercontent.com/Tabasco1410/AmongUsModManeger/main/version.txt");
                string version = versionText.Trim();

                logBox.AppendText($"�ŐV�o�[�W����: {version}\r\n");

                // �_�E�����[�hURL���쐬
                string zipUrl = $"https://github.com/Tabasco1410/AmongUsModManeger/releases/download/{version}/Among.Us_ModManeger{version}.zip";
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"AmongUsModManager_{version}.zip");

                logBox.AppendText("�A�b�v�f�[�g�t�@�C�����_�E�����[�h��...\r\n");

                // �_�E�����[�h�J�n
                progressBar1.Value = 10;
                await client.DownloadFileTaskAsync(new Uri(zipUrl), tempZipPath);
                logBox.AppendText("�_�E�����[�h����\r\n");

                // ��
                progressBar1.Value = 50;
                logBox.AppendText("�t�@�C����W�J��...\r\n");

                string extractPath = AppDomain.CurrentDomain.BaseDirectory;
                ZipFile.ExtractToDirectory(tempZipPath, extractPath, true); // �㏑���L��
                logBox.AppendText("�W�J����\r\n");

                // ����1�t�H���_��������Ȃ璆�g���ړ�
                string[] directories = Directory.GetDirectories(extractPath);
                if (directories.Length == 1)
                {
                    string innerDir = directories[0];
                    logBox.AppendText($"�T�u�t�H���_ {Path.GetFileName(innerDir)} �̒��g���ړ���...\r\n");

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

                // �N��
                progressBar1.Value = 80;
                string exePath = Path.Combine(extractPath, "Among Us_ModManeger.exe");

                if (File.Exists(exePath))
                {
                    logBox.AppendText("Mod Manager ���N����...\r\n");
                    Process.Start(exePath);
                }
                else
                {
                    logBox.AppendText("�G���[: Among Us_ModManeger.exe ��������܂���\r\n");
                }

                progressBar1.Value = 100;
                logBox.AppendText("�A�b�v�f�[�g����\r\n");

                // �A�b�v�f�[�^�[���g���I��
                await Task.Delay(1000);
                Application.Exit();
            }
            catch (Exception ex)
            {
                logBox.AppendText($"�G���[: {ex.Message}\r\n");
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // �g��Ȃ��Ȃ��ł�OK
        }
    }
}
