// ============================================================
// LoginPage.cs — 로그인 화면 (왼쪽 브랜딩 + 오른쪽 로그인 카드)
// ============================================================
using System.Text.RegularExpressions;
using PersonalPro;

namespace OttMaui;

public class LoginPage : ContentPage
{
    // 비밀번호 규칙: 영문 + 숫자 + 특수문자(@$!%*#?&) 포함, 8자 이상 (Windows 앱과 같음)
    public static readonly Regex PasswordPattern = new(
        @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$", RegexOptions.Compiled);
    public const string PasswordRule = "영문·숫자·특수문자(@$!%*#?&) 포함, 최소 8자";

    private readonly Entry _txtId, _txtPw;
    private readonly CheckBox _chkAuto;
    private readonly Button _btnLogin;
    private readonly bool _allowAutoLogin;
    private bool _started;

    /// <param name="allowAutoLogin">false = 로그아웃 직후라 자동 로그인하지 않음</param>
    public LoginPage(bool allowAutoLogin = true)
    {
        _allowAutoLogin = allowAutoLogin;
        this.Bg(Pal.Bg);

        // ── 왼쪽: 브랜딩 패널 ────────────────────────────────────
        var features = new VerticalStackLayout { Spacing = 14, Margin = new Thickness(0, 60, 0, 0) };
        foreach (var f in new[] { "멀티 화면 지원 (최대 4개)", "OTT 커스텀 추가 · 관리", "레이아웃 저장 & 복구" })
            features.Add(new HorizontalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new BoxView { WidthRequest = 5, HeightRequest = 5, Color = Pal.Red, VerticalOptions = LayoutOptions.Center },
                    new Label { Text = f, FontSize = 12, TextColor = Color.FromRgb(148, 150, 172) }
                }
            });

