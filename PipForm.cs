// ============================================================
// PipForm.cs — Picture-in-Picture(PiP) 플로팅 창
//
// 이 창이 하는 일:
//   - 선택한 OTT 사이트를 작은 창으로 띄워 항상 위에 표시
//   - 타이틀바를 드래그해서 창을 원하는 곳으로 이동
//   - 창 가장자리를 끌어서 크기 조절 (모든 방향)
//   - ▲(앱 위) / ●(중간) / ▼(앱 아래) 버튼으로 레이어 설정
//   - WebView2로 실제 OTT 웹사이트를 화면 안에 표시
// ============================================================
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PersonalPro
{
    // Form = 윈도우 창의 기본 클래스. 여기서 상속받아 PiP 창을 만듦
    public class PipForm : Form
    {
        // ── Windows API (운영체제 기능 직접 호출) ─────────────────────
        // DllImport = C# 코드에서 Windows 내부 함수를 가져다 쓰는 방법
        // ReleaseCapture = 마우스 잡기를 해제해서 창 이동 가능하게 함
        [DllImport("user32.dll")] static extern bool   ReleaseCapture();
        // SendMessage = 운영체제에 특정 메시지를 전달하는 함수
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        // SetWindowPos = 창의 z-순서(앞/뒤 레이어)를 바꾸는 함수
        [DllImport("user32.dll")] static extern bool   SetWindowPos(IntPtr hWnd, IntPtr hWndAfter, int x, int y, int cx, int cy, uint flags);

        // ── SetWindowPos에 쓰는 특수 핸들 값 ─────────────────────────
        // IntPtr = 포인터(주소)를 담는 타입 (Windows API에서 자주 씀)
        static readonly IntPtr HWND_TOP     = new IntPtr( 0);  // 맨 위 (보통 창)
        static readonly IntPtr HWND_BOTTOM  = new IntPtr( 1);  // 맨 아래
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);  // 항상 최상단 (다른 앱 위)
        static readonly IntPtr HWND_NOTOP   = new IntPtr(-2);  // 항상 최상단 해제

        // SetWindowPos 플래그 — 이동·크기 변경 없이 레이어만 바꿀 때 씀
        const uint SWP_NOMOVE = 0x0002;  // 위치 유지
        const uint SWP_NOSIZE = 0x0001;  // 크기 유지

        // ── 리사이즈 설정 ─────────────────────────────────────────────
        // int = 정수 타입
        // 창 가장자리에서 이 두께(픽셀) 안에 마우스가 있으면 리사이즈 영역으로 인식
        private const int EDGE  = 6;
        // 타이틀바의 높이 (픽셀)
        private const int TITLE = 32;

        // ── 레이어 모드 열거형 ────────────────────────────────────────
        // enum = 정해진 선택지 목록 타입
        private enum LayerMode
        {
            Top,    // 앱 위 — 항상 모든 창 위에 표시
            Normal, // 중간 — 일반 창처럼 동작
            Bottom  // 앱 아래 — 항상 다른 창 뒤에 표시
        }

        // 현재 레이어 모드 저장 (기본값: Top = 항상 위)
        private LayerMode _layer = LayerMode.Top;

        // ── 컨트롤 필드 ───────────────────────────────────────────────
        // WebView2 = 창 안에 웹 브라우저를 넣어주는 컴포넌트
        // ? = null(아직 없음)일 수 있다는 표시
        private WebView2? _wv;

        // 레이어 버튼들 (▲ ● ▼) — 나중에 색상 업데이트할 때 필요해서 보관
        private Button? _btnTop, _btnMid, _btnBot;

        // 음소거 버튼 (♪)
        private Button? _btnMute;

        // OTT 이름, 주소, 사용자 아이디 — 생성자에서 받아서 저장
        // readonly = 처음 설정 후 바꿀 수 없음
        private readonly string _name;    // OTT 이름 (예: "NETFLIX")
        private readonly string _url;     // OTT 주소 (예: "https://www.netflix.com")
        private readonly string _userId;  // 사용자 아이디 (WebView2 데이터 폴더에 씀)

        // ── 색상 팔레트 (다크 테마) ───────────────────────────────────
        // Color = 색상을 나타내는 타입. FromArgb(R, G, B)로 RGB 색상 지정
        // static readonly = 클래스 전체에서 공유하는 상수 색상
        static readonly Color C_BG     = Color.FromArgb(18, 18, 26);   // 창 배경 (아주 어두운 남색)
        static readonly Color C_TITLE  = Color.FromArgb(28, 28, 38);   // 타이틀바 배경
        static readonly Color C_BTN    = Color.FromArgb(55, 55, 72);   // 버튼 hover 색
        static readonly Color C_ACTIVE = Color.FromArgb(229, 9, 20);   // 활성화된 버튼 색 (빨간색)
        static readonly Color C_RED    = Color.FromArgb(229, 9, 20);   // 닫기 버튼 hover 색
        static readonly Color C_TEXT   = Color.FromArgb(180, 180, 195);// 기본 텍스트 색 (밝은 회색)

        // ── 생성자 ────────────────────────────────────────────────────
        // 생성자 = new PipForm(...) 로 만들 때 실행되는 메서드
        // name: OTT 이름, url: OTT 주소, userId: 사용자 아이디
        public PipForm(string name, string url, string userId)
        {
            _name   = name;
            _url    = url;
            _userId = userId;

            BuildUI();           // 화면 구성 요소 만들기
            _ = InitWebViewAsync(); // WebView2 초기화 (비동기, 결과 무시)
        }

        // ── UI 구성 ───────────────────────────────────────────────────

        /// <summary>
        /// 창의 모든 시각적 요소(타이틀바, 버튼 등)를 만들고 배치합니다.
        /// </summary>
        private void BuildUI()
        {
            // FormBorderStyle.None = 기본 윈도우 테두리 없애기 (직접 디자인)
            FormBorderStyle = FormBorderStyle.None;
            // 창 초기 크기: 480×302 픽셀 (16:9 비율)
            Size            = new Size(480, 302);
            // 최소 크기 제한 — 이보다 작게 줄일 수 없음
            MinimumSize     = new Size(240, 166);
            BackColor       = C_BG;      // 창 배경색 설정
            TopMost         = true;      // 기본값: 항상 다른 앱 위에 표시
            StartPosition   = FormStartPosition.Manual;  // 위치를 코드로 직접 지정
            DoubleBuffered  = true;      // 화면 깜빡임 방지

            // 창 초기 위치: 화면 오른쪽 아래 구석
            // Screen.FromPoint = 마우스가 있는 화면 정보, WorkingArea = 작업표시줄 제외 영역
            var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(wa.Right - Width - 16, wa.Bottom - Height - 16);

            // ── 타이틀바 패널 (높이 32px) ─────────────────────────────
            var titleBar = new Panel { Dock = DockStyle.Top, Height = TITLE, BackColor = C_TITLE };

            // 타이틀바 클릭·드래그로 창 이동
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();                         // 마우스 잡기 해제
                    SendMessage(Handle, 0xA1, 0x2, 0);       // 창 이동 메시지 전송
                }
            };

            // OTT 이름 표시 라벨 (예: "PiP  ·  NETFLIX")
            var lblName = new Label
            {
                Text      = $"PiP  ·  {_name}",   // $ = 문자열 안에 변수 값 넣기
                ForeColor = C_TEXT,
                Font      = new Font("Segoe UI", 8.5f),
                AutoSize  = true,    // 텍스트 길이에 맞게 자동 크기 조절
                Location  = new Point(10, 8)
            };

            // 닫기 버튼 (✕)
            var btnClose = MakeTitleBtn("✕", 32);
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = C_RED;   btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = C_TITLE; btnClose.ForeColor = C_TEXT; };
            btnClose.Click      += (s, e) => Close();  // 클릭 시 창 닫기

            // ── 레이어 선택 버튼 3개 ──────────────────────────────────
            // Dock=Right로 오른쪽부터 쌓임 — 닫기 버튼 왼쪽에 나란히 표시
            _btnBot = MakeTitleBtn("▼", 28);  // 앱 아래 버튼
            _btnMid = MakeTitleBtn("●", 28);  // 중간 버튼
            _btnTop = MakeTitleBtn("▲", 28);  // 앱 위 버튼

            // 각 버튼에 툴팁(마우스 올렸을 때 설명) 추가
            new ToolTip().SetToolTip(_btnTop, "앱 위 (항상 최상단)");
            new ToolTip().SetToolTip(_btnMid, "중간 (일반)");
            new ToolTip().SetToolTip(_btnBot, "앱 아래");

            // 버튼 클릭 시 레이어 변경
            _btnTop.Click += (s, e) => SetLayer(LayerMode.Top);

            // 이 PiP 창만 음소거 (켜지면 빨간색)
            _btnMute = MakeTitleBtn("♪", 28);
            new ToolTip().SetToolTip(_btnMute, "음소거");
            _btnMute.Click += (s, e) =>
            {
                if (_wv?.CoreWebView2 == null) return;
                _wv.CoreWebView2.IsMuted = !_wv.CoreWebView2.IsMuted;
                _btnMute.ForeColor = _wv.CoreWebView2.IsMuted ? C_ACTIVE : C_TEXT;
            };
            _btnMid.Click += (s, e) => SetLayer(LayerMode.Normal);
            _btnBot.Click += (s, e) => SetLayer(LayerMode.Bottom);

            // 앱 아래 모드일 때 이 창이 활성화되면 즉시 다시 맨 뒤로 밀어넣기
            Activated += (s, e) =>
            {
                if (_layer == LayerMode.Bottom)
                    SetWindowPos(Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            };

            // 타이틀바에 버튼 추가 (Dock=Right 순서: 먼저 추가 = 가장 오른쪽)
            titleBar.Controls.Add(btnClose);  // 가장 오른쪽
            titleBar.Controls.Add(_btnBot);
            titleBar.Controls.Add(_btnMid);
            titleBar.Controls.Add(_btnTop);
            titleBar.Controls.Add(_btnMute);  // 가장 왼쪽 (닫기 버튼 기준)
            titleBar.Controls.Add(lblName);
            Controls.Add(titleBar);

            // 창 크기 바뀔 때마다 WebView2도 같이 크기 조절
            SizeChanged += (s, e) => ResizeWebView();

            // 창 닫힐 때 WebView2 메모리 해제
            FormClosed += (s, e) => _wv?.Dispose();

            // 시청 시간 기록 (30초마다)
            var statsTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            statsTimer.Tick += (s, e) => WatchStats.Add(_userId, _name, 30);
            statsTimer.Start();
            FormClosed += (s, e) => statsTimer.Dispose();

            // 처음 시작할 때 ▲(앱 위) 버튼을 활성(빨간색)으로 표시
            UpdateLayerButtons();
        }

        // ── 레이어 전환 ────────────────────────────────────────────────

        /// <summary>
        /// 창의 레이어(앞뒤 순서)를 변경하고 버튼 색상을 업데이트합니다.
        /// </summary>
        /// <param name="mode">LayerMode 열거형: Top(위) / Normal(중간) / Bottom(아래)</param>
        private void SetLayer(LayerMode mode)
        {
            _layer = mode;  // 현재 레이어 상태 저장

            switch (mode)
            {
                case LayerMode.Top:
                    // TopMost = true → 항상 모든 창 위에 표시
                    TopMost = true;
                    break;

                case LayerMode.Normal:
                    // TopMost = false 먼저 해제한 뒤, HWND_NOTOP으로 일반 창으로 복귀
                    TopMost = false;
                    SetWindowPos(Handle, HWND_NOTOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    break;

                case LayerMode.Bottom:
                    // TopMost 해제 후 HWND_BOTTOM으로 모든 창 뒤로 이동
                    TopMost = false;
                    SetWindowPos(Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    break;
            }

            UpdateLayerButtons();  // 버튼 색상 갱신
        }

        /// <summary>
        /// 현재 레이어 모드에 해당하는 버튼을 빨간색으로, 나머지는 회색으로 표시합니다.
        /// </summary>
        private void UpdateLayerButtons()
        {
            // null 체크 — 버튼이 아직 만들어지지 않았으면 아무것도 안 함
            if (_btnTop == null) return;

            // 현재 모드와 일치하는 버튼 = 빨간색(활성), 나머지 = 회색(비활성)
            _btnTop.ForeColor = _layer == LayerMode.Top    ? C_ACTIVE : C_TEXT;
            _btnMid.ForeColor = _layer == LayerMode.Normal ? C_ACTIVE : C_TEXT;
            _btnBot.ForeColor = _layer == LayerMode.Bottom ? C_ACTIVE : C_TEXT;
        }

        // ── 버튼 팩토리 ────────────────────────────────────────────────

        /// <summary>
        /// 타이틀바용 버튼을 만들어 반환합니다 (Dock=Right 스타일).
        /// </summary>
        /// <param name="text">버튼에 표시할 텍스트 (string)</param>
        /// <param name="width">버튼 너비 픽셀 (int)</param>
        /// <returns>만들어진 Button 컨트롤</returns>
        private static Button MakeTitleBtn(string text, int width)
        {
            var b = new Button
            {
                Text      = text,
                Dock      = DockStyle.Right,  // 오른쪽부터 쌓이게
                Width     = width,
                FlatStyle = FlatStyle.Flat,   // 테두리 없는 납작한 스타일
                ForeColor = C_TEXT,
                BackColor = C_TITLE,
                Font      = new Font("Segoe UI", 8f),
                Cursor    = Cursors.Hand      // 마우스 올리면 손가락 모양
            };
            b.FlatAppearance.BorderSize         = 0;      // 테두리 두께 0
            b.FlatAppearance.MouseOverBackColor = C_BTN;  // hover 시 배경색
            return b;
        }

        // ── WebView2 크기 조절 ─────────────────────────────────────────

        /// <summary>
        /// 창 크기에 맞게 WebView2의 위치와 크기를 계산해서 다시 설정합니다.
        /// EDGE(6px) 여백을 남겨서 마우스로 가장자리를 잡아 리사이즈할 수 있게 합니다.
        /// </summary>
        private void ResizeWebView()
        {
            if (_wv == null) return;  // WebView2가 아직 없으면 아무것도 안 함

            // SetBounds(x, y, 너비, 높이) = 위치와 크기를 한 번에 설정
            // EDGE만큼 안쪽으로 들여쓰기 → 가장자리 6px가 리사이즈 영역으로 남음
            _wv.SetBounds(EDGE, TITLE, Width - EDGE * 2, Height - TITLE - EDGE);
        }

        // ── WebView2 초기화 ────────────────────────────────────────────

        /// <summary>
        /// WebView2(내장 브라우저)를 비동기로 초기화하고 OTT 사이트를 엽니다.
        /// async Task = 비동기 메서드 (시간이 걸리는 작업을 기다리는 동안 화면이 멈추지 않음)
        /// </summary>
        private async Task InitWebViewAsync()
        {
            // WebView2 컨트롤 생성 — 타이틀바 아래, EDGE 여백 안쪽에 배치
            _wv = new WebView2
            {
                Location               = new Point(EDGE, TITLE),
                Size                   = new Size(Width - EDGE * 2, Height - TITLE - EDGE),
                DefaultBackgroundColor = Color.White  // 브라우저 기본 배경색 (배경 미지정 사이트가 검게 보이지 않도록)
            };
            Controls.Add(_wv);  // 폼에 WebView2 추가

            // 데이터 폴더 경로: 메인 창과 같은 폴더 사용 → 로그인 세션이 공유됨
            // LocalApplicationData = "C:\Users\사용자\AppData\Local"
            // 예: C:\Users\나\AppData\Local\PersonalPro\내아이디\NETFLIX
            string dir = DatabaseHelper.DataPath(_userId, _name.Replace(" ", "_"));

            try
            {
                // await = 이 작업이 끝날 때까지 기다리기 (화면은 계속 반응)
                var env = await CoreWebView2Environment.CreateAsync(null, dir, BrowserExtensions.Options());
                await _wv.EnsureCoreWebView2Async(env);
                await BrowserExtensions.LoadAsync(_wv.CoreWebView2);

                // OTT 사이트 열기
                _wv.Source = new Uri(_url);   // Uri = 웹 주소(URL)를 나타내는 타입
            }
            catch (Exception ex)
            {
                // 초기화 중 창을 닫은 경우에는 알릴 필요 없음
                if (IsDisposed) return;
                MessageBox.Show($"'{_name}' 화면을 열지 못했습니다.\n{ex.Message}", "화면 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
            }
        }

        // ── 리사이즈 처리 (Windows 메시지 가로채기) ──────────────────

        /// <summary>
        /// Windows 메시지를 가로채서 창 가장자리 리사이즈를 구현합니다.
        ///
        /// WM_NCHITTEST(0x84) = 마우스가 창의 어느 부분에 있는지 물어보는 메시지
        /// 운영체제가 이 메시지를 보내면 "여기는 리사이즈 영역이야"라고 응답함
        ///
        /// WebView2가 Dock=Fill이 아닌 EDGE 여백 안쪽에 있어서
        /// 창 가장자리 6px 영역에는 폼이 직접 메시지를 받을 수 있음
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84)  // WM_NCHITTEST 메시지
            {
                // 마우스 좌표를 창 기준 좌표로 변환
                var p = PointToClient(new Point(unchecked((int)m.LParam.ToInt64())));
                int w = ClientSize.Width;
                int h = ClientSize.Height;

                // 마우스가 각 가장자리 EDGE px 안에 있는지 확인 (bool = 참/거짓)
                bool l = p.X < EDGE;        // 왼쪽 가장자리
                bool r = p.X > w - EDGE;    // 오른쪽 가장자리
                bool t = p.Y < EDGE;        // 위쪽 가장자리
                bool b = p.Y > h - EDGE;    // 아래쪽 가장자리

                // 모서리와 가장자리별로 리사이즈 방향 코드 반환
                // IntPtr = Windows API에서 사용하는 포인터/숫자 타입
                if      (l && t) { m.Result = (IntPtr)13; return; } // 왼쪽 위 모서리
                else if (r && t) { m.Result = (IntPtr)14; return; } // 오른쪽 위 모서리
                else if (l && b) { m.Result = (IntPtr)16; return; } // 왼쪽 아래 모서리
                else if (r && b) { m.Result = (IntPtr)17; return; } // 오른쪽 아래 모서리
                else if (l)      { m.Result = (IntPtr)10; return; } // 왼쪽 가장자리
                else if (r)      { m.Result = (IntPtr)11; return; } // 오른쪽 가장자리
                else if (t)      { m.Result = (IntPtr)12; return; } // 위쪽 가장자리
                else if (b)      { m.Result = (IntPtr)15; return; } // 아래쪽 가장자리
            }

            // 다른 메시지는 기본 처리에 위임
            base.WndProc(ref m);
        }
    }
}
