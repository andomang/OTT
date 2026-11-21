// ============================================================
// FormPage.cs — 입력 카드 화면 (회원가입, 닉네임 변경, 비밀번호 변경, 비밀번호 확인)
//
// Windows 앱의 RegisterForm / UpdateUserForm / ChangePasswordForm을 하나로 합친 화면입니다.
// ============================================================
namespace OttMaui;

/// <summary>입력칸 하나</summary>
public record FormField(string Label, string Placeholder, bool Password = false, string Initial = "");

public class FormPage : ContentPage
{
    private readonly TaskCompletionSource<string[]?> _result = new();
    private readonly Entry[] _entries;
    private readonly Label _error;
    private readonly Button _ok;
    private readonly string _okText;
    private readonly Func<string[], Task<string?>> _submit;

    /// <summary>
    /// 입력 화면을 띄우고 결과를 기다립니다.
    /// submit은 오류 문구를 반환하면 화면에 표시하고, null을 반환하면 화면을 닫습니다.
    /// </summary>
    /// <returns>입력값 배열. 취소하면 null</returns>
    public static async Task<string[]?> ShowAsync(INavigation nav, string title, string subtitle,
        FormField[] fields, string okText, Func<string[], Task<string?>> submit, string? hint = null)
    {
        var page = new FormPage(title, subtitle, fields, okText, submit, hint);
        await nav.PushModalAsync(page);
        return await page._result.Task;
    }

    private FormPage(string title, string subtitle, FormField[] fields, string okText,
        Func<string[], Task<string?>> submit, string? hint)
    {
        _submit = submit;
        _okText = okText;
        this.Bg(Pal.Bg);

        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Add(Ui.Text(title, 22, Pal.Text, bold: true));
        stack.Add(Ui.Text(subtitle, 12, Pal.TextMid));
        stack.Add(new BoxView { HeightRequest = 1, Margin = new Thickness(0, 10) }.Bg(Pal.Border));

        _entries = new Entry[fields.Length];
        for (int i = 0; i < fields.Length; i++)
        {
            stack.Add(Ui.Text(fields[i].Label, 11, Pal.TextMid, bold: true));
            var (box, entry) = Ui.Input(fields[i].Placeholder, fields[i].Password);
            entry.Text = fields[i].Initial;
            entry.Completed += async (s, e) => await SubmitAsync();
            box.Margin = new Thickness(0, 0, 0, 8);
            stack.Add(box);
            _entries[i] = entry;
        }
        if (hint != null) stack.Add(Ui.Text(hint, 11, Pal.TextGray));

        _error = new Label { FontSize = 12, TextColor = Pal.Red, MinimumHeightRequest = 20 };
        stack.Add(_error);

        _ok = Ui.Primary(okText);
        _ok.Clicked += async (s, e) => await SubmitAsync();
        stack.Add(_ok);

        var cancel = Ui.Flat("취소", Pal.TextGray, 12);
        cancel.Clicked += async (s, e) => await CloseAsync(null);
        stack.Add(cancel);

        var card = new Border
        {
            Content = stack, Padding = new Thickness(28), WidthRequest = 380, StrokeThickness = 1,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
        }.Bg(Pal.Panel);
        card.SetAppThemeColor(Border.StrokeProperty, Pal.Border.Light, Pal.Border.Dark);

        Content = new ScrollView { Content = card };
    }

    private async Task SubmitAsync()
    {
        if (!_ok.IsEnabled) return;  // 처리 중 중복 실행 방지
        var values = _entries.Select(e => e.Text?.Trim() ?? "").ToArray();

        _error.Text  = "";
        _ok.IsEnabled = false;
        _ok.Text     = "처리 중...";
        string? error = await _submit(values);
        _ok.IsEnabled = true;
        _ok.Text     = _okText;

        if (error != null) _error.Text = error;
        else await CloseAsync(values);
    }

    private async Task CloseAsync(string[]? values)
    {
        if (_result.Task.IsCompleted) return;
        _result.TrySetResult(values);
        await Navigation.PopModalAsync();
    }

    // 창을 닫는 등 다른 방법으로 사라지면 취소로 처리
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _result.TrySetResult(null);
    }
}
