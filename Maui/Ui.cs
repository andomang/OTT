// ============================================================
// Ui.cs — 색상 팔레트와 자주 쓰는 화면 요소
//
// 색은 (라이트, 다크) 한 쌍으로 정의하고, 다크 모드를 바꾸면 자동으로 전환됩니다.
// 값은 Windows 앱(MainForm, Theme.cs)과 같습니다.
// ============================================================
namespace OttMaui;

/// <summary>라이트/다크 한 쌍의 색</summary>
public readonly record struct TC(Color Light, Color Dark)
{
    public Color Current => Application.Current?.RequestedTheme == AppTheme.Dark ? Dark : Light;
}

public static class Pal
{
    public static readonly Color Red      = Color.FromRgb(229, 9, 20);
    public static readonly Color RedHover = Color.FromRgb(205, 8, 18);
    public static readonly Color Brand    = Color.FromRgb(17, 17, 27);    // 로그인 왼쪽 패널
    public static readonly Color ViewBg   = Color.FromRgb(10, 10, 10);    // 멀티뷰 배경

    public static readonly TC Bg       = new(Color.FromRgb(248, 249, 252), Color.FromRgb(24, 24, 32));
    public static readonly TC Panel    = new(Colors.White,                 Color.FromRgb(32, 32, 42));
    public static readonly TC RedTint  = new(Color.FromRgb(255, 242, 242), Color.FromRgb(58, 30, 34));
    public static readonly TC Text     = new(Color.FromRgb(17, 24, 39),    Color.FromRgb(232, 234, 240));
    public static readonly TC TextMid  = new(Color.FromRgb(75, 85, 99),    Color.FromRgb(176, 180, 192));
    public static readonly TC TextGray = new(Color.FromRgb(156, 163, 175), Color.FromRgb(130, 135, 150));
    public static readonly TC Border   = new(Color.FromRgb(229, 231, 235), Color.FromRgb(58, 58, 72));
    public static readonly TC Hover    = new(Color.FromRgb(243, 244, 246), Color.FromRgb(48, 48, 60));
    public static readonly TC InputBg  = new(Color.FromRgb(249, 250, 251), Color.FromRgb(40, 40, 52));
    public static readonly TC Welcome  = new(Color.FromRgb(158, 18, 18),   Color.FromRgb(255, 150, 150));
    public static readonly TC Blue     = new(Color.FromRgb(55, 110, 200),  Color.FromRgb(110, 160, 240));
    public static readonly TC Green    = new(Color.FromRgb(46, 139, 87),   Color.FromRgb(90, 190, 130));
    public static readonly TC Danger   = new(Color.FromRgb(185, 50, 50),   Color.FromRgb(240, 110, 110));
}

public static class Ui
{
    // ── 테마 색 연결 ──────────────────────────────────────────────

    public static T Bg<T>(this T v, TC c) where T : VisualElement
    {
        v.SetAppThemeColor(VisualElement.BackgroundColorProperty, c.Light, c.Dark);
        return v;
    }

    public static Label Fg(this Label l, TC c)
    {
        l.SetAppThemeColor(Label.TextColorProperty, c.Light, c.Dark);
        return l;
    }

    public static Button Fg(this Button b, TC c)
    {
        b.SetAppThemeColor(Button.TextColorProperty, c.Light, c.Dark);
        return b;
    }

    // ── 요소 생성 ─────────────────────────────────────────────────

    public static Label Text(string text, double size, TC color, bool bold = false) =>
        new Label { Text = text, FontSize = size, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None }.Fg(color);

    public static BoxView Line() => new BoxView { HeightRequest = 1 }.Bg(Pal.Border);

    /// <summary>"O"만 빨간색인 OTT Multi-View 로고</summary>
    public static Label Logo(double size, Color rest) => new()
    {
        FontSize = size, FontAttributes = FontAttributes.Bold,
        FormattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = "O", TextColor = Pal.Red },
                new Span { Text = "TT Multi-View", TextColor = rest }
            }
        }
    };

    /// <summary>입력칸 (밝은 회색 배경 + 테두리)</summary>
    public static (Border Box, Entry Entry) Input(string placeholder, bool password = false)
    {
        var entry = new Entry { Placeholder = placeholder, IsPassword = password, FontSize = 14 };
        entry.SetAppThemeColor(Entry.TextColorProperty, Pal.Text.Light, Pal.Text.Dark);
        entry.SetAppThemeColor(Entry.PlaceholderColorProperty, Pal.TextGray.Light, Pal.TextGray.Dark);
        entry.BackgroundColor = Colors.Transparent;
        var box = new Border { Content = entry, StrokeThickness = 1, Padding = new Thickness(6, 0), HeightRequest = 40 }
            .Bg(Pal.InputBg);
        box.SetAppThemeColor(Border.StrokeProperty, Pal.Border.Light, Pal.Border.Dark);
        return (box, entry);
    }

    /// <summary>빨간 기본 버튼</summary>
    public static Button Primary(string text) => new()
    {
        Text = text, BackgroundColor = Pal.Red, TextColor = Colors.White, CornerRadius = 0,
        FontAttributes = FontAttributes.Bold, FontSize = 14, HeightRequest = 48, BorderWidth = 0
    };

    /// <summary>배경 없는 납작한 버튼 (제목 표시줄 아이콘 등)</summary>
    public static Button Flat(string text, TC fg, double size = 13, double width = -1)
    {
        var b = new Button
        {
            Text = text, FontSize = size, CornerRadius = 0, BorderWidth = 0, Padding = new Thickness(6, 0),
            BackgroundColor = Colors.Transparent, WidthRequest = width
        }.Fg(fg);
        return b;
    }

    /// <summary>
    /// 왼쪽 정렬 글자 한 줄짜리 클릭 영역 (사이드바 버튼).
    /// 마우스를 올리면 배경이 바뀝니다.
    /// </summary>
    public static Grid Row(string text, TC fg, Action onTap, double height = 40, TC? bg = null)
    {
        var label = Text(text, 12, fg);
        label.VerticalOptions = VerticalOptions.Center;
        var row = new Grid { HeightRequest = height, Padding = new Thickness(16, 0, 8, 0), Children = { label } };
        Clickable(row, onTap, bg ?? Pal.Panel);
        return row;
    }

    /// <summary>탭 동작과 마우스 올림 효과를 붙입니다.</summary>
    public static void Clickable(View view, Action onTap, TC normal)
    {
        view.Bg(normal);
        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) => onTap();
        view.GestureRecognizers.Add(tap);
        Hover(view, normal);
    }

    public static void Hover(View view, TC normal)
    {
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (s, e) => view.BackgroundColor = Pal.Hover.Current;
        pointer.PointerExited  += (s, e) => view.Bg(normal);
        view.GestureRecognizers.Add(pointer);
    }

    /// <summary>둥근 아바타 (빨간 원 + 흰색 O)</summary>
    public static Border Avatar(double size) => new()
    {
        WidthRequest = size, HeightRequest = size, BackgroundColor = Pal.Red, StrokeThickness = 0,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
        Content = new Label
        {
            Text = "O", TextColor = Colors.White, FontSize = size * 0.42, FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
        }
    };

    /// <summary>Google 파비콘 서비스의 사이트 아이콘</summary>
    public static Image Favicon(string url)
    {
        var img = new Image { WidthRequest = 16, HeightRequest = 16, VerticalOptions = LayoutOptions.Center };
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            img.Source = ImageSource.FromUri(new Uri($"https://www.google.com/s2/favicons?sz=32&domain={uri.Host}"));
        return img;
    }
}
