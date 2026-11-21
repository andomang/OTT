// ============================================================
// App.cs — 앱 시작점
//   로그인 창을 열고, 로그인 후에는 같은 창의 화면을 메인 화면으로 바꿉니다.
// ============================================================
using PersonalPro;

namespace OttMaui;

public class App : Application
{
    public App()
    {
        UserAppTheme = AppSettings.Current.DarkMode ? AppTheme.Dark : AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new LoginPage()) { Title = "OTT Multi-View", Width = 820, Height = 560 };
        // 종료할 때 창 위치 저장 (로그인 후에만)
        window.Destroying += (s, e) => (window.Page as MainPage)?.SaveWindowState();
        return window;
    }
}
