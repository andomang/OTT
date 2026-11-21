// ============================================================
// OttCell.cs — 멀티뷰 한 칸 (상단 바 + 웹 화면 + 오류 안내)
//
//   - 상단 바: 이름, 음량, 음소거, 크게 보기, PiP로 보내기, 닫기
//   - 상단 바를 끌어 다른 칸의 상단 바에 놓으면 자리 교체
//   - 접속 실패 시 안내와 다시 시도 버튼 표시
// ============================================================
namespace OttMaui;

public class OttCell : ContentView
{
    static readonly Color C_HEADER = Color.FromRgb(28, 28, 38);
    static readonly Color C_TEXT   = Color.FromRgb(180, 180, 195);

    public string OttName { get; }
    public WebView View { get; }
    public int Volume { get; private set; } = 100;
    public string? CurrentUrl { get; private set; }

    public event Action<OttCell>? CloseClicked, MuteClicked, MainClicked, PipClicked, HeaderPressed, Navigated, FocusGained;
    public event Action<OttCell, OttCell>? SwapRequested;  // (끌어온 칸, 놓은 칸)
    public event Action<OttCell, bool>? FullScreenChanged;

    private readonly Grid _header;
    private readonly Label _title;
    private readonly Button _btnMute, _btnMain;
    private readonly Slider _volume;
    private readonly VerticalStackLayout _error;
    private readonly Label _errorText;
    private bool _muted, _mutedApplied;
    private double? _volumeOnce;

    public OttCell(string name, string url)
    {
        OttName = name;
        BackgroundColor = Pal.ViewBg;
        View = new WebView { Source = url };

        // ── 상단 바 ───────────────────────────────────────────────
        _title = new Label
        {
            Text = name, FontSize = 11, TextColor = C_TEXT, LineBreakMode = LineBreakMode.TailTruncation,
            VerticalOptions = LayoutOptions.Center, Padding = new Thickness(8, 0, 0, 0)
        };
        _volume = new Slider(0, 100, 100)
        {
            IsVisible = false, MinimumTrackColor = Pal.Red, ThumbColor = Pal.Red, VerticalOptions = LayoutOptions.Center
        };
        _volume.ValueChanged += (s, e) =>
        {
            Volume = (int)e.NewValue;
            _volumeOnce = Volume / 100.0;
            _ = ApplyMediaAsync(force: true);
        };

        var btnVol   = HeaderBtn("Vol", "음량 조절", 34);
        _btnMute     = HeaderBtn("♪", "음소거");
        _btnMain     = HeaderBtn("⛶", "크게 보기");
        var btnPip   = HeaderBtn("⧉", "PiP로 보내기");
        var btnClose = HeaderBtn("✕", "닫기");
        btnVol.Clicked   += (s, e) => { _volume.IsVisible = !_volume.IsVisible; _title.IsVisible = !_volume.IsVisible; };
        _btnMute.Clicked += (s, e) => MuteClicked?.Invoke(this);
        _btnMain.Clicked += (s, e) => MainClicked?.Invoke(this);
        btnPip.Clicked   += (s, e) => PipClicked?.Invoke(this);
        btnClose.Clicked += (s, e) => CloseClicked?.Invoke(this);

        _header = new Grid
        {
            HeightRequest = 26, BackgroundColor = C_HEADER,
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto),
                                  new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) }
        };
        _header.Add(_title);
        _header.Add(_volume);
        _header.Add(btnVol, 1, 0);
        _header.Add(_btnMute, 2, 0);
        _header.Add(_btnMain, 3, 0);
        _header.Add(btnPip, 4, 0);
        _header.Add(btnClose, 5, 0);

        // 이름 부분을 누르면 포커스, 끌어서 다른 칸 상단 바에 놓으면 자리 교체
        _title.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => HeaderPressed?.Invoke(this)) });
        var drag = new DragGestureRecognizer();
        drag.DragStarting += (s, e) => e.Data.Properties["ottcell"] = this;
        _title.GestureRecognizers.Add(drag);
        var drop = new DropGestureRecognizer();
        drop.Drop += (s, e) =>
        {
            if (e.Data.Properties.TryGetValue("ottcell", out var o) && o is OttCell source && source != this)
                SwapRequested?.Invoke(source, this);
        };
        _header.GestureRecognizers.Add(drop);

        // ── 오류 안내 ─────────────────────────────────────────────
        _errorText = new Label { TextColor = C_TEXT, FontSize = 13, HorizontalTextAlignment = TextAlignment.Center };
        var retry = new Button { Text = "다시 시도", BackgroundColor = Pal.Red, TextColor = Colors.White, CornerRadius = 0, WidthRequest = 110 };
        retry.Clicked += (s, e) => { _error.IsVisible = false; View.IsVisible = true; View.Reload(); };
        _error = new VerticalStackLayout
        {
            IsVisible = false, Spacing = 12, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center,
            Children = { _errorText, retry }
        };

        // ── 웹 화면 이벤트 ────────────────────────────────────────
        View.Navigating += (s, e) => _title.Text = $"{name}  ·  불러오는 중...";
        View.Navigated  += async (s, e) =>
        {
            _title.Text = name;
            CurrentUrl  = e.Url;
#if !WINDOWS
            // 맥: 접속 실패 (Windows는 아래 HookWindows에서 오류 종류까지 확인)
            if (e.Result == WebNavigationResult.Failure) ShowError();
#endif
            await ApplyMediaAsync(force: true);  // 새 페이지에도 음소거/음량 적용
            Navigated?.Invoke(this);
        };
