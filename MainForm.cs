using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;
using System.Linq;

namespace PersonalPro
{
    public partial class MainForm : Form
    {
        private Panel? pnlTitleBar, pnlSideMenu, pnlContent, pnlOttButtons, pnlLoginArea;
        private TextBox? txtId, txtPw;
        private Button? btnLogin, btnRegister, btnLogout, btnToggleMenu, btnMaximize, btnClose, btnUpdate, btnClearLayout, btnHelp, btnSaveLayout, btnLoadLayout;

        // 내비게이션 버튼 및 추적 변수
        private Button? btnNavBack, btnNavRefresh;
        private string lastFocusedOtt = "";

        private Label? lblWelcome;
        private ContextMenuStrip? cmsMenu;

        private Dictionary<string, WebView2> dicWebViews = new Dictionary<string, WebView2>();
        private string?[] layoutSlots = new string?[4];
        private int currentUserIdx = -1;
        private string currentUserId = "";
        private bool isMenuExpanded = true;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public MainForm()
        {
            DatabaseHelper.InitializeDatabase();
            InitializeComponent();
            InitContextMenu();
            InitCustomUi();
            this.Text = "OTT Multi-View";
            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) ExitFullScreen();
            };
        }

        private void InitContextMenu()
        {
            cmsMenu = new ContextMenuStrip();
            var mEdit = new ToolStripMenuItem("수정");
            var mDel = new ToolStripMenuItem("삭제");
            var mMute = new ToolStripMenuItem("음소거 토글");
            mEdit.Click += (s, e) => EditSelectedOtt();
            mDel.Click += (s, e) => DeleteSelectedOtt();
            mMute.Click += (s, e) => ToggleMuteSelected();
            cmsMenu.Items.AddRange(new ToolStripItem[] { mEdit, mDel, new ToolStripSeparator(), mMute });
        }

        private void InitCustomUi()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(350, 450);
            this.BackColor = Color.FromArgb(15, 15, 15);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;

            pnlTitleBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Black };
            pnlTitleBar.MouseDown += (s, e) => {
                if (e.Clicks == 2) ToggleMaximize();
                else { ReleaseCapture(); SendMessage(this.Handle, 0xA1, 0x2, 0); }
            };

            Label lblLogo = new Label { Text = "O", ForeColor = Color.Red, Font = new Font("Arial", 20, FontStyle.Bold), AutoSize = true, Location = new Point(10, 3) };
            btnToggleMenu = new Button { Text = "≡", Location = new Point(50, 5), Size = new Size(30, 30), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Visible = false };
            btnToggleMenu.FlatAppearance.BorderSize = 0;
            btnToggleMenu.Click += (s, e) => { isMenuExpanded = !isMenuExpanded; if (pnlSideMenu != null) pnlSideMenu.Visible = isMenuExpanded; };

            btnHelp = new Button { Text = "?", Location = new Point(85, 5), Size = new Size(30, 30), FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, Visible = false };
            btnHelp.FlatAppearance.BorderSize = 0;
            btnHelp.Click += (s, e) => ShowHelpForm();

            // 상단 내비게이션 버튼
            btnNavBack = new Button { Text = "◀", Location = new Point(120, 5), Size = new Size(30, 30), FlatStyle = FlatStyle.Flat, ForeColor = Color.DimGray, Visible = false };
            btnNavBack.FlatAppearance.BorderSize = 0;
            btnNavBack.Click += (s, e) => {
                if (!string.IsNullOrEmpty(lastFocusedOtt) && dicWebViews.TryGetValue(lastFocusedOtt, out var wv))
                    if (wv.CoreWebView2.CanGoBack) wv.CoreWebView2.GoBack();
            };

            btnNavRefresh = new Button { Text = "↻", Location = new Point(155, 5), Size = new Size(30, 30), FlatStyle = FlatStyle.Flat, ForeColor = Color.DimGray, Visible = false };
            btnNavRefresh.FlatAppearance.BorderSize = 0;
            btnNavRefresh.Click += (s, e) => {
                if (!string.IsNullOrEmpty(lastFocusedOtt) && dicWebViews.TryGetValue(lastFocusedOtt, out var wv))
                    wv.Reload();
            };

            btnClose = new Button { Text = "✕", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Application.Exit();

            btnMaximize = new Button { Text = "⬜", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray };
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.Click += (s, e) => ToggleMaximize();

            pnlTitleBar.Controls.AddRange(new Control[] { lblLogo, btnToggleMenu, btnHelp, btnNavBack, btnNavRefresh, btnMaximize, btnClose });

            pnlSideMenu = new Panel { Dock = DockStyle.Left, Width = 85, BackColor = Color.FromArgb(25, 25, 25), Visible = false };
            pnlOttButtons = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            btnLogout = new Button { Text = "나가기", Dock = DockStyle.Bottom, Height = 45, FlatStyle = FlatStyle.Flat, ForeColor = Color.DimGray, Font = new Font("맑은 고딕", 8) };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) => Application.Restart();

            lblWelcome = new Label { Text = "", Dock = DockStyle.Bottom, Height = 45, ForeColor = Color.White, Font = new Font("맑은 고딕", 8, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };

            btnUpdate = new Button { Text = "정보 수정", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, Font = new Font("맑은 고딕", 8) };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += (s, e) => {
                using (var form = new UpdateUserForm(currentUserIdx, DatabaseHelper.CurrentNickname))
                {
                    if (form.ShowDialog() == DialogResult.OK) if (lblWelcome != null) lblWelcome.Text = $"{DatabaseHelper.CurrentNickname}님\n환영합니다";
                }
            };

            btnLoadLayout = new Button { Text = "배치 복구", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.SpringGreen, Font = new Font("맑은 고딕", 8) };
            btnLoadLayout.FlatAppearance.BorderSize = 0;
            btnLoadLayout.Click += (s, e) => LoadPresetLayout();

            btnSaveLayout = new Button { Text = "배치 저장", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.LightSkyBlue, Font = new Font("맑은 고딕", 8) };
            btnSaveLayout.FlatAppearance.BorderSize = 0;
            btnSaveLayout.Click += (s, e) => SavePresetLayout();

            btnClearLayout = new Button { Text = "화면 비우기", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.IndianRed, Font = new Font("맑은 고딕", 8, FontStyle.Bold) };
            btnClearLayout.FlatAppearance.BorderSize = 0;
            btnClearLayout.Click += (s, e) => { Array.Clear(layoutSlots, 0, 4); lastFocusedOtt = ""; UpdateNavButtonColor(); ReorganizeWebViews(); };

            pnlSideMenu.Controls.AddRange(new Control[] { pnlOttButtons, btnClearLayout, btnSaveLayout, btnLoadLayout, btnUpdate, lblWelcome, btnLogout });

            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Visible = false };
            pnlContent.SizeChanged += (s, e) => ReorganizeWebViews();

            pnlLoginArea = new Panel { Size = new Size(300, 300), BackColor = Color.Transparent };
            txtId = new TextBox { Location = new Point(0, 60), Width = 300, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.Gray, Font = new Font("맑은 고딕", 12), BorderStyle = BorderStyle.FixedSingle, Text = "아이디", Tag = "아이디" };
            txtId.Enter += (s, e) => { if (txtId.Text == txtId.Tag?.ToString()) { txtId.Text = ""; txtId.ForeColor = Color.White; } };
            txtId.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtId.Text)) { txtId.Text = txtId.Tag?.ToString() ?? ""; txtId.ForeColor = Color.Gray; } };
            txtPw = new TextBox { Location = new Point(0, 110), Width = 300, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.Gray, Font = new Font("맑은 고딕", 12), BorderStyle = BorderStyle.FixedSingle, Text = "비밀번호", Tag = "비밀번호" };
            txtPw.Enter += (s, e) => { if (txtPw.Text == txtPw.Tag?.ToString()) { txtPw.Text = ""; txtPw.ForeColor = Color.White; txtPw.UseSystemPasswordChar = true; } };
            txtPw.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtPw.Text)) { txtPw.Text = txtPw.Tag?.ToString() ?? ""; txtPw.ForeColor = Color.Gray; txtPw.UseSystemPasswordChar = false; } };
            txtPw.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoginProcess(); };
            btnLogin = new Button { Text = "로그인", Location = new Point(0, 170), Size = new Size(300, 45), FlatStyle = FlatStyle.Flat, BackColor = Color.Red, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += (s, e) => LoginProcess();
            btnRegister = new Button { Text = "계정 만들기", Location = new Point(0, 225), Size = new Size(300, 30), FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += (s, e) => new RegisterForm().ShowDialog();
            pnlLoginArea.Controls.AddRange(new Control[] { txtId, txtPw, btnLogin, btnRegister });

            this.Controls.Add(pnlLoginArea);
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSideMenu);
            this.Controls.Add(pnlTitleBar);
            this.Load += (s, e) => { if (pnlLoginArea != null) pnlLoginArea.Location = new Point((this.Width - 300) / 2, (this.Height - 300) / 2); };
        }

        private void ShowHelpForm()
        {
            Form helpForm = new Form
            {
                Text = "도움말",
                Size = new Size(500, 450),
                BackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.White,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label lblHelp = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                Font = new Font("맑은 고딕", 10),
                TextAlign = ContentAlignment.TopLeft,
                ForeColor = Color.White,
                Text = "▶ [1. 자동 로그인 및 보안]\n최초 로그인 시 정보가 로컬 DB에 안전하게 저장됩니다.\n\n" +
                       "▶ [2. 멀티뷰 제어 안내]\n- 클릭: 단일 모드 / Ctrl+클릭: 다중 모드\n" +
                       "- 제어 버튼: 상단 [◀] [↻] 버튼은 마지막으로 '직접 클릭하여 조작한 화면'을 제어합니다.\n" +
                       "- 우클릭 메뉴: 사이드바 아이콘 우클릭으로 개별 음소거가 가능합니다.\n" +
                       "▶ [3. 제작자 정보]\n제작자: 안도영 / dksehvkf1206@naver.com"
            };
            helpForm.Controls.Add(lblHelp);
            helpForm.ShowDialog();
        }

        private async void LoginProcess()
        {
            if (txtId == null || txtPw == null || btnLogin == null) return;
            string idVal = txtId.Text == txtId.Tag?.ToString() ? "" : txtId.Text;
            string pwVal = txtPw.Text == txtPw.Tag?.ToString() ? "" : txtPw.Text;
            btnLogin.Enabled = false; btnLogin.Text = "연결 중...";
            int resultIdx = await Task.Run(() => DatabaseHelper.LoginCheckReturnIdx(idVal, pwVal));
            if (resultIdx != -1)
            {
                currentUserIdx = resultIdx; currentUserId = idVal;
                if (lblWelcome != null) lblWelcome.Text = $"{DatabaseHelper.CurrentNickname}님\n환영합니다";
                this.Size = new Size(1300, 850); this.CenterToScreen();
                pnlLoginArea!.Visible = false; pnlSideMenu!.Visible = true; pnlContent!.Visible = true;
                btnToggleMenu!.Visible = true; btnHelp!.Visible = true;
                btnNavBack!.Visible = true; btnNavRefresh!.Visible = true;
                RefreshOttList();
                LoadPresetLayout(true);
            }
            else { MessageBox.Show("실패"); btnLogin.Enabled = true; btnLogin.Text = "로그인"; }
        }

        private void RefreshOttList()
        {
            if (pnlOttButtons == null) return;
            pnlOttButtons.Controls.Clear();
            AddButtonUI("NETFLIX", "https://www.netflix.com/login", false);
            AddButtonUI("YOUTUBE", "https://www.youtube.com", false);
            AddButtonUI("LINKKF", "https://linkkf.app", false);
            var list = DatabaseHelper.GetUserOtts(currentUserIdx);
            foreach (var item in list) AddButtonUI(item[1], item[2], true, item[0]);

            int yPos = (pnlOttButtons.Controls.Count * 60) + 10;
            Button btnAdd = new Button { Text = "＋", Size = new Size(50, 50), Location = new Point(16, yPos), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(45, 45, 45) };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += async (s, e) => {
                string n = Interaction.InputBox("OTT 이름:", "추가");
                string u = Interaction.InputBox("URL 주소:", "추가", "https://");
                if (!string.IsNullOrEmpty(n) && u.StartsWith("http"))
                {
                    await Task.Run(() => DatabaseHelper.AddUserOtt(currentUserIdx, n, u));
                    RefreshOttList();
                }
            };
            pnlOttButtons.Controls.Add(btnAdd);
        }

        private void AddButtonUI(string name, string url, bool isCustom, string dbIdx = "")
        {
            int yPos = (pnlOttButtons!.Controls.Count * 60) + 10;
            Button btn = new Button { Tag = new string[] { url, dbIdx, name }, Size = new Size(50, 50), Location = new Point(16, yPos), FlatStyle = FlatStyle.Flat };
            btn.FlatAppearance.BorderSize = 0;
            if (cmsMenu != null) btn.ContextMenuStrip = cmsMenu;

            btn.Click += async (s, e) => {
                if (Control.ModifierKeys == Keys.Control)
                {
                    if (!layoutSlots.Contains(name))
                    {
                        for (int i = 0; i < 4; i++) if (layoutSlots[i] == null) { layoutSlots[i] = name; break; }
                    }
                }
                else
                {
                    Array.Clear(layoutSlots, 0, 4);
                    layoutSlots[0] = name;
                }
                // 사이드바 클릭 시에도 일단 포커스 업데이트
                lastFocusedOtt = name;
                UpdateNavButtonColor();
                await LoadWebView(name, url);
                ReorganizeWebViews();
            };

            _ = Task.Run(async () => {
                try
                {
                    using var client = new HttpClient();
                    byte[] bytes = await client.GetByteArrayAsync($"https://www.google.com/s2/favicons?sz=64&domain={new Uri(url).Host}");
                    this.Invoke(new Action(() => { using var ms = new MemoryStream(bytes); btn.Image = new Bitmap(new Bitmap(ms), new Size(24, 24)); }));
                }
                catch { }
            });

            pnlOttButtons.Controls.Add(btn);
        }

        private async Task LoadWebView(string name, string url)
        {
            if (!dicWebViews.ContainsKey(name))
            {
                WebView2 wv = new WebView2 { Dock = DockStyle.None, DefaultBackgroundColor = Color.Black, Visible = false };
                pnlContent?.Controls.Add(wv);
                dicWebViews.Add(name, wv);

                // --- 핵심: 실제 웹뷰 내 '클릭' 감지 로직 ---
                // WebView2 내의 메시지를 감시하여 클릭이 발생하면 조작 대상으로 지정
                wv.GotFocus += (s, e) => { lastFocusedOtt = name; UpdateNavButtonColor(); };

                var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PersonalPro", currentUserId, name.Replace(" ", "_")));
                await wv.EnsureCoreWebView2Async(env);

                // 웹뷰 내부에서 마우스 클릭 시 포커스를 뺏어오도록 설정
                wv.CoreWebView2.NavigationCompleted += (s, e) => {
                    // 페이지 로드 후 클릭 이벤트를 감지하기 위해 스크립트 삽입(옵션)
                    wv.ExecuteScriptAsync("window.addEventListener('mousedown', () => { window.chrome.webview.postMessage('focus'); });");
                };

                wv.CoreWebView2.WebMessageReceived += (s, e) => {
                    if (e.TryGetWebMessageAsString() == "focus")
                    {
                        lastFocusedOtt = name;
                        this.Invoke(new Action(UpdateNavButtonColor));
                    }
                };

                wv.CoreWebView2.ContainsFullScreenElementChanged += (s, e) => { if (wv.CoreWebView2.ContainsFullScreenElement) EnterFullScreen(); else ExitFullScreen(); };
                wv.Source = new Uri(url);
            }
        }

        private void UpdateNavButtonColor()
        {
            if (btnNavBack != null) btnNavBack.ForeColor = string.IsNullOrEmpty(lastFocusedOtt) ? Color.DimGray : Color.White;
            if (btnNavRefresh != null) btnNavRefresh.ForeColor = string.IsNullOrEmpty(lastFocusedOtt) ? Color.DimGray : Color.White;
        }

        private void ReorganizeWebViews()
        {
            if (pnlContent == null) return;
            int w = pnlContent.Width; int h = pnlContent.Height;
            foreach (var kvp in dicWebViews.Values) kvp.Visible = false;

            var activeList = layoutSlots.Select((name, index) => new { name, index }).Where(x => x.name != null).ToList();
            int count = activeList.Count;

            for (int i = 0; i < count; i++)
            {
                var slot = activeList[i];
                if (dicWebViews.TryGetValue(slot.name!, out WebView2? wv))
                {
                    wv.Visible = true; wv.BringToFront();
                    if (count == 1) { wv.Size = new Size(w, h); wv.Location = new Point(0, 0); }
                    else if (count == 2) { wv.Size = new Size(w / 2, h); wv.Location = new Point(i * (w / 2), 0); }
                    else { wv.Size = new Size(w / 2, h / 2); wv.Location = new Point((slot.index % 2) * (w / 2), (slot.index / 2) * (h / 2)); }
                }
            }
        }

        private void SavePresetLayout()
        {
            string userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PersonalPro", currentUserId);
            if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);
            File.WriteAllText(Path.Combine(userDir, "layout_preset.txt"), string.Join(",", layoutSlots.Select(s => s ?? "null")));
            MessageBox.Show("배치가 저장되었습니다.");
        }

        private async void LoadPresetLayout(bool isAuto = false)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PersonalPro", currentUserId, "layout_preset.txt");
            if (!File.Exists(filePath)) return;
            string[] savedNames = File.ReadAllText(filePath).Split(',');
            for (int i = 0; i < savedNames.Length; i++)
            {
                if (savedNames[i] != "null")
                {
                    layoutSlots[i] = savedNames[i];
                    string url = GetUrlByName(savedNames[i]);
                    if (!string.IsNullOrEmpty(url))
                    {
                        if (i == 0) lastFocusedOtt = savedNames[i];
                        await LoadWebView(savedNames[i], url);
                    }
                }
            }
            UpdateNavButtonColor();
            ReorganizeWebViews();
        }

        private string GetUrlByName(string name)
        {
            if (name == "NETFLIX") return "https://www.netflix.com/login";
            if (name == "YOUTUBE") return "https://www.youtube.com";
            if (name == "LINKKF") return "https://linkkf.app";
            var userOtts = DatabaseHelper.GetUserOtts(currentUserIdx);
            foreach (var ott in userOtts) if (ott[1] == name) return ott[2];
            return "";
        }

        private void ToggleMuteSelected()
        {
            if (cmsMenu?.SourceControl is Button btn)
            {
                string name = ((string[])btn.Tag!)[2];
                if (dicWebViews.TryGetValue(name, out WebView2? wv)) wv.CoreWebView2.IsMuted = !wv.CoreWebView2.IsMuted;
            }
        }

        private void EnterFullScreen()
        {
            pnlTitleBar!.Visible = false; pnlSideMenu!.Visible = false;
            this.WindowState = FormWindowState.Normal; this.WindowState = FormWindowState.Maximized;
        }
        private void ExitFullScreen()
        {
            this.WindowState = FormWindowState.Normal;
            pnlTitleBar!.Visible = true; pnlSideMenu!.Visible = isMenuExpanded;
        }
        private void ToggleMaximize() { this.WindowState = (this.WindowState == FormWindowState.Maximized) ? FormWindowState.Normal : FormWindowState.Maximized; }

        private async void EditSelectedOtt()
        {
            if (cmsMenu?.SourceControl is Button btn)
            {
                string[] data = (string[])btn.Tag!;
                string n = Interaction.InputBox("수정 이름:", "수정", data[2]);
                string u = Interaction.InputBox("수정 URL:", "수정", data[0]);
                if (!string.IsNullOrEmpty(n)) { await Task.Run(() => DatabaseHelper.UpdateUserOtt(data[1], n, u)); RefreshOttList(); }
            }
        }
        private async void DeleteSelectedOtt()
        {
            if (cmsMenu?.SourceControl is Button btn)
            {
                string[] data = (string[])btn.Tag!;
                if (MessageBox.Show("삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo) == DialogResult.Yes) { await Task.Run(() => DatabaseHelper.DeleteUserOtt(data[1])); RefreshOttList(); }
            }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84)
            {
                Point pos = this.PointToClient(new Point(m.LParam.ToInt32()));
                if (pos.X >= this.ClientSize.Width - 15 && pos.Y >= this.ClientSize.Height - 15)
                {
                    m.Result = (IntPtr)17; return;
                }
            }
            base.WndProc(ref m);
        }
    }
}