// ============================================================
// MainPage.cs — 메인 화면 (사이드바 + 멀티뷰)
//
// Windows 앱의 MainForm 로그인 후 화면과 같은 기능입니다.
//   - 클릭 → 화면 하나, Ctrl+클릭(Windows) 또는 ＋ 버튼 → 화면 추가 (최대 4개)
//   - 칸 끌어 자리 교체, 경계선 끌어 크기 조절, 크게 보기
//   - 레이아웃 저장/복구(서버 동기화), OTT 관리, 계정 관리, PiP, 통계, 다크 모드
// ============================================================
using PersonalPro;

namespace OttMaui;

public class MainPage : ContentPage
{
    private const int STATS_INTERVAL = 30;  // 시청 시간 기록 간격 (초)
    private const int GAP = 3;              // 칸 사이 경계선 절반 두께

    private readonly int _userIdx;
    private readonly string _userId;

    // ── 화면 상태 ─────────────────────────────────────────────────
    private readonly Dictionary<string, OttCell> _cells = new();      // OTT 이름 → 칸
    private readonly Dictionary<string, string> _loadedUrls = new();  // OTT 이름 → 처음 연 URL
    private readonly HashSet<string> _userMuted = new();
    private readonly string?[] _slots = new string?[4];               // 최대 4개 화면
    private List<string[]> _userOtts = new();                         // [0]=DB인덱스 [1]=이름 [2]=URL
    private string _lastFocused = "", _mainOtt = "";
    private double _splitX = 0.5, _splitY = 0.5, _panStart;
    private bool _isPipMode, _alive = true, _started, _menuExpanded = true;
    private OttCell? _fullScreen;
    private Window? _pipLauncher;

    // ── 화면 요소 ─────────────────────────────────────────────────
    private readonly Grid _topBar, _sidebar;
    private readonly ColumnDefinition _sideCol = new(230);
    private readonly RowDefinition _topRow = new(46);
    private readonly AbsoluteLayout _content = new() { BackgroundColor = Pal.ViewBg };
    private readonly BoxView _splitV = new() { Color = Color.FromRgb(70, 70, 86), IsVisible = false };
    private readonly BoxView _splitH = new() { Color = Color.FromRgb(70, 70, 86), IsVisible = false };
    private readonly VerticalStackLayout _ottList = new();
    private readonly Label _lblWelcome;
    private readonly Entry _txtAddress;
    private readonly Button _btnBack, _btnForward, _btnRefresh;
    private Label _lblTheme = null!;

