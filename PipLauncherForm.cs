// ============================================================
// PipLauncherForm.cs — PiP 모드 OTT 선택 창
//
// 이 창이 하는 일:
//   - PiP 버튼을 누르면 메인 창 대신 이 작은 창이 나타남
//   - 사이드바처럼 OTT 목록을 보여줌
//   - OTT를 클릭하면 그 OTT가 PipForm(PiP 창)으로 열림
//   - 여러 OTT를 동시에 PiP 창으로 열 수 있음
//   - "← 메인으로" 버튼을 누르면 메인 창으로 돌아감
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PersonalPro
{
    // Form = 윈도우 창의 기본 클래스
    public class PipLauncherForm : Form
    {
        // ── Windows API ───────────────────────────────────────────────
        // 타이틀바 드래그로 창 이동에 필요한 Windows 함수
        [DllImport("user32.dll")] static extern bool   ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        // ── 색상 팔레트 (메인 창과 동일한 밝은 테마) ─────────────────
        // Color = 색상 타입. FromArgb(R, G, B)로 원하는 색 만들기
        static readonly Color C_WHITE     = Color.White;
        static readonly Color C_RED       = Color.FromArgb(229, 9, 20);    // 브랜드 빨간색
        static readonly Color C_RED_TINT  = Color.FromArgb(255, 242, 242); // 연한 분홍 (사용자 헤더 배경)
        static readonly Color C_TEXT      = Color.FromArgb(17, 24, 39);    // 진한 텍스트
        static readonly Color C_TEXT_MID  = Color.FromArgb(75, 85, 99);    // 중간 회색 텍스트
        static readonly Color C_TEXT_GRAY = Color.FromArgb(156, 163, 175); // 연한 회색 텍스트
        static readonly Color C_BORDER    = Color.FromArgb(229, 231, 235); // 테두리 색
        static readonly Color C_HOVER     = Color.FromArgb(243, 244, 246); // 마우스 올렸을 때 배경

        // ── 앱 상태 필드 ──────────────────────────────────────────────
        // int = 정수 타입. 사용자 DB 번호
        private readonly int    _userIdx;
        // string = 문자열 타입. 사용자 로그인 아이디 (WebView2 폴더에 씀)
        private readonly string _userId;
        // 사용자 닉네임 (헤더에 표시)
        private readonly string _nickname;
        // OTT 버튼들이 들어가는 스크롤 패널
        // Panel? = Panel 타입인데 null(아직 없음)일 수 있음
        private Panel?   _pnlOttButtons;
        // 마지막으로 서버에서 받은 사용자 OTT 목록
        private List<string[]> _userOtts = new();
        // 사용자 OTT 우클릭 메뉴 (수정/삭제)
        private readonly ContextMenuStrip _cmsMenu = new() { Font = new Font("Segoe UI", 9f) };

        // ── 파비콘 로드용 HTTP 클라이언트 ────────────────────────────
        // HttpClient = 인터넷 요청 도구. 5초 타임아웃
        private static readonly HttpClient FaviconHttp = new() { Timeout = TimeSpan.FromSeconds(5) };

        // ── 생성자 ────────────────────────────────────────────────────
        /// <summary>
        /// PiP 런처 창을 만듭니다.
        /// </summary>
        /// <param name="userIdx">사용자 번호 (int)</param>
        /// <param name="userId">사용자 아이디 (string)</param>
        /// <param name="nickname">사용자 닉네임 (string)</param>
        public PipLauncherForm(int userIdx, string userId, string nickname)
        {
            _userIdx  = userIdx;
            _userId   = userId;
            _nickname = nickname;

            BuildUI();  // 화면 요소 만들기
            Theme.Apply(this);

            // Shown = 창이 처음 화면에 나타난 직후 발생하는 이벤트
            // 이때 OTT 목록을 불러오면 패널 너비가 정확해서 버튼 크기가 올바르게 설정됨
            Shown += async (s, e) => await RefreshOttList();

            _cmsMenu.Items.Add("수정", null, (s, e) => EditSelectedOtt());
            _cmsMenu.Items.Add("삭제", null, (s, e) => DeleteSelectedOtt());
        }

        // ── UI 구성 ───────────────────────────────────────────────────

        /// <summary>
        /// 창의 모든 시각적 요소를 만들고 배치합니다.
        /// </summary>
        private void BuildUI()
        {
            FormBorderStyle = FormBorderStyle.None;   // 기본 창 테두리 제거
            Size            = new Size(230, 500);     // 초기 크기: 사이드바 폭과 동일
            MinimumSize     = new Size(230, 300);     // 최소 높이 제한
            BackColor       = C_WHITE;
            TopMost         = false;   // 일반 레이어 (다른 앱과 같은 층)
            StartPosition   = FormStartPosition.Manual;  // 위치를 코드로 직접 지정
            DoubleBuffered  = true;    // 화면 깜빡임 방지

            // 화면 오른쪽 아래에 위치
            var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(wa.Right - Width - 16, wa.Bottom - Height - 16);

            // 오른쪽 경계선 그리기 (Paint 이벤트 = 창이 다시 그려질 때마다 호출)
            Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Theme.C(C_BORDER)), Width - 1, 0, Width - 1, Height);

            // ── 타이틀바 (32px) ────────────────────────────────────────
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = C_WHITE };
            // 타이틀바 하단 구분선
            titleBar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Theme.C(C_BORDER)), 0, 31, titleBar.Width, 31);
            // 드래그로 창 이동
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); }
            };

            // "PiP 모드" 제목 텍스트
            var lblTitle = new Label
            {
                Text      = "PiP 모드",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = C_TEXT,
                AutoSize  = true,
                Location  = new Point(12, 8)
            };

            // ✕ 닫기 버튼 — 누르면 런처 닫히고 메인 창으로 돌아감
            var btnClose = MakeBtn("✕", 32, DockStyle.Right);
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = C_RED;   btnClose.ForeColor = C_WHITE; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Theme.C(C_WHITE); btnClose.ForeColor = Theme.C(C_TEXT_MID); };
            btnClose.Click      += (s, e) => Close();  // 창 닫기

            titleBar.Controls.Add(btnClose);
            titleBar.Controls.Add(lblTitle);

            // ── 사용자 정보 헤더 (60px) ────────────────────────────────
            var pnlUser = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = C_RED_TINT };
            // 헤더 하단 구분선
            pnlUser.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Theme.C(C_BORDER)), 0, 59, pnlUser.Width, 59);

            // 원형 아바타 — Paint 이벤트로 직접 그림 (빨간 원 + 흰색 O 글자)
            var pnlAvatar = new Panel { Location = new Point(12, 12), Size = new Size(36, 36), BackColor = Color.Transparent };
            pnlAvatar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;  // 부드러운 곡선 렌더링
                g.FillEllipse(new SolidBrush(C_RED), 0, 0, 35, 35);  // 빨간 원 채우기
                using var f  = new Font("Arial", 13f, FontStyle.Bold);
                // StringFormat = 텍스트 정렬 방식 설정
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("O", f, Brushes.White, new RectangleF(0, 0, 35, 35), sf);
            };

            // "닉네임님 · PiP 모드" 텍스트
            var lblNick = new Label
            {
                Text      = $"{_nickname}님 · PiP 모드",
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(158, 18, 18),  // 진한 빨간색
                Location  = new Point(56, 20),
                AutoSize  = true
            };
            pnlUser.Controls.AddRange(new Control[] { pnlAvatar, lblNick });

            // ── OTT 버튼 목록 영역 (나머지 공간 전체, 스크롤 가능) ────
            _pnlOttButtons = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = C_WHITE };

            // ── 하단 "← 메인으로" 버튼 (44px) ────────────────────────
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = C_WHITE };
            // 상단 구분선
            pnlBottom.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Theme.C(C_BORDER)), 0, 0, pnlBottom.Width, 0);

            var btnBack = MakeBtn("← 메인으로", 0, DockStyle.Fill);
            btnBack.ForeColor = C_TEXT_MID;
            btnBack.Font      = new Font("Segoe UI", 8.5f);
            btnBack.TextAlign = ContentAlignment.MiddleCenter;
            btnBack.Click    += (s, e) => Close();  // 닫으면 MainForm에서 Show() 됨
            pnlBottom.Controls.Add(btnBack);

            // ── 조립 순서 ─────────────────────────────────────────────
            // 나중에 추가한 컨트롤이 위쪽 Dock=Top을 먼저 차지함
            Controls.Add(_pnlOttButtons);  // Dock=Fill — 남은 공간 전체
            Controls.Add(pnlBottom);       // Dock=Bottom
            Controls.Add(pnlUser);         // Dock=Top (아래)
            Controls.Add(titleBar);        // Dock=Top (맨 위)
        }

        // ── OTT 목록 갱신 ─────────────────────────────────────────────

        /// <summary>
        /// OTT 버튼 목록을 서버에서 다시 불러와서 화면을 새로고침합니다.
        /// 기본 OTT + 사용자가 추가한 OTT들 + "OTT 추가" 버튼 순서로 표시됩니다.
        /// 서버 통신에 실패하면 마지막으로 받은 목록을 유지합니다.
        /// </summary>
        private async Task RefreshOttList()
        {
            if (_pnlOttButtons == null) return;

            var fetched = await Task.Run(() => DatabaseHelper.GetUserOtts(_userIdx));
            if (IsDisposed) return;  // 불러오는 중 창을 닫은 경우
            if (fetched != null) _userOtts = fetched;
            else MessageBox.Show("OTT 목록을 불러오지 못했습니다. 인터넷 연결을 확인해주세요.", "목록 오류",
                     MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // 기존 버튼 모두 지우고 다시 만들기
            _pnlOttButtons.Controls.Clear();

            // 기본 OTT와 사용자 OTT를 저장된 순서대로 추가 (기본 OTT는 삭제/수정 불가)
            // item[0]=DB인덱스, item[1]=이름, item[2]=URL
            foreach (var item in OttInput.AllOtts(_userId, _userOtts))
                AddOttBtn(item[1], item[2], item[0] != "", item[0]);

            // ── "＋ OTT 추가" 버튼 ────────────────────────────────────
            var btnAdd = new Button
            {
                Text      = "＋  OTT 추가",
                // y 좌표: 현재 버튼 개수 × 50 + 여백 8px
                Location  = new Point(0, _pnlOttButtons.Controls.Count * 50 + 8),
                Size      = new Size(_pnlOttButtons.Width, 42),
                FlatStyle = FlatStyle.Flat,
                ForeColor = C_TEXT_GRAY,
                BackColor = C_WHITE,
                Font      = new Font("Segoe UI", 8.5f),
                Cursor    = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0),
                // Anchor = 창 크기 바뀌어도 버튼이 늘어나게 고정
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnAdd.FlatAppearance.BorderSize         = 0;
            btnAdd.FlatAppearance.MouseOverBackColor = C_HOVER;

            // async = 비동기 이벤트 핸들러 (서버 요청 동안 화면이 멈추지 않음)
            btnAdd.Click += async (s, e) =>
            {
                if (OttInput.Prompt("OTT 추가", "", "https://", _userOtts) is not { } o) return;

                // Task.Run = 서버 요청을 백그라운드 스레드에서 실행 (UI 안 멈춤)
                if (await Task.Run(() => DatabaseHelper.AddUserOtt(_userIdx, o.Name, o.Url)))
                    await RefreshOttList();  // 추가 후 목록 새로고침
                else OttInput.ShowSaveError();
            };
            _pnlOttButtons.Controls.Add(btnAdd);
            Theme.Apply(this);
        }

        /// <summary>
        /// OTT 버튼 하나를 만들어 목록에 추가합니다.
        /// 클릭하면 해당 OTT의 PipForm이 새로 열립니다.
        /// </summary>
        /// <param name="name">OTT 이름 (string)</param>
        /// <param name="url">OTT 주소 (string)</param>
        /// <param name="isCustom">사용자가 추가한 OTT면 true, 기본 OTT면 false (bool)</param>
        /// <param name="dbIdx">DB 인덱스 (string, 기본 OTT는 "" 빈 문자열)</param>
        private void AddOttBtn(string name, string url, bool isCustom, string dbIdx = "")
        {
            // 버튼 y 좌표: 현재 버튼 수 × 50px + 여백 8px
            int y = _pnlOttButtons!.Controls.Count * 50 + 8;

            var btn = new Button
            {
                // Tag = 버튼에 데이터를 보관하는 공간 (클릭 시 꺼내 씀)
                // string[] = 문자열 배열: [0]=URL, [1]=DB인덱스, [2]=이름
                Tag       = new string[] { url, dbIdx, name },
                Location  = new Point(0, y),
                Size      = new Size(_pnlOttButtons.Width, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = C_WHITE,
                ForeColor = C_TEXT,
                Font      = new Font("Segoe UI", 9f),
                Text      = $"  {name}",   // 앞에 공백 2칸으로 파비콘 자리 확보
                TextAlign = ContentAlignment.MiddleLeft,
                // 이미지(파비콘)가 있으면 텍스트 왼쪽에 표시
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = C_HOVER;

            // 툴팁: 마우스 올렸을 때 OTT 이름 표시
            new ToolTip().SetToolTip(btn, name);

            // 사용자가 추가한 OTT만 수정/삭제 가능
            if (isCustom) btn.ContextMenuStrip = _cmsMenu;

            // 클릭 시 해당 OTT의 PiP 창 열기 (여러 개 동시 오픈 가능)
            btn.Click += (s, e) => new PipForm(name, url, _userId).Show();

            // 구글 파비콘 서비스로 아이콘 비동기 로드 (결과 무시 = _ =)
            _ = LoadFaviconAsync(btn, url);
            _pnlOttButtons.Controls.Add(btn);
        }

        /// <summary>
        /// 우클릭 메뉴 "수정": 새 이름과 URL을 입력받아 서버에 저장합니다.
        /// </summary>
        private async void EditSelectedOtt()
        {
            if (_cmsMenu.SourceControl is not Button btn) return;
            var d = (string[])btn.Tag!;
            if (OttInput.Prompt("수정", d[2], d[0], _userOtts) is not { } o) return;
            if (await Task.Run(() => DatabaseHelper.UpdateUserOtt(d[1], o.Name, o.Url)))
                await RefreshOttList();
            else OttInput.ShowSaveError();
        }

        /// <summary>
        /// 우클릭 메뉴 "삭제": 확인 후 서버에서 OTT를 삭제합니다.
        /// </summary>
        private async void DeleteSelectedOtt()
        {
            if (_cmsMenu.SourceControl is not Button btn) return;
            var d = (string[])btn.Tag!;
            if (MessageBox.Show($"'{d[2]}'을(를) 삭제하시겠습니까?", "삭제",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (await Task.Run(() => DatabaseHelper.DeleteUserOtt(d[1])))
                await RefreshOttList();
            else OttInput.ShowSaveError();
        }

        /// <summary>
        /// 구글 파비콘 서비스에서 아이콘 이미지를 다운로드해서 버튼에 설정합니다.
        /// async Task = 비동기 메서드 (다운로드 중에도 화면이 멈추지 않음)
        /// </summary>
        /// <param name="btn">아이콘을 붙일 버튼 (Button)</param>
        /// <param name="url">아이콘을 가져올 사이트 주소 (string)</param>
        private async Task LoadFaviconAsync(Button btn, string url)
        {
            try
            {
                // Uri(url).Host = 주소에서 도메인만 추출 (예: "www.netflix.com")
                string host  = new Uri(url).Host;

                // 구글 파비콘 API에서 32×32 크기 아이콘 다운로드
                // byte[] = 바이트 배열 (이미지 데이터)
                byte[] bytes = await FaviconHttp.GetByteArrayAsync(
                    $"https://www.google.com/s2/favicons?sz=32&domain={host}");

                // this.Invoke = UI 스레드에서 실행 (다른 스레드에서 UI 건드리면 오류)
                this.Invoke(() =>
                {
                    // 버튼이 이미 닫혔으면(Disposed) 처리하지 않음
                    if (!btn.IsDisposed)
                    {
                        // MemoryStream = 메모리를 파일처럼 쓰는 스트림
                        using var ms = new MemoryStream(bytes);
                        // 16×16으로 리사이즈해서 버튼 이미지로 설정
                        btn.Image    = new Bitmap(new Bitmap(ms), new Size(16, 16));
                        btn.Padding  = new Padding(10, 0, 0, 0);  // 아이콘 왼쪽 여백
                    }
                });
            }
            catch { }  // 파비콘 로드 실패 시 무시 (아이콘 없어도 동작)
        }

        // ── 버튼 팩토리 ────────────────────────────────────────────────

        /// <summary>
        /// 지정한 스타일의 버튼을 만들어 반환합니다.
        /// </summary>
        /// <param name="text">버튼 텍스트 (string)</param>
        /// <param name="width">버튼 너비 — DockStyle.Right 에만 적용 (int)</param>
        /// <param name="dock">버튼 Dock 방식 (DockStyle 열거형)</param>
        /// <returns>만들어진 Button 컨트롤</returns>
        private static Button MakeBtn(string text, int width, DockStyle dock)
        {
            var b = new Button
            {
                Text      = text,
                Dock      = dock,
                Width     = width,
                FlatStyle = FlatStyle.Flat,
                ForeColor = C_TEXT_MID,
                BackColor = C_WHITE,
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = C_HOVER;
            return b;
        }

        // ── 높이 리사이즈 (WndProc) ───────────────────────────────────

        /// <summary>
        /// Windows 메시지를 가로채서 창 위/아래 가장자리 드래그 리사이즈를 구현합니다.
        /// 이 창은 너비를 230px로 고정하고 높이만 조절 가능합니다.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84)  // WM_NCHITTEST
            {
                var p = PointToClient(new Point(unchecked((int)m.LParam.ToInt64())));

                // 하단 8px 안 → 아래쪽 리사이즈 영역 (HTBOTTOM = 15)
                if (p.Y > Height - 8) { m.Result = (IntPtr)15; return; }
                // 상단 8px 안 → 위쪽 리사이즈 영역 (HTTOP = 12)
                if (p.Y < 8)          { m.Result = (IntPtr)12; return; }
            }
            base.WndProc(ref m);
        }
    }
}
