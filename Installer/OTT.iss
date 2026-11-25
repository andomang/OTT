; ============================================================
; OTT.iss — 설치 프로그램 (Inno Setup)
;
; 만드는 법: release.ps1 실행 (게시 → 이 스크립트로 설치 파일 생성)
; 설치 위치: C:\OTT
; 관리자 권한 없이 설치 → 자동 업데이트 때 UAC 창이 뜨지 않음
; ============================================================

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\bin\publish"
#endif

[Setup]
; AppId는 바꾸지 말 것 — 바꾸면 업데이트가 아니라 별도 프로그램으로 설치됨
AppId={{8C2A6E51-3F4B-4C7D-9A1E-0F5B2D6C7E31}
AppName=OTT Multi-View
AppVersion={#AppVersion}
AppPublisher=Personal Pro
DefaultDirName=C:\OTT
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\bin\installer
OutputBaseFilename=OTT-Setup-{#AppVersion}
SetupIconFile=..\favicon.ico
UninstallDisplayIcon={app}\OTT.exe
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; 실행 중인 앱을 닫고 파일을 교체
CloseApplications=force
RestartApplications=no

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "바탕 화면에 바로 가기 만들기"; GroupDescription: "추가 작업:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
; 예전 버전의 확장 폴더가 남지 않도록 설치 전에 지움 (확장 버전이 바뀐 경우)
Type: filesandordirs; Name: "{app}\Extensions"

[Icons]
Name: "{userprograms}\OTT Multi-View"; Filename: "{app}\OTT.exe"
Name: "{userdesktop}\OTT Multi-View"; Filename: "{app}\OTT.exe"; Tasks: desktopicon

[Run]
; 일반 설치는 마지막 화면에서 선택, 자동 업데이트(/SILENT)는 끝나면 바로 실행
Filename: "{app}\OTT.exe"; Description: "OTT Multi-View 실행"; Flags: nowait postinstall
