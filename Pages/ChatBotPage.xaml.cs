using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AmongUsModManager.Pages
{
    public sealed partial class ChatBotPage : Page
    {
        public ChatBotPage()
        {
            this.InitializeComponent();
            AddBubble("assistant",
                "こんにちは！AmongUsModManager サポートBOT (β) です\n\n" +
                "下のボタンからご質問の内容を選んでください。\n" +
                "※ このチャットボットはβ版です。内容が不正確な場合はDiscordでお知らせください。");
        }

        private async void QuickBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string tag = btn.Tag?.ToString() ?? "";

            // Discord は即時処理
            if (tag == "discord")
            {
                AddBubble("user", "Discordサーバーを教えて");
                await Task.Delay(150);
                AddBubble("assistant",
                    "Discord サーバーはこちらです！\n" +
                    "バグ報告・質問・要望など何でもどうぞ\n\n" +
                    "https://discord.com/invite/nFhkYmf9At");
                Process.Start(new ProcessStartInfo("https://discord.com/invite/nFhkYmf9At") { UseShellExecute = true });
                return;
            }

            string question = tag switch
            {
                "install"  => "Modのインストール方法を教えてください",
                "library"  => "Modライブラリの使い方を教えてください",
                "error"    => "エラーが出たときの対処法を教えてください",
                "github"   => "GitHub連携の設定方法を教えてください",
                "update"   => "アプリやModのアップデート方法を教えてください",
                "version"  => "バージョン管理について教えてください",
                _          => tag
            };

            AddBubble("user", question);
            await Task.Delay(150);
            AddBubble("assistant", GetReply(tag));
            ScrollToBottom();
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            MessagePanel.Children.Clear();
            AddBubble("assistant", "会話をリセットしました。ボタンから質問を選んでください！");
        }

        // ─── 固定回答 ────────────────────────────────────────────────
        private static string GetReply(string tag) => tag switch
        {
            "install" =>
                "Modのインストール手順：\n\n" +
                "1. 左メニューの「インストール」を開く\n" +
                "2. インストールしたいModのカードを選択\n" +
                "3. バージョン・ファイルを選んで「インストールを実行」\n\n" +
                "※ Among UsのバージョンにあったModを選んでください。",

            "library" =>
                "Modライブラリでできること：\n\n" +
                "・インストール済みModの一覧を確認\n" +
                "・ゲームの起動\n" +
                "・Modフォルダを開く\n" +
                "・GitHub連携でバージョン管理\n\n" +
                "左メニューの「ライブラリ」から開けます。",

            "error" =>
                "エラーが発生した場合の対処法：\n\n" +
                "1. アプリを再起動してもう一度試す\n" +
                "2. 設定のログ表示でエラー内容を確認\n" +
                "3. Among UsのバージョンとModの対応表を確認\n" +
                "4. Modを一度アンインストールして再インストール\n\n" +
                "解決しない場合はDiscordへ！\nhttps://discord.com/invite/nFhkYmf9At",

            "github" =>
                "GitHub連携の設定手順：\n\n" +
                "1. 左メニューの「アカウント」を開く\n" +
                "2. 「GitHubでログイン」をクリック\n" +
                "3. ブラウザで認証を完了する\n\n" +
                "連携すると：\n" +
                "・Modの最新バージョンを自動チェック\n" +
                "・ワンクリックでアップデート\n" +
                "・プライベートリポジトリにも対応",

            "update" =>
                "アップデート方法：\n\n" +
                "【アプリ本体】\n" +
                "起動時に自動で新しいバージョンを確認します。\n" +
                "通知が出たら「アップデート」ボタンを押してください。\n\n" +
                "【Mod】\n" +
                "1. ライブラリを開く\n" +
                "2. 更新があるModに「↑」バッジが表示される\n" +
                "3. カードを選択して「アップデート」を押す",

            "version" =>
                "バージョン管理について：\n\n" +
                "・インストール済みModのバージョンはライブラリで確認できます\n" +
                "・GitHub連携を設定するとリリース情報が自動で取得されます\n" +
                "・古いバージョンに戻したい場合はインストール画面でバージョンを指定できます\n\n" +
                "Among Usのバージョンに合ったModを使うことが重要です。",

            _ =>
                "ご質問ありがとうございます。\n" +
                "詳しくはDiscordでお気軽にどうぞ！\nhttps://discord.com/invite/nFhkYmf9At"
        };

        // ─── UI ─────────────────────────────────────────────────────
        private void AddBubble(string role, string text)
        {
            bool isUser = role == "user";
            var tb = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = new SolidColorBrush(Colors.White),
                IsTextSelectionEnabled = true,
            };
            var bubble = new Border
            {
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 520,
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Background = isUser
                    ? (SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"]
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 50, 62)),
                Child = tb,
            };
            var time = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 180, 180, 180)),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(6, 2, 6, 0),
            };
            var container = new StackPanel { Spacing = 2 };
            container.Children.Add(bubble);
            container.Children.Add(time);
            MessagePanel.Children.Add(container);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            DispatcherQueue.TryEnqueue(() =>
                MessageScrollViewer.ScrollToVerticalOffset(MessageScrollViewer.ScrollableHeight));
        }
    }
}