#if WINDOWS
        View.HandlerChanged += (s, e) => HookWindows();
#endif

        var body = new Grid { Children = { View, _error } };
        var root = new Grid { RowDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
        root.Add(_header);
        root.Add(body, 0, 1);
        Content = root;
    }

    public bool HeaderVisible { set => _header.IsVisible = value; }

    public void SetMain(bool main) => _btnMain.TextColor = main ? Pal.Red : C_TEXT;

    /// <param name="muted">실제로 소리를 끌지 (숨겨진 칸, PiP 모드 포함)</param>
    /// <param name="userMuted">사용자가 직접 음소거했는지 (버튼 색 표시용)</param>
    public void SetMuted(bool muted, bool userMuted)
    {
        _btnMute.TextColor = userMuted ? Pal.Red : C_TEXT;
        _muted = muted;
        _ = ApplyMediaAsync(force: false);
    }

    private async Task ApplyMediaAsync(bool force)
    {
        if (!force && _muted == _mutedApplied) return;
        _mutedApplied = _muted;
        double? keep = Volume == 100 ? null : Volume / 100.0;
        string script = WebMedia.Script(_muted, keep, _volumeOnce ?? keep);
        _volumeOnce = null;
        try { await View.EvaluateJavaScriptAsync(script); }
        catch { }  // 페이지 로딩 중이면 다음 페이지 표시 때 다시 적용됨
    }

    private void ShowError()
    {
        _errorText.Text  = $"'{OttName}'에 접속하지 못했습니다.\n인터넷 연결을 확인해주세요.";
        View.IsVisible   = false;
        _error.IsVisible = true;
    }

    private static Button HeaderBtn(string text, string tip, double width = 28)
    {
        var b = new Button
        {
            Text = text, FontSize = 11, WidthRequest = width, HeightRequest = 26, Padding = 0, CornerRadius = 0,
            BorderWidth = 0, BackgroundColor = C_HEADER, TextColor = C_TEXT
        };
        ToolTipProperties.SetText(b, tip);
        return b;
    }

#if WINDOWS
    private bool _hooked;

    /// <summary>Windows: 사이트 전체화면, 클릭 포커스, 접속 오류 종류를 WebView2에서 직접 받음</summary>
    private void HookWindows()
    {
        if (_hooked || View.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.WebView2 pv) return;
        _hooked = true;
        pv.GotFocus += (s, e) => FocusGained?.Invoke(this);
        pv.CoreWebView2Initialized += (s, e) =>
        {
            var core = pv.CoreWebView2;
            if (core == null) return;
            core.ContainsFullScreenElementChanged += (s2, e2) =>
                FullScreenChanged?.Invoke(this, core.ContainsFullScreenElement);
            core.NavigationCompleted += (s2, e2) =>
            {
                // 취소된 이동과 사이트가 준 4xx/5xx 페이지는 오류 화면을 띄우지 않음
                var st = e2.WebErrorStatus;
                if (!e2.IsSuccess &&
                    st != Microsoft.Web.WebView2.Core.CoreWebView2WebErrorStatus.Unknown &&
                    st != Microsoft.Web.WebView2.Core.CoreWebView2WebErrorStatus.OperationCanceled &&
                    st != Microsoft.Web.WebView2.Core.CoreWebView2WebErrorStatus.ConnectionAborted)
                    ShowError();
            };
        };
    }
#endif
}
