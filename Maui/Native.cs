// ============================================================
// Native.cs — OS마다 다르게 동작하는 기능
//
// Windows: 창 숨기기, 항상 위/맨 아래, 전체화면, 최대화, Ctrl 키 확인, 단축키
// 맥: 운영체제가 공개 API로 제공하지 않는 기능은 동작하지 않음 (안전하게 무시)
// ============================================================
namespace OttMaui;

[Flags]
public enum Mods { None = 0, Ctrl = 1, Alt = 2, Shift = 4, Cmd = 8 }

/// <summary>단축키 하나. Windows와 맥의 키를 따로 정합니다.</summary>
public record Shortcut(string Label, string WinKey, Mods WinMods, string MacKey, Mods MacMods, Action Run)
{
    /// <summary>도움말에 표시할 키 이름 (예: Ctrl+M, ⌘⇧M)</summary>
    public string KeyText => Native.IsMac
        ? $"{(MacMods.HasFlag(Mods.Ctrl) ? "⌃" : "")}{(MacMods.HasFlag(Mods.Alt) ? "⌥" : "")}{(MacMods.HasFlag(Mods.Shift) ? "⇧" : "")}{(MacMods.HasFlag(Mods.Cmd) ? "⌘" : "")}{MacKey}"
        : $"{(WinMods.HasFlag(Mods.Ctrl) ? "Ctrl+" : "")}{(WinMods.HasFlag(Mods.Alt) ? "Alt+" : "")}{(WinMods.HasFlag(Mods.Shift) ? "Shift+" : "")}{WinKey.Replace("Left", "←").Replace("Right", "→")}";
}

public static class Native
{
    public static bool IsMac => DeviceInfo.Platform == DevicePlatform.MacCatalyst;

    /// <summary>PiP 창의 항상 위/맨 아래 지원 여부</summary>
    public static bool SupportsLayers => !IsMac;

#if WINDOWS
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    private static Microsoft.UI.Xaml.Window? WinUi(Window? w) => w?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
    private static Microsoft.UI.Windowing.AppWindow? AppWin(Window? w) => WinUi(w)?.AppWindow;
    private static Microsoft.UI.Windowing.OverlappedPresenter? Presenter(Window? w) =>
        AppWin(w)?.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;

    public static bool IsCtrlDown() =>
        Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

    public static void Hide(Window? w) => AppWin(w)?.Hide();
    public static void Show(Window? w) => AppWin(w)?.Show();

    public static void SetFullScreen(Window? w, bool on) =>
        AppWin(w)?.SetPresenter(on ? Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen
                                   : Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);

    public static bool IsMaximized(Window? w) => Presenter(w)?.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized;
    public static bool IsMinimized(Window? w) => Presenter(w)?.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized;
    public static void Maximize(Window? w) => Presenter(w)?.Maximize();

    /// <summary>0 = 항상 위, 1 = 일반, 2 = 맨 아래</summary>
    public static void SetLayer(Window? w, int mode)
    {
        if (Presenter(w) is { } p) p.IsAlwaysOnTop = mode == 0;
        if (mode == 2 && WinUi(w) is { } native)
            SetWindowPos(WinRT.Interop.WindowNative.GetWindowHandle(native), new IntPtr(1), 0, 0, 0, 0, 0x0003);
    }

    public static bool IsWebView2Missing()
    {
        try { return string.IsNullOrEmpty(Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString()); }
        catch { return true; }
    }

    /// <summary>사용자별 브라우저 저장소 (사이트 로그인 정보를 계정마다 분리)</summary>
    public static void SetWebDataFolder(string dir) => Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", dir);

    /// <summary>창 전체에 단축키를 등록합니다. 기존 단축키는 지웁니다.</summary>
    public static void SetShortcuts(Window? w, IEnumerable<Shortcut> shortcuts)
    {
        if (WinUi(w)?.Content is not Microsoft.UI.Xaml.UIElement root) return;
        root.KeyboardAccelerators.Clear();
        root.KeyboardAcceleratorPlacementMode = Microsoft.UI.Xaml.Input.KeyboardAcceleratorPlacementMode.Hidden;
        foreach (var s in shortcuts)
        {
            string key = s.WinKey.Length == 1 && char.IsDigit(s.WinKey[0]) ? "Number" + s.WinKey : s.WinKey;
            var mods = Windows.System.VirtualKeyModifiers.None;
            if (s.WinMods.HasFlag(Mods.Ctrl))  mods |= Windows.System.VirtualKeyModifiers.Control;
            if (s.WinMods.HasFlag(Mods.Alt))   mods |= Windows.System.VirtualKeyModifiers.Menu;
            if (s.WinMods.HasFlag(Mods.Shift)) mods |= Windows.System.VirtualKeyModifiers.Shift;
            var ka = new Microsoft.UI.Xaml.Input.KeyboardAccelerator
            {
                Key = Enum.Parse<Windows.System.VirtualKey>(key), Modifiers = mods
            };
            ka.Invoked += (x, e) => { e.Handled = true; s.Run(); };
            root.KeyboardAccelerators.Add(ka);
        }
    }
#else
    public static bool IsCtrlDown() => false;
    public static void Hide(Window? w) { }
    public static void Show(Window? w) { }
    public static void SetFullScreen(Window? w, bool on) { }
    public static bool IsMaximized(Window? w) => false;
    public static bool IsMinimized(Window? w) => false;
    public static void Maximize(Window? w) { }
    public static void SetLayer(Window? w, int mode) { }
    public static bool IsWebView2Missing() => false;
    public static void SetWebDataFolder(string dir) { }
    public static void SetShortcuts(Window? w, IEnumerable<Shortcut> shortcuts) { }  // 맥은 메뉴 막대 단축키 사용
#endif
}