        var leftContent = new Grid
        {
            Padding = new Thickness(36, 80, 20, 16),
            RowDefinitions = { new(GridLength.Star), new(GridLength.Auto) }
        };
        leftContent.Add(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Logo(26, Colors.White),
                new Label { Text = "좋아하는 OTT를\n한 화면에서 즐기세요", FontSize = 14, TextColor = Color.FromRgb(156, 158, 178) },
                features
            }
        });
        leftContent.Add(new Label { Text = "© 2025  Personal Pro", FontSize = 10, TextColor = Color.FromRgb(68, 70, 90) }, 0, 1);

        var left = new Grid
        {
            BackgroundColor = Pal.Brand,
            ColumnDefinitions = { new(4), new(GridLength.Star) },
            Children = { new BoxView { Color = Pal.Red } }
        };
        left.Add(leftContent, 1, 0);

        // ── 오른쪽: 로그인 카드 ──────────────────────────────────
        var (idBox, txtId) = Ui.Input("아이디를 입력하세요");
        var (pwBox, txtPw) = Ui.Input("비밀번호를 입력하세요", password: true);
        _txtId = txtId;
        _txtPw = txtPw;
        _txtPw.Completed += async (s, e) => await LoginAsync();

        _chkAuto = new CheckBox { Color = Pal.Red, VerticalOptions = LayoutOptions.Center };
        var lblAuto = Ui.Text("자동 로그인", 11, Pal.TextMid);
        lblAuto.VerticalOptions = LayoutOptions.Center;
        lblAuto.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => _chkAuto.IsChecked = !_chkAuto.IsChecked) });

        var lblForgot = Ui.Text("비밀번호 찾기", 11, Pal.TextMid);
        lblForgot.VerticalOptions = LayoutOptions.Center;
        lblForgot.TextDecorations = TextDecorations.Underline;
        lblForgot.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await ResetPasswordAsync()) });

        var optionRow = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        optionRow.Add(new HorizontalStackLayout { Children = { _chkAuto, lblAuto } });
        optionRow.Add(lblForgot, 2, 0);

        _btnLogin = Ui.Primary("로그인");
        _btnLogin.Clicked += async (s, e) => await LoginAsync();

        var btnRegister = Ui.Flat("계정이 없으신가요?  회원가입", Pal.TextGray, 12);
        btnRegister.Clicked += async (s, e) => await RegisterAsync();

        var card = new Border
        {
            WidthRequest = 360, Padding = new Thickness(28), StrokeThickness = 1,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    Ui.Text("로그인", 26, Pal.Text, bold: true),
                    Ui.Text("계정에 로그인하여 멀티뷰를 시작하세요", 12, Pal.TextMid),
                    new BoxView { HeightRequest = 1, Margin = new Thickness(0, 10) }.Bg(Pal.Border),
                    Ui.Text("아이디", 11, Pal.TextMid, bold: true), idBox,
                    Ui.Text("비밀번호", 11, Pal.TextMid, bold: true), pwBox,
                    optionRow, _btnLogin, btnRegister
                }
            }
        }.Bg(Pal.Panel);
        card.SetAppThemeColor(Border.StrokeProperty, Pal.Border.Light, Pal.Border.Dark);

        var root = new Grid { ColumnDefinitions = { new(310), new(GridLength.Star) } };
        root.Add(left);
        root.Add(new ScrollView { Content = card, VerticalOptions = LayoutOptions.Center }, 1, 0);
        Content = root;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;

        // Windows: 화면 표시에 필요한 WebView2 런타임 확인
        if (Native.IsWebView2Missing())
        {
            if (await DisplayAlertAsync("WebView2 필요",
                    "화면을 표시하려면 Microsoft Edge WebView2 런타임이 필요합니다.\n설치 페이지를 여시겠습니까?", "열기", "닫기"))
                await Launcher.OpenAsync("https://developer.microsoft.com/microsoft-edge/webview2/");
            Application.Current?.Quit();
            return;
        }

        if (_allowAutoLogin && await Dialogs.LoadAutoLoginAsync() is { } saved)
        {
            _txtId.Text = saved.Id;
            _txtPw.Text = saved.Pw;
            _chkAuto.IsChecked = true;
            await LoginAsync();
        }
    }

    private async Task LoginAsync()
    {
        string id = _txtId.Text?.Trim() ?? "", pw = _txtPw.Text ?? "";
        if (id == "" || pw == "" || !_btnLogin.IsEnabled) return;

        _btnLogin.IsEnabled = false;
        _btnLogin.Text = "연결 중...";
        int idx = await Task.Run(() => DatabaseHelper.LoginCheckReturnIdx(id, pw));

        if (idx >= 0)
        {
            if (_chkAuto.IsChecked) await Dialogs.SaveAutoLoginAsync(id, pw);
            else Dialogs.ClearAutoLogin();

            Native.SetWebDataFolder(DatabaseHelper.DataPath(id, "web"));
            Window.Page = new MainPage(idx, id, DatabaseHelper.CurrentNickname);
            return;
        }

        if (idx == -1) Dialogs.ClearAutoLogin();  // 바뀐 비밀번호로 재시도 방지
        await DisplayAlertAsync("로그인 실패",
            idx == -1 ? "아이디 또는 비밀번호가 올바르지 않습니다." : "서버에 연결하지 못했습니다. 인터넷 연결을 확인해주세요.", "확인");
        _btnLogin.IsEnabled = true;
        _btnLogin.Text = "로그인";
    }

    private async Task RegisterAsync()
    {
        var result = await FormPage.ShowAsync(Navigation, "회원가입", "새 계정을 만듭니다",
            new[]
            {
                new FormField("아이디", "아이디를 입력하세요"),
                new FormField("닉네임", "닉네임을 입력하세요"),
                new FormField("비밀번호", "8자 이상 + 특수문자", Password: true),
                new FormField("비밀번호 확인", "비밀번호를 다시 입력하세요", Password: true)
            },
            "가입 완료",
            async v =>
            {
                if (v[0] == "" || v[1] == "") return "아이디와 닉네임을 입력해주세요.";
                if (v[2] == "") return "비밀번호를 입력해주세요.";
                if (v[2] != v[3]) return "비밀번호가 일치하지 않습니다.";
                if (!PasswordPattern.IsMatch(v[2])) return "영문·숫자·특수문자 포함 8자 이상이어야 합니다.";
                return await Task.Run(() => DatabaseHelper.RegisterUser(v[0], v[2], v[1]))
                    ? null : "이미 사용 중인 아이디이거나 서버 오류가 발생했습니다.";
            },
            hint: PasswordRule);

        if (result != null)
        {
            _txtId.Text = result[0];
            await DisplayAlertAsync("가입 성공", "가입이 완료되었습니다!\n로그인해 주세요.", "확인");
        }
    }

    private async Task ResetPasswordAsync()
    {
        string? id = (await DisplayPromptAsync("비밀번호 찾기", "아이디:", "다음", "취소"))?.Trim();
        if (string.IsNullOrEmpty(id)) return;
        string? email = (await DisplayPromptAsync("비밀번호 찾기", "등록한 복구 이메일:", "확인", "취소", keyboard: Keyboard.Email))?.Trim();
        if (string.IsNullOrEmpty(email)) return;

        bool sent = await Task.Run(() => DatabaseHelper.ResetPassword(id, email));
        await DisplayAlertAsync("비밀번호 찾기", sent ? "임시 비밀번호를 메일로 보냈습니다."
            : "아이디와 이메일을 확인해주세요. 복구 이메일을 등록한 계정만 찾을 수 있습니다.", "확인");
    }
}
