using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;
using AmongUsModManager.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace AmongUsModManager.Pages
{
    public class ModPreset
    {
        public string Name            { get; set; } = "";
        public string Description     { get; set; } = "";
        public string LongDescription { get; set; } = "";
        public string Owner           { get; set; } = "";
        public string Repository      { get; set; } = "";
        public bool   IsReactor       { get; set; } = false;
        public string? ReactorBepInExUrl { get; set; }

        public string ThumbnailUrl { get; set; } = "";
        public bool HasThumbnail => !string.IsNullOrEmpty(ThumbnailUrl);
        public bool DarkBackground { get; set; } = false;
        public Microsoft.UI.Xaml.Media.Stretch ImageStretch { get; set; }
            = Microsoft.UI.Xaml.Media.Stretch.Uniform;
    }

    public sealed partial class ModInstallPage : Page
    {
        private HttpClient _httpClient => GitHubAuthService.GetClient();
        private List<GitHubRelease>? _currentReleases;
        private ModPreset? _selectedMod;

        private ContentDialog? _installProgressDialog;
        private TextBlock?     _statusTextBlock;
        private ProgressBar?   _installProgressBar;

<<<<<<< HEAD
=======
        private bool _isListView   = false;
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
        private bool _isDetailRight = false;

        private static string ToRawUrl(string githubBlobUrl)
            => githubBlobUrl
                .Replace("github.com", "raw.githubusercontent.com")
                .Replace("/blob/", "/");

        public static readonly List<ModPreset> SupportedMods = new List<ModPreset>
        {
            new ModPreset
            {
                Name = "TownOfHost",
                Description = "Town Of Host - ホストMod",
                LongDescription = "この Mod はホストのクライアントに導入するだけで動作し、他のクライアントの Mod の導入/未導入及び端末の種類に関係なく動作します。\nまた、カスタムサーバーを利用した Mod と違い、URL やファイル編集などによるサーバー追加も不要なため、ホスト以外のプレイヤーは Town Of Host を導入したホストの部屋に参加するだけで追加役職を楽しむことができます。",
                Owner = "tukasa0001", Repository = "TownOfHost",
                ThumbnailUrl = ToRawUrl("https://github.com/tukasa0001/TownOfHost/blob/main/Resources/TownOfHost-Logo.png"),
                DarkBackground = true
            },
            new ModPreset
            {
                Name = "TownOfHost-K",
                Description = "Town Of Host-K - TownOfHostに役職や機能を追加したMod",
                LongDescription = "他のAmongUsのModとはまた一味違った、斬新で独特な機能や役職が多いModです。\n\nHostModなので部屋主のみModを導入すれば、\n参加者はModを導入する必要もカスタムサーバー追加等の面倒な手間なしで\n導入者が部屋を建て、その部屋に入ることでTownOfHost-Kを遊ぶことができます！",
                Owner = "KYMario", Repository = "TownOfHost-K",
                ThumbnailUrl = ToRawUrl("https://github.com/KYMario/TownOfHost-K/blob/main/Resources/TownOfHost-K.png"),
                DarkBackground = true
            },
            new ModPreset
            {
                Name = "TownOfHost Enhanced",
                Description = "TownOfHost Enhanced - TOH系の海外Mod",
                LongDescription = "TOHEは、Among Usの体験をガラッと変えたいすべての人に贈る、ナンバーワンのホスト専用（Host-Only）MODです！",
                Owner = "EnhancedNetwork", Repository = "TownofHost-Enhanced",
                ThumbnailUrl = ToRawUrl("https://github.com/EnhancedNetwork/TownofHost-Enhanced/blob/main/Resources/Background/TOHE-Background-Old.jpg"),
                DarkBackground = false,
                ImageStretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
            },
            new ModPreset
            {
                Name = "Project: Lotus",
                Description = "Project: Lotus",
                LongDescription = "Gemini の回答\nProject: Lotusは、ホスト（PC）さえ導入していれば、他のプレイヤーは未導入（スマホ等）でも参加できる、非常に汎用性の高いAmong Us用拡張MODです。\n\n参加側もMODを導入することで、役職専用の演出やUI、独自のアビリティボタン、カスタム衣装といった豊富な追加機能を利用できるようになります。\n\n独自の役職や詳細なゲーム設定はもちろん、設定のテンプレート化やロビー情報の共有機能、さらに公式の制限を回避する独自リージョンの提供など、快適にマルチプレイを楽しむためのサポートが充実しているのが特徴です。",
                Owner = "Lotus-AU", Repository = "LotusContinued",
                ThumbnailUrl = ToRawUrl("https://avatars.githubusercontent.com/u/173427715"),
                DarkBackground = false,
                ImageStretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
            },
            new ModPreset
             {
                Name = "EndlessHostRoles",
                Description = "EHR - 450以上の役職を追加するホスト専用Mod",
                LongDescription = "EHRは、Among Us最大のホスト専用MODです。450種類以上の役職やアドオン、16種類のゲームモードを搭載しており、専用の「Custom Team Assigner（カスタムチームアサイナー）」を使えば、ゲームを自由自在にカスタマイズできます！",
                Owner = "Gurge44", Repository = "EndlessHostRoles",
                ThumbnailUrl = ToRawUrl("https://github.com/Gurge44/EndlessHostRoles/blob/main/Resources/Images/EHR-Icon.png"),
                DarkBackground = false,
                ImageStretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
            },
            new ModPreset
            {
                Name = "SuperNewRoles",
                Description = "モードや役職など、様々な要素があるAmongUsのMOD、SuperNewRoles!!!!",
                LongDescription = "SuperNewRolesは、150種類以上の多彩な役職や独自のゲームモードを搭載した、Among Usの自由度を劇的に広げる拡張MODです。\n\n役職の出現率やマップギミック、デバイス制限などを細かく調整・保存できるため、プレイヤー好みの完璧なゲームバランスを構築できます。さらに、豊富なカスタムコスメティクスで外見の個性も追求でき、PCとAndroid間でのマルチプレイにも対応しています。",
                Owner = "SuperNewRoles", Repository = "SuperNewRoles",
                ThumbnailUrl = ToRawUrl("https://github.com/SuperNewRoles/SuperNewRoles/blob/master/images/SNRImage.png"),
                DarkBackground = false
            },
            new ModPreset
            {
                Name = "NebulaOnTheShip",
                Description = "Nebula on the Ship - 高品質なカスタムMod",
                LongDescription = "クオリティの高い役職や機能が集まったMod。Nebula on the Ship は Among Us に様々な役職や新機能を追加するModです。",
                Owner = "Dolly1016", Repository = "Nebula",
                ThumbnailUrl = ToRawUrl("https://github.com/Dolly1016/Nebula-Public/blob/master/NebulaPluginNova/Resources/NebulaLogo.png"),
                DarkBackground = true
            },
            new ModPreset
            {
                Name = "ExtremeRoles",
                Description = "独自の役職、そして高速な動作。",
                LongDescription = "第三陣営に加えて第四陣営「リベラル」や幽霊役職、100以上の役職が集まったMod。軽量かつ高速な動作。",
                Owner = "yukieiji", Repository = "ExtremeRoles",
                ThumbnailUrl = "https://raw.githubusercontent.com/yukieiji/ExtremeRoles/master/Design/ExtremeRolesIcon.png",
                DarkBackground = false
            },
            /*
            new ModPreset
            {
                Name = "The Other Roles",
                Description = "The Other Roles - 多彩な役職Mod",
                LongDescription = "海外で非常に人気の高いMod。探偵・メディック・エンジニアなど多彩な役職を追加します。",
                Owner = "TheOtherRolesAU", Repository = "TheOtherRoles",
                ThumbnailUrl = "https://raw.githubusercontent.com/TheOtherRolesAU/TheOtherRoles/main/TheOtherRoles/Resources/logo.png",
                DarkBackground = true
            },*/
            new ModPreset
            {
                Name = "Town Of Us Mira",
                Description = "Town Of Us Mira - AU Avengersによる役職Mod",
                LongDescription = "海外のクライアントMod。「Town of Us Reactivated」が、MiraAPIを使用して、より洗練され、多くの改善を加えて復活しました！",
                Owner = "AU-Avengers", Repository = "TOU-Mira",
                ThumbnailUrl = ToRawUrl("https://github.com/AU-Avengers/TOU-Mira/blob/main/Images/Logo.png"),
                DarkBackground = true
            },
            new ModPreset
            {
                Name = "Reactor",
                Description = "",
                LongDescription = "",
                Owner = "NuclearPowered", Repository = "Reactor",
                IsReactor = true,
                ReactorBepInExUrl = "https://builds.bepinex.dev/projects/bepinex_be/752/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.752%2Bdd0655f.zip",
                ThumbnailUrl = "https://docs.reactor.gg/img/logo.png",
                DarkBackground = true
            },
            new ModPreset
            {
                Name = "MiraAPI",
                Description = "",
                LongDescription = "",               
                Owner = "All-Of-Us-Mods", Repository = "MiraAPI",
                ThumbnailUrl = ToRawUrl("https://avatars.githubusercontent.com/u/78455861?s=200&v=4"),
                DarkBackground = true
            },
        };

        public ModInstallPage()
        {
            this.InitializeComponent();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AmongUsModManager-App");
            ModGridView.ItemsSource = SupportedMods;
            LogService.Info("ModInstallPage", "ページ初期化完了");
        }

        private void LayoutToggle_Checked(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD
=======
            _isListView = true;
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
            LayoutToggleIcon.Glyph = "\uE8FD";
            ModGridView.Visibility = Visibility.Collapsed;
            ModListView.Visibility = Visibility.Visible;
            ModListView.ItemsSource = ModGridView.ItemsSource;
            LogService.Debug("ModInstallPage", "リスト表示に切り替え");
        }

        private void LayoutToggle_Unchecked(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD
=======
            _isListView = false;
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
            LayoutToggleIcon.Glyph = "\uF0E2";
            ModListView.Visibility = Visibility.Collapsed;
            ModGridView.Visibility = Visibility.Visible;
            LogService.Debug("ModInstallPage", "グリッド表示に切り替え");
        }

        private void DetailPosToggle_Checked(object sender, RoutedEventArgs e)
        {
            _isDetailRight = true;
            DetailPosIcon.Glyph = "\uE951";
            ApplyDetailPosition();
            LogService.Debug("ModInstallPage", "詳細パネル: 右側");
        }

        private void DetailPosToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _isDetailRight = false;
            DetailPosIcon.Glyph = "\uE74C";
            ApplyDetailPosition();
            LogService.Debug("ModInstallPage", "詳細パネル: 下側");
        }

        private void ApplyDetailPosition()
        {
            if (InstallDetailArea.Visibility != Visibility.Visible) return;

            if (_isDetailRight)
            {
                Grid.SetRow(InstallDetailArea, 0);
                Grid.SetColumn(InstallDetailArea, 1);
                InstallDetailArea.Margin = new Thickness(12, 0, 0, 0);
                DetailRow.Height = new GridLength(0);
                DetailCol.Width  = new GridLength(340);
            }
            else
            {
                Grid.SetRow(InstallDetailArea, 1);
                Grid.SetColumn(InstallDetailArea, 0);
                InstallDetailArea.Margin = new Thickness(0, 12, 0, 0);
                DetailRow.Height = new GridLength(1, GridUnitType.Auto);
                DetailCol.Width  = new GridLength(0);
            }
        }

        private void ShowDetailPanel()
        {
            InstallDetailArea.Visibility = Visibility.Visible;
            ApplyDetailPosition();
        }

        private void ModSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = ModSearchBox.Text.Trim().ToLower();
            var filtered = string.IsNullOrEmpty(q)
                ? SupportedMods
                : SupportedMods.Where(m =>
                    m.Name.ToLower().Contains(q) ||
                    m.Description.ToLower().Contains(q) ||
                    m.Owner.ToLower().Contains(q)).ToList();
            ModGridView.ItemsSource = filtered;
            ModListView.ItemsSource = filtered;
        }

        private async void OpenAumanager_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".aumanager");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            var data = ShareCodeService.Decode(file.Path);
            if (data == null)
            {
                await new ContentDialog { Title = "読み込みエラー",
                    Content = "ファイルを読み込めませんでした。",
                    CloseButtonText = "OK", XamlRoot = this.XamlRoot }.ShowAsync();
                return;
            }
            await HandleShareCodeDataAsync(data);
        }

        private async Task HandleShareCodeDataAsync(ShareCodeData data)
        {
            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(new TextBlock
                { Text = $"{data.ModName}  {data.Version}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontSize = 15 });
            panel.Children.Add(new TextBlock
                { Text = $"{data.GitHubOwner}/{data.GitHubRepo}", FontSize = 12, Opacity = 0.7 });
            if (!string.IsNullOrEmpty(data.DownloadUrl))
                panel.Children.Add(new TextBlock
                    { Text = data.DownloadUrl, FontSize = 11, TextWrapping = TextWrapping.Wrap, Opacity = 0.6 });

            var dlg = new ContentDialog
            {
                Title = "共有コードのMod情報",
                Content = panel,
                PrimaryButtonText = "インストール開始",
                CloseButtonText = "キャンセル",
                XamlRoot = this.XamlRoot
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            var matched = SupportedMods.FirstOrDefault(m =>
                m.Owner.Equals(data.GitHubOwner, StringComparison.OrdinalIgnoreCase) &&
                m.Repository.Equals(data.GitHubRepo, StringComparison.OrdinalIgnoreCase));
            _selectedMod = matched ?? new ModPreset
                { Name = data.ModName, Owner = data.GitHubOwner, Repository = data.GitHubRepo };

            GitHubUrlBox.Text = $"https://github.com/{data.GitHubOwner}/{data.GitHubRepo}";
            await FetchGitHubData(data.GitHubOwner, data.GitHubRepo, data.ModName);
        }

        private void EnsureProgressDialog()
        {
            if (_installProgressDialog != null) return;
            _statusTextBlock    = new TextBlock { Text = "準備中...", TextWrapping = TextWrapping.Wrap };
            _installProgressBar = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0 };
            _installProgressDialog = new ContentDialog
            {
                Title = "Modをインストール中",
                CloseButtonText = "閉じる",
                Content = new StackPanel
                {
                    Spacing = 15, Width = 300,
                    Children = { _statusTextBlock, _installProgressBar }
                }
            };
        }

        private void ShowStatus(string msg)
        {
            if (_statusTextBlock != null) _statusTextBlock.Text = msg;
            LogService.Info("ModInstallPage", msg);
        }

        private void SetProgress(double value)
        {
            if (_installProgressBar != null) _installProgressBar.Value = value;
            LogService.Trace("ModInstallPage", $"進捗: {value}%");
        }

        private async void ModGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModGridView.SelectedItem is ModPreset selected)
                await OnModSelected(selected);
        }

        private async void ModListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModListView.SelectedItem is ModPreset selected)
                await OnModSelected(selected);
        }

        private async Task OnModSelected(ModPreset selected)
        {
            _selectedMod = selected;
            LogService.Info("ModInstallPage", $"Mod選択: {selected.Name}");
            if (selected.IsReactor)
                await FetchReactorData(selected);
            else
                await FetchGitHubData(selected.Owner, selected.Repository, selected.Name);
        }

        private void CloseDetail_Click(object sender, RoutedEventArgs e)
        {
            LogService.Info("ModInstallPage", "詳細エリアを閉じる");
            InstallDetailArea.Visibility = Visibility.Collapsed;
            DetailRow.Height = new GridLength(0);
            DetailCol.Width  = new GridLength(0);
            ModGridView.SelectedItem = null;
            ModListView.SelectedItem = null;
            _selectedMod = null;
        }

        private async void GitHubFetch_Click(object sender, RoutedEventArgs e)
        {
            string url = GitHubUrlBox.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;
            LogService.Info("ModInstallPage", $"URL指定取得: {url}");
            try
            {
                var uri   = new Uri(url);
                var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return;
                string owner = parts[0];
                string repo  = parts[1].Replace(".git", "");

                var matched = SupportedMods.FirstOrDefault(m =>
                    m.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase) &&
                    m.Repository.Equals(repo, StringComparison.OrdinalIgnoreCase));

                if (matched != null)
                {
                    _selectedMod = matched;
                    ModGridView.SelectedItem = matched;
                    await FetchGitHubData(owner, repo, matched.Name);
                }
                else
                {
                    var similar = SupportedMods.Where(m =>
                        m.Name.Contains(repo, StringComparison.OrdinalIgnoreCase) ||
                        m.Repository.Contains(repo, StringComparison.OrdinalIgnoreCase) ||
                        repo.Contains(m.Repository, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (similar.Count > 0)
                    {
                        string candidates = string.Join(Environment.NewLine, similar.Select(m => $"\u2022 {m.Name} ({m.Owner}/{m.Repository})"));
                        var suggestDialog = new ContentDialog
                        {
                            Title = "似ているModが見つかりました",
                            Content = new StackPanel { Spacing = 8, Children =
                            {
                                new TextBlock { Text = $"入力されたURL ({owner}/{repo}) に似ているModがリストにあります：", TextWrapping = TextWrapping.Wrap },
                                new TextBlock { Text = candidates, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                                    FontSize = 12, TextWrapping = TextWrapping.Wrap },
                                new TextBlock { Text = "「このまま追加」を選ぶとURLのリポジトリをそのまま取得します。", FontSize = 11, TextWrapping = TextWrapping.Wrap,
                                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) }
                            }},
                            PrimaryButtonText = "このまま追加",
                            CloseButtonText = "キャンセル",
                            XamlRoot = this.XamlRoot
                        };
                        var ans = await suggestDialog.ShowAsync();
                        if (ans != ContentDialogResult.Primary) return;
                    }

                    _selectedMod = new ModPreset { Name = repo, Owner = owner, Repository = repo };
                    await FetchGitHubData(owner, repo, repo);
                }
            }
            catch (Exception ex) { LogService.Error("ModInstallPage", "URL解析エラー", ex); ShowStatus("URL解析エラー: " + ex.Message); }
        }

        private async Task FetchGitHubData(string owner, string repo, string modName)
        {
            ShowDetailPanel();
            LoadingPanel.Visibility   = Visibility.Visible;
            SelectionPanel.Visibility = Visibility.Collapsed;
            SelectedModTitle.Text     = modName;

            if (!string.IsNullOrEmpty(_selectedMod?.LongDescription))
            {
                SelectedModDescription.Text       = _selectedMod.LongDescription;
                SelectedModDescription.Visibility = Visibility.Visible;
            }
            else
            {
                SelectedModDescription.Visibility = Visibility.Collapsed;
            }

            LogService.Info("ModInstallPage", $"GitHub リリース取得開始: {owner}/{repo}");
            try
            {
                _currentReleases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>(
                    $"https://api.github.com/repos/{owner}/{repo}/releases");
                if (_currentReleases?.Count > 0)
                {
                    VersionCombo.ItemsSource   = _currentReleases.Select(r => r.tag_name).ToList();
                    VersionCombo.SelectedIndex = 0;
                    LogService.Info("ModInstallPage", $"リリース {_currentReleases.Count} 件取得");
                }
                else ShowStatus("リリース情報が見つかりませんでした。");
            }
            catch (Exception ex)
            {
                LogService.Error("ModInstallPage", "GitHub データ取得エラー", ex);
                string msg = ex.Message ?? "";
                if (msg.Contains("403") || msg.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                    ShowStatus("⚠️ GitHub の通信制限（Rate Limit）に達しました。\nしばらく（目安：1時間）待ってから再試行してください。");
                else
                    ShowStatus("GitHubデータ取得エラー: " + ex.Message);
            }
            finally
            {
                LoadingPanel.Visibility   = Visibility.Collapsed;
                SelectionPanel.Visibility = Visibility.Visible;
            }
        }

        private async Task FetchReactorData(ModPreset preset)
        {
            ShowDetailPanel();
            LoadingPanel.Visibility   = Visibility.Visible;
            SelectionPanel.Visibility = Visibility.Collapsed;
            SelectedModTitle.Text     = preset.Name;

            if (!string.IsNullOrEmpty(preset.LongDescription))
            {
                SelectedModDescription.Text       = preset.LongDescription;
                SelectedModDescription.Visibility = Visibility.Visible;
            }
            else
            {
                SelectedModDescription.Visibility = Visibility.Collapsed;
            }

            LogService.Info("ModInstallPage", "Reactor データ取得開始");
            try
            {
                _currentReleases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>(
                    $"https://api.github.com/repos/{preset.Owner}/{preset.Repository}/releases");
                if (_currentReleases?.Count > 0)
                {
                    VersionCombo.ItemsSource   = _currentReleases.Select(r => r.tag_name).ToList();
                    VersionCombo.SelectedIndex = 0;
                }
                else ShowStatus("Reactorのリリース情報が見つかりませんでした。");
            }
            catch (Exception ex) { LogService.Error("ModInstallPage", "Reactor データ取得エラー", ex); ShowStatus("Reactorデータ取得エラー: " + ex.Message); }
            finally
            {
                LoadingPanel.Visibility   = Visibility.Collapsed;
                SelectionPanel.Visibility = Visibility.Visible;
            }
        }

        private void VersionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionCombo.SelectedItem is string tagName && _currentReleases != null)
            {
                var release = _currentReleases.FirstOrDefault(r => r.tag_name == tagName);
                if (release != null)
                {
                    List<string> assets = _selectedMod?.IsReactor == true
                        ? release.assets.Where(a => a.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).Select(a => a.name).ToList()
                        : release.assets.Where(a => a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).Select(a => a.name).ToList();
<<<<<<< HEAD

                    AssetCombo.ItemsSource = assets;

                    // メインプラットフォームに合わせてデフォルトアセットを選択
                    // ファイル名に "Steam" / "Epic" が含まれる場合は優先的に選ぶ
                    if (assets.Count > 0)
                    {
                        string platform = ConfigService.Load().MainPlatform
                            .Equals("Epic", StringComparison.OrdinalIgnoreCase) ? "Epic" : "Steam";

                        int bestIdx = assets.FindIndex(a =>
                            a.IndexOf(platform, StringComparison.OrdinalIgnoreCase) >= 0);

                        AssetCombo.SelectedIndex = bestIdx >= 0 ? bestIdx : 0;
                    }

                    // フォルダ名はMod名のみ（バージョンは含めない）
                    InstallFolderName.Text = SelectedModTitle.Text;
=======

                    AssetCombo.ItemsSource = assets;
                    if (assets.Count > 0) AssetCombo.SelectedIndex = 0;
                    InstallFolderName.Text = $"{SelectedModTitle.Text}_{tagName.Replace(".", "_")}";
>>>>>>> 9b70396323094b50176708b54875479518ab7e99
                    LogService.Debug("ModInstallPage", $"バージョン選択: {tagName}, アセット数: {assets.Count}");
                }
            }
        }

        private async void StartInstall_Click(object sender, RoutedEventArgs e)
        {
            var config = ConfigService.Load();
            if (string.IsNullOrEmpty(config?.GameInstallPath) || string.IsNullOrEmpty(config?.ModDataPath))
            {
                await ShowError("設定画面でインストールパスを設定してください。");
                return;
            }

            var release = _currentReleases?.FirstOrDefault(r => r.tag_name == (string?)VersionCombo.SelectedItem);
            if (release == null) { await ShowError("バージョン情報を取得できません。"); return; }

            EnsureProgressDialog();
            _installProgressDialog!.XamlRoot = this.XamlRoot;
            ShowStatus("準備中...");
            SetProgress(0);
            _ = _installProgressDialog.ShowAsync();

            LogService.Info("ModInstallPage", $"インストール開始: {_selectedMod?.Name} {release.tag_name}");
            try
            {
                string targetDir = Path.Combine(config.ModDataPath, InstallFolderName.Text);
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                Directory.CreateDirectory(targetDir);

                ShowStatus("本体をコピー中...");
                await Task.Run(() => CopyDirectory(config.GameInstallPath, targetDir));
                SetProgress(20);

                if (_selectedMod?.IsReactor == true)
                    await InstallReactor(release, targetDir);
                else
                    await InstallNormalMod(release, targetDir);

                _installProgressDialog!.Hide();
                bool installSplash = await AskInstallSplashScreen();
                if (installSplash)
                {
                    EnsureProgressDialog();
                    _installProgressDialog!.XamlRoot = this.XamlRoot;
                    ShowStatus("スプラッシュスクリーンをインストール中...");
                    _ = _installProgressDialog.ShowAsync();
                    await InstallSplashScreen(targetDir);
                    _installProgressDialog.Hide();
                }

                var newMod = new VanillaPathInfo
                {
                    Name = InstallFolderName.Text, Path = targetDir,
                    GitHubOwner = _selectedMod?.Owner, GitHubRepo = _selectedMod?.Repository,
                    CurrentVersion = release.tag_name,
                    IsAutoUpdateEnabled = !(_selectedMod?.IsReactor ?? false),
                    LastChecked = DateTime.Now
                };

                if (!config.VanillaPaths.Any(v => v.Path == targetDir))
                {
                    config.VanillaPaths.Add(newMod);
                    ConfigService.Save(config);
                }

                string platform = !string.IsNullOrEmpty(config.MainPlatform) ? config.MainPlatform : config.Platform;
                string shareCode = ShareCodeService.Generate(newMod, platform,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                 "AmongUsModManager", "SharedCodes"));

                SetProgress(100);
                ShowStatus("完了！");
                LogService.Info("ModInstallPage", $"インストール完了: {newMod.Name}");
                await Task.Delay(500);
                _installProgressDialog.Hide();
                await ShowPostInstallSetup(newMod, shareCode);
            }
            catch (Exception ex)
            {
                LogService.Error("ModInstallPage", "インストール中にエラー発生", ex);
                ShowStatus($"エラー: {ex.Message}");
            }
        }

        private async Task InstallNormalMod(GitHubRelease release, string targetDir)
        {
            var asset = release.assets.FirstOrDefault(a => a.name == (string?)AssetCombo.SelectedItem);
            if (asset == null) throw new Exception("ZIPファイルが見つかりません。");

            ShowStatus("ダウンロード中...");
            SetProgress(40);
            string tempZip = Path.Combine(Path.GetTempPath(), asset.name);
            LogService.Debug("ModInstallPage", $"ダウンロード: {asset.browser_download_url}");
            using (var stream = await _httpClient.GetStreamAsync(asset.browser_download_url))
            using (var fs    = new FileStream(tempZip, FileMode.Create))
                await stream.CopyToAsync(fs);

            ShowStatus("展開中...");
            SetProgress(70);
            string extractPath = Path.Combine(Path.GetTempPath(), "AUMMExtract_" + Guid.NewGuid());
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(tempZip, extractPath);
                var bepDir  = Directory.GetDirectories(extractPath, "BepInEx", SearchOption.AllDirectories).FirstOrDefault();
                string srcRoot = bepDir != null ? Directory.GetParent(bepDir)!.FullName : extractPath;
                CopyDirectory(srcRoot, targetDir);
                Directory.Delete(extractPath, true);
                if (File.Exists(tempZip)) File.Delete(tempZip);
            });
            SetProgress(90);
        }

        private async Task InstallReactor(GitHubRelease release, string targetDir)
        {
            ShowStatus("BepInEx BE をダウンロード中...");
            SetProgress(30);
            string bepTempZip = Path.Combine(Path.GetTempPath(), "BepInEx_BE.zip");
            using (var stream = await _httpClient.GetStreamAsync(_selectedMod!.ReactorBepInExUrl!))
            using (var fs    = new FileStream(bepTempZip, FileMode.Create))
                await stream.CopyToAsync(fs);

            ShowStatus("BepInEx BE を展開中...");
            SetProgress(50);
            string bepExtract = Path.Combine(Path.GetTempPath(), "BepInEx_BE_" + Guid.NewGuid());
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(bepTempZip, bepExtract);
                CopyDirectory(bepExtract, targetDir);
                Directory.Delete(bepExtract, true);
                if (File.Exists(bepTempZip)) File.Delete(bepTempZip);
            });

            ShowStatus("Reactor.dll をダウンロード中...");
            SetProgress(70);
            var dllAsset = release.assets.FirstOrDefault(a => a.name.Equals("Reactor.dll", StringComparison.OrdinalIgnoreCase));
            if (dllAsset == null) throw new Exception("Reactor.dll が見つかりません。");

            string pluginsDir = Path.Combine(targetDir, "BepInEx", "plugins");
            Directory.CreateDirectory(pluginsDir);
            byte[] dllBytes = await _httpClient.GetByteArrayAsync(dllAsset.browser_download_url);
            File.WriteAllBytes(Path.Combine(pluginsDir, dllAsset.name), dllBytes);
            SetProgress(90);
        }

        private async Task InstallSplashScreen(string targetDir)
        {
            const string splashUrl = "https://github.com/Tabasco1410/AmongUs.BepInEx.SplashScreen.Japanese/releases/download/1.0/AmongUs.BepInEx.SplashScreen.Japanese.zip";
            string tempZip = Path.Combine(Path.GetTempPath(), "SplashScreen.zip");
            using (var stream = await _httpClient.GetStreamAsync(splashUrl))
            using (var fs    = new FileStream(tempZip, FileMode.Create))
                await stream.CopyToAsync(fs);

            string extractPath = Path.Combine(Path.GetTempPath(), "SplashExtract_" + Guid.NewGuid());
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(tempZip, extractPath);
                var bepDir = Directory.GetDirectories(extractPath, "BepInEx", SearchOption.AllDirectories).FirstOrDefault();
                if (bepDir != null) CopyDirectory(Directory.GetParent(bepDir)!.FullName, targetDir);
                else                CopyDirectory(extractPath, targetDir);
                Directory.Delete(extractPath, true);
                if (File.Exists(tempZip)) File.Delete(tempZip);
            });
        }

        private async Task<bool> AskInstallSplashScreen()
        {
            var dialog = new ContentDialog
            {
                Title = "スプラッシュスクリーン",
                Content = new StackPanel { Spacing = 8, Children =
                {
                    new TextBlock { Text = "日本語スプラッシュスクリーンアプリをインストールしますか？", TextWrapping = TextWrapping.Wrap, FontSize = 13 },
                    new TextBlock { Text = "Among Usの起動時にスプラッシュ画面を表示するBepInEx拡張です。", FontSize = 12, TextWrapping = TextWrapping.Wrap,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) }
                }},
                PrimaryButtonText = "インストールする", SecondaryButtonText = "しない",
                DefaultButton = ContentDialogButton.Secondary, XamlRoot = this.XamlRoot
            };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private async void InstallFromZip_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".zip");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            LogService.Info("ModInstallPage", $"ZIPからインストール: {file.Path}");

            var folderBox = new TextBox
            {
                PlaceholderText = "例: MyCoolMod_1.0.0",
                Text = System.IO.Path.GetFileNameWithoutExtension(file.Name),
                Width = 340
            };
            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(new TextBlock { Text = "インストール先フォルダ名を入力してください。", TextWrapping = TextWrapping.Wrap, FontSize = 13 });
            panel.Children.Add(folderBox);
            panel.Children.Add(new TextBlock { Text = "※ GitHub 連携・自動アップデートは無効になります。", FontSize = 11, TextWrapping = TextWrapping.Wrap,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });

            var dlg = new ContentDialog
            {
                Title = "🗜️ ZIPファイルからインストール", Content = panel,
                PrimaryButtonText = "インストール", CloseButtonText = "キャンセル", XamlRoot = this.XamlRoot
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            string folderName = folderBox.Text.Trim();
            if (string.IsNullOrEmpty(folderName)) { await ShowError("フォルダ名を入力してください。"); return; }

            var config = ConfigService.Load();
            if (string.IsNullOrEmpty(config?.ModDataPath)) { await ShowError("設定画面でインストールパスを設定してください。"); return; }

            EnsureProgressDialog();
            _installProgressDialog!.XamlRoot = this.XamlRoot;
            ShowStatus("準備中...");
            SetProgress(0);
            _ = _installProgressDialog.ShowAsync();

            try
            {
                string targetDir = System.IO.Path.Combine(config.ModDataPath, folderName);
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                Directory.CreateDirectory(targetDir);

                if (!string.IsNullOrEmpty(config.GameInstallPath))
                {
                    ShowStatus("本体をコピー中...");
                    await Task.Run(() => CopyDirectory(config.GameInstallPath, targetDir));
                }
                SetProgress(30);

                ShowStatus("ZIPを展開中...");
                string extractPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AUMMZip_" + Guid.NewGuid());
                await Task.Run(() =>
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(file.Path, extractPath);
                    var bepDir = Directory.GetDirectories(extractPath, "BepInEx", SearchOption.AllDirectories).FirstOrDefault();
                    string srcRoot = bepDir != null ? Directory.GetParent(bepDir)!.FullName : extractPath;
                    CopyDirectory(srcRoot, targetDir);
                    Directory.Delete(extractPath, true);
                });
                SetProgress(80);

                var newMod = new VanillaPathInfo
                {
                    Name = folderName, Path = targetDir,
                    CurrentVersion = "手動インストール", IsAutoUpdateEnabled = false, LastChecked = DateTime.Now
                };
                if (!config.VanillaPaths.Any(v => v.Path == targetDir)) { config.VanillaPaths.Add(newMod); ConfigService.Save(config); }

                string platform2 = !string.IsNullOrEmpty(config.MainPlatform) ? config.MainPlatform : config.Platform;
                string shareCode = ShareCodeService.Generate(newMod, platform2,
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManager", "SharedCodes"));

                SetProgress(100); ShowStatus("完了！");
                LogService.Info("ModInstallPage", $"ZIPインストール完了: {folderName}");
                await Task.Delay(500);
                _installProgressDialog.Hide();
                await ShowPostInstallSetup(newMod, shareCode);
            }
            catch (Exception ex) { LogService.Error("ModInstallPage", "ZIPインストールエラー", ex); ShowStatus($"エラー: {ex.Message}"); }
        }

        private async void InstallFromDirectUrl_Click(object sender, RoutedEventArgs e)
        {
            var urlBox    = new TextBox { PlaceholderText = "https://example.com/mod.zip", Width = 340 };
            var folderBox = new TextBox { PlaceholderText = "例: MyCoolMod_1.0.0", Width = 340 };
            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(new TextBlock { Text = "ダウンロードURL（.zip ファイルの直接リンク）", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            panel.Children.Add(urlBox);
            panel.Children.Add(new TextBlock { Text = "インストール先フォルダ名", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 0) });
            panel.Children.Add(folderBox);
            panel.Children.Add(new TextBlock { Text = "※ GitHub 連携・自動アップデートは無効になります。", FontSize = 11, TextWrapping = TextWrapping.Wrap,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });

            var dlg = new ContentDialog
            {
                Title = "🔗 直接DLリンクからインストール", Content = panel,
                PrimaryButtonText = "ダウンロード＆インストール", CloseButtonText = "キャンセル", XamlRoot = this.XamlRoot
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            string url = urlBox.Text.Trim();
            string folderName = folderBox.Text.Trim();
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out _)) { await ShowError("有効なURLを入力してください。"); return; }
            if (string.IsNullOrEmpty(folderName)) { await ShowError("フォルダ名を入力してください。"); return; }

            var config = ConfigService.Load();
            if (string.IsNullOrEmpty(config?.ModDataPath)) { await ShowError("設定画面でインストールパスを設定してください。"); return; }

            EnsureProgressDialog();
            _installProgressDialog!.XamlRoot = this.XamlRoot;
            ShowStatus("ダウンロード中...");
            SetProgress(0);
            _ = _installProgressDialog.ShowAsync();

            try
            {
                string tempZip = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AUMMDirect_" + Guid.NewGuid() + ".zip");
                using (var stream = await _httpClient.GetStreamAsync(url))
                using (var fs    = new FileStream(tempZip, FileMode.Create))
                    await stream.CopyToAsync(fs);
                SetProgress(40);

                string targetDir = System.IO.Path.Combine(config.ModDataPath, folderName);
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                Directory.CreateDirectory(targetDir);

                if (!string.IsNullOrEmpty(config.GameInstallPath))
                {
                    ShowStatus("本体をコピー中...");
                    await Task.Run(() => CopyDirectory(config.GameInstallPath, targetDir));
                }
                SetProgress(60);

                ShowStatus("ZIPを展開中...");
                string extractPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AUMMDirectExtract_" + Guid.NewGuid());
                await Task.Run(() =>
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, extractPath);
                    var bepDir = Directory.GetDirectories(extractPath, "BepInEx", SearchOption.AllDirectories).FirstOrDefault();
                    string srcRoot = bepDir != null ? Directory.GetParent(bepDir)!.FullName : extractPath;
                    CopyDirectory(srcRoot, targetDir);
                    Directory.Delete(extractPath, true);
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                });
                SetProgress(85);

                var newMod = new VanillaPathInfo
                {
                    Name = folderName, Path = targetDir,
                    CurrentVersion = "手動インストール", IsAutoUpdateEnabled = false, LastChecked = DateTime.Now
                };
                if (!config.VanillaPaths.Any(v => v.Path == targetDir)) { config.VanillaPaths.Add(newMod); ConfigService.Save(config); }

                string platform3 = !string.IsNullOrEmpty(config.MainPlatform) ? config.MainPlatform : config.Platform;
                string shareCode = ShareCodeService.Generate(newMod, platform3,
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsModManager", "SharedCodes"));

                SetProgress(100); ShowStatus("完了！");
                LogService.Info("ModInstallPage", $"直接URLインストール完了: {folderName}");
                await Task.Delay(500);
                _installProgressDialog.Hide();
                await ShowPostInstallSetup(newMod, shareCode);
            }
            catch (Exception ex)
            {
                LogService.Error("ModInstallPage", "直接URLインストールエラー", ex);
                string msg = ex.Message ?? "";
                if (msg.Contains("403") || msg.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                    ShowStatus("⚠️ ダウンロード先が認証を要求しています（403）。URLが公開直リンクか確認してください。");
                else
                    ShowStatus($"エラー: {ex.Message}");
            }
        }

        private async void SelectZip_Click(object sender, RoutedEventArgs e) => InstallFromZip_Click(sender, e);

        private async Task ShowPostInstallSetup(VanillaPathInfo mod, string shareCode)
        {
            var autoUpdateCheck = new CheckBox { Content = "このModの自動アップデートを有効にする", IsChecked = mod.IsAutoUpdateEnabled };
            var codeBox = new TextBox
            {
                Text = shareCode, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 13, IsReadOnly = true, TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                BorderThickness = new Thickness(1), Padding = new Thickness(8, 6, 8, 6)
            };
            var copyBtn = new Button { Content = "📋 コピー", HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 4, 0, 0) };
            copyBtn.Click += (_, _) =>
            {
                var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dp.SetText(shareCode);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
                copyBtn.Content = "✅ コピー済み";
            };
            var codeRow = new StackPanel { Spacing = 4 };
            codeRow.Children.Add(codeBox);
            codeRow.Children.Add(copyBtn);

            var panel = new StackPanel { Spacing = 10, Width = 400 };
            panel.Children.Add(new TextBlock { Text = "インストールが完了しました！", TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(new TextBlock { Text = "共有コード", FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            panel.Children.Add(codeRow);
            panel.Children.Add(new TextBlock { Text = "このコードを渡すと相手もワンクリックでインストールできます（.aumanagerファイルも同時に保存済み）",
                FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            panel.Children.Add(autoUpdateCheck);

            var dialog = new ContentDialog { Title = "インストール完了", Content = panel, PrimaryButtonText = "ライブラリへ", CloseButtonText = "閉じる", XamlRoot = this.XamlRoot };
            var result = await dialog.ShowAsync();

            var config = ConfigService.Load();
            var target = config.VanillaPaths.FirstOrDefault(v => v.Path == mod.Path);
            if (target != null) { target.IsAutoUpdateEnabled = autoUpdateCheck.IsChecked ?? false; ConfigService.Save(config); }

            if (result == ContentDialogResult.Primary)
                if (App.MainWindowInstance is MainWindow mw) mw.NavigateToPendingPage("Library");
        }

        private async Task ShowError(string msg)
        {
            LogService.Warn("ModInstallPage", $"エラーダイアログ表示: {msg}");
            await new ContentDialog { Title = "エラー", Content = msg, CloseButtonText = "OK", XamlRoot = this.XamlRoot }.ShowAsync();
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var f in Directory.GetFiles(source))
                File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(source))
                CopyDirectory(d, Path.Combine(dest, Path.GetFileName(d)));
        }
    }
}
