using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Among_Us_ModManager.Pages.Install.GitHub
{
    /// <summary>
    /// ChooseVersionPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ChooseVersionPage : Page
    {
        public ChooseVersionPage()
        {
            InitializeComponent();
            LoadReleases();
        }

        /// <summary>
        /// GitHub Releases を取得して ListView に表示
        /// </summary>
        private async void LoadReleases()
        {
            try
            {
                var releases = await GetReleasesAsync();
                ReleaseListView.ItemsSource = releases;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"リリース情報の取得に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// GitHub API からリリース一覧を取得
        /// </summary>
        private async Task<List<GitHubRelease>> GetReleasesAsync()
        {
            using (var client = new HttpClient())
            {
                // GitHub API にアクセスする場合、User-Agent ヘッダが必要
                client.DefaultRequestHeaders.Add("User-Agent", "TownOfHost-Fun-App");

                string url = "https://api.github.com/repos/Tabasco1410/AmongUsModManeger/releases";
                //string url = "https://api.github.com/repos/ToritenKabosu/TownOfHost-Fun/releases";
                var response = await client.GetStringAsync(url);

                // JSON を List<GitHubRelease> にデシリアライズ
                return JsonConvert.DeserializeObject<List<GitHubRelease>>(response);
            }
        }

        /// <summary>
        /// 選択ボタンを押したとき
        /// </summary>
        private void SelectVersion_Click(object sender, RoutedEventArgs e)
        {
            if (ReleaseListView.SelectedItem is GitHubRelease selectedRelease)
            {
                MessageBox.Show($"選択されたリリース:\n{selectedRelease.Name}\n公開日: {selectedRelease.PublishedAt:yyyy/MM/dd}");
                // ここにインストール処理や次ページへの遷移などを書けます
            }
            else
            {
                MessageBox.Show("リリースを選択してください。");
            }
        }
    }

    /// <summary>
    /// GitHub Release データモデル
    /// </summary>
    public class GitHubRelease
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }
    }
}
