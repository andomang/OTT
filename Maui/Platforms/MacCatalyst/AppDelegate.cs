using Foundation;

namespace OttMaui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

// PiP 창 등 여러 창을 열기 위한 장면 대리자 (Info.plist의 UISceneDelegateClassName과 연결)
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
