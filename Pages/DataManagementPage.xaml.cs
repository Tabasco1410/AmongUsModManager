using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using AmongUsModManager.Models;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager.Pages
{
    public class BanListEntry
    {
        public string Code { get; set; } = "";
    }

    public sealed partial class DataManagementPage : Page
    {
        private string? _currentBanListPath;

        public DataManagementPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var config = ConfigService.Load();

            FriendListView.ItemsSource = config.Friends;
            FriendEmptyText.Visibility = config.Friends.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;

            BanModSelector.Items.Clear();
            foreach (var mod in config.VanillaPaths)
                BanModSelector.Items.Add(new ComboBoxItem { Content = mod.Name, Tag = mod.Path });
        }

        

        private void BanModSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BanModSelector.SelectedItem is not ComboBoxItem item) return;
            string modPath = item.Tag?.ToString() ?? "";
            LoadBanList(modPath);
        }

        private void LoadBanList(string modPath)
        {
            _currentBanListPath = null;

            string[] priorityCandidates = new[]
            {
                Path.Combine(modPath, "BanList.txt"),
                Path.Combine(modPath, "BepInEx", "BanList.txt"),
                Path.Combine(modPath, "ban.txt"),
            };
            _currentBanListPath = priorityCandidates.FirstOrDefault(File.Exists);

            if (_currentBanListPath == null && Directory.Exists(modPath))
            {
                var found = Directory.GetFiles(modPath, "BanList.txt", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(modPath, "ban.txt", SearchOption.AllDirectories))
                    .FirstOrDefault();
                _currentBanListPath = found;
            }

            if (_currentBanListPath != null)
            {
                BanFilePathText.Text = _currentBanListPath;
                var lines = File.ReadAllLines(_currentBanListPath)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => new BanListEntry { Code = l.Trim() })
                    .ToList();
                BanListView.ItemsSource = lines;
                BanEmptyText.Visibility = lines.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                BanEmptyText.Text = "バンリストは空です";
            }
            else
            {
                BanFilePathText.Text = "BanList.txt が見つかりません";
                BanListView.ItemsSource = null;
                BanEmptyText.Visibility = Visibility.Visible;
                BanEmptyText.Text = "このModフォルダにBanList.txtが見つかりません";
            }
        }

        private void RefreshBanList_Click(object sender, RoutedEventArgs e)
        {
            if (BanModSelector.SelectedItem is ComboBoxItem item)
                LoadBanList(item.Tag?.ToString() ?? "");
        }

        private async void AddBan_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBanListPath == null)
            {
                if (BanModSelector.SelectedItem is not ComboBoxItem selItem)
                {
                    var noModDlg = new ContentDialog
                    {
                        Title = "Modが選択されていません",
                        Content = "まず対象のModを選択してください。",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await noModDlg.ShowAsync();
                    return;
                }

                string modPath = selItem.Tag?.ToString() ?? "";
                string newPath = Path.Combine(modPath, "BanList.txt");
                var createDlg = new ContentDialog
                {
                    Title = "BanList.txtを作成",
                    Content = $"BanList.txtが存在しません。\n{newPath}\nに新規作成しますか？",
                    PrimaryButtonText = "作成",
                    CloseButtonText = "キャンセル",
                    XamlRoot = this.XamlRoot
                };
                if (await createDlg.ShowAsync() != ContentDialogResult.Primary) return;
                File.WriteAllText(newPath, "");
                _currentBanListPath = newPath;
                BanFilePathText.Text = newPath;
            }

            var codeBox = new TextBox { Header = "フレンドコード（BANするコード）", PlaceholderText = "XXXXXX" };
            var dialog = new ContentDialog
            {
                Title = "バンリストに追加",
                Content = codeBox,
                PrimaryButtonText = "追加",
                CloseButtonText = "キャンセル",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(codeBox.Text))
            {
                File.AppendAllText(_currentBanListPath, codeBox.Text.Trim() + Environment.NewLine);
                if (BanModSelector.SelectedItem is ComboBoxItem item)
                    LoadBanList(item.Tag?.ToString() ?? "");
            }
        }

        private async void DeleteBanEntry_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn && btn.Tag is BanListEntry entry)) return;
            if (_currentBanListPath == null || !File.Exists(_currentBanListPath)) return;

            var dialog = new ContentDialog
            {
                Title = "削除の確認",
                Content = $"「{entry.Code}」をバンリストから削除しますか？",
                PrimaryButtonText = "削除",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var lines = File.ReadAllLines(_currentBanListPath)
                    .Where(l => l.Trim() != entry.Code)
                    .ToList();
                File.WriteAllLines(_currentBanListPath, lines);
                if (BanModSelector.SelectedItem is ComboBoxItem item)
                    LoadBanList(item.Tag?.ToString() ?? "");
            }
        }

        
        private async void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            var nameBox = new TextBox { Header = "名前", PlaceholderText = "プレイヤー名" };
            var codeBox = new TextBox { Header = "フレンドコード", PlaceholderText = "XXXXXX" };
            var memoBox = new TextBox { Header = "メモ（任意）" };

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(nameBox);
            panel.Children.Add(codeBox);
            panel.Children.Add(memoBox);

            var dialog = new ContentDialog
            {
                Title = "フレンドコードを追加",
                Content = panel,
                PrimaryButtonText = "追加",
                CloseButtonText = "キャンセル",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
            {
                var config = ConfigService.Load();
                config.Friends.Add(new FriendEntry { Name = nameBox.Text, Code = codeBox.Text, Memo = memoBox.Text });
                ConfigService.Save(config);
                LoadData();
            }
        }

        private void CopyCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string code)
            {
                var dp = new DataPackage();
                dp.SetText(code);
                Clipboard.SetContent(dp);
            }
        }

        private async void EditFriend_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn && btn.Tag is FriendEntry entry)) return;

            var nameBox = new TextBox { Header = "名前", Text = entry.Name };
            var codeBox = new TextBox { Header = "フレンドコード", Text = entry.Code };
            var memoBox = new TextBox { Header = "メモ", Text = entry.Memo };

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(nameBox);
            panel.Children.Add(codeBox);
            panel.Children.Add(memoBox);

            var dialog = new ContentDialog
            {
                Title = "フレンドコードを編集",
                Content = panel,
                PrimaryButtonText = "保存",
                CloseButtonText = "キャンセル",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var config = ConfigService.Load();
                var target = config.Friends.FirstOrDefault(f => f.Code == entry.Code && f.Name == entry.Name);
                if (target != null)
                {
                    target.Name = nameBox.Text;
                    target.Code = codeBox.Text;
                    target.Memo = memoBox.Text;
                    ConfigService.Save(config);
                    LoadData();
                }
            }
        }

        private async void DeleteFriend_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn && btn.Tag is FriendEntry entry)) return;

            var dialog = new ContentDialog
            {
                Title = "削除の確認",
                Content = $"「{entry.Name}」を削除しますか？",
                PrimaryButtonText = "削除",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var config = ConfigService.Load();
                config.Friends.RemoveAll(f => f.Code == entry.Code && f.Name == entry.Name);
                ConfigService.Save(config);
                LoadData();
            }
        }
    }
}