    public MainPage(int userIdx, string userId, string nickname)
    {
        _userIdx = userIdx;
        _userId  = userId;
        this.Bg(Pal.Bg);

        // ── 상단 바 ───────────────────────────────────────────────
        var btnMenu = Ui.Flat("≡", Pal.TextMid, 18, 38);
        var btnHelp = Ui.Flat("?", Pal.TextMid, 14, 38);
        _btnBack    = Ui.Flat("←", Pal.TextGray, 14, 38);
        _btnForward = Ui.Flat("→", Pal.TextGray, 14, 38);
        _btnRefresh = Ui.Flat("↻", Pal.TextGray, 14, 38);
        btnMenu.Clicked     += (s, e) => ToggleSidebar();
        btnHelp.Clicked     += async (s, e) => await Navigation.PushModalAsync(new HelpPage(Shortcuts()));
        _btnBack.Clicked    += (s, e) => { if (Focused() is { } c && c.View.CanGoBack) c.View.GoBack(); };
        _btnForward.Clicked += (s, e) => { if (Focused() is { } c && c.View.CanGoForward) c.View.GoForward(); };
        _btnRefresh.Clicked += (s, e) => Focused()?.View.Reload();

        var (addrBox, txtAddress) = Ui.Input("주소를 입력하고 Enter");
        _txtAddress = txtAddress;
        _txtAddress.Completed += (s, e) => NavigateAddress();
        addrBox.HeightRequest = 32;
        addrBox.Margin = new Thickness(8, 7, 12, 7);

        _topBar = new Grid
        {
            Padding = new Thickness(6, 0, 0, 0),
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto),
                                  new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) },
            RowDefinitions = { new(GridLength.Star), new(1) }
        }.Bg(Pal.Panel);
        var topButtons = new View[] { btnMenu, btnHelp, _btnBack, _btnForward, _btnRefresh, addrBox };
        for (int i = 0; i < topButtons.Length; i++) _topBar.Add(topButtons[i], i, 0);
        var topLine = Ui.Line();
        _topBar.Add(topLine, 0, 1);
        Grid.SetColumnSpan(topLine, 6);

        // ── 사이드바: 사용자 헤더 ─────────────────────────────────
        _lblWelcome = Ui.Text(Welcome(nickname), 12, Pal.Welcome, bold: true);
        _lblWelcome.VerticalOptions = LayoutOptions.Center;
        var userHeader = new Grid
        {
            HeightRequest = 74, Padding = new Thickness(16, 0), ColumnSpacing = 10,
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }
        }.Bg(Pal.RedTint);
        var avatar = Ui.Avatar(38);
        avatar.VerticalOptions = LayoutOptions.Center;
        userHeader.Add(avatar);
        userHeader.Add(_lblWelcome, 1, 0);

        // ── 사이드바: 하단 기능 버튼 ──────────────────────────────
        var saveRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
        saveRow.Add(CenteredRow("저장", Pal.Blue, async () => await SavePresetAsync()));
        saveRow.Add(CenteredRow("복구", Pal.Green, async () => await ShowPresetMenuAsync()), 1, 0);

        var themeRow = Ui.Row(AppSettings.Current.DarkMode ? "라이트 모드" : "다크 모드", Pal.TextMid, ToggleTheme);
        _lblTheme = (Label)themeRow.Children[0];

        var actions = new VerticalStackLayout
        {
            Children =
            {
                Ui.Line(), saveRow, Ui.Line(),
                Ui.Row("화면 초기화", Pal.Danger, ClearLayout),
                Ui.Row("닉네임 변경", Pal.TextMid, async () => await ChangeNicknameAsync()),
                Ui.Row("비밀번호 변경", Pal.TextMid, async () => await ChangePasswordAsync()),
                Ui.Row("PiP 화면", Pal.TextMid, OpenPipLauncher),
                Ui.Row("시청 통계", Pal.TextMid, async () => await Navigation.PushModalAsync(new StatsPage(_userId))),
                themeRow,
                Ui.Row("계정 관리", Pal.TextMid, async () => await AccountMenuAsync()),
                Ui.Row("나가기", Pal.TextGray, Logout)
            }
        };

        _sidebar = new Grid
        {
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnDefinitions = { new(GridLength.Star), new(1) }
        }.Bg(Pal.Panel);
        _sidebar.Add(userHeader);
        _sidebar.Add(new ScrollView { Content = _ottList }, 0, 1);
        _sidebar.Add(actions, 0, 2);
        var sideLine = new BoxView().Bg(Pal.Border);
        _sidebar.Add(sideLine, 1, 0);
        Grid.SetRowSpan(sideLine, 3);

        // ── 멀티뷰 영역과 경계선 ──────────────────────────────────
        _content.SizeChanged += (s, e) => Reorganize();
        AddSplitter(_splitV, vertical: true);
        AddSplitter(_splitH, vertical: false);

        var root = new Grid { RowDefinitions = { _topRow, new(GridLength.Star) }, ColumnDefinitions = { _sideCol, new(GridLength.Star) } };
        root.Add(_topBar);
        Grid.SetColumnSpan(_topBar, 2);
        root.Add(_sidebar, 0, 1);
        root.Add(_content, 1, 1);
        Content = root;

        // 맥: 메뉴 막대에 단축키 등록 (Windows는 창에 직접 등록)
        if (Native.IsMac) BuildMacMenu();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;

        RestoreWindowState();
        Native.SetShortcuts(Window, Shortcuts());
        await RefreshOttList();
        await SyncLayoutsFromServer();
        await LoadPresetAsync(PresetPath("기본"), silent: true);

        Dispatcher.StartTimer(TimeSpan.FromSeconds(STATS_INTERVAL), () =>
        {
            RecordWatchTime();
            return _alive;
        });
    }

    private static string Welcome(string nick) => $"{nick}님\n환영합니다";

    private static Grid CenteredRow(string text, TC fg, Action onTap)
    {
        var row = Ui.Row(text, fg, onTap, 42);
        var label = (Label)row.Children[0];
        label.HorizontalOptions = LayoutOptions.Center;
        label.FontAttributes = FontAttributes.Bold;
        row.Padding = 0;
        return row;
    }

    private OttCell? Focused() => _cells.GetValueOrDefault(_lastFocused);

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // OTT 목록
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>서버에서 목록을 다시 받아 사이드바를 갱신합니다. 실패하면 마지막 목록을 유지합니다.</summary>
    private async Task RefreshOttList()
    {
        var fetched = await Task.Run(() => DatabaseHelper.GetUserOtts(_userIdx));
        if (fetched != null) _userOtts = fetched;
        else await DisplayAlertAsync("목록 오류", "OTT 목록을 불러오지 못했습니다. 인터넷 연결을 확인해주세요.", "확인");

        _ottList.Clear();
        foreach (var item in Dialogs.AllOtts(_userId, _userOtts))
            _ottList.Add(OttRow(item[1], item[2], item[0]));
        _ottList.Add(Ui.Row("＋  OTT 추가", Pal.TextGray, async () => await AddOttAsync(), 42));

        if (fetched != null) await SyncCells();
    }

    /// <summary>사이드바의 OTT 한 줄 (아이콘 + 이름 + 화면 추가 버튼, 우클릭 메뉴)</summary>
    private View OttRow(string name, string url, string dbIdx)
    {
        bool custom = dbIdx != "";
        var label = Ui.Text(name, 13, Pal.Text);
        label.VerticalOptions = LayoutOptions.Center;
        label.LineBreakMode = LineBreakMode.TailTruncation;

        var btnAdd = Ui.Flat("＋", Pal.TextGray, 13, 30);
        ToolTipProperties.SetText(btnAdd, "화면에 추가 (멀티뷰)");
        btnAdd.Clicked += (s, e) => OpenOtt(name, url, multi: true);

        var row = new Grid
        {
            HeightRequest = 44, Padding = new Thickness(16, 0, 4, 0), ColumnSpacing = 10,
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }
        };
        row.Add(Ui.Favicon(url));
        row.Add(label, 1, 0);
        row.Add(btnAdd, 2, 0);
        Ui.Clickable(row, () => OpenOtt(name, url, multi: Native.IsCtrlDown()), Pal.Panel);
        ToolTipProperties.SetText(row, name);

        var menu = new MenuFlyout();
        void Item(string text, Action run, bool enabled = true)
        {
            var mi = new MenuFlyoutItem { Text = text, IsEnabled = enabled };
            mi.Clicked += (s, e) => run();
            menu.Add(mi);
        }
        Item("화면에 추가", () => OpenOtt(name, url, multi: true));
        Item("수정", async () => await EditOttAsync(name, url, dbIdx), custom);   // 기본 OTT는 수정/삭제 불가
        Item("삭제", async () => await DeleteOttAsync(name, dbIdx), custom);
        menu.Add(new MenuFlyoutSeparator());
        Item("음소거 토글", () => ToggleMute(name));
        menu.Add(new MenuFlyoutSeparator());
        Item("위로 이동", async () => await MoveOttAsync(name, -1));
        Item("아래로 이동", async () => await MoveOttAsync(name, 1));
        FlyoutBase.SetContextFlyout(row, menu);
        return row;
    }

    private async Task AddOttAsync()
    {
        if (await Dialogs.PromptOttAsync(this, "OTT 추가", "", "https://", _userOtts) is not { } o) return;
        if (await Task.Run(() => DatabaseHelper.AddUserOtt(_userIdx, o.Name, o.Url))) await RefreshOttList();
        else await Dialogs.ShowSaveError(this);
    }

    private async Task EditOttAsync(string name, string url, string dbIdx)
    {
        if (await Dialogs.PromptOttAsync(this, "수정", name, url, _userOtts) is not { } o) return;
        if (!await Task.Run(() => DatabaseHelper.UpdateUserOtt(dbIdx, o.Name, o.Url)))
        {
            await Dialogs.ShowSaveError(this);
            return;
        }

        // 이름이 바뀌면 열려 있던 화면 자리를 새 이름으로 넘김
        int slot = Array.IndexOf(_slots, name);
        if (slot >= 0) _slots[slot] = o.Name;
        if (_lastFocused == name) _lastFocused = o.Name;

        await RefreshOttList();
        if (slot >= 0)
        {
            EnsureCell(o.Name, o.Url);
            Reorganize();
        }
    }

    private async Task DeleteOttAsync(string name, string dbIdx)
    {
        if (!await DisplayAlertAsync("삭제", $"'{name}'을(를) 삭제하시겠습니까?", "삭제", "취소")) return;
        if (await Task.Run(() => DatabaseHelper.DeleteUserOtt(dbIdx))) await RefreshOttList();
        else await Dialogs.ShowSaveError(this);
    }

    /// <summary>사이드바 순서를 바꿔 PC에 저장합니다.</summary>
    private async Task MoveOttAsync(string name, int delta)
    {
        var names = Dialogs.AllOtts(_userId, _userOtts).Select(o => o[1]);
        AppSettings.Current.OttOrder[_userId] = OttOrdering.Move(names, name, delta);
        AppSettings.Current.Save();
        await RefreshOttList();
    }

    private string GetUrlByName(string name) =>
        Dialogs.AllOtts(_userId, _userOtts).FirstOrDefault(o => o[1] == name)?[2] ?? "";

    /// <summary>열린 칸을 최신 목록에 맞춥니다. URL이 바뀌면 다시 열고, 삭제된 OTT는 닫습니다.</summary>
    private Task SyncCells()
    {
        foreach (var name in _cells.Keys.ToList())
        {
            string url = GetUrlByName(name);
            if (!_loadedUrls.TryGetValue(name, out var loaded) || url == loaded) continue;

            DropCell(name);
            int slot = Array.IndexOf(_slots, name);
            if (slot < 0) continue;
            if (url == "") _slots[slot] = null;
            else EnsureCell(name, url);
        }
        if (!_slots.Contains(_lastFocused)) _lastFocused = "";
        UpdateNav();
        Reorganize();
        return Task.CompletedTask;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 멀티뷰
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <param name="multi">true = 화면 추가, false = 이 화면 하나만</param>
    private async void OpenOtt(string name, string url, bool multi)
    {
        if (multi)
        {
            if (!_slots.Contains(name))
            {
                int free = Array.IndexOf(_slots, null);
                if (free < 0)
                {
                    await DisplayAlertAsync("멀티뷰", "화면은 최대 4개까지 열 수 있습니다.", "확인");
                    return;
                }
                _slots[free] = name;
            }
        }
        else
        {
            Array.Clear(_slots);
            _slots[0] = name;
        }
        _lastFocused = name;
        EnsureCell(name, url);
        UpdateNav();
        Reorganize();
    }

    private void EnsureCell(string name, string url)
    {
        if (_cells.ContainsKey(name)) return;
        var cell = new OttCell(name, url) { IsVisible = false };
        cell.CloseClicked      += c => CloseCell(c.OttName);
        cell.MuteClicked       += c => ToggleMute(c.OttName);
        cell.MainClicked       += c => ToggleMainView(c.OttName);
        cell.PipClicked        += c => SendToPip(c.OttName);
        cell.HeaderPressed     += c => { _lastFocused = c.OttName; UpdateNav(); };
        cell.FocusGained       += c => { _lastFocused = c.OttName; UpdateNav(); };
        cell.Navigated         += c => { if (c.OttName == _lastFocused) UpdateNav(); };
        cell.SwapRequested     += SwapCells;
        cell.FullScreenChanged += OnFullScreenChanged;
        _cells[name]      = cell;
        _loadedUrls[name] = url;
        _content.Children.Insert(0, cell);  // 경계선보다 아래에
    }

    /// <summary>칸을 화면에서 내리고 웹 화면 자원을 해제합니다.</summary>
    private void DropCell(string name)
    {
        if (!_cells.Remove(name, out var cell)) return;
        if (cell == _fullScreen) OnFullScreenChanged(cell, false);
        if (name == _mainOtt) _mainOtt = "";
        _loadedUrls.Remove(name);
        _userMuted.Remove(name);
        _content.Children.Remove(cell);
        cell.View.Handler?.DisconnectHandler();
    }

    private void CloseCell(string name)
    {
        int slot = Array.IndexOf(_slots, name);
        if (slot >= 0) _slots[slot] = null;
        DropCell(name);
        if (_lastFocused == name) _lastFocused = _slots.FirstOrDefault(n => n != null) ?? "";
        UpdateNav();
        Reorganize();
    }

    private void ClearLayout()
    {
        Array.Clear(_slots);
        _lastFocused = "";
        foreach (var name in _cells.Keys.ToList()) DropCell(name);
        UpdateNav();
        Reorganize();
    }

    private void ToggleMainView(string name)
    {
        _mainOtt = _mainOtt == name ? "" : name;
        _splitX  = _mainOtt == "" ? 0.5 : 0.72;
        Reorganize();
    }

    private void SwapCells(OttCell a, OttCell b)
    {
        int ia = Array.IndexOf(_slots, a.OttName), ib = Array.IndexOf(_slots, b.OttName);
        if (ia < 0 || ib < 0) return;
        (_slots[ia], _slots[ib]) = (_slots[ib], _slots[ia]);
        Reorganize();
    }

    private void SendToPip(string name)
    {
        if (!_cells.TryGetValue(name, out var cell)) return;
        PipPage.Open(name, cell.CurrentUrl ?? _loadedUrls[name], _userId);
        CloseCell(name);
    }

    /// <summary>칸 위치를 다시 계산합니다 (계산은 LayoutCalculator, Windows 앱과 같음).</summary>
    private void Reorganize()
    {
        double w = _content.Width, h = _content.Height;
        if (w <= 0 || h <= 0) return;

        var names = _fullScreen != null
            ? new List<string> { _fullScreen.OttName }
            : _slots.Where(n => n != null && _cells.ContainsKey(n)).Select(n => n!).ToList();
        int mainIndex = _fullScreen == null ? names.IndexOf(_mainOtt) : -1;
        var rects = LayoutCalculator.Compute(names.Count, (int)w, (int)h, mainIndex, _splitX, _splitY);

        foreach (var (name, cell) in _cells)
        {
            cell.IsVisible = names.Contains(name);
            cell.SetMain(name == _mainOtt);
        }
        for (int i = 0; i < names.Count; i++)
        {
            var r = rects[i];
            // 칸 사이에 경계선 자리를 남김
            double left = r.Left > 0 ? r.Left + GAP : r.Left, top = r.Top > 0 ? r.Top + GAP : r.Top;
            double right = r.Right < (int)w ? r.Right - GAP : w, bottom = r.Bottom < (int)h ? r.Bottom - GAP : h;
            var cell = _cells[names[i]];
            cell.HeaderVisible = _fullScreen == null;
            AbsoluteLayout.SetLayoutBounds(cell, new Rect(left, top, right - left, bottom - top));
        }

        // 경계선: 세로는 2칸 이상, 가로는 위아래로 나뉜 배치에서만 표시
        double sx = (int)(w * _splitX), sy = (int)(h * _splitY);
        bool grid = names.Count >= 3 && mainIndex < 0 && _fullScreen == null;
        _splitV.IsVisible = names.Count >= 2 && _fullScreen == null;
        _splitH.IsVisible = grid;
        AbsoluteLayout.SetLayoutBounds(_splitV, new Rect(sx - GAP, 0, GAP * 2, names.Count == 3 && grid ? sy : h));
        AbsoluteLayout.SetLayoutBounds(_splitH, new Rect(0, sy - GAP, w, GAP * 2));

        ApplyMute();
    }

    private void AddSplitter(BoxView splitter, bool vertical)
    {
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (s, e) =>
        {
            if (e.StatusType == GestureStatus.Started) _panStart = vertical ? _splitX : _splitY;
            if (e.StatusType != GestureStatus.Running) return;
            double ratio = _panStart + (vertical ? e.TotalX / _content.Width : e.TotalY / _content.Height);
            if (vertical) _splitX = LayoutCalculator.ClampSplit(ratio);
            else          _splitY = LayoutCalculator.ClampSplit(ratio);
            Reorganize();
        };
        splitter.GestureRecognizers.Add(pan);
        _content.Children.Add(splitter);
    }

    /// <summary>보이지 않는 칸, PiP 모드 중인 메인 화면, 사용자가 음소거한 칸의 소리를 끕니다.</summary>
    private void ApplyMute()
    {
        foreach (var (name, cell) in _cells)
            cell.SetMuted(_isPipMode || !cell.IsVisible || _userMuted.Contains(name), _userMuted.Contains(name));
    }

    private void ToggleMute(string name)
    {
        if (!_userMuted.Add(name)) _userMuted.Remove(name);
        ApplyMute();
    }

    /// <summary>사이트 전체화면(Windows): 해당 칸만 창 전체에 표시</summary>
    private void OnFullScreenChanged(OttCell cell, bool on)
    {
        if (on == (_fullScreen != null)) return;
        _fullScreen = on ? cell : null;
        _topRow.Height     = on ? 0 : 46;
        _topBar.IsVisible  = !on;
        _sidebar.IsVisible = !on && _menuExpanded;
        _sideCol.Width     = !on && _menuExpanded ? 230 : 0;
        Native.SetFullScreen(Window, on);
        Reorganize();
    }

    private void ToggleSidebar()
    {
        _menuExpanded      = !_menuExpanded;
        _sidebar.IsVisible = _menuExpanded;
        _sideCol.Width     = _menuExpanded ? 230 : 0;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 내비게이션 / 주소 표시줄 / 단축키
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void UpdateNav()
    {
        var tc = _lastFocused != "" ? Pal.Text : Pal.TextGray;
        _btnBack.Fg(tc);
        _btnForward.Fg(tc);
        _btnRefresh.Fg(tc);
        if (!_txtAddress.IsFocused) _txtAddress.Text = Focused()?.CurrentUrl ?? "";
    }

    private void NavigateAddress()
    {
        if (Focused() is not { } cell) return;
        string text = _txtAddress.Text?.Trim() ?? "";
        if (!text.Contains("://")) text = "https://" + text;
        if (Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            cell.View.Source = uri.ToString();
            cell.View.Focus();
        }
    }

    private void FocusScreen(int index)
    {
        if (_slots.Where(n => n != null).ElementAtOrDefault(index) is not { } name) return;
        _lastFocused = name;
        UpdateNav();
        _cells.GetValueOrDefault(name)?.View.Focus();
    }

    /// <summary>단축키 목록 (도움말에도 같은 목록을 표시)</summary>
    public List<Shortcut> Shortcuts() => new()
    {
        new("화면 1~4 선택", "1", Mods.Ctrl, "1", Mods.Cmd, () => FocusScreen(0)),
        new("", "2", Mods.Ctrl, "2", Mods.Cmd, () => FocusScreen(1)),
        new("", "3", Mods.Ctrl, "3", Mods.Cmd, () => FocusScreen(2)),
        new("", "4", Mods.Ctrl, "4", Mods.Cmd, () => FocusScreen(3)),
        new("음소거", "M", Mods.Ctrl, "M", Mods.Cmd | Mods.Shift, () => { if (_lastFocused != "") ToggleMute(_lastFocused); }),
        new("칸 닫기", "W", Mods.Ctrl, "W", Mods.Cmd | Mods.Shift, () => { if (_lastFocused != "") CloseCell(_lastFocused); }),
        new("주소 표시줄", "L", Mods.Ctrl, "L", Mods.Cmd, () => _txtAddress.Focus()),
        new("사이드바 열기/닫기", "B", Mods.Ctrl, "B", Mods.Cmd | Mods.Shift, ToggleSidebar),
        new("PiP 모드", "P", Mods.Ctrl, "P", Mods.Cmd | Mods.Shift, OpenPipLauncher),
        new("새로고침", "F5", Mods.None, "R", Mods.Cmd, () => Focused()?.View.Reload()),
        new("뒤로", "Left", Mods.Alt, "[", Mods.Cmd, () => { if (Focused() is { } c && c.View.CanGoBack) c.View.GoBack(); }),
        new("앞으로", "Right", Mods.Alt, "]", Mods.Cmd, () => { if (Focused() is { } c && c.View.CanGoForward) c.View.GoForward(); })
    };

    private void BuildMacMenu()
    {
        var menu = new MenuBarItem { Text = "화면" };
        int screen = 1;
        foreach (var s in Shortcuts())
        {
            var mods = KeyboardAcceleratorModifiers.None;
            if (s.MacMods.HasFlag(Mods.Cmd))   mods |= KeyboardAcceleratorModifiers.Cmd;
            if (s.MacMods.HasFlag(Mods.Shift)) mods |= KeyboardAcceleratorModifiers.Shift;
            if (s.MacMods.HasFlag(Mods.Alt))   mods |= KeyboardAcceleratorModifiers.Alt;
            var item = new MenuFlyoutItem { Text = s.Label == "" || s.Label.StartsWith("화면 1") ? $"화면 {screen++} 선택" : s.Label };
            item.KeyboardAccelerators.Add(new KeyboardAccelerator { Modifiers = mods, Key = s.MacKey });
            item.Clicked += (x, e) => s.Run();
            menu.Add(item);
        }
        MenuBarItems.Add(menu);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 레이아웃 저장 / 복구
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>'기본'은 Windows 앱과 같은 파일(layout_preset.txt)을 사용합니다.</summary>
    private string PresetPath(string name) =>
        DatabaseHelper.DataPath(_userId, name == "기본" ? "layout_preset.txt" : $"layout_{name}.txt");

    private async Task SavePresetAsync()
    {
        if (_slots.All(s => s == null))
        {
            await DisplayAlertAsync("저장", "저장할 화면이 없습니다. OTT를 먼저 열어주세요.", "확인");
            return;
        }
        string? name = (await DisplayPromptAsync("저장", "레이아웃 이름 ('기본'은 로그인 때 자동 복구):", "저장", "취소", initialValue: "기본"))?.Trim();
        if (string.IsNullOrEmpty(name)) return;

        try
        {
            string path = PresetPath(name), slots = PresetFormat.Serialize(_slots);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, slots);
            bool uploaded = await Task.Run(() => DatabaseHelper.SaveLayout(_userIdx, name, slots));
            await DisplayAlertAsync("저장", uploaded ? "레이아웃이 저장되었습니다." : "이 기기에만 저장되었습니다. (서버 저장 실패)", "확인");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await DisplayAlertAsync("저장 실패", $"레이아웃을 저장하지 못했습니다.\n{ex.Message}", "확인");
        }
    }

    private async Task ShowPresetMenuAsync()
    {
        string dir = DatabaseHelper.DataPath(_userId);
        var files = Directory.Exists(dir) ? Directory.GetFiles(dir, "layout_*.txt") : Array.Empty<string>();
        if (files.Length == 0)
        {
            await DisplayAlertAsync("복구", "저장된 레이아웃이 없습니다.", "확인");
            return;
        }
        var names = files.Select(f => Path.GetFileNameWithoutExtension(f)["layout_".Length..])
                         .Select(n => n == "preset" ? "기본" : n).ToArray();
        string? pick = await DisplayActionSheetAsync("복구할 레이아웃", "취소", null, names);
        if (pick != null && pick != "취소") await LoadPresetAsync(PresetPath(pick), silent: false);
    }

    private async Task LoadPresetAsync(string path, bool silent)
    {
        if (!File.Exists(path)) return;
        var saved = PresetFormat.Parse(File.ReadAllText(path));

        Array.Clear(_slots);  // 기존 화면과 섞이지 않도록 비우고 시작
        _lastFocused = "";
        bool missing = false;
        for (int i = 0; i < saved.Length; i++)
        {
            if (saved[i] is not { } ott) continue;
            string url = GetUrlByName(ott);
            if (url == "") { missing = true; continue; }
            _slots[i] = ott;
            if (_lastFocused == "") _lastFocused = ott;
            EnsureCell(ott, url);
        }
        if (missing && !silent)
            await DisplayAlertAsync("복구", "삭제되었거나 이름이 바뀐 OTT는 복구하지 못했습니다.", "확인");
        UpdateNav();
        Reorganize();
    }

    /// <summary>서버의 레이아웃을 이 기기로 내려받습니다. 실패하면 기기의 파일을 그대로 씁니다.</summary>
    private async Task SyncLayoutsFromServer()
    {
        var rows = await Task.Run(() => DatabaseHelper.GetLayouts(_userIdx));
        if (rows == null) return;
        try
        {
            foreach (var (name, slots) in rows)
            {
                string path = PresetPath(name);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, slots);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 계정
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async Task ChangeNicknameAsync()
    {
        var result = await FormPage.ShowAsync(Navigation, "내 정보 수정", "새 닉네임을 입력하세요",
            new[] { new FormField("새 닉네임", "닉네임", Initial: DatabaseHelper.CurrentNickname) },
            "수정 완료",
            async v =>
            {
                if (v[0] == "") return "닉네임을 입력해주세요.";
                return await Task.Run(() => DatabaseHelper.UpdateNickname(_userIdx, v[0]))
                    ? null : "서버 오류가 발생했습니다. 다시 시도해주세요.";
            });
        if (result != null) _lblWelcome.Text = Welcome(DatabaseHelper.CurrentNickname);
    }

    private async Task ChangePasswordAsync()
    {
        var result = await FormPage.ShowAsync(Navigation, "비밀번호 변경", "새 비밀번호를 입력하세요",
            new[]
            {
                new FormField("새 비밀번호", "8자 이상 + 특수문자", Password: true),
                new FormField("비밀번호 확인", "비밀번호를 다시 입력하세요", Password: true)
            },
            "변경 완료",
            async v =>
            {
                if (v[0] == "") return "새 비밀번호를 입력해주세요.";
                if (v[0] != v[1]) return "비밀번호가 일치하지 않습니다.";
                if (!LoginPage.PasswordPattern.IsMatch(v[0])) return "영문·숫자·특수문자 포함 8자 이상이어야 합니다.";
                return await Task.Run(() => DatabaseHelper.UpdatePassword(_userIdx, v[0]))
                    ? null : "서버 오류가 발생했습니다. 다시 시도해주세요.";
            },
            hint: LoginPage.PasswordRule);
        if (result != null) await DisplayAlertAsync("변경 완료", "비밀번호가 변경되었습니다.", "확인");
    }

    /// <summary>현재 비밀번호 확인 창. 취소하면 null</summary>
    private async Task<string?> AskPasswordAsync(string title)
    {
        string? pw = null;
        var result = await FormPage.ShowAsync(Navigation, title, "현재 비밀번호를 입력하세요",
            new[] { new FormField("현재 비밀번호", "비밀번호", Password: true) }, "확인",
            v => Task.FromResult(v[0] == "" ? "비밀번호를 입력해주세요." : (pw = v[0]) == null ? "" : null));
        return result == null ? null : pw;
    }

    private async Task AccountMenuAsync()
    {
        string? pick = await DisplayActionSheetAsync("계정 관리", "취소", null, "복구 이메일 등록", "회원 탈퇴");
        if (pick == "복구 이메일 등록") await RegisterEmailAsync();
        else if (pick == "회원 탈퇴") await DeleteAccountAsync();
    }

    private async Task RegisterEmailAsync()
    {
        string? email = (await DisplayPromptAsync("복구 이메일 등록", "복구 이메일:", "다음", "취소", keyboard: Keyboard.Email))?.Trim();
        if (string.IsNullOrEmpty(email)) return;
        if (!email.Contains('@'))
        {
            await DisplayAlertAsync("복구 이메일 등록", "이메일 주소를 확인해주세요.", "확인");
            return;
        }
        if (await AskPasswordAsync("복구 이메일 등록") is not { } pw) return;

        bool ok = await Task.Run(() => DatabaseHelper.SetEmail(_userId, pw, email));
        await DisplayAlertAsync("복구 이메일 등록", ok ? "복구 이메일이 등록되었습니다." : "비밀번호가 틀렸거나 서버 오류가 발생했습니다.", "확인");
    }

    private async Task DeleteAccountAsync()
    {
        if (!await DisplayAlertAsync("회원 탈퇴",
                "계정과 서버에 저장된 OTT 목록, 레이아웃이 모두 삭제됩니다.\n되돌릴 수 없습니다. 탈퇴하시겠습니까?", "탈퇴", "취소")) return;
        if (await AskPasswordAsync("회원 탈퇴") is not { } pw) return;

        if (await Task.Run(() => DatabaseHelper.DeleteAccount(_userId, pw))) Logout();
        else await DisplayAlertAsync("회원 탈퇴", "비밀번호가 틀렸거나 서버 오류가 발생했습니다.", "확인");
    }

    /// <summary>나가기: 자동 로그인을 지우고 로그인 화면으로 돌아갑니다.</summary>
    private void Logout()
    {
        Dialogs.ClearAutoLogin();
        _alive = false;
        Native.SetShortcuts(Window, Array.Empty<Shortcut>());
        foreach (var w in Application.Current!.Windows.ToList())
            if (w != Window) Application.Current.CloseWindow(w);
        ClearLayout();
        Window.Page = new LoginPage(allowAutoLogin: false);
        Window.Width  = 820;
        Window.Height = 560;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PiP / 테마 / 시청 기록 / 창 상태
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void OpenPipLauncher()
    {
        if (_pipLauncher != null) return;
        var window = new Window(new PipLauncherPage(_userIdx, _userId, DatabaseHelper.CurrentNickname))
        {
            Title = "PiP 모드", Width = 240, Height = 520
        };
        window.Destroying += async (s, e) =>
        {
            _pipLauncher = null;
            _isPipMode = false;
            Native.Show(Window);
            ApplyMute();
            await RefreshOttList();  // 런처에서 바뀐 OTT 반영
        };
        _pipLauncher = window;
        _isPipMode = true;
        ApplyMute();                 // 메인 화면 소리 끄기
        Application.Current!.OpenWindow(window);
        Native.Hide(Window);         // Windows: 메인 창 숨김 (맥은 그대로 둠)
    }

    private void ToggleTheme()
    {
        AppSettings.Current.DarkMode = !AppSettings.Current.DarkMode;
        AppSettings.Current.Save();
        Application.Current!.UserAppTheme = AppSettings.Current.DarkMode ? AppTheme.Dark : AppTheme.Light;
        _lblTheme.Text = AppSettings.Current.DarkMode ? "라이트 모드" : "다크 모드";
    }

    private void RecordWatchTime()
    {
        if (_isPipMode || Native.IsMinimized(Window)) return;
        foreach (var (name, cell) in _cells)
            if (cell.IsVisible) WatchStats.Add(_userId, name, STATS_INTERVAL);
    }

    private void RestoreWindowState()
    {
        var st = AppSettings.Current;
        if (st.Width >= 820 && st.Height >= 520)
        {
            (Window.X, Window.Y, Window.Width, Window.Height) = (st.X, st.Y, st.Width, st.Height);
            if (st.Maximized) Native.Maximize(Window);
        }
        else
        {
            var d = DeviceDisplay.Current.MainDisplayInfo;
            double sw = d.Width / d.Density, sh = d.Height / d.Density;
            (Window.Width, Window.Height) = (Math.Min(1300, sw), Math.Min(860, sh - 40));
            (Window.X, Window.Y) = ((sw - Window.Width) / 2, (sh - Window.Height) / 2);
        }
    }

    /// <summary>종료할 때 창 위치를 저장합니다. 최대화 중에는 이전 크기를 유지합니다.</summary>
    public void SaveWindowState()
    {
        if (_fullScreen != null || Window == null) return;
        var st = AppSettings.Current;
        st.Maximized = Native.IsMaximized(Window);
        if (!st.Maximized && !Native.IsMinimized(Window))
            (st.X, st.Y, st.Width, st.Height) = ((int)Window.X, (int)Window.Y, (int)Window.Width, (int)Window.Height);
        st.Save();
    }
}
