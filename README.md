# OTT Multi-View

넷플릭스·유튜브 등 여러 OTT를 **한 화면에서 동시에** 보는 Windows 데스크톱 앱입니다.

[소개 페이지](https://domang.org/ottindex) · [다운로드 (Releases)](https://github.com/andomang/OTT/releases)

## 주요 기능

- **멀티뷰 (최대 4분할):** 서로 다른 OTT를 1~4칸으로 나눠 동시에 시청하고, 드래그로 칸 위치를 바꿈. 분할 비율 조절 가능
- **PiP (Picture-in-Picture):** 원하는 화면을 항상 위에 뜨는 작은 창으로 분리
- **개별 음량·음소거:** 칸마다 따로 조절 (하나만 소리 켜고 나머지는 음소거)
- **레이아웃 저장·복원:** 열어 둔 OTT 조합과 분할 비율을 저장했다가 다음에 한 번에 복원
- **OTT 목록 관리:** 직접 추가·순서 변경, 파비콘 자동 표시
- **시청 통계:** OTT별 시청 시간 집계
- **회원 기능:** 로그인·회원가입·자동 로그인·닉네임/비밀번호 변경, 라이트/다크 모드

## OTT 계정 보안

넷플릭스·유튜브 등의 OTT 계정 정보는 **서버로 보내지 않습니다.** 각 OTT 로그인 상태는 WebView2 쿠키로 사용자 PC에만 저장되며, 앱을 지우면 함께 지워집니다. 앱 회원 비밀번호는 PC에서 SHA-256으로 해시한 뒤 전송하고, 서버는 사용자별 salt를 붙여 한 번 더 해시해 저장합니다.

## 기술 스택

| 구분 | 사용 |
|---|---|
| 데스크톱 앱 | C# / .NET 8 WinForms (`net8.0-windows`) |
| 웹 화면 | Microsoft Edge WebView2 |
| 회원 서버 | Google Apps Script + Google Sheets (`Server/Code.gs`) |
| 자동 로그인 저장 | Windows DPAPI (`ProtectedData`) |
| 모바일 (실험) | .NET MAUI (`Maui/`) |
| 테스트 | `Tests/` — 레이아웃 계산, 입력 검증, 통계 집계 등 단위 테스트 |

## 실행

필요: Windows 10 이상, [WebView2 런타임](https://developer.microsoft.com/microsoft-edge/webview2/), .NET 8 SDK

```bash
dotnet run --project Per2.csproj
dotnet test Tests/Per2.Tests.csproj
```

## 폴더

```
MainForm.cs          메인 화면 (멀티뷰, OTT 목록, 레이아웃)
OttCell.cs           화면 한 칸 (WebView2, 음량)
PipForm.cs           PiP 창
StatsForm.cs         시청 통계
AppLogic.cs          화면 분할 계산, 입력 검증, 통계 집계 (UI와 분리, 테스트 대상)
DatabaseHelper.cs    회원 서버(Apps Script) 통신, 자동 로그인
Server/Code.gs       회원·OTT 목록·레이아웃 저장 서버
Maui/                MAUI 버전
```
