using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PersonalPro
{
    /// <summary>
    /// 신규 계정 생성 폼.
    /// InitializeComponent()는 RegisterForm.Designer.cs에 정의되어 있습니다.
    /// </summary>
    public partial class RegisterForm : Form
    {
        // 비밀번호 규칙: 영문 + 숫자 + 특수문자(@$!%*#?&) 포함, 8자 이상
        private static readonly Regex PasswordPattern = new(
            @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$",
            RegexOptions.Compiled);

        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(System.IntPtr hWnd, int msg, int wp, int lp);

        public RegisterForm()
        {
            InitializeComponent();
            WireEvents();
        }

        // ── 이벤트 연결 ──────────────────────────────────────────────

        /// <summary>
        /// 모든 UI 이벤트 핸들러를 등록합니다.
        /// Designer.cs에서 컨트롤을 생성한 후 여기서 동작을 연결합니다.
        /// </summary>
        private void WireEvents()
        {
            // 테두리 없는 폼 드래그 이동 (타이틀바 패널 기준)
            pnlDragBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };

            // 닫기 버튼 (Designer.cs에서 btnXClose로 선언됨)
            btnXClose.Click += (s, e) => this.Close();
            btnCancel.Click += (s, e) => this.Close();

            // 각 필드 플레이스홀더 동작 등록
            AttachPlaceholder(txtId);
            AttachPlaceholder(txtNick);
            AttachPasswordPlaceholder(txtPw);
            AttachPasswordPlaceholder(txtConfirm);

            // 가입 완료 버튼 및 Enter 키
            btnRegister.Click += async (s, e) => await RegisterAsync();
            this.KeyDown      += async (s, e) => { if (e.KeyCode == Keys.Enter) await RegisterAsync(); };
        }

        // ── 플레이스홀더 헬퍼 ─────────────────────────────────────────

        /// <summary>
        /// 일반 텍스트 필드에 플레이스홀더 색상 전환을 적용합니다.
        /// Tag 속성에 저장된 문자열이 플레이스홀더 텍스트로 사용됩니다.
        /// </summary>
        private static void AttachPlaceholder(TextBox tb)
        {
            tb.Enter += (s, e) =>
            {
                if (tb.Text == tb.Tag?.ToString())
                {
                    tb.Text      = "";
                    tb.ForeColor = Color.White;
                }
            };
            tb.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text      = tb.Tag?.ToString() ?? "";
                    tb.ForeColor = Color.FromArgb(130, 130, 130);
                }
            };
        }

        /// <summary>
        /// 비밀번호 필드에 플레이스홀더 + 입력 시 마스킹 전환을 적용합니다.
        /// 포커스 받을 때 UseSystemPasswordChar = true, 잃을 때 false로 복원합니다.
        /// </summary>
        private static void AttachPasswordPlaceholder(TextBox tb)
        {
            tb.Enter += (s, e) =>
            {
                if (tb.Text == tb.Tag?.ToString())
                {
                    tb.Text                  = "";
                    tb.ForeColor             = Color.White;
                    tb.UseSystemPasswordChar = true;
                }
            };
            tb.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.UseSystemPasswordChar = false;
                    tb.Text                  = tb.Tag?.ToString() ?? "";
                    tb.ForeColor             = Color.FromArgb(130, 130, 130);
                }
            };
        }

        // ── 비즈니스 로직 ─────────────────────────────────────────────

        /// <summary>
        /// 회원가입 처리:
        /// 1) 입력값 유효성 검사
        /// 2) DatabaseHelper.RegisterUser() 비동기 호출
        /// 3) 결과에 따라 성공 메시지 또는 인라인 에러 표시
        /// </summary>
        private async Task RegisterAsync()
        {
            // 이미 처리 중이면 중복 실행 방지
            if (!btnRegister.Enabled) return;

            string id      = GetValue(txtId);
            string nick    = GetValue(txtNick);
            string pw      = GetValue(txtPw);
            string confirm = GetValue(txtConfirm);

            // 필수 항목 공란 검사
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(nick))
            {
                ShowError("아이디와 닉네임을 입력해주세요.");
                return;
            }
            if (string.IsNullOrEmpty(pw))
            {
                ShowError("비밀번호를 입력해주세요.");
                return;
            }

            // 비밀번호 일치 검사
            if (pw != confirm)
            {
                ShowError("비밀번호가 일치하지 않습니다.");
                return;
            }

            // 비밀번호 복잡도 검사 (영문·숫자·특수문자 포함 8자 이상)
            if (!PasswordPattern.IsMatch(pw))
            {
                ShowError("영문·숫자·특수문자 포함 8자 이상이어야 합니다.");
                return;
            }

            // API 호출 시작 — 버튼 비활성화로 중복 요청 차단
            ShowError("");
            SetLoading(true);

            bool success = await Task.Run(() => DatabaseHelper.RegisterUser(id, pw, nick));

            if (success)
            {
                MessageBox.Show("가입이 완료되었습니다!\n로그인해 주세요.", "가입 성공",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                ShowError("이미 사용 중인 아이디이거나 서버 오류가 발생했습니다.");
                SetLoading(false);
            }
        }

        /// <summary>
        /// 플레이스홀더 텍스트와 동일하면 빈 문자열을 반환합니다.
        /// </summary>
        private static string GetValue(TextBox tb)
        {
            string val = tb.Text.Trim();
            return val == tb.Tag?.ToString() ? "" : val;
        }

        /// <summary>
        /// lblError에 인라인 에러 메시지를 표시합니다.
        /// 빈 문자열을 전달하면 메시지가 지워집니다.
        /// </summary>
        private void ShowError(string message) => lblError.Text = message;

        /// <summary>
        /// 로딩 상태에 따라 버튼 텍스트와 활성화 여부를 전환합니다.
        /// </summary>
        private void SetLoading(bool loading)
        {
            btnRegister.Enabled = !loading;
            btnRegister.Text    = loading ? "처리 중..." : "가입 완료";
        }
    }
}
