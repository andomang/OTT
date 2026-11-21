// ============================================================
// MainForm.cs — 앱의 메인 창 (로그인 화면 + 멀티뷰 화면)
//
// 이 창이 하는 일:
//   - 앱 시작 시 로그인 화면을 보여줌
//   - 로그인 성공 후 OTT 사이드바와 멀티뷰(최대 4개 화면) 표시
//   - 사이드바에서 OTT 클릭 → WebView2로 해당 사이트 열기
//   - Ctrl+클릭으로 최대 4개 동시 표시 (2×2 분할 또는 세로 2분할)
//   - 레이아웃 저장/복구, 닉네임·비밀번호 변경, PiP 모드 진입
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PersonalPro
{
    // partial = MainForm.Designer.cs와 합쳐지는 클래스
    // Form = 윈도우 창의 기본 클래스
    public partial class MainForm : Form
    {
        // ── Windows API ───────────────────────────────────────────────
        // 타이틀바 드래그 이동에 필요한 Windows 함수
        [DllImport("user32.dll")] public static extern bool   ReleaseCapture();
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        // ── 색상 팔레트 ───────────────────────────────────────────────
        // Color = 색상 타입. static readonly = 클래스 전체에서 공유하는 상수 색상
        static readonly Color C_BG        = Color.FromArgb(248, 249, 252); // 앱 배경 (연한 회색)
        static readonly Color C_WHITE     = Color.White;                    // 흰색
        static readonly Color C_DARK      = Color.FromArgb(17, 17, 27);    // 로그인 왼쪽 패널 배경
        static readonly Color C_RED       = Color.FromArgb(229, 9, 20);    // 브랜드 빨간색
        static readonly Color C_RED_H     = Color.FromArgb(205, 8, 18);    // 버튼 hover 빨간색
        static readonly Color C_RED_TINT  = Color.FromArgb(255, 242, 242); // 사이드바 헤더 배경
        static readonly Color C_TEXT      = Color.FromArgb(17, 24, 39);    // 진한 텍스트
        static readonly Color C_TEXT_MID  = Color.FromArgb(75, 85, 99);    // 중간 회색 텍스트
        static readonly Color C_TEXT_GRAY = Color.FromArgb(156, 163, 175); // 연한 회색 텍스트
        static readonly Color C_BORDER    = Color.FromArgb(229, 231, 235); // 테두리 색
        static readonly Color C_HOVER     = Color.FromArgb(243, 244, 246); // 마우스 hover 배경
        static readonly Color C_INPUT_BG  = Color.FromArgb(249, 250, 251); // 입력창 배경

        // ── UI 컨트롤 필드 ────────────────────────────────────────────
        // ? = null(아직 만들어지지 않음)일 수 있다는 표시

        // 타이틀바 (창 최상단 46px 영역)
        private Panel?  pnlTitleBar;    // 타이틀바 전체 패널
        private Panel?  pnlLogoTitle;   // 로그인 전 "OTT Multi-View" 로고 (Paint로 색상 분리)
        private Button? btnToggleMenu;  // ≡ 사이드바 열기/닫기 버튼
        private Button? btnHelp;        // ? 도움말 버튼
        private Button? btnNavBack;     // ← 뒤로가기 버튼
        private Button? btnNavForward;  // → 앞으로가기 버튼
        private Button? btnNavRefresh;  // ↻ 새로고침 버튼
        private TextBox? txtAddress;    // 주소 표시줄
        private Button? btnMinimize;    // ─ 최소화 버튼
        private Button? btnMaximize;    // □ 최대화/복원 버튼
        private Button? btnClose;       // ✕ 닫기 버튼

        // 사이드바 (왼쪽 230px, 로그인 후 표시)
        private Panel?  pnlSideMenu;    // 사이드바 전체 패널 (로그인 전 Dock=None, 후 Dock=Left)
        private Panel?  pnlOttButtons;  // OTT 버튼 목록이 들어가는 스크롤 가능 패널
        private Panel?  pnlUserInfo;    // 상단 사용자 정보 헤더 패널
        private Label?  lblWelcome;     // "닉네임님 환영합니다" 텍스트
        private Button? btnLogout;      // 나가기(재시작) 버튼
        private Button? btnChangeNick;  // 닉네임 변경 버튼
        private Button? btnChangePw;    // 비밀번호 변경 버튼
        private Button? btnPip;         // PiP 모드 버튼
        private Button? btnSaveLayout;  // 레이아웃 저장 버튼
        private Button? btnLoadLayout;  // 레이아웃 복구 버튼
        private Button? btnClearLayout; // 화면 초기화 버튼
        private Button? btnStats;       // 시청 통계 버튼
        private Button? btnTheme;       // 다크 모드 전환 버튼
        private Button? btnAccount;     // 계정 관리 버튼

        // 콘텐츠 영역 / 로그인 화면
        private Panel?   pnlContent;    // WebView2들이 배치되는 검은 콘텐츠 패널
        private Panel?   pnlLoginArea;  // 로그인 화면 전체 패널 (Dock=Fill)
        private Panel?   pnlLoginCard;  // 로그인 폼 카드 (pnlLoginArea에 직접 배치, 중앙 정렬)
        private TextBox? txtId;         // 아이디 입력칸
        private TextBox? txtPw;         // 비밀번호 입력칸
        private Button?  btnLogin;      // 로그인 버튼
        private Button?  btnRegister;   // 회원가입 이동 버튼
        private CheckBox? chkAutoLogin; // 자동 로그인 체크박스

        // ── 앱 상태 변수 ─────────────────────────────────────────────
        // Dictionary<string, WebView2> = OTT 이름 → WebView2 컨트롤 매핑 (이름으로 찾기)
        private readonly Dictionary<string, WebView2> dicWebViews = new();

        // OTT 이름 → 해당 WebView2를 담은 칸
        private readonly Dictionary<string, OttCell> dicCells = new();

        // 크게 보기 중인 OTT 이름 ("" = 일반 배치)
        private string mainOtt = "";

        // 칸 경계선 비율 (0.2~0.8)
        private double splitX = 0.5, splitY = 0.5;
        private Panel? pnlSplitV, pnlSplitH;

        // 시청 시간 기록 간격 (초)
        private const int STATS_INTERVAL = 30;

        // 페이지에서 새 영상이 재생될 때도 지정한 음량을 적용하는 스크립트
        // __ottVol이 null(음량 100 = 조절 안 함)이면 사이트 자체 음량을 건드리지 않음
        // — 항상 덮어쓰면 재생/광고/다음 영상마다 100%로 튀었다가 사이트가 원래 음량으로 되돌려 소리가 오르내림
        private const string VolumeScript =
            "window.__ottVol=null;document.addEventListener('play',e=>{if(window.__ottVol!=null)try{e.target.volume=window.__ottVol}catch(_){}},true);";

        // OTT 이름 → 해당 WebView2에 연 URL (OTT 수정 시 다시 열지 판단)
        private readonly Dictionary<string, string> loadedUrls = new();

        // 사용자가 우클릭 메뉴로 음소거한 OTT 이름
        private readonly HashSet<string> userMuted = new();

        // 마지막으로 서버에서 받은 사용자 OTT 목록 ([0]=DB인덱스, [1]=이름, [2]=URL)
        private List<string[]> userOtts = new();

        // PiP 모드 중이면 true — 숨겨진 메인 창 소리를 끄는 데 사용
        private bool isPipMode;

        // 사이트 전체화면 중인 WebView2 (null = 전체화면 아님)
        private WebView2? fullScreenView;

        // 전체화면 진입 전 창 상태 (해제 시 복원)
        private FormWindowState stateBeforeFullScreen;

        // string?[] = null 가능한 문자열 배열. 4칸 = 최대 4개 OTT 슬롯
        // 예: ["NETFLIX", "YOUTUBE", null, null] = 2개 화면 표시
        private string?[] layoutSlots    = new string?[4];

        // string = 마지막으로 클릭했거나 포커스된 OTT 이름 (내비게이션 버튼에 사용)
        private string     lastFocusedOtt = "";

        // int = 로그인한 사용자의 DB 번호 (-1 = 로그인 안 됨)
        private int        currentUserIdx = -1;

        // string = 로그인한 사용자의 아이디 (WebView2 데이터 폴더 경로에 사용)
        private string     currentUserId  = "";

        // bool = 사이드바가 열려있으면 true, 닫혀있으면 false
        private bool       isMenuExpanded = true;

        // 우클릭 시 나타나는 컨텍스트 메뉴 (수정/삭제/음소거)
        private ContextMenuStrip? cmsMenu;

        // OTT 파비콘(아이콘) 다운로드용 HTTP 클라이언트 (5초 타임아웃)
        private static readonly HttpClient FaviconHttp = new() { Timeout = TimeSpan.FromSeconds(5) };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 생성자
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 메인 창을 만들고 초기화합니다. new MainForm() 으로 생성 시 자동 실행됩니다.
        /// </summary>
        public MainForm()
        {
            DatabaseHelper.InitializeDatabase(); // DB 초기화 (현재 비어있음)
            InitializeComponent();  // Designer.cs UI 초기화
            InitContextMenu();      // 우클릭 메뉴 만들기
            BuildUI();              // 전체 UI 구성
            Theme.Apply(this);
            if (Theme.Dark) btnTheme!.Text = "라이트 모드";
            this.Text       = "OTT Multi-View";  // 작업표시줄 표시 이름
            this.KeyPreview = true;  // 키보드 입력을 Form이 먼저 받음
            // ESC 키 → 전체화면 해제
            this.KeyDown   += (s, e) =>
            {
                if (e.KeyCode != Keys.Escape || fullScreenView == null) return;
                _ = fullScreenView.ExecuteScriptAsync("document.exitFullscreen()");
                ExitFullScreen();
            };
            this.Shown     += (s, e) => { if (CheckWebView2Runtime()) TryAutoLogin(); };
            this.FormClosing += (s, e) => SaveWindowState();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 컨텍스트 메뉴 (우클릭 팝업 메뉴)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// OTT 버튼 우클릭 시 나타나는 팝업 메뉴를 만듭니다.
        /// 수정 / 삭제 / 음소거 토글 항목을 포함합니다.
        /// </summary>
        private void InitContextMenu()
        {
            cmsMenu = new ContextMenuStrip { BackColor = C_WHITE, ForeColor = C_TEXT, Font = new Font("Segoe UI", 9f) };
            var mEdit = new ToolStripMenuItem("수정");
            var mDel  = new ToolStripMenuItem("삭제");
            var mMute = new ToolStripMenuItem("음소거 토글");
            mEdit.Click += (s, e) => EditSelectedOtt();
            mDel.Click  += (s, e) => DeleteSelectedOtt();
            mMute.Click += (s, e) => ToggleMuteSelected();
            var mUp   = new ToolStripMenuItem("위로 이동");
            var mDown = new ToolStripMenuItem("아래로 이동");
            mUp.Click   += (s, e) => MoveSelectedOtt(-1);
            mDown.Click += (s, e) => MoveSelectedOtt(1);
            cmsMenu.Items.AddRange(new ToolStripItem[]
                { mEdit, mDel, new ToolStripSeparator(), mMute, new ToolStripSeparator(), mUp, mDown });

            // 기본 OTT는 서버에 없으므로 수정/삭제 불가 (음소거만 가능)
            cmsMenu.Opening += (s, e) =>
            {
                bool custom = cmsMenu.SourceControl?.Tag is string[] d && d[1] != "";
                mEdit.Enabled = custom;
                mDel.Enabled  = custom;
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 전체 UI 조립
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 창의 모든 시각적 요소를 만들고 조립합니다.
        /// 타이틀바 → 사이드바 → 콘텐츠 → 로그인 화면 순으로 구성합니다.
        /// </summary>
        private void BuildUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size            = new Size(820, 520);
            this.BackColor       = C_BG;
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.DoubleBuffered  = true;

            BuildTitleBar();
            BuildSidebar();

            // 웹뷰 콘텐츠 영역
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 10, 10), Visible = false };
            pnlContent.SizeChanged += (s, e) => ReorganizeWebViews();

            // 칸 경계선 — 끌어서 칸 비율 조절
            pnlSplitV = new Panel { Width = 4, BackColor = Color.FromArgb(70, 70, 86), Cursor = Cursors.VSplit, Visible = false };
            pnlSplitH = new Panel { Height = 4, BackColor = Color.FromArgb(70, 70, 86), Cursor = Cursors.HSplit, Visible = false };
            pnlSplitV.MouseMove += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                splitX = LayoutCalculator.ClampSplit((pnlSplitV.Left + e.X) / (double)pnlContent.Width);
                ReorganizeWebViews();
            };
            pnlSplitH.MouseMove += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                splitY = LayoutCalculator.ClampSplit((pnlSplitH.Top + e.Y) / (double)pnlContent.Height);
                ReorganizeWebViews();
            };
            pnlContent.Controls.Add(pnlSplitV);
            pnlContent.Controls.Add(pnlSplitH);

            // 로그인 화면 (Dock=Fill → 투명 Panel 없이 확실하게 표시됨)
            BuildLoginArea();

            // 조립 순서: 나중에 Add = 높은 z-order = 앞에 렌더링
            this.Controls.Add(pnlContent);     // z=0  (뒤)
            this.Controls.Add(pnlSideMenu!);   // z=1
            this.Controls.Add(pnlLoginArea!);  // z=2  (로그인 화면이 콘텐츠 위를 덮음)
            this.Controls.Add(pnlTitleBar!);   // z=3  (항상 최상단)

        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 타이틀 바
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 창 맨 위의 타이틀바를 만듭니다.
        /// 로고, 내비게이션 버튼(← → ↻), 창 제어 버튼(─ □ ✕)을 포함합니다.
        /// 로그인 전엔 내비게이션 버튼이 숨겨져 있다가 로그인 후 나타납니다.
        /// </summary>
        private void BuildTitleBar()
        {
            pnlTitleBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = C_WHITE };
            pnlTitleBar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Theme.C(C_BORDER)), 0, 45, pnlTitleBar.Width, 45);

            // 로그인 전 로고 — Paint로 "O"(빨간색)와 "TT Multi-View"(어두운색)를 한 줄에 렌더링
            pnlLogoTitle = new Panel { Location = new Point(12, 7), Size = new Size(210, 32), BackColor = C_WHITE };
            pnlLogoTitle.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                using var fnt = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                g.DrawString("O", fnt, new SolidBrush(C_RED), 0f, 5f);
                float ox = g.MeasureString("O", fnt).Width - 4f;
                g.DrawString("TT Multi-View", fnt, new SolidBrush(Theme.C(C_TEXT)), ox, 5f);
            };

            // 로그인 후 버튼 (처음엔 숨김)
            // x좌표: 14, 52, 90, 126, 162 — 간격 38px, 겹침 없음
            btnToggleMenu = TitleIconBtn("≡", 14, new Font("Segoe UI", 13f));
            btnToggleMenu.Visible = false;
            btnToggleMenu.Click  += (s, e) =>
            {
                isMenuExpanded = !isMenuExpanded;
                if (isMenuExpanded) { pnlSideMenu!.Dock = DockStyle.Left; pnlSideMenu!.Visible = true; }
                else                { pnlSideMenu!.Dock = DockStyle.None;  pnlSideMenu!.Visible = false; }
            };

            btnHelp = TitleIconBtn("?", 52);
            btnHelp.Visible = false;
            btnHelp.Click  += (s, e) => ShowHelpDialog();

            btnNavBack = TitleIconBtn("←", 90);
            btnNavBack.ForeColor = C_TEXT_GRAY;
            btnNavBack.Visible   = false;
            btnNavBack.Click    += (s, e) =>
            {
                if (!string.IsNullOrEmpty(lastFocusedOtt) &&
                    dicWebViews.TryGetValue(lastFocusedOtt, out var wv) &&
                    wv.CoreWebView2?.CanGoBack == true)
                    wv.CoreWebView2.GoBack();
            };

            btnNavForward = TitleIconBtn("→", 126);
            btnNavForward.ForeColor = C_TEXT_GRAY;
            btnNavForward.Visible   = false;
            btnNavForward.Click    += (s, e) =>
            {
                if (!string.IsNullOrEmpty(lastFocusedOtt) &&
                    dicWebViews.TryGetValue(lastFocusedOtt, out var wv) &&
                    wv.CoreWebView2?.CanGoForward == true)
                    wv.CoreWebView2.GoForward();
            };

            btnNavRefresh = TitleIconBtn("↻", 162);
            btnNavRefresh.ForeColor = C_TEXT_GRAY;
            btnNavRefresh.Visible   = false;
            btnNavRefresh.Click    += (s, e) =>
            {
                if (!string.IsNullOrEmpty(lastFocusedOtt) &&
                    dicWebViews.TryGetValue(lastFocusedOtt, out var wv))
                    wv.CoreWebView2?.Reload();
            };

            // 주소 표시줄 — Enter로 포커스 화면 이동 (로그인 후 너비 확정)
            txtAddress = new TextBox
            {
                Location = new Point(206, 11), Width = 300, Visible = false,
                BorderStyle = BorderStyle.FixedSingle, BackColor = C_INPUT_BG, ForeColor = C_TEXT,
                Font = new Font("Segoe UI", 9f)
            };
            txtAddress.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                NavigateAddress();
            };

            // 창 제어 버튼 (항상 표시, Dock=Right)
            btnClose    = TitleDockBtn("✕", 44);
            btnMaximize = TitleDockBtn("□", 44);
            btnMinimize = TitleDockBtn("─", 44);

            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = C_RED; btnClose.ForeColor = C_WHITE; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Theme.C(C_WHITE); btnClose.ForeColor = Theme.C(C_TEXT_MID); };
            btnClose.Click    += (s, e) => Application.Exit();
            btnMaximize.Click += (s, e) => ToggleMaximize();
            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            pnlTitleBar.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                if (e.Clicks == 2) ToggleMaximize();
                else { ReleaseCapture(); SendMessage(this.Handle, 0xA1, 0x2, 0); }
            };

            pnlTitleBar.Controls.AddRange(new Control[]
            {
                btnMinimize, btnMaximize, btnClose,         // Dock=Right (먼저 추가)
                pnlLogoTitle,                               // 로그인 전 OTT 로고
                btnToggleMenu, btnHelp,
                btnNavBack, btnNavForward, btnNavRefresh,   // 내비게이션
                txtAddress
            });
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 사이드 바
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 왼쪽 사이드바를 만듭니다.
        /// 위에서부터: 사용자 정보 헤더 → OTT 버튼 목록 → 하단 기능 버튼들 (저장/복구/초기화/닉네임변경/비밀번호변경/PiP/나가기)
        ///
        /// 주의: 로그인 전에는 Dock=None으로 설정합니다.
        /// Dock=Left인 채로 Visible=false면 WinForms 레이아웃 엔진이 여전히 230px를 확보해서
        /// 로그인 카드 위치가 왼쪽으로 밀리는 버그가 생기기 때문입니다.
        /// </summary>
        private void BuildSidebar()
        {
            // Dock=None으로 시작 — 로그인 전에 DockStyle.Left가 230px를 점유해 로그인 카드 레이아웃 깨짐 방지
            pnlSideMenu = new Panel { Dock = DockStyle.None, Width = 230, BackColor = C_WHITE, Visible = false };
            // 오른쪽 경계선
            pnlSideMenu.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Theme.C(C_BORDER)), pnlSideMenu.Width - 1, 0,
                    pnlSideMenu.Width - 1, pnlSideMenu.Height);

            // ── 사용자 정보 헤더 (Dock=Top) ──────────────────────────
            pnlUserInfo = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = C_RED_TINT };
            pnlUserInfo.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Theme.C(C_BORDER)), 0, 73, pnlUserInfo.Width, 73);

            // 원형 아바타 (Paint로 그리기)
            Panel pnlAvatar = new Panel
            {
                Location  = new Point(16, 17),
                Size      = new Size(38, 38),
                BackColor = Color.Transparent
            };
            pnlAvatar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(new SolidBrush(C_RED), 0, 0, 37, 37);
                using var f  = new Font("Arial", 15f, FontStyle.Bold);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("O", f, Brushes.White, new RectangleF(0, 0, 37, 37), sf);
            };

            lblWelcome = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(158, 18, 18),
                Location  = new Point(62, 12),
                Size      = new Size(158, 48),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlUserInfo.Controls.AddRange(new Control[] { pnlAvatar, lblWelcome });

            // ── OTT 버튼 목록 (Dock=Fill) ─────────────────────────────
            pnlOttButtons = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = C_WHITE };

            // ── 하단 기능 버튼 영역 (Dock=Bottom) ─────────────────────
            // Height: 42(저장/복구 행) + 1(구분선) + 40×4(기능버튼) = 203px
            Panel pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 363, BackColor = C_WHITE };
            pnlActions.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Theme.C(C_BORDER)), 0, 0, pnlActions.Width, 0);

            // 배치 저장 / 복구 — 나란히 (TableLayoutPanel)
            var tblSaveRow = new TableLayoutPanel
            {
                Dock        = DockStyle.Top,
                Height      = 42,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = C_WHITE,
                Padding     = new Padding(0)
            };
            tblSaveRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblSaveRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            btnSaveLayout = ActionGridBtn("저장", Color.FromArgb(55, 110, 200));
            btnLoadLayout = ActionGridBtn("복구", Color.FromArgb(46, 139, 87));
            tblSaveRow.Controls.Add(btnSaveLayout, 0, 0);
            tblSaveRow.Controls.Add(btnLoadLayout, 1, 0);

            // 구분선
            Panel sepLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = C_BORDER };

            // 단독 기능 버튼 (Dock=Top, 순서대로 쌓임)
            btnClearLayout = ActionBtn("화면 초기화", Color.FromArgb(185, 50, 50));
            btnChangeNick  = ActionBtn("닉네임 변경", C_TEXT_MID);
            btnChangePw    = ActionBtn("비밀번호 변경", C_TEXT_MID);
            btnPip         = ActionBtn("PiP 화면",    C_TEXT_MID);
            btnStats       = ActionBtn("시청 통계",   C_TEXT_MID);
            btnTheme       = ActionBtn("다크 모드",   C_TEXT_MID);
            btnAccount     = ActionBtn("계정 관리",   C_TEXT_MID);
            btnLogout      = ActionBtn("나가기",       C_TEXT_GRAY);

            btnSaveLayout.Click  += (s, e) => SavePresetLayout();
            btnLoadLayout.Click  += (s, e) => ShowPresetMenu();
            btnClearLayout.Click += (s, e) =>
            {
                Array.Clear(layoutSlots, 0, 4);
                lastFocusedOtt = "";
                foreach (var name in dicWebViews.Keys.ToList()) DropWebView(name);  // 메모리 해제
                UpdateNavState();
                ReorganizeWebViews();
            };
            btnChangeNick.Click += (s, e) =>
            {
                using var form = new UpdateUserForm(currentUserIdx, DatabaseHelper.CurrentNickname);
                if (form.ShowDialog() == DialogResult.OK && lblWelcome != null)
                    lblWelcome.Text = FormatWelcome(DatabaseHelper.CurrentNickname);
            };
            btnChangePw.Click += (s, e) => new ChangePasswordForm(currentUserIdx).ShowDialog();
            btnAccount.Click += (s, e) =>
            {
                var menu = new ContextMenuStrip { Font = new Font("Segoe UI", 9f) };
                menu.Items.Add("복구 이메일 등록", null, async (s2, e2) => await RegisterEmailAsync());
                menu.Items.Add("회원 탈퇴", null, async (s2, e2) => await DeleteAccountAsync());
                menu.Show(btnAccount, new Point(0, btnAccount.Height));
            };
            btnStats.Click += (s, e) =>
            {
                using var form = new StatsForm(currentUserId);
                form.ShowDialog(this);
            };
            btnTheme.Click += (s, e) =>
            {
                AppSettings.Current.DarkMode = !Theme.Dark;
                AppSettings.Current.Save();
                btnTheme.Text = Theme.Dark ? "라이트 모드" : "다크 모드";
                Theme.Apply(this);
                UpdateNavState();
            };
            btnPip.Click += (s, e) =>
            {
                this.Hide();
                isPipMode = true;
                ApplyMute();  // 숨겨진 메인 창 소리 끄기
                var launcher = new PipLauncherForm(currentUserIdx, currentUserId, DatabaseHelper.CurrentNickname);
                launcher.FormClosed += async (s2, e2) =>
                {
                    if (this.IsDisposed) return;
                    this.Show();
                    isPipMode = false;
                    ApplyMute();
                    await RefreshOttList();  // 런처에서 바뀐 OTT 반영
                };
                launcher.Show();
            };
            btnLogout.Click   += (s, e) =>
            {
                DatabaseHelper.ClearAutoLogin();  // 재시작 후 다시 로그인되지 않도록
                Application.Restart();
            };

            // Dock=Top 순서: 먼저 추가 = 위쪽 / 나중 추가 = 아래쪽
            pnlActions.Controls.Add(tblSaveRow);     // 맨 위: 저장/복구
            pnlActions.Controls.Add(sepLine);
            pnlActions.Controls.Add(btnClearLayout);
            pnlActions.Controls.Add(btnChangeNick);
            pnlActions.Controls.Add(btnChangePw);
            pnlActions.Controls.Add(btnPip);
            pnlActions.Controls.Add(btnStats);
            pnlActions.Controls.Add(btnTheme);
            pnlActions.Controls.Add(btnAccount);
            pnlActions.Controls.Add(btnLogout);      // 맨 아래

            // 사이드바 조립
            // Dock=Bottom 순서: 나중에 Add = 시각적으로 아래
            pnlSideMenu.Controls.Add(pnlOttButtons); // Dock=Fill
            pnlSideMenu.Controls.Add(pnlActions);    // Dock=Bottom
            pnlSideMenu.Controls.Add(pnlUserInfo);   // Dock=Top
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 로그인 화면 — 2컬럼 레이아웃
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 로그인 화면을 만듭니다. 왼쪽(다크 브랜딩 패널) + 오른쪽(로그인 카드) 2컬럼 구조입니다.
        /// 로그인 카드는 pnlLoginArea에 직접 배치되며, 창 크기 변경 시 중앙에 재정렬됩니다.
        /// </summary>
        private void BuildLoginArea()
        {
            // 불투명 Dock=Fill 패널 → 렌더링 문제 없음
            pnlLoginArea = new Panel { Dock = DockStyle.Fill, BackColor = C_WHITE };

            // ── 왼쪽: 브랜딩 패널 (다크 네이비) ─────────────────────
            Panel pnlLeft = new Panel { Dock = DockStyle.Left, Width = 310, BackColor = C_DARK };

            // 빨간 왼쪽 강조선
            Panel accentBar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = C_RED };

            // 내용 패널 (왼쪽 여백 처리용)
            Panel pnlLeftContent = new Panel { Dock = DockStyle.Fill, BackColor = C_DARK, Padding = new Padding(36, 0, 20, 0) };

            // "OTT Multi-View" — Paint로 O(빨간색)와 TT Multi-View(흰색)를 한 줄에 렌더링
            Panel pnlBrand = new Panel { Location = new Point(36, 80), Size = new Size(240, 48), BackColor = C_DARK };
            pnlBrand.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var fnt = new Font("Segoe UI", 22f, FontStyle.Bold);
                g.DrawString("O", fnt, new SolidBrush(C_RED), 0f, 0f);
                float ox = g.MeasureString("O", fnt).Width - 5f;
                g.DrawString("TT Multi-View", fnt, new SolidBrush(C_WHITE), ox, 0f);
            };
            Label lblTagline = new Label
            {
                Text      = "좋아하는 OTT를\n한 화면에서 즐기세요",
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(156, 158, 178),
                Location  = new Point(38, 144),
                Size      = new Size(232, 58),
                AutoSize  = false
            };

            // 기능 소개 버릿
            string[] feats = { "멀티 화면 지원 (최대 4개)", "OTT 커스텀 추가 · 관리", "레이아웃 저장 & 복구" };
            for (int i = 0; i < feats.Length; i++)
            {
                int yy = 270 + i * 32;
                // 빨간 점
                Panel dot = new Panel { Location = new Point(38, yy + 7), Size = new Size(5, 5), BackColor = C_RED };
                Label lbl = new Label
                {
                    Text      = feats[i],
                    ForeColor = Color.FromArgb(148, 150, 172),
                    Font      = new Font("Segoe UI", 8.5f),
                    AutoSize  = true,
                    Location  = new Point(52, yy)
                };
                pnlLeftContent.Controls.Add(dot);
                pnlLeftContent.Controls.Add(lbl);
            }

            // 버전 텍스트 (하단 고정)
            Label lblVer = new Label
            {
                Text      = "© 2025  Personal Pro",
                ForeColor = Color.FromArgb(68, 70, 90),
                Font      = new Font("Segoe UI", 7.5f),
                Dock      = DockStyle.Bottom,
                Height    = 34,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(38, 0, 0, 0)
            };

            pnlLeftContent.Controls.AddRange(new Control[] { pnlBrand, lblTagline, lblVer });
            pnlLeft.Controls.Add(pnlLeftContent);
            pnlLeft.Controls.Add(accentBar);

            // ── 오른쪽 배경 (Fill) ──────────────────────────────────
            // pnlLoginRight 중간 컨테이너 제거 — 카드를 pnlLoginArea에 직접 배치
            // x = 310(좌패널) + (510-360)/2 = 385, y = (474-340)/2 = 67
            pnlLoginArea.BackColor = C_BG;

            pnlLoginCard = new Panel
            {
                Size      = new Size(360, 362),
                Location  = new Point(385, 56),
                BackColor = C_WHITE
            };

            // 카드 테두리
            pnlLoginCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.C(C_BORDER));
                e.Graphics.DrawRectangle(pen, 0, 0, pnlLoginCard.Width - 1, pnlLoginCard.Height - 1);
            };

            // 폼 크기 변경 시 카드 재정렬 (pnlLoginArea가 Fill이라 Width가 항상 정확함)
            pnlLoginArea.Resize += (s, e) =>
            {
                const int leftW = 310;
                int availW = pnlLoginArea.Width  - leftW;
                int availH = pnlLoginArea.Height;
                if (availW < pnlLoginCard.Width || availH < pnlLoginCard.Height) return;
                pnlLoginCard.Location = new Point(
                    leftW + (availW - pnlLoginCard.Width)  / 2,
                    (availH - pnlLoginCard.Height) / 2);
            };

            // 카드 내부 컨트롤
            Label lblHead = new Label
            {
                Text = "로그인", Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = C_TEXT, AutoSize = true, Location = new Point(28, 28)
            };
            Label lblSub = new Label
            {
                Text = "계정에 로그인하여 멀티뷰를 시작하세요",
                Font = new Font("Segoe UI", 8.5f), ForeColor = C_TEXT_MID,
                AutoSize = true, Location = new Point(30, 72)
            };
            Panel divider = new Panel
            {
                Location = new Point(28, 96), Size = new Size(304, 1), BackColor = C_BORDER
            };

            // 아이디
            Label lId = CardLabel("아이디",    112);
            txtId     = CardInput("아이디를 입력하세요", 130, false);

            // 비밀번호
            Label lPw = CardLabel("비밀번호",  186);
            txtPw     = CardInput("비밀번호를 입력하세요", 204, true);
            txtPw.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoginProcess(); };

            chkAutoLogin = new CheckBox
            {
                Text = "자동 로그인", Location = new Point(28, 238), AutoSize = true,
                Font = new Font("Segoe UI", 8f), ForeColor = C_TEXT_MID, BackColor = C_WHITE
            };

            var lnkForgot = new LinkLabel
            {
                Text = "비밀번호 찾기", Location = new Point(250, 240), AutoSize = true,
                Font = new Font("Segoe UI", 8f), LinkColor = C_TEXT_MID, LinkBehavior = LinkBehavior.HoverUnderline
            };
            lnkForgot.LinkClicked += async (s, e) => await ResetPasswordAsync();

            btnLogin = new Button
            {
                Text      = "로그인",
                Location  = new Point(28, 264),
                Size      = new Size(304, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = C_RED,
                ForeColor = C_WHITE,
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize         = 0;
            btnLogin.FlatAppearance.MouseOverBackColor = C_RED_H;
            btnLogin.Click += (s, e) => LoginProcess();

            btnRegister = new Button
            {
                Text      = "계정이 없으신가요?  회원가입",
                Location  = new Point(28, 322),
                Size      = new Size(304, 32),
                FlatStyle = FlatStyle.Flat,
                ForeColor = C_TEXT_GRAY,
                BackColor = C_WHITE,
                Font      = new Font("Segoe UI", 8.5f),
                Cursor    = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize         = 0;
            btnRegister.FlatAppearance.MouseOverBackColor = C_HOVER;
            btnRegister.MouseEnter += (s, e) => btnRegister.ForeColor = C_RED;
            btnRegister.MouseLeave += (s, e) => btnRegister.ForeColor = Theme.C(C_TEXT_GRAY);
            btnRegister.Click      += (s, e) => new RegisterForm().ShowDialog();

            pnlLoginCard.Controls.AddRange(new Control[]
            {
                lblHead, lblSub, divider,
                lId, txtId, lPw, txtPw, chkAutoLogin, lnkForgot,
                btnLogin, btnRegister
            });

            pnlLoginArea.Controls.Add(pnlLeft);       // 왼쪽 다크 패널
            pnlLoginArea.Controls.Add(pnlLoginCard);  // 카드는 pnlLoginArea에 직접
        }

        // ── 로그인 카드 내부 컨트롤 팩토리 ──────────────────────────

        /// <summary>
        /// 로그인 카드 안에서 사용하는 필드 라벨("아이디", "비밀번호")을 만듭니다.
        /// </summary>
        /// <param name="text">라벨 텍스트 (string)</param>
        /// <param name="y">세로 위치 픽셀 (int)</param>
        /// <returns>만들어진 Label 컨트롤</returns>
        private static Label CardLabel(string text, int y) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = C_TEXT_MID,
            AutoSize  = true,
            Location  = new Point(28, y)
        };

        /// <summary>
        /// 로그인 카드 안에서 사용하는 입력칸(아이디/비밀번호)을 만듭니다.
        /// 플레이스홀더 텍스트와 비밀번호 마스킹 동작이 포함됩니다.
        /// </summary>
        /// <param name="placeholder">포커스 없을 때 표시할 안내 텍스트 (string)</param>
        /// <param name="y">세로 위치 픽셀 (int)</param>
        /// <param name="isPassword">true면 비밀번호 마스킹(●●●) 적용 (bool)</param>
        /// <returns>만들어진 TextBox 컨트롤</returns>
        private TextBox CardInput(string placeholder, int y, bool isPassword)
        {
            var tb = new TextBox
            {
                Location    = new Point(28, y),
                Size        = new Size(304, 38),
                BackColor   = C_INPUT_BG,
                ForeColor   = C_TEXT_GRAY,
                Font        = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                Text        = placeholder,
                Tag         = placeholder
            };
            tb.Enter += (s, e) =>
            {
                if (tb.Text == tb.Tag?.ToString())
                { tb.Text = ""; tb.ForeColor = Theme.C(C_TEXT); if (isPassword) tb.UseSystemPasswordChar = true; }
            };
            tb.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                { if (isPassword) tb.UseSystemPasswordChar = false; tb.Text = tb.Tag?.ToString() ?? ""; tb.ForeColor = Theme.C(C_TEXT_GRAY); }
            };
            return tb;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 버튼 팩토리 — 반복되는 버튼 생성 코드를 메서드로 묶음
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 타이틀바에 절대 좌표로 배치하는 아이콘 버튼을 만듭니다 (≡ ? ← → ↻).
        /// </summary>
        /// <param name="text">버튼 텍스트 (string)</param>
        /// <param name="x">타이틀바 기준 가로 위치 픽셀 (int)</param>
        /// <param name="font">사용할 폰트. null이면 기본 폰트 사용 (Font?)</param>
        /// <returns>만들어진 Button 컨트롤</returns>
        private static Button TitleIconBtn(string text, int x, Font? font = null)
        {
            var b = new Button
            {
                Text = text, Location = new Point(x, 9), Size = new Size(34, 28),
                FlatStyle = FlatStyle.Flat, ForeColor = C_TEXT_MID,
                BackColor = C_WHITE, Font = font ?? new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = C_HOVER;
            return b;
        }

        /// <summary>
        /// 타이틀바 오른쪽에 Dock=Right로 쌓이는 창 제어 버튼을 만듭니다 (─ □ ✕).
        /// </summary>
        /// <param name="text">버튼 텍스트 (string)</param>
        /// <param name="width">버튼 너비 픽셀 (int)</param>
        /// <returns>만들어진 Button 컨트롤</returns>
        private static Button TitleDockBtn(string text, int width)
        {
            var b = new Button
            {
                Text = text, Dock = DockStyle.Right, Width = width,
                FlatStyle = FlatStyle.Flat, ForeColor = C_TEXT_MID,
                BackColor = C_WHITE, Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = C_HOVER;
            return b;
        }

        /// <summary>
        /// 사이드바 하단의 "저장/복구" 나란히 버튼을 만듭니다 (TableLayoutPanel 안에 Dock=Fill).
        /// </summary>
        /// <param name="text">버튼 텍스트 (string)</param>
        /// <param name="fore">글자 색상 (Color)</param>
        /// <returns>만들어진 Button 컨트롤</returns>
        private static Button ActionGridBtn(string text, Color fore)
        {
            var b = new Button
            {
                Text = text, Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat, ForeColor = fore,
                BackColor = C_WHITE, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = C_HOVER;
            return b;
        }

        /// <summary>
        /// 사이드바 하단의 단독 기능 버튼을 만듭니다 (Dock=Top, 높이 40px).
        /// 화면초기화, 닉네임변경, 비밀번호변경, PiP, 나가기 버튼에 사용됩니다.
        /// </summary>
        /// <param name="text">버튼 텍스트 (string)</param>
        /// <param name="fore">글자 색상 (Color)</param>
        /// <returns>만들어진 Button 컨트롤</returns>
        private static Button ActionBtn(string text, Color fore)
        {
            var b = new Button
            {
                Text = text, Dock = DockStyle.Top, Height = 40,
                FlatStyle = FlatStyle.Flat, ForeColor = fore,
                BackColor = C_WHITE, Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = C_HOVER;
            return b;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 도움말
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 도움말 팝업 창을 표시합니다.
        /// 멀티뷰 조작 방법, 내비게이션, 레이아웃 저장/복구 방법을 안내합니다.
        /// </summary>
        private void ShowHelpDialog()
        {
            using var f = new Form
            {
                Text = "도움말", Size = new Size(520, 620),
                BackColor = C_WHITE, ForeColor = C_TEXT,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false
            };
            string help =
                "[ 화면 열기 ]\n" +
                "• 클릭 → 화면 하나로 열기\n" +
                "• Ctrl + 클릭 → 화면 추가 (최대 4개)\n" +
                "• 화면 개수에 맞춰 2분할, 위 2칸+아래 1칸, 2×2로 나뉩니다\n\n" +
                "[ 칸 상단 버튼 ]\n" +
                "• Vol : 칸별 음량 조절 (자체 플레이어를 쓰는 사이트는 안 될 수 있음)\n" +
                "• ♪ : 음소거\n" +
                "• ⛶ : 크게 보기 (왼쪽 큰 화면 + 오른쪽 작은 화면)\n" +
                "• ⧉ : 보던 페이지를 PiP 창으로 보내기\n" +
                "• ✕ : 이 칸만 닫기\n\n" +
                "[ 칸 배치 ]\n" +
                "• 칸 상단 바를 끌어 다른 칸에 놓기 → 자리 교체\n" +
                "• 칸 사이 경계선 끌기 → 크기 조절\n" +
                "• 화면에 보이지 않는 칸은 소리가 자동으로 꺼집니다\n\n" +
                "[ 이동 ]\n" +
                "• ← → ↻ : 마지막으로 클릭한 화면의 뒤로, 앞으로, 새로고침\n" +
                "• 주소 표시줄 : 주소를 입력하고 Enter로 이동\n" +
                "• ≡ : 사이드바 열기/닫기\n\n" +
                "[ 단축키 ]\n" +
                "• Ctrl+1~4 : 화면 선택\n" +
                "• Ctrl+M : 음소거   /   Ctrl+W : 칸 닫기\n" +
                "• Ctrl+L : 주소 표시줄   /   Ctrl+B : 사이드바\n" +
                "• Ctrl+P : PiP 모드   /   F5 : 새로고침\n" +
                "• Alt+← / Alt+→ : 뒤로, 앞으로   /   F11 : 최대화\n" +
                "• ESC : 전체화면 해제\n\n" +
                "[ OTT 관리 ]\n" +
                "• ＋ OTT 추가 : 이름과 주소로 사이트 추가\n" +
                "• 우클릭 : 수정, 삭제, 음소거, 위로/아래로 이동\n" +
                "• 기본 OTT는 수정과 삭제를 할 수 없습니다\n" +
                "• 이름을 바꾸면 그 사이트에 다시 로그인해야 합니다\n" +
                "• 이름과 주소에는 쉼표(,)와 세로줄(|)을 쓸 수 없습니다\n\n" +
                "[ 레이아웃 ]\n" +
                "• 저장 : 현재 화면 구성을 이름을 붙여 저장 (다른 PC에서도 사용 가능)\n" +
                "• 복구 : 저장한 구성 목록에서 선택\n" +
                "• '기본' 레이아웃은 로그인할 때 자동으로 복구됩니다\n" +
                "• 화면 초기화 : 모든 칸을 닫습니다\n\n" +
                "[ PiP 모드 ]\n" +
                "• 작은 창을 띄워 다른 작업을 하면서 볼 수 있습니다 (여러 개 가능)\n" +
                "• ▲ 항상 위 / ● 일반 / ▼ 맨 아래, ♪ 음소거\n" +
                "• 제목 표시줄을 끌어 이동, 가장자리를 끌어 크기 조절\n" +
                "• PiP 모드 중에는 메인 창의 소리가 꺼집니다\n\n" +
                "[ 계정 ]\n" +
                "• 자동 로그인 : 로그인 화면에서 체크 ('나가기'를 누르면 해제)\n" +
                "• 닉네임 변경 / 비밀번호 변경\n" +
                "• 계정 관리 → 복구 이메일 등록 : 비밀번호 찾기에 필요합니다\n" +
                "• 비밀번호 찾기 : 로그인 화면에서 임시 비밀번호를 메일로 받습니다\n" +
                "• 계정 관리 → 회원 탈퇴 : 서버의 OTT 목록과 레이아웃도 삭제되며 되돌릴 수 없습니다\n\n" +
                "[ 기타 ]\n" +
                "• 시청 통계 : OTT별 시청 시간을 오늘, 최근 7일, 전체로 확인\n" +
                "• 다크 모드 : 어두운 화면으로 전환\n" +
                "• 창 크기와 위치는 다음 실행 때 그대로 복원됩니다\n\n" +
                "[ 제작자 ]\n" +
                "안도영  /  dksehvkf1206@naver.com";
            // 내용이 길어 스크롤되는 읽기 전용 입력칸으로 표시
            f.Controls.Add(new TextBox
            {
                Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
                ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, TabStop = false,
                Font = new Font("Segoe UI", 9.5f), ForeColor = C_TEXT_MID, BackColor = C_WHITE,
                Text = help.ReplaceLineEndings()
            });
            Theme.Apply(f);
            f.ShowDialog();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 로그인 처리
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 로그인 버튼 클릭 시 실행됩니다.
        /// 아이디/비밀번호를 서버에 전송하고, 성공하면 메인 화면으로 전환합니다.
        /// async void = 비동기 이벤트 핸들러 (서버 응답 기다리는 동안 화면 안 멈춤)
        /// </summary>
        private async void LoginProcess()
        {
            if (txtId == null || txtPw == null || btnLogin == null) return;
            string id = txtId.Text == txtId.Tag?.ToString() ? "" : txtId.Text;
            string pw = txtPw.Text == txtPw.Tag?.ToString() ? "" : txtPw.Text;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(pw)) return;

            btnLogin.Enabled = false;
            btnLogin.Text    = "연결 중...";

            int idx = await Task.Run(() => DatabaseHelper.LoginCheckReturnIdx(id, pw));

            if (idx >= 0)
            {
                if (chkAutoLogin!.Checked) DatabaseHelper.SaveAutoLogin(id, pw);
                else DatabaseHelper.ClearAutoLogin();

                currentUserIdx = idx;
                currentUserId  = id;

                // 폼 크기 확장 (저장된 창 상태가 있으면 복원)
                RestoreWindowState();

                // 로그인 전 UI 숨김
                pnlLoginArea!.Visible = false;

                // 로그인 후 UI 표시 — Dock=Left로 전환 후 표시 (로그인 전엔 None이었음)
                pnlSideMenu!.Dock    = DockStyle.Left;
                pnlSideMenu!.Visible = true;
                pnlContent!.Visible  = true;

                // 타이틀바: 로고 숨기고 내비게이션 버튼 표시
                if (pnlLogoTitle != null) pnlLogoTitle.Visible = false;
                btnToggleMenu!.Visible = true;
                btnHelp!.Visible       = true;
                btnNavBack!.Visible    = true;
                btnNavForward!.Visible = true;
                btnNavRefresh!.Visible = true;

                // 주소 표시줄: 창 제어 버튼 앞까지 채움
                txtAddress!.Width   = pnlTitleBar!.ClientSize.Width - txtAddress.Left - 144;
                txtAddress.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                txtAddress.Visible  = true;

                // 시청 시간 기록
                var statsTimer = new System.Windows.Forms.Timer { Interval = STATS_INTERVAL * 1000 };
                statsTimer.Tick += (s, e) => RecordWatchTime();
                statsTimer.Start();

                if (lblWelcome != null)
                    lblWelcome.Text = FormatWelcome(DatabaseHelper.CurrentNickname);

                await RefreshOttList();
                await SyncLayoutsFromServer();
                await LoadPresetAsync(PresetPath("기본"), silent: true);
            }
            else
            {
                if (idx == -1) DatabaseHelper.ClearAutoLogin();  // 바뀐 비밀번호로 재시도 방지
                MessageBox.Show(
                    idx == -1 ? "아이디 또는 비밀번호가 올바르지 않습니다."
                              : "서버에 연결하지 못했습니다. 인터넷 연결을 확인해주세요.",
                    "로그인 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnLogin.Enabled = true;
                btnLogin.Text    = "로그인";
            }
        }

        /// <summary>
        /// 저장된 자동 로그인 정보가 있으면 입력칸을 채우고 로그인합니다.
        /// </summary>
        private void TryAutoLogin()
        {
            if (DatabaseHelper.LoadAutoLogin() is not { } saved) return;
            txtId!.Text      = saved.Id;
            txtId.ForeColor  = Theme.C(C_TEXT);
            txtPw!.UseSystemPasswordChar = true;
            txtPw.Text       = saved.Pw;
            txtPw.ForeColor  = Theme.C(C_TEXT);
            chkAutoLogin!.Checked = true;
            LoginProcess();
        }

        /// <summary>
        /// 사이드바 상단에 표시할 환영 메시지를 만들어 반환합니다.
        /// </summary>
        /// <param name="nick">사용자 닉네임 (string)</param>
        /// <returns>"닉네임님\n환영합니다" 형식의 문자열 (string)</returns>
        private static string FormatWelcome(string nick) => $"{nick}님\n환영합니다";


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // OTT 목록 관리
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 사이드바의 OTT 버튼 목록을 서버에서 다시 불러와 갱신합니다.
        /// 기본 OTT + 사용자 추가 OTT + "＋ OTT 추가" 버튼 순서로 표시됩니다.
        /// 서버 통신에 실패하면 마지막으로 받은 목록을 유지합니다.
        /// </summary>
        private async Task RefreshOttList()
        {
            if (pnlOttButtons == null) return;

            var fetched = await Task.Run(() => DatabaseHelper.GetUserOtts(currentUserIdx));
            if (fetched != null) userOtts = fetched;
            else MessageBox.Show("OTT 목록을 불러오지 못했습니다. 인터넷 연결을 확인해주세요.", "목록 오류",
                     MessageBoxButtons.OK, MessageBoxIcon.Warning);

            pnlOttButtons.Controls.Clear();

            foreach (var item in OttInput.AllOtts(currentUserId, userOtts))
                AddOttBtn(item[1], item[2], item[0] != "", item[0]);

            // ＋ 추가 버튼
            int yAdd = pnlOttButtons.Controls.Count * 50 + 8;
            var add = new Button
            {
                Text = "＋  OTT 추가",
                Location = new Point(0, yAdd),
                Size     = new Size(pnlOttButtons.Width > 0 ? pnlOttButtons.Width : 230, 42),
                FlatStyle = FlatStyle.Flat,
                ForeColor = C_TEXT_GRAY, BackColor = C_WHITE,
                Font = new Font("Segoe UI", 8.5f), Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            add.FlatAppearance.BorderSize         = 0;
            add.FlatAppearance.MouseOverBackColor = C_HOVER;
            add.Click += async (s, e) =>
            {
                if (OttInput.Prompt("OTT 추가", "", "https://", userOtts) is not { } o) return;
                if (await Task.Run(() => DatabaseHelper.AddUserOtt(currentUserIdx, o.Name, o.Url)))
                    await RefreshOttList();
                else OttInput.ShowSaveError();
            };
            pnlOttButtons.Controls.Add(add);
            Theme.Apply(pnlOttButtons);

            if (fetched != null) await SyncWebViews();
        }

        /// <summary>
        /// 열려 있는 WebView2를 최신 OTT 목록에 맞춥니다.
        /// URL이 바뀌었으면 다시 열고, 삭제된 OTT는 화면에서 내립니다.
        /// </summary>
        private async Task SyncWebViews()
        {
            foreach (var name in dicWebViews.Keys.ToList())
            {
                string url = GetUrlByName(name);
                if (!loadedUrls.TryGetValue(name, out var loaded) || url == loaded) continue;

                DropWebView(name);
                int slot = Array.IndexOf(layoutSlots, name);
                if (slot < 0) continue;
                if (url == "") layoutSlots[slot] = null;
                else await LoadWebViewAsync(name, url);
            }
            if (!layoutSlots.Contains(lastFocusedOtt)) lastFocusedOtt = "";
            UpdateNavState();
            ReorganizeWebViews();
        }

        /// <summary>
        /// OTT 버튼 하나를 만들어 사이드바 목록에 추가합니다.
        /// 클릭 시 단일 또는 다중(Ctrl+클릭) 화면으로 열립니다.
        /// </summary>
        /// <param name="name">OTT 이름 (string, 예: "NETFLIX")</param>
        /// <param name="url">OTT 주소 (string)</param>
        /// <param name="isCustom">사용자가 추가한 OTT면 true, 기본 OTT면 false (bool)</param>
        /// <param name="dbIdx">DB 인덱스 번호 — 기본 OTT는 "" (string)</param>
        private void AddOttBtn(string name, string url, bool isCustom, string dbIdx = "")
        {
            int y   = pnlOttButtons!.Controls.Count * 50 + 8;
            int w   = pnlOttButtons.Width > 0 ? pnlOttButtons.Width : 230;

            var btn = new Button
            {
                Tag       = new string[] { url, dbIdx, name },
                Location  = new Point(0, y),
                Size      = new Size(w, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = C_WHITE,
                ForeColor = C_TEXT,
                Font      = new Font("Segoe UI", 9f),
                Text      = $"  {name}",
                TextAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = C_HOVER;
            if (cmsMenu != null) btn.ContextMenuStrip = cmsMenu;

            new ToolTip().SetToolTip(btn, name);

            btn.Click += async (s, e) =>
            {
                if (Control.ModifierKeys == Keys.Control)
                {
                    if (!layoutSlots.Contains(name))
                    {
                        int free = Array.IndexOf(layoutSlots, null);
                        if (free < 0)
                        {
                            MessageBox.Show("화면은 최대 4개까지 열 수 있습니다.", "멀티뷰",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        layoutSlots[free] = name;
                    }
                }
                else
                {
                    Array.Clear(layoutSlots, 0, 4);
                    layoutSlots[0] = name;
                }
                lastFocusedOtt = name;
                UpdateNavState();
                await LoadWebViewAsync(name, url);
                ReorganizeWebViews();
            };

            _ = LoadFaviconAsync(btn, url);
            pnlOttButtons.Controls.Add(btn);
        }

        /// <summary>
        /// 구글 파비콘 서비스에서 OTT 아이콘을 다운로드해 버튼에 붙입니다.
        /// 실패해도 예외 없이 무시합니다 (아이콘 없어도 버튼이 동작하므로).
        /// </summary>
        /// <param name="btn">아이콘을 붙일 Button 컨트롤</param>
        /// <param name="url">아이콘을 가져올 사이트 주소 (string)</param>
        private async Task LoadFaviconAsync(Button btn, string url)
        {
            try
            {
                string host  = new Uri(url).Host;
                byte[] bytes = await FaviconHttp.GetByteArrayAsync(
                    $"https://www.google.com/s2/favicons?sz=32&domain={host}");
                this.Invoke(() =>
                {
                    if (!btn.IsDisposed)
                    {
                        using var ms  = new MemoryStream(bytes);
                        btn.Image     = new Bitmap(new Bitmap(ms), new Size(16, 16));
                        btn.Padding   = new Padding(10, 0, 0, 0);
                    }
                });
            }
            catch { }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // WebView2 관리
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// WebView2(내장 브라우저 창)를 만들고 OTT 사이트를 로드합니다.
        /// 이미 같은 이름의 WebView2가 있으면 새로 만들지 않고 재사용합니다.
        /// 웹페이지 클릭 감지(lastFocusedOtt 갱신)와 전체화면 처리도 설정합니다.
        /// </summary>
        /// <param name="name">OTT 이름 — dicWebViews의 키로 사용됨 (string)</param>
        /// <param name="url">열 OTT 주소 (string)</param>
        private async Task LoadWebViewAsync(string name, string url)
        {
            if (dicWebViews.ContainsKey(name)) return;

            // 배경은 브라우저 기본값인 흰색 — 배경색을 지정하지 않는 사이트(Pinterest 등)가 검게 보이지 않도록
            var wv   = new WebView2 { DefaultBackgroundColor = Color.White };
            var cell = new OttCell(name, wv) { Visible = false };
            cell.CloseClicked  += c => CloseCell(c.OttName);
            cell.MuteClicked   += c => ToggleMute(c.OttName);
            cell.MainClicked   += c => ToggleMainView(c.OttName);
            cell.PipClicked    += c => SendToPip(c.OttName);
            cell.HeaderPressed += c => { lastFocusedOtt = c.OttName; UpdateNavState(); };
            cell.VolumeChanged += ApplyVolume;
            cell.SwapRequested += SwapCells;
            pnlContent?.Controls.Add(cell);
            dicCells[name]    = cell;
            dicWebViews[name] = wv;
            loadedUrls[name]  = url;

            wv.GotFocus += (s, e) => { lastFocusedOtt = name; UpdateNavState(); };

            string dir = DatabaseHelper.DataPath(currentUserId, name.Replace(" ", "_"));

            Uri uri;
            try
            {
                uri = new Uri(url);  // 예전에 저장된 잘못된 URL이면 여기서 걸러짐
                var env = await CoreWebView2Environment.CreateAsync(null, dir);
                await wv.EnsureCoreWebView2Async(env);
                await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(VolumeScript);
            }
            catch (Exception ex)
            {
                // 초기화 중 화면 초기화로 이미 지워진 경우에는 알릴 필요 없음
                if (!dicWebViews.TryGetValue(name, out var cur) || cur != wv) return;
                DropWebView(name);
                int slot = Array.IndexOf(layoutSlots, name);
                if (slot >= 0) layoutSlots[slot] = null;
                MessageBox.Show($"'{name}' 화면을 열지 못했습니다.\n{ex.Message}", "화면 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 웹 내 클릭 → lastFocusedOtt 갱신 (스크립트 메시지 방식)
            wv.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                wv.ExecuteScriptAsync(
                    "window.addEventListener('mousedown',()=>" +
                    "window.chrome.webview.postMessage('focus'));");

                cell.SetLoading(false);
                if (cell.Volume != 100) ApplyVolume(cell);

                // 사용자가 취소한 이동과 사이트가 준 4xx/5xx 페이지는 오류 화면을 띄우지 않음
                if (!e.IsSuccess &&
                    e.WebErrorStatus != CoreWebView2WebErrorStatus.Unknown &&
                    e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled &&
                    e.WebErrorStatus != CoreWebView2WebErrorStatus.ConnectionAborted)
                    cell.ShowError($"'{name}'에 접속하지 못했습니다.\n인터넷 연결을 확인해주세요.");
            };
            wv.CoreWebView2.NavigationStarting += (s, e) => cell.SetLoading(true);
            wv.CoreWebView2.SourceChanged += (s, e) => { if (name == lastFocusedOtt) UpdateNavState(); };

            wv.CoreWebView2.WebMessageReceived += (s, e) =>
            {
                if (e.TryGetWebMessageAsString() == "focus")
                { lastFocusedOtt = name; this.Invoke(UpdateNavState); }
            };

            wv.CoreWebView2.ContainsFullScreenElementChanged += (s, e) =>
            {
                if (wv.CoreWebView2.ContainsFullScreenElement) EnterFullScreen(wv);
                else ExitFullScreen();
            };

            wv.Source = uri;
        }

        /// <summary>
        /// WebView2를 닫고 메모리를 해제합니다.
        /// </summary>
        /// <param name="name">닫을 OTT 이름 (string)</param>
        private void DropWebView(string name)
        {
            if (!dicWebViews.Remove(name, out var wv)) return;
            if (wv == fullScreenView) ExitFullScreen();
            loadedUrls.Remove(name);
            userMuted.Remove(name);
            if (name == mainOtt) mainOtt = "";
            if (dicCells.Remove(name, out var cell))
            {
                pnlContent?.Controls.Remove(cell);
                cell.Dispose();  // 안의 WebView2도 함께 해제됨
            }
        }

        /// <summary>
        /// 칸 하나를 닫습니다. 포커스는 남은 화면 중 첫 번째로 옮깁니다.
        /// </summary>
        /// <param name="name">닫을 OTT 이름 (string)</param>
        private void CloseCell(string name)
        {
            int slot = Array.IndexOf(layoutSlots, name);
            if (slot >= 0) layoutSlots[slot] = null;
            DropWebView(name);
            if (lastFocusedOtt == name) lastFocusedOtt = layoutSlots.FirstOrDefault(n => n != null) ?? "";
            UpdateNavState();
            ReorganizeWebViews();
        }

        /// <summary>
        /// 크게 보기를 켜거나 끕니다. 켜면 해당 칸이 왼쪽에 크게 표시됩니다.
        /// </summary>
        private void ToggleMainView(string name)
        {
            mainOtt = mainOtt == name ? "" : name;
            splitX  = mainOtt == "" ? 0.5 : 0.72;
            ReorganizeWebViews();
        }

        /// <summary>
        /// 보던 페이지를 PiP 창으로 옮기고 칸을 닫습니다.
        /// </summary>
        private void SendToPip(string name)
        {
            if (!dicWebViews.TryGetValue(name, out var wv)) return;
            string url = wv.CoreWebView2?.Source ?? loadedUrls[name];
            new PipForm(name, url, currentUserId).Show();
            CloseCell(name);
        }

        /// <summary>
        /// 두 칸의 자리를 바꿉니다.
        /// </summary>
        private void SwapCells(OttCell a, OttCell b)
        {
            int ia = Array.IndexOf(layoutSlots, a.OttName), ib = Array.IndexOf(layoutSlots, b.OttName);
            if (ia < 0 || ib < 0) return;
            (layoutSlots[ia], layoutSlots[ib]) = (layoutSlots[ib], layoutSlots[ia]);
            ReorganizeWebViews();
        }

        /// <summary>
        /// 칸의 음량을 페이지의 영상/오디오에 적용합니다.
        /// 사이트가 자체 플레이어로 음량을 관리하면 적용되지 않을 수 있습니다.
        /// </summary>
        private void ApplyVolume(OttCell cell)
        {
            if (cell.View.CoreWebView2 == null) return;
            string v = (cell.Volume / 100.0).ToString(CultureInfo.InvariantCulture);
            // 100으로 되돌리면 한 번만 적용하고 이후엔 사이트 음량에 맡김
            string keep = cell.Volume == 100 ? "null" : v;
            _ = cell.View.ExecuteScriptAsync(
                $"window.__ottVol={keep};document.querySelectorAll('video,audio').forEach(m=>m.volume={v});");
        }

        /// <summary>
        /// 화면에 보이지 않는 OTT의 소리를 끕니다.
        /// 사용자가 음소거한 OTT와 PiP 모드 중인 메인 창도 소리를 끕니다.
        /// </summary>
        private void ApplyMute()
        {
            foreach (var (name, wv) in dicWebViews)
                if (wv.CoreWebView2 != null)
                {
                    wv.CoreWebView2.IsMuted = isPipMode || !dicCells[name].Visible || userMuted.Contains(name);
                    dicCells[name].SetMuted(userMuted.Contains(name));
                }
        }

        /// <summary>
        /// layoutSlots 배열을 기준으로 WebView2들의 크기와 위치를 재계산해서 배치합니다.
        /// 화면 수에 따라 다르게 배치됩니다:
        ///   크기 계산은 LayoutCalculator 참고
        ///   사이트 전체화면 중 → 해당 화면만 전체
        /// </summary>
        private void ReorganizeWebViews()
        {
            if (pnlContent == null) return;
            int w = pnlContent.Width, h = pnlContent.Height;

            var names = fullScreenView != null
                ? dicCells.Values.Where(c => c.View == fullScreenView).Select(c => c.OttName).ToList()
                : layoutSlots.Where(n => n != null && dicCells.ContainsKey(n)).Select(n => n!).ToList();

            int mainIndex = fullScreenView == null ? names.IndexOf(mainOtt) : -1;
            var rects     = LayoutCalculator.Compute(names.Count, w, h, mainIndex, splitX, splitY);

            foreach (var (name, cell) in dicCells)
                cell.Visible = names.Contains(name);
            for (int i = 0; i < names.Count; i++)
            {
                var cell = dicCells[names[i]];
                cell.HeaderVisible = fullScreenView == null;
                cell.Bounds        = rects[i];
                foreach (var (n, c) in dicCells) c.SetMain(n == mainOtt);
            }

            // 경계선: 세로는 2칸 이상, 가로는 위아래로 나뉜 배치에서만 표시
            int sx = (int)(w * splitX), sy = (int)(h * splitY);
            bool grid = names.Count >= 3 && mainIndex < 0;
            pnlSplitV!.Visible = names.Count >= 2;
            pnlSplitH!.Visible = grid;
            pnlSplitV.Bounds   = new Rectangle(sx - 2, 0, 4, names.Count == 3 && grid ? sy : h);
            pnlSplitH.Bounds   = new Rectangle(0, sy - 2, w, 4);
            pnlSplitV.BringToFront();
            pnlSplitH.BringToFront();

            ApplyMute();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 내비게이션 상태 / 레이아웃 저장·복구
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 내비게이션 버튼(← → ↻)의 색상을 현재 상태에 맞게 업데이트합니다.
        /// OTT가 선택되어 있으면 진한 색, 없으면 연한 회색으로 표시합니다.
        /// </summary>
        private void UpdateNavState()
        {
            bool active = !string.IsNullOrEmpty(lastFocusedOtt);
            Color c = Theme.C(active ? C_TEXT : C_TEXT_GRAY);
            if (btnNavBack    != null) btnNavBack.ForeColor    = c;
            if (btnNavForward != null) btnNavForward.ForeColor = c;
            if (btnNavRefresh != null) btnNavRefresh.ForeColor = c;

            // 입력 중에는 덮어쓰지 않음
            if (txtAddress != null && !txtAddress.Focused)
                txtAddress.Text = dicWebViews.TryGetValue(lastFocusedOtt, out var wv)
                    ? wv.CoreWebView2?.Source ?? "" : "";
        }

        /// <summary>
        /// 레이아웃 이름에 해당하는 파일 경로를 만듭니다.
        /// '기본'은 기존 저장 파일(layout_preset.txt)을 그대로 사용합니다.
        /// </summary>
        /// <param name="name">레이아웃 이름 (string)</param>
        /// <returns>파일 경로 (string)</returns>
        private string PresetPath(string name) =>
            DatabaseHelper.DataPath(currentUserId, name == "기본" ? "layout_preset.txt" : $"layout_{name}.txt");

        /// <summary>
        /// 현재 화면 구성(layoutSlots)을 이름을 붙여 로컬 파일에 저장합니다.
        /// 파일 경로: AppData\Local\PersonalPro\사용자아이디\layout_이름.txt
        /// </summary>
        private async void SavePresetLayout()
        {
            if (layoutSlots.All(s => s == null))
            {
                MessageBox.Show("저장할 화면이 없습니다. OTT를 먼저 열어주세요.", "저장",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string name = Interaction.InputBox("레이아웃 이름 ('기본'은 로그인 때 자동 복구):", "저장", "기본").Trim();
            if (name == "") return;

            try
            {
                string path = PresetPath(name);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                string slots = PresetFormat.Serialize(layoutSlots);
                File.WriteAllText(path, slots);

                bool uploaded = await Task.Run(() => DatabaseHelper.SaveLayout(currentUserIdx, name, slots));
                MessageBox.Show(uploaded ? "레이아웃이 저장되었습니다." : "이 PC에만 저장되었습니다. (서버 저장 실패)", "저장",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show($"레이아웃을 저장하지 못했습니다.\n{ex.Message}", "저장 실패",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 서버의 레이아웃을 이 PC로 내려받습니다. 같은 이름은 서버 값으로 덮어씁니다.
        /// 실패하면 이 PC의 파일을 그대로 사용합니다.
        /// </summary>
        private async Task SyncLayoutsFromServer()
        {
            var rows = await Task.Run(() => DatabaseHelper.GetLayouts(currentUserIdx));
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

        /// <summary>
        /// 저장된 레이아웃 목록을 복구 버튼 아래에 메뉴로 보여줍니다.
        /// </summary>
        private void ShowPresetMenu()
        {
            string dir = DatabaseHelper.DataPath(currentUserId);
            string[] files = Directory.Exists(dir) ? Directory.GetFiles(dir, "layout_*.txt") : Array.Empty<string>();
            if (files.Length == 0)
            {
                MessageBox.Show("저장된 레이아웃이 없습니다.", "복구",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var menu = new ContextMenuStrip { Font = new Font("Segoe UI", 9f) };
            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file)["layout_".Length..];
                menu.Items.Add(name == "preset" ? "기본" : name, null,
                    async (s, e) => await LoadPresetAsync(file, silent: false));
            }
            menu.Show(btnLoadLayout!, new Point(0, btnLoadLayout!.Height));
        }

        /// <summary>
        /// 저장된 레이아웃 파일을 읽어서 화면을 복원합니다.
        /// 로그인 직후 자동 복원 시 silent=true로 호출해서 오류 팝업을 숨깁니다.
        /// </summary>
        /// <param name="path">레이아웃 파일 경로 (string)</param>
        /// <param name="silent">true = 찾지 못한 OTT가 있어도 알리지 않음 (bool)</param>
        private async Task LoadPresetAsync(string path, bool silent)
        {
            if (!File.Exists(path)) return;

            var saved = PresetFormat.Parse(File.ReadAllText(path));

            // 기존 화면과 섞이지 않도록 비우고 시작
            Array.Clear(layoutSlots, 0, 4);
            lastFocusedOtt = "";

            bool missing = false;
            for (int i = 0; i < saved.Length; i++)
            {
                if (saved[i] is not { } ott) continue;
                string url = GetUrlByName(ott);
                if (string.IsNullOrEmpty(url)) { missing = true; continue; }

                layoutSlots[i] = ott;
                if (lastFocusedOtt == "") lastFocusedOtt = ott;
                await LoadWebViewAsync(ott, url);
            }
            if (missing && !silent)
                MessageBox.Show("삭제되었거나 이름이 바뀐 OTT는 복구하지 못했습니다.", "복구",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            UpdateNavState();
            ReorganizeWebViews();
        }

        /// <summary>
        /// OTT 이름으로 URL을 찾아 반환합니다.
        /// 기본 OTT와 마지막으로 받은 사용자 OTT 목록에서 찾습니다.
        /// </summary>
        /// <param name="name">OTT 이름 (string)</param>
        /// <returns>해당 OTT의 URL. 없으면 "" 빈 문자열 (string)</returns>
        private string GetUrlByName(string name) =>
            OttInput.AllOtts(currentUserId, userOtts).FirstOrDefault(o => o[1] == name)?[2] ?? "";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 우클릭 메뉴 액션
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 우클릭 메뉴 "수정"을 실행합니다.
        /// InputBox로 새 이름과 URL을 입력받아 서버에 업데이트합니다.
        /// </summary>
        private async void EditSelectedOtt()
        {
            if (cmsMenu?.SourceControl is not Button btn) return;
            var d = (string[])btn.Tag!;
            if (OttInput.Prompt("수정", d[2], d[0], userOtts) is not { } o) return;
            if (!await Task.Run(() => DatabaseHelper.UpdateUserOtt(d[1], o.Name, o.Url)))
            { OttInput.ShowSaveError(); return; }

            // 이름이 바뀌면 열려 있던 화면 자리를 새 이름으로 넘김
            int slot = Array.IndexOf(layoutSlots, d[2]);
            if (slot >= 0) layoutSlots[slot] = o.Name;
            if (lastFocusedOtt == d[2]) lastFocusedOtt = o.Name;

            await RefreshOttList();
            if (slot >= 0)
            {
                await LoadWebViewAsync(o.Name, o.Url);
                ReorganizeWebViews();
            }
        }

        /// <summary>
        /// 우클릭 메뉴 "삭제"를 실행합니다.
        /// 확인 팝업 후 서버에서 OTT를 삭제하고 목록을 갱신합니다.
        /// </summary>
        private async void DeleteSelectedOtt()
        {
            if (cmsMenu?.SourceControl is not Button btn) return;
            var d = (string[])btn.Tag!;
            if (MessageBox.Show($"'{d[2]}'을(를) 삭제하시겠습니까?", "삭제",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (await Task.Run(() => DatabaseHelper.DeleteUserOtt(d[1])))
                await RefreshOttList();
            else OttInput.ShowSaveError();
        }

        /// <summary>
        /// 우클릭 메뉴 "음소거 토글"을 실행합니다.
        /// 현재 음소거 상태를 반전시킵니다 (켜져있으면 끄고, 꺼져있으면 켬).
        /// </summary>
        private void ToggleMuteSelected()
        {
            if (cmsMenu?.SourceControl is not Button btn) return;
            ToggleMute(((string[])btn.Tag!)[2]);
        }

        /// <summary>
        /// OTT의 음소거 상태를 반전시킵니다.
        /// </summary>
        private void ToggleMute(string name)
        {
            if (!userMuted.Add(name)) userMuted.Remove(name);
            ApplyMute();
        }

        /// <summary>
        /// 우클릭 메뉴 "위로/아래로 이동": 사이드바 순서를 바꿔 PC에 저장합니다.
        /// </summary>
        /// <param name="delta">-1 = 위로, 1 = 아래로 (int)</param>
        private async void MoveSelectedOtt(int delta)
        {
            if (cmsMenu?.SourceControl is not Button btn) return;
            var names = OttInput.AllOtts(currentUserId, userOtts).Select(o => o[1]);
            AppSettings.Current.OttOrder[currentUserId] =
                OttOrdering.Move(names, ((string[])btn.Tag!)[2], delta);
            AppSettings.Current.Save();
            await RefreshOttList();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 주소 표시줄 / 단축키 / 시청 기록
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 비밀번호 찾기: 아이디와 복구 이메일이 맞으면 임시 비밀번호를 메일로 받습니다.
        /// </summary>
        private async Task ResetPasswordAsync()
        {
            string id = Interaction.InputBox("아이디:", "비밀번호 찾기").Trim();
            if (id == "") return;
            string email = Interaction.InputBox("등록한 복구 이메일:", "비밀번호 찾기").Trim();
            if (email == "") return;

            bool sent = await Task.Run(() => DatabaseHelper.ResetPassword(id, email));
            MessageBox.Show(sent ? "임시 비밀번호를 메일로 보냈습니다."
                                 : "아이디와 이메일을 확인해주세요. 복구 이메일을 등록한 계정만 찾을 수 있습니다.",
                "비밀번호 찾기", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 계정 관리: 비밀번호 확인 후 복구 이메일을 등록합니다.
        /// </summary>
        private async Task RegisterEmailAsync()
        {
            string email = Interaction.InputBox("복구 이메일:", "복구 이메일 등록").Trim();
            if (!email.Contains('@'))
            {
                if (email != "") MessageBox.Show("이메일 주소를 확인해주세요.", "복구 이메일 등록",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (OttInput.AskPassword("복구 이메일 등록", "현재 비밀번호를 입력하세요.") is not { } pw) return;

            bool ok = await Task.Run(() => DatabaseHelper.SetEmail(currentUserId, pw, email));
            MessageBox.Show(ok ? "복구 이메일이 등록되었습니다." : "비밀번호가 틀렸거나 서버 오류가 발생했습니다.",
                "복구 이메일 등록", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 계정 관리: 비밀번호 확인 후 계정을 삭제하고 앱을 다시 시작합니다.
        /// </summary>
        private async Task DeleteAccountAsync()
        {
            if (MessageBox.Show("계정과 서버에 저장된 OTT 목록, 레이아웃이 모두 삭제됩니다.\n되돌릴 수 없습니다. 탈퇴하시겠습니까?",
                    "회원 탈퇴", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            if (OttInput.AskPassword("회원 탈퇴", "현재 비밀번호를 입력하세요.") is not { } pw) return;

            if (await Task.Run(() => DatabaseHelper.DeleteAccount(currentUserId, pw)))
            {
                DatabaseHelper.ClearAutoLogin();
                Application.Restart();
            }
            else MessageBox.Show("비밀번호가 틀렸거나 서버 오류가 발생했습니다.", "회원 탈퇴",
                     MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 주소 표시줄 입력값으로 포커스 화면을 이동합니다.
        /// http(s)가 없으면 https://를 붙입니다.
        /// </summary>
        private void NavigateAddress()
        {
            if (!dicWebViews.TryGetValue(lastFocusedOtt, out var wv) || wv.CoreWebView2 == null) return;
            string text = txtAddress!.Text.Trim();
            if (!text.Contains("://")) text = "https://" + text;
            if (Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                wv.CoreWebView2.Navigate(uri.ToString());
                wv.Focus();
            }
        }

        /// <summary>
        /// 앱 단축키를 처리합니다. WebView2에 포커스가 있어도 Ctrl/Alt/F키 조합은 여기로 들어옵니다.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (currentUserIdx >= 0 && RunShortcut(keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// 단축키 하나를 실행합니다.
        /// </summary>
        /// <returns>처리했으면 true (bool)</returns>
        private bool RunShortcut(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.D1: FocusScreen(0); return true;
                case Keys.Control | Keys.D2: FocusScreen(1); return true;
                case Keys.Control | Keys.D3: FocusScreen(2); return true;
                case Keys.Control | Keys.D4: FocusScreen(3); return true;
                case Keys.Control | Keys.M:
                    if (lastFocusedOtt != "") ToggleMute(lastFocusedOtt);
                    return true;
                case Keys.Control | Keys.W:
                    if (lastFocusedOtt != "") CloseCell(lastFocusedOtt);
                    return true;
                case Keys.Control | Keys.L:
                    txtAddress!.Focus();
                    txtAddress.SelectAll();
                    return true;
                case Keys.Control | Keys.B: btnToggleMenu!.PerformClick(); return true;
                case Keys.Control | Keys.P: btnPip!.PerformClick(); return true;
                case Keys.F5:               btnNavRefresh!.PerformClick(); return true;
                case Keys.Alt | Keys.Left:  btnNavBack!.PerformClick(); return true;
                case Keys.Alt | Keys.Right: btnNavForward!.PerformClick(); return true;
                case Keys.F11:              ToggleMaximize(); return true;
                default: return false;
            }
        }

        /// <summary>
        /// 열려 있는 화면 중 index번째로 포커스를 옮깁니다.
        /// </summary>
        private void FocusScreen(int index)
        {
            if (layoutSlots.Where(n => n != null).ElementAtOrDefault(index) is not { } name) return;
            lastFocusedOtt = name;
            UpdateNavState();
            if (dicWebViews.TryGetValue(name, out var wv)) wv.Focus();
        }

        /// <summary>
        /// 화면에 보이는 칸의 시청 시간을 기록합니다. 창이 숨겨져 있으면 기록하지 않습니다.
        /// </summary>
        private void RecordWatchTime()
        {
            if (!this.Visible || this.WindowState == FormWindowState.Minimized) return;
            foreach (var (name, cell) in dicCells)
                if (cell.Visible) WatchStats.Add(currentUserId, name, STATS_INTERVAL);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 시작 확인 / 창 상태
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// WebView2 런타임이 없으면 설치 페이지를 안내하고 앱을 종료합니다.
        /// </summary>
        /// <returns>런타임이 있으면 true (bool)</returns>
        private bool CheckWebView2Runtime()
        {
            try
            {
                CoreWebView2Environment.GetAvailableBrowserVersionString();
                return true;
            }
            catch (WebView2RuntimeNotFoundException)
            {
                if (MessageBox.Show(
                        "화면을 표시하려면 Microsoft Edge WebView2 런타임이 필요합니다.\n설치 페이지를 여시겠습니까?",
                        "WebView2 필요", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo(
                        "https://developer.microsoft.com/microsoft-edge/webview2/") { UseShellExecute = true });
                Application.Exit();
                return false;
            }
        }

        /// <summary>
        /// 로그인 후 창을 지난번 위치로 복원합니다. 저장값이 없거나 연결된 화면 밖이면 기본 크기로 엽니다.
        /// </summary>
        private void RestoreWindowState()
        {
            var st   = AppSettings.Current;
            var rect = new Rectangle(st.X, st.Y, st.Width, st.Height);
            if (st.Width >= 820 && st.Height >= 520 &&
                Screen.AllScreens.Any(sc => sc.WorkingArea.IntersectsWith(rect)))
            {
                this.Bounds = rect;
                if (st.Maximized) this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.Size = new Size(1300, 860);
                this.CenterToScreen();
            }
        }

        /// <summary>
        /// 종료할 때 창 위치를 저장합니다. 로그인 전과 전체화면 중에는 저장하지 않습니다.
        /// </summary>
        private void SaveWindowState()
        {
            if (currentUserIdx < 0 || fullScreenView != null) return;
            var st = AppSettings.Current;
            var b  = this.WindowState == FormWindowState.Normal ? this.Bounds : this.RestoreBounds;
            (st.X, st.Y, st.Width, st.Height) = (b.X, b.Y, b.Width, b.Height);
            st.Maximized = this.WindowState == FormWindowState.Maximized;
            st.Save();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 전체화면 / 최대화 / 리사이즈
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 전체화면 모드로 전환합니다.
        /// 타이틀바와 사이드바를 숨기고 창을 최대화합니다.
        /// OTT 웹사이트가 자체 전체화면 버튼을 누를 때 자동으로 호출됩니다.
        /// 멀티뷰 중이어도 전체화면을 요청한 화면만 크게 표시합니다.
        /// </summary>
        /// <param name="wv">전체화면을 요청한 WebView2</param>
        private void EnterFullScreen(WebView2 wv)
        {
            if (fullScreenView != null) return;
            fullScreenView = wv;
            stateBeforeFullScreen = this.WindowState;
            pnlTitleBar!.Visible = false;
            pnlSideMenu!.Dock    = DockStyle.None;
            pnlSideMenu!.Visible = false;
            this.WindowState = FormWindowState.Normal;
            this.WindowState = FormWindowState.Maximized;
            ReorganizeWebViews();
        }
        /// <summary>
        /// 전체화면 모드를 해제합니다.
        /// 창을 일반 크기로 복원하고 타이틀바, 사이드바를 다시 표시합니다.
        /// ESC 키 또는 OTT 사이트가 전체화면을 해제할 때 호출됩니다.
        /// </summary>
        private void ExitFullScreen()
        {
            if (fullScreenView == null) return;
            fullScreenView = null;
            this.WindowState = stateBeforeFullScreen;
            pnlTitleBar!.Visible = true;
            if (isMenuExpanded) { pnlSideMenu!.Dock = DockStyle.Left; pnlSideMenu!.Visible = true; }
            ReorganizeWebViews();
        }
        /// <summary>
        /// 창 최대화/일반 크기를 토글합니다.
        /// 최대화 상태면 일반 크기로, 일반 크기면 최대화합니다.
        /// </summary>
        private void ToggleMaximize()
        {
            this.WindowState = this.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        /// <summary>
        /// Windows 메시지를 가로채서 창 오른쪽 아래 모서리 드래그 리사이즈를 구현합니다.
        /// FormBorderStyle.None 이므로 운영체제 기본 리사이즈가 없어서 직접 처리해야 합니다.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84)
            {
                var p = this.PointToClient(new Point(unchecked((int)m.LParam.ToInt64())));
                if (p.X >= this.ClientSize.Width - 15 && p.Y >= this.ClientSize.Height - 15)
                { m.Result = (IntPtr)17; return; }
            }
            base.WndProc(ref m);
        }
    }
}
