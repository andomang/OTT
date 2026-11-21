// ============================================================
// StatsForm.cs — OTT별 시청 시간 통계
//
// 한 가지 값(시간)의 크기 비교라 단일 색 가로 막대를 사용합니다.
// 값은 막대 끝에 표시하고, 마우스를 올리면 비율을 보여줍니다.
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PersonalPro
{
    public class StatsForm : Form
    {
        private const int MAX_ROWS   = 8;   // 넘치면 "기타"로 합침
        private const int ROW_H      = 34;
        private const int BAR_H      = 14;
        private const int LABEL_W    = 120;
        private const int VALUE_W    = 90;

        static readonly Color C_WHITE = Color.White;
        static readonly Color C_TEXT  = Color.FromArgb(17, 24, 39);
        static readonly Color C_MID   = Color.FromArgb(75, 85, 99);
        static readonly Color C_BORDER = Color.FromArgb(229, 231, 235);
        static readonly Color C_RED   = Color.FromArgb(229, 9, 20);

        // 깜빡임 없이 다시 그리는 패널
        private class ChartPanel : Panel
        {
            public ChartPanel() { DoubleBuffered = true; }
        }

        private readonly Dictionary<string, Dictionary<string, int>> _data;
        private readonly ChartPanel _chart;
        private readonly Label      _lblTotal;
        private readonly ToolTip    _tip = new();
        private readonly Button[]   _periodBtns;
        private List<(string Name, int Seconds)> _rows = new();
        private int _total, _hoverRow = -1;

        public StatsForm(string userId)
        {
            _data = WatchStats.Load(userId);

            Text            = "시청 통계";
            Size            = new Size(540, 460);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = C_WHITE;
            ForeColor       = C_TEXT;
            Font            = new Font("Segoe UI", 9f);

            // ── 기간 선택 ─────────────────────────────────────────────
            var row = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(16, 10, 0, 0), BackColor = C_WHITE };
            _periodBtns = new[]
            {
                PeriodBtn("오늘", 0), PeriodBtn("최근 7일", 6), PeriodBtn("전체", -1)
            };
            row.Controls.AddRange(_periodBtns);

            _lblTotal = new Label
            {
                Dock = DockStyle.Top, Height = 46, ForeColor = C_TEXT, BackColor = C_WHITE,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold), Padding = new Padding(18, 8, 0, 0)
            };

            _chart = new ChartPanel { Dock = DockStyle.Fill, BackColor = C_WHITE };
            _chart.Paint     += (s, e) => DrawChart(e.Graphics);
            _chart.MouseMove += OnChartMouseMove;
            _chart.MouseLeave += (s, e) => { _hoverRow = -1; _tip.Hide(_chart); _chart.Invalidate(); };

            Controls.Add(_chart);
            Controls.Add(_lblTotal);
            Controls.Add(row);

            Theme.Apply(this);
            SelectPeriod(_periodBtns[1], 6);
        }

        private Button PeriodBtn(string text, int daysBack)
        {
            var b = new Button
            {
                Text = text, Size = new Size(84, 26), FlatStyle = FlatStyle.Flat,
                BackColor = C_WHITE, ForeColor = C_MID, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Theme.C(C_BORDER);
            b.Click += (s, e) => SelectPeriod(b, daysBack);
            return b;
        }

        private void SelectPeriod(Button selected, int daysBack)
        {
            foreach (var b in _periodBtns)
                b.ForeColor = b == selected ? C_RED : Theme.C(C_MID);

            string? from = daysBack < 0 ? null : DateTime.Today.AddDays(-daysBack).ToString("yyyy-MM-dd");
            var all = StatsAggregator.Total(_data, from);
            _total = all.Sum(r => r.Seconds);
            _rows  = StatsAggregator.FoldOther(all, MAX_ROWS);
            _lblTotal.Text = $"총 {StatsAggregator.FormatDuration(_total)}";
            _chart.Invalidate();
        }

        private void DrawChart(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var text  = new SolidBrush(Theme.C(C_TEXT));
            using var mid   = new SolidBrush(Theme.C(C_MID));
            using var bar   = new SolidBrush(C_RED);
            using var hover = new SolidBrush(Theme.C(Color.FromArgb(243, 244, 246)));
            using var nowrap = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center };

            if (_rows.Count == 0)
            {
                g.DrawString("이 기간에는 시청 기록이 없습니다.", Font, mid, 18, 16);
                return;
            }

            int barMax = _chart.Width - LABEL_W - VALUE_W - 32;
            int top = _rows[0].Seconds;
            for (int i = 0; i < _rows.Count; i++)
            {
                var (name, seconds) = _rows[i];
                int y = 4 + i * ROW_H;
                if (i == _hoverRow) g.FillRectangle(hover, 8, y, _chart.Width - 16, ROW_H);

                g.DrawString(name, Font, text, new RectangleF(18, y, LABEL_W - 8, ROW_H), nowrap);

                int w = Math.Max(4, (int)((long)barMax * seconds / top));
                var rect = new Rectangle(18 + LABEL_W, y + (ROW_H - BAR_H) / 2, w, BAR_H);
                using (var path = RoundedEnd(rect, 4)) g.FillPath(bar, path);

                g.DrawString(StatsAggregator.FormatDuration(seconds), Font, mid,
                    new RectangleF(rect.Right + 8, y, VALUE_W, ROW_H), nowrap);
            }
        }

        // 기준선 쪽은 각지게, 값 쪽 끝만 둥글게
        private static GraphicsPath RoundedEnd(Rectangle r, int radius)
        {
            int rad = Math.Min(radius, r.Width);
            var p = new GraphicsPath();
            p.AddLine(r.Left, r.Top, r.Right - rad, r.Top);
            p.AddArc(r.Right - rad * 2, r.Top, rad * 2, rad * 2, 270, 90);
            p.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
            p.AddLine(r.Right - rad, r.Bottom, r.Left, r.Bottom);
            p.CloseFigure();
            return p;
        }

        private void OnChartMouseMove(object? sender, MouseEventArgs e)
        {
            int row = (e.Y - 4) / ROW_H;
            if (e.Y < 4 || row >= _rows.Count) row = -1;
            if (row == _hoverRow) return;

            _hoverRow = row;
            _chart.Invalidate();
            if (row < 0) { _tip.Hide(_chart); return; }

            var (name, seconds) = _rows[row];
            _tip.Show($"{name}: {StatsAggregator.FormatDuration(seconds)} ({seconds * 100L / _total}%)",
                _chart, e.X + 12, e.Y + 12);
        }
    }
}
