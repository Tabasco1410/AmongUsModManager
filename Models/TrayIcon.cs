using System;
using System.Runtime.InteropServices;
using System.Threading;
using AmongUsModManager.Services;

namespace AmongUsModManager.Models
{
    /// <summary>
    /// Win32 P/Invoke だけで実装したタスクトレイアイコン。
    /// System.Windows.Forms / WPF への依存なし。
    /// </summary>
    public sealed class TrayIcon : IDisposable
    {
        // ━━━ Win32 定数 ━━━
        private const int WM_USER       = 0x0400;
        private const int WM_TRAY       = WM_USER + 1;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP  = 0x0205;
        private const int NIM_ADD       = 0;
        private const int NIM_DELETE    = 2;
        private const int NIF_MESSAGE   = 0x01;
        private const int NIF_ICON      = 0x02;
        private const int NIF_TIP       = 0x04;
        private const int NIF_INFO      = 0x10;
        private const int NIIF_INFO     = 0x01;
        private const int MF_STRING     = 0x00000000;
        private const int MF_SEPARATOR  = 0x00000800;
        private const int TPM_RETURNCMD = 0x0100;
        private const int TPM_RIGHTALIGN = 0x0008;
        private const int IDI_APPLICATION = 0x7F00;
        private const int IMAGE_ICON    = 1;
        private const int LR_SHARED     = 0x8000;

        // ━━━ Win32 構造体 ━━━
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int    cbSize;
            public IntPtr hWnd;
            public uint   uID;
            public uint   uFlags;
            public uint   uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint   dwState;
            public uint   dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint   uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint   dwInfoFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint   message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint   time;
            public POINT  pt;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public int      cbSize;
            public int      style;
            public IntPtr   lpfnWndProc;
            public int      cbClsExtra;
            public int      cbWndExtra;
            public IntPtr   hInstance;
            public IntPtr   hIcon;
            public IntPtr   hCursor;
            public IntPtr   hbrBackground;
            public string?  lpszMenuName;
            public string   lpszClassName;
            public IntPtr   hIconSm;
        }

        // ━━━ P/Invoke ━━━
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpdata);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName,
            int dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpmsg);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, IntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr hMenu, int uFlags, int x, int y,
            int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadImageW(IntPtr hinst, string lpszName, uint uType,
            int cxDesired, int cyDesired, uint fuLoad);

        private static IntPtr LoadImageFromFile(string path, int w, int h)
        {
            const uint IMAGE_ICON = 1;
            const uint LR_LOADFROMFILE = 0x0010;
            const uint LR_DEFAULTSIZE  = 0x0040;
            return LoadImageW(IntPtr.Zero, path, IMAGE_ICON, w, h, LR_LOADFROMFILE | LR_DEFAULTSIZE);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr LoadImage(IntPtr hinst, IntPtr lpszName, uint uType,
            int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        // ━━━ フィールド ━━━
        private IntPtr _hwnd;
        private IntPtr _hIcon;
        private Thread? _thread;
        private bool _disposed;
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate? _wndProcDelegate; // GC対策で保持

        private static readonly int MENU_SHOW = 1001;
        private static readonly int MENU_EXIT = 1002;

        public event Action? ShowRequested;
        public event Action? ExitRequested;

        // ━━━ 公開API ━━━

        public void Show(string tooltip = "AmongUsModManager")
        {
            if (_thread != null) return;
            _thread = new Thread(() => RunMessageLoop(tooltip))
            {
                IsBackground = true,
                Name = "TrayIconThread"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        public void Hide()
        {
            if (_hwnd == IntPtr.Zero) return;
            var data = BuildNid(_hwnd, IntPtr.Zero, "");
            Shell_NotifyIcon(NIM_DELETE, ref data);
        }

        public void ShowBalloon(string title, string message)
        {
            if (_hwnd == IntPtr.Zero) return;
            var data = BuildNid(_hwnd, _hIcon, "AmongUsModManager");
            data.uFlags    |= NIF_INFO;
            data.szInfo     = message;
            data.szInfoTitle = title;
            data.dwInfoFlags = NIIF_INFO;
            data.uTimeoutOrVersion = 3000;
            Shell_NotifyIcon(1 /*NIM_MODIFY*/, ref data);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Hide();
            if (_hwnd != IntPtr.Zero)
            {
                PostQuitMessage(0);
                _hwnd = IntPtr.Zero;
            }
        }

        // ━━━ メッセージループ ━━━

        private void RunMessageLoop(string tooltip)
        {
            try
            {
                _wndProcDelegate = WndProc;
                var hInstance = GetModuleHandle(null);

                string className = "AUMMTrayWnd_" + Guid.NewGuid().ToString("N")[..8];
                var wc = new WNDCLASSEX
                {
                    cbSize      = Marshal.SizeOf<WNDCLASSEX>(),
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                    hInstance   = hInstance,
                    lpszClassName = className
                };
                RegisterClassEx(ref wc);

                _hwnd = CreateWindowEx(0, className, "TrayHost", 0,
                    0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

                // アプリアイコン：icoファイルがあればそちらを使う
                string? icoPath = MainWindow.ResolveIconPath();
                if (icoPath != null)
                {
                    _hIcon = LoadImageFromFile(icoPath, 32, 32);
                }
                // フォールバック：標準アプリアイコン
                if (_hIcon == IntPtr.Zero)
                {
                    _hIcon = LoadImage(IntPtr.Zero, new IntPtr(IDI_APPLICATION), IMAGE_ICON,
                        0, 0, LR_SHARED);
                }

                var data = BuildNid(_hwnd, _hIcon, tooltip);
                Shell_NotifyIcon(NIM_ADD, ref data);

                while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

                var del = BuildNid(_hwnd, IntPtr.Zero, "");
                Shell_NotifyIcon(NIM_DELETE, ref del);
            }
            catch (Exception ex)
            {
                LogService.Warn("TrayIcon", $"トレイスレッドエラー: {ex.Message}");
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_TRAY)
            {
                int trayMsg = (int)(lParam.ToInt64() & 0xFFFF);
                if (trayMsg == WM_LBUTTONDBLCLK)
                {
                    ShowRequested?.Invoke();
                }
                else if (trayMsg == WM_RBUTTONUP)
                {
                    ShowContextMenu(hWnd);
                }
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void ShowContextMenu(IntPtr hWnd)
        {
            GetCursorPos(out var pt);
            SetForegroundWindow(hWnd);

            IntPtr hMenu = CreatePopupMenu();
            AppendMenu(hMenu, MF_STRING,   new IntPtr(MENU_SHOW), "AmongUsModManager を開く");
            AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, "");
            AppendMenu(hMenu, MF_STRING,   new IntPtr(MENU_EXIT), "終了");

            int cmd = TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_RIGHTALIGN,
                pt.x, pt.y, 0, hWnd, IntPtr.Zero);
            DestroyMenu(hMenu);

            if (cmd == MENU_SHOW) ShowRequested?.Invoke();
            else if (cmd == MENU_EXIT) ExitRequested?.Invoke();
        }

        private static NOTIFYICONDATA BuildNid(IntPtr hWnd, IntPtr hIcon, string tip)
        {
            return new NOTIFYICONDATA
            {
                cbSize          = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd            = hWnd,
                uID             = 1,
                uFlags          = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAY,
                hIcon           = hIcon,
                szTip           = tip,
                szInfo          = "",
                szInfoTitle     = ""
            };
        }
    }
}
