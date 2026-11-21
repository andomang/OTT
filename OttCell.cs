// ============================================================
// OttCell.cs — 멀티뷰 한 칸 (상단 컨트롤 + WebView2 + 오류 안내)
//
//   - 상단 바: 이름, 음량, 음소거, 크게 보기, PiP로 보내기, 닫기
//   - 상단 바를 다른 칸으로 끌어다 놓으면 자리 교체
//   - 접속 실패 시 안내와 다시 시도 버튼 표시
// ============================================================
using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace PersonalPro
{
    public class OttCell : Panel
    {
        private const int HEADER = 26;

        static readonly Color C_HEADER = Color.FromArgb(28, 28, 38);
        static readonly Color C_HOVER  = Color.FromArgb(55, 55, 72);
        static readonly Color C_TEXT   = Color.FromArgb(180, 180, 195);
        static readonly Color C_ACTIVE = Color.FromArgb(229, 9, 20);

        public string OttName { get; }
        public WebView2 View { get; }
        public int Volume { get; private set; } = 100;

        public event Action<OttCell>? CloseClicked;
        public event Action<OttCell>? MuteClicked;
        public event Action<OttCell>? MainClicked;
        public event Action<OttCell>? PipClicked;
        public event Action<OttCell>? HeaderPressed;
        public event Action<OttCell>? VolumeChanged;
        public event Action<OttCell, OttCell>? SwapRequested;

        private readonly Panel  _header;
        private readonly Label  _title;
        private readonly Button _btnMute, _btnMain;
        private readonly Panel  _errorPanel;
        private readonly Label  _errorLabel;

        public OttCell(string name, WebView2 view)
        {
            OttName   = name;
            View      = view;
            BackColor = Color.FromArgb(10, 10, 10);

            view.Dock = DockStyle.Fill;

            // ── 오류 안내 ─────────────────────────────────────────────
            _errorPanel = new Panel { Dock = DockStyle.Fill, BackColor = BackColor, Visible = false };
            _errorLabel = new Label
            {
                Dock = DockStyle.Top, Height = 80, ForeColor = C_TEXT,
                TextAlign = ContentAlignment.BottomCenter, Font = new Font("Segoe UI", 9f)
            };
            var btnRetry = new Button
            {
                Text = "다시 시도", Size = new Size(100, 32), Location = new Point(12, 92),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = C_ACTIVE,
                Anchor = AnchorStyles.Top, Cursor = Cursors.Hand
            };
            btnRetry.FlatAppearance.BorderSize = 0;
            btnRetry.Click += (s, e) =>
            {
                _errorPanel.Visible = false;
                View.Visible = true;
                View.CoreWebView2?.Reload();
            };
            _errorPanel.Resize += (s, e) => btnRetry.Left = (_errorPanel.Width - btnRetry.Width) / 2;
            _errorPanel.Controls.Add(btnRetry);
            _errorPanel.Controls.Add(_errorLabel);

            // ── 상단 바 ───────────────────────────────────────────────
            _header = new Panel { Dock = DockStyle.Top, Height = HEADER, BackColor = C_HEADER, Cursor = Cursors.SizeAll };
            _title  = new Label
            {
                Text = name, AutoEllipsis = true, Dock = DockStyle.Fill, ForeColor = C_TEXT,
                Font = new Font("Segoe UI", 8f), TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0), Cursor = Cursors.SizeAll
            };

            var btnClose = HeaderBtn("✕", "닫기 (Ctrl+W)");
            var btnPip   = HeaderBtn("⧉", "PiP로 보내기");
            _btnMain     = HeaderBtn("⛶", "크게 보기");
            _btnMute     = HeaderBtn("♪", "음소거 (Ctrl+M)");
            var btnVol   = HeaderBtn("Vol", "음량 조절");
            btnVol.Width = 34;

            btnClose.Click += (s, e) => CloseClicked?.Invoke(this);
            btnPip.Click   += (s, e) => PipClicked?.Invoke(this);
            _btnMain.Click += (s, e) => MainClicked?.Invoke(this);
            _btnMute.Click += (s, e) => MuteClicked?.Invoke(this);
            btnVol.Click   += (s, e) => ShowVolumePopup(btnVol);

            _header.Controls.Add(_title);
            _header.Controls.AddRange(new Control[] { btnVol, _btnMute, _btnMain, btnPip, btnClose });
            WireDrag(_title);
            WireDrag(_header);

            // Dock 순서: 나중에 추가한 상단 바가 먼저 자리를 잡고, 나머지가 아래를 채움
            Controls.Add(View);
            Controls.Add(_errorPanel);
            Controls.Add(_header);
        }

        public bool HeaderVisible { set => _header.Visible = value; }

        public void SetLoading(bool loading) => _title.Text = loading ? $"{OttName}  ·  불러오는 중..." : OttName;
        public void SetMuted(bool muted)     => _btnMute.ForeColor = muted ? C_ACTIVE : C_TEXT;
        public void SetMain(bool main)       => _btnMain.ForeColor = main  ? C_ACTIVE : C_TEXT;

        public void ShowError(string message)
        {
            _errorLabel.Text    = message;
            View.Visible        = false;
            _errorPanel.Visible = true;
        }

        private static Button HeaderBtn(string text, string tip)
        {
            var b = new Button
            {
                Text = text, Dock = DockStyle.Right, Width = 28, FlatStyle = FlatStyle.Flat,
                ForeColor = C_TEXT, BackColor = C_HEADER, Font = new Font("Segoe UI", 8f), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = C_HOVER;
            new ToolTip().SetToolTip(b, tip);
            return b;
        }

        /// <summary>마우스를 놓은 위치의 칸과 자리를 바꿉니다.</summary>
        private void WireDrag(Control handle)
        {
            handle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) HeaderPressed?.Invoke(this); };
            handle.MouseUp   += (s, e) =>
            {
                if (e.Button != MouseButtons.Left || Parent == null) return;
                var target = Parent.GetChildAtPoint(Parent.PointToClient(Cursor.Position)) as OttCell;
                if (target != null && target != this) SwapRequested?.Invoke(this, target);
            };
        }

        private void ShowVolumePopup(Control anchor)
        {
            var bar = new TrackBar
            {
                Minimum = 0, Maximum = 100, TickStyle = TickStyle.None, Value = Volume, Width = 130, AutoSize = false, Height = 28
            };
            bar.ValueChanged += (s, e) => { Volume = bar.Value; VolumeChanged?.Invoke(this); };
            var popup = new ToolStripDropDown { Padding = Padding.Empty };
            popup.Items.Add(new ToolStripControlHost(bar) { Margin = Padding.Empty, Padding = Padding.Empty });
            popup.Show(anchor, new Point(0, anchor.Height));
        }
    }
}
