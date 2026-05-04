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
            /*AddBubble("assistant",
                "こんにちは！AmongUsModManager サポートです 👋\n\n" +
                "下のボタンからご質問の内容を選んでください。");*/
        }

        /*private async void QuickBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string tag = btn.Tag?.ToString() ?? "";

            if (tag == "discord")
            {
                AddBubble("user", "Discordサーバーを教えて");
                await Task.Delay(150);
                AddBubble("assistant",
                    "Discord サーバーはこちらです！\n" +
                    "バグ報告・質問・要望など何でもどうぞ 🎉\n\n" +
                    "https://discord.com/invite/nFhkYmf9At");
                Process.Start(new ProcessStartInfo("https://discord.com/invite/nFhkYmf9At") { UseShellExecute = true });
                return;
            }

            string question = tag switch
            {
                "what_is_this" => "このアプリって何？",
                "install"      => "Modのインストール方法を教えてください",
                "library"      => "ライブラリの使い方を教えてください",
                "launch"       => "ゲームの起動方法を教えてください",
                "error"        => "エラーが出たときの対処法を教えてください",
                "epic"         => "Epic版について教えてください",
                "github"       => "GitHub連携の設定方法を教えてください",
                "update"       => "アップデート方法を教えてください",
                "version"      => "バージョン管理について教えてください",
                "vanilla"      => "Modなしで遊ぶにはどうすればいいですか？",
                "notification" => "通知やお知らせの使い方を教えてください",
                "settings"     => "設定画面の使い方を教えてください",
                _              => tag
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

        // ─── 回答 ───────────────────────────────────────────────────────
        private static string GetReply(string tag) => tag switch
        {
            "what_is_this" =>
                "AmongUsModManager（AUMM）は、Among Us の Mod を簡単に管理できるツールです！\n\n" +
                "📦 できること:\n" +
                "・TownOfHost / ExtremeRoles などの人気Modをワンクリックでインストール\n" +
                "・インストール済みModの一覧管理・バージョン確認\n" +
                "・Mod入り / バニラ（Mod無し）を切り替えてゲームを起動\n" +
                "・GitHub と連携してModの最新バージョンを自動チェック\n" +
                "・Steam / Epic Games / itch.io など複数のプラットフォームに対応\n" +
                "・お知らせ・更新通知の受け取り\n\n" +
                "開発者向け機能（デバッグコンソール・ログ表示など）も搭載しています。",

            "install" =>
                "Modのインストール手順：\n\n" +
                "1. 左メニューの「インストール」を開く\n" +
                "2. インストールしたいModのカードを選択\n" +
                "3. バージョン・ファイルを選んで「インストールを実行」\n" +
                "4. 完了後は「ライブラリ」から起動できます\n\n" +
                "⚠️ Among Us のバージョンに合ったModを選んでください。\n" +
                "バージョンが合わないと起動しないことがあります。",

            "library" =>
                "ライブラリでできること：\n\n" +
                "・インストール済みModの一覧を確認\n" +
                "・「起動」ボタンでゲームをその設定で開始\n" +
                "・「Modフォルダを開く」でファイルを直接確認\n" +
                "・GitHub連携でバージョン管理\n" +
                "・更新があるModには「更新あり」バッジが表示される\n\n" +
                "左メニューの「ライブラリ」から開けます。",

            "launch" =>
                "ゲームの起動方法：\n\n" +
                "【Modありで遊ぶ場合】\n" +
                "「ライブラリ」からModを選択 →「起動」ボタン\n\n" +
                "【バニラ（Modなし）で遊ぶ場合】\n" +
                "「ライブラリ」でバニラ（Modなし）を選択 →「起動」\n" +
                "または Steam / Epic からそのまま起動してもOKです\n\n" +
                "【Epic版の場合】\n" +
                "アカウントにログイン済みであれば Epic Games Launcher なしで\n" +
                "アプリから直接起動できます。\n" +
                "ログインはアカウントページまたはセットアップ画面から行えます。",

            "error" =>
                "エラーが発生した場合の対処法：\n\n" +
                "1. アプリを再起動してもう一度試す\n" +
                "2. 「ログ」ページでエラー内容を確認する\n" +
                "3. Among Us のバージョンとModの対応を確認する\n" +
                "   → Modの GitHub リリースページに対応バージョンが書いてあります\n" +
                "4. Modを一度アンインストールして再インストール\n" +
                "5. 設定 →「ゲームフォルダ」のパスが正しいか確認\n\n" +
                "解決しない場合は Discord へ！\nhttps://discord.com/invite/nFhkYmf9At",

            "epic" =>
                "Epic Games 版について：\n\n" +
                "【ログインについて】\n" +
                "・セットアップまたは「アカウント」ページから Epic Games にログインできます\n" +
                "・ログインすると Epic Games Launcher なしで直接起動できます\n" +
                "・オフラインでMod専用プレイのみなら省略できます\n\n" +
                "【起動オプション（設定で変更可能）】\n" +
                "・「ランチャー経由で起動」ON → オンラインマッチ・実績が正常に動作\n" +
                "・「ランチャー経由で起動」OFF → exe を直接起動（オフラインMod専用向け）\n\n" +
                "Epic のログイン状態は「アカウント」ページで確認できます。",

            "github" =>
                "GitHub連携の設定手順：\n\n" +
                "1. 左メニューの「アカウント」を開く\n" +
                "2. 「GitHubでログイン」をクリック\n" +
                "3. 表示されたコードをブラウザで入力して認証\n\n" +
                "連携すると：\n" +
                "・Modの最新バージョンを自動チェック\n" +
                "・ワンクリックでアップデート\n" +
                "・GitHub API のレート制限緩和（未連携より多くリクエストできる）\n\n" +
                "セットアップ画面でも連携できます。",

            "update" =>
                "アップデート方法：\n\n" +
                "【アプリ本体の更新】\n" +
                "起動時に自動で最新バージョンを確認します。\n" +
                "左パネルに「アップデート」ボタンが表示されたらクリックしてください。\n\n" +
                "【Modの更新】\n" +
                "1. 「ライブラリ」を開く\n" +
                "2. 更新があるModに「更新あり」バッジが表示される\n" +
                "3. カードを選んで「アップデート」を押す\n\n" +
                "GitHub連携をしておくと更新検出が自動で行われます。",

            "version" =>
                "バージョン管理について：\n\n" +
                "・インストール済みModのバージョンはライブラリで確認できます\n" +
                "・GitHub連携を設定するとリリース情報が自動で取得されます\n" +
                "・古いバージョンに戻したい場合はインストール画面でバージョンを指定できます\n" +
                "  →「インストール」→ Modを選択 →「バージョンを選ぶ」\n\n" +
                "Among Us のバージョンに合ったModを使うことが重要です。\n" +
                "バージョンが合わないとゲームが起動しないことがあります。",

            "vanilla" =>
                "Modなし（バニラ）で遊ぶには：\n\n" +
                "方法1: Steam / Epic からそのまま起動する\n" +
                "方法2: ライブラリで「バニラ（Modなし）」を選んで起動する\n\n" +
                "ライブラリにバニラが表示されない場合は、\n" +
                "設定 →「ゲームフォルダ」にバニラ用のパスを登録してください。\n\n" +
                "⚠️ Modが入ったまま公式サーバーに接続すると\n" +
                "BANされる場合があります。Modなし版で接続しましょう。",

            "notification" =>
                "通知・お知らせについて：\n\n" +
                "【お知らせ（ニュース）】\n" +
                "・左メニューの🔔アイコンで確認できます\n" +
                "・未読があると数字のバッジが表示されます\n" +
                "・起動時にバックグラウンドで未読数を取得します\n\n" +
                "【通知の種類】\n" +
                "・Modの更新あり通知\n" +
                "・アプリ本体の更新通知\n" +
                "・Epic ログイン状態の警告\n\n" +
                "通知の ON/OFF は設定画面で変更できます（準備中）。",

            "settings" =>
                "設定画面の主な項目：\n\n" +
                "【ゲームフォルダ】\n" +
                "Among Us のインストール先フォルダを登録・管理します。\n" +
                "複数のプラットフォームを同時に登録できます。\n\n" +
                "【メインプラットフォーム】\n" +
                "起動時に使うプラットフォームを選択します。\n\n" +
                "【Modインストール先】\n" +
                "Modのコピー先フォルダを変更できます。\n\n" +
                "【起動・常駐設定】\n" +
                "Windows 起動時の自動起動、トレイ常駐の設定です。\n\n" +
                "【Epic Games 設定】\n" +
                "Epic 版のランチャー経由起動の設定です。\n\n" +
                "【ログ設定】\n" +
                "ログファイルの管理方法を設定します。\n" +
                "「新規ファイル」モードでは起動ごとに別のファイルが作成されます。",

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
        }*/
    }
}


