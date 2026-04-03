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

namespace PersonalPro
{
    public partial class MainForm : Form
    {
        private Panel pnlTitleBar, pnlSideMenu, pnlContent, pnlOttButtons, pnlLoginArea;
        private TextBox txtId, txtPw;
        private Button btnLogin, btnRegister, btnLogout, btnToggleMenu, btnMaximize, btnClose, btnUpdate;
        private Label lblWelcome;
        private ContextMenuStrip cmsMenu;

        private Dictionary<string, WebView2> dicWebViews = new Dictionary<string, WebView2>();
        private List<Button> ottButtons = new List<Button>();
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

            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape && this.WindowState == FormWindowState.Maximized) ExitFullScreen();
            };
        }

        private void InitContextMenu()
        {
            cmsMenu = new ContextMenuStrip();
            var mEdit = new ToolStripMenuItem("수정");
            var mDel = new ToolStripMenuItem("삭제");
            mEdit.Click += (s, e) => EditSelectedOtt();
            mDel.Click += (s, e) => DeleteSelectedOtt();
            cmsMenu.Items.AddRange(new ToolStripItem[] { mEdit, mDel });
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
            btnToggleMenu.Click += (s, e) => {
                isMenuExpanded = !isMenuExpanded;
                if (pnlSideMenu != null) pnlSideMenu.Visible = isMenuExpanded;
            };

            btnClose = new Button { Text = "✕", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Application.Exit();

            btnMaximize = new Button { Text = "⬜", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray };
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.Click += (s, e) => ToggleMaximize();
            pnlTitleBar.Controls.AddRange(new Control[] { lblLogo, btnToggleMenu, btnMaximize, btnClose });

            pnlSideMenu = new Panel { Dock = DockStyle.Left, Width = 75, BackColor = Color.FromArgb(25, 25, 25), Visible = false };
            pnlOttButtons = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            btnLogout = new Button { Text = "OUT", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.DimGray };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) => Application.Restart();

            lblWelcome = new Label { Text = "", Dock = DockStyle.Bottom, Height = 40, ForeColor = Color.White, Font = new Font("맑은 고딕", 8, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };

            btnUpdate = new Button { Text = "⚙ 정보 수정", Dock = DockStyle.Bottom, Height = 35, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, Font = new Font("맑은 고딕", 7) };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += (s, e) => {
                using (var form = new UpdateUserForm(currentUserIdx, DatabaseHelper.CurrentNickname))
                {
                    if (form.ShowDialog() == DialogResult.OK) lblWelcome.Text = $"{DatabaseHelper.CurrentNickname}님\n환영합니다";
                }
            };

            pnlSideMenu.Controls.AddRange(new Control[] { pnlOttButtons, btnUpdate, lblWelcome, btnLogout });

            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Padding = new Padding(0), Visible = false };

            pnlLoginArea = new Panel { Size = new Size(300, 300), BackColor = Color.Transparent };

            // 로그인 아이디 텍스트박스
            txtId = new TextBox { Location = new Point(0, 60), Width = 300, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.Gray, Font = new Font("맑은 고딕", 12), BorderStyle = BorderStyle.FixedSingle, Text = "아이디", Tag = "아이디" };
            txtId.Enter += (s, e) => { if (txtId.Text == txtId.Tag.ToString()) { txtId.Text = ""; txtId.ForeColor = Color.White; } };
            txtId.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtId.Text)) { txtId.Text = txtId.Tag.ToString(); txtId.ForeColor = Color.Gray; } };

            // 로그인 비밀번호 텍스트박스
            txtPw = new TextBox { Location = new Point(0, 110), Width = 300, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.Gray, Font = new Font("맑은 고딕", 12), BorderStyle = BorderStyle.FixedSingle, Text = "비밀번호", Tag = "비밀번호" };
            txtPw.Enter += (s, e) => { if (txtPw.Text == txtPw.Tag.ToString()) { txtPw.Text = ""; txtPw.ForeColor = Color.White; txtPw.UseSystemPasswordChar = true; } };
            txtPw.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtPw.Text)) { txtPw.Text = txtPw.Tag.ToString(); txtPw.ForeColor = Color.Gray; txtPw.UseSystemPasswordChar = false; } };
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

        private async void LoginProcess()
        {
            if (txtId == null || txtPw == null || btnLogin == null) return;
            string idVal = txtId.Text == txtId.Tag.ToString() ? "" : txtId.Text;
            string pwVal = txtPw.Text == txtPw.Tag.ToString() ? "" : txtPw.Text;

            btnLogin.Enabled = false; btnLogin.Text = "연결 중...";
            int resultIdx = await Task.Run(() => DatabaseHelper.LoginCheckReturnIdx(idVal, pwVal));

            if (resultIdx != -1)
            {
                currentUserIdx = resultIdx;
                currentUserId = idVal;
                if (lblWelcome != null) lblWelcome.Text = $"{DatabaseHelper.CurrentNickname}님\n환영합니다";
                this.Size = new Size(1300, 850);
                this.CenterToScreen();
                pnlLoginArea.Visible = false; pnlSideMenu.Visible = true; pnlContent.Visible = true; btnToggleMenu.Visible = true;
                this.Update();
                RefreshOttList();
            }
            else
            {
                MessageBox.Show("로그인 정보가 올바르지 않습니다.");
                btnLogin.Enabled = true; btnLogin.Text = "로그인";
            }
        }

        private async void RefreshOttList()
        {
            pnlOttButtons.Controls.Clear();
            ottButtons.Clear();
            AddButtonUI("NETFLIX", "https://www.netflix.com/login", false);
            AddButtonUI("YOUTUBE", "https://www.youtube.com", false);
            AddButtonUI("LINKKF", "https://linkkf.app", false);

            var list = await Task.Run(() => DatabaseHelper.GetUserOtts(currentUserIdx));
            foreach (var item in list) AddButtonUI(item[1], item[2], true, item[0]);

            int yPos = (ottButtons.Count * 60) + 10;
            Button btnAdd = new Button { Text = "＋", Size = new Size(50, 50), Location = new Point(12, yPos), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(45, 45, 45) };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += async (s, e) => {
                string n = Interaction.InputBox("OTT 이름:", "추가");
                string u = Interaction.InputBox("URL 주소:", "추가", "https://");
                if (!string.IsNullOrEmpty(n) && u.StartsWith("http")) { await Task.Run(() => DatabaseHelper.AddUserOtt(currentUserIdx, n, u)); RefreshOttList(); }
            };
            pnlOttButtons.Controls.Add(btnAdd);
        }

        private void AddButtonUI(string name, string url, bool isCustom, string dbIdx = "")
        {
            int yPos = (ottButtons.Count * 60) + 10;
            Button btn = new Button { Tag = new string[] { url, dbIdx, name }, Size = new Size(50, 50), Location = new Point(12, yPos), FlatStyle = FlatStyle.Flat };
            btn.FlatAppearance.BorderSize = 0;
            if (isCustom && cmsMenu != null) btn.ContextMenuStrip = cmsMenu;

            _ = Task.Run(async () => {
                try
                {
                    using var client = new HttpClient();
                    byte[] bytes = await client.GetByteArrayAsync($"https://www.google.com/s2/favicons?sz=64&domain={new Uri(url).Host}");
                    this.Invoke(new Action(() => {
                        using var ms = new MemoryStream(bytes);
                        btn.Image = new Bitmap(new Bitmap(ms), new Size(24, 24));
                    }));
                }
                catch { }
            });

            btn.Click += async (s, e) => await SwitchWeb(name, ((string[])btn.Tag)[0]);
            pnlOttButtons.Controls.Add(btn);
            ottButtons.Add(btn);
        }

        private async Task SwitchWeb(string name, string url)
        {
            foreach (var kvp in dicWebViews) kvp.Value.Visible = false;
            if (dicWebViews.ContainsKey(name)) { dicWebViews[name].Visible = true; }
            else
            {
                WebView2 wv = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.Black };
                pnlContent.Controls.Add(wv);
                dicWebViews.Add(name, wv);
                string userDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PersonalPro", currentUserId, name.Replace(" ", "_"));
                var env = await CoreWebView2Environment.CreateAsync(null, userDataPath);
                await wv.EnsureCoreWebView2Async(env);
                wv.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x44) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
                wv.CoreWebView2.ContainsFullScreenElementChanged += (s, e) => { if (wv.CoreWebView2.ContainsFullScreenElement) EnterFullScreen(); else ExitFullScreen(); };
                wv.Source = new Uri(url);
            }
        }

        private void EnterFullScreen() { pnlTitleBar.Visible = false; pnlSideMenu.Visible = false; this.Padding = new Padding(0); this.WindowState = FormWindowState.Normal; this.WindowState = FormWindowState.Maximized; }
        private void ExitFullScreen() { this.WindowState = FormWindowState.Normal; pnlTitleBar.Visible = true; pnlSideMenu.Visible = isMenuExpanded; }
        private async void EditSelectedOtt() { if (cmsMenu?.SourceControl is Button btn) { string[] data = (string[])btn.Tag; string n = Interaction.InputBox("수정 이름:", "수정", data[2]); string u = Interaction.InputBox("수정 URL:", "수정", data[0]); if (!string.IsNullOrEmpty(n)) { await Task.Run(() => DatabaseHelper.UpdateUserOtt(data[1], n, u)); RefreshOttList(); } } }
        private async void DeleteSelectedOtt() { if (cmsMenu?.SourceControl is Button btn) { string[] data = (string[])btn.Tag; if (MessageBox.Show("삭제?", "삭제", MessageBoxButtons.YesNo) == DialogResult.Yes) { await Task.Run(() => DatabaseHelper.DeleteUserOtt(data[1])); RefreshOttList(); } } }
        private void ToggleMaximize() { this.WindowState = (this.WindowState == FormWindowState.Maximized) ? FormWindowState.Normal : FormWindowState.Maximized; }
        protected override void WndProc(ref Message m) { if (m.Msg == 0x84) { Point pos = this.PointToClient(new Point(m.LParam.ToInt32())); if (pos.X >= this.ClientSize.Width - 15 && pos.Y >= this.ClientSize.Height - 15) { m.Result = (IntPtr)17; return; } } base.WndProc(ref m); }
    }
}