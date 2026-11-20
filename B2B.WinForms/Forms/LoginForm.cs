
using B2B.Core;
using B2B.WinForms.Database;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace B2B.WinForms.Forms;

public class LoginForm : Form
{
    private readonly TextBox txtUsername = new()
    {
        Width = 250,
        Font = new Font("Segoe UI", 10)
    };

    private readonly TextBox txtPassword = new()
    {
        Width = 250,
        UseSystemPasswordChar = true,
        Font = new Font("Segoe UI", 10)
    };

    private readonly Button btnLogin = new()
    {
        Text = "Đăng nhập",
        Width = 130,
        Height = 40,
        BackColor = Color.FromArgb(35, 110, 190),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Cursor = Cursors.Hand,
        UseVisualStyleBackColor = false
    };

    public LoginForm()
    {
        Text = "B2B Wholesale - Admin Login";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(480, 300);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10);

        var heading = new Label
        {
            Text = "ĐĂNG NHẬP QUẢN TRỊ",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(35, 75, 120),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 25),
            Size = new Size(440, 45)
        };

        var userLabel = new Label
        {
            Text = "Tên đăng nhập",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(50, 105),
            Size = new Size(115, 30)
        };

        txtUsername.Location = new Point(175, 105);

        var passLabel = new Label
        {
            Text = "Mật khẩu",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(50, 155),
            Size = new Size(115, 30)
        };

        txtPassword.Location = new Point(175, 155);
        btnLogin.Location = new Point(175, 215);

        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += async (_, _) => await LoginAsync();

        AcceptButton = btnLogin;

        Controls.AddRange([
            heading,
            userLabel,
            txtUsername,
            passLabel,
            txtPassword,
            btnLogin
        ]);
    }

    private async Task LoginAsync()
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (username.Length == 0 || password.Length == 0)
        {
            MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.",
                "Thiếu thông tin",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        btnLogin.Enabled = false;

        try
        {
            LoginCheckResult result = await CheckAdminLoginAsync(username, password);

            if (!result.Success)
            {
                MessageBox.Show(result.Message,
                    "Đăng nhập thất bại",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            Hide();

            using var main = new MainForm(username);
            main.ShowDialog(this);

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể kết nối hoặc truy vấn Database.\n" + ex.Message,
                "Lỗi hệ thống",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed)
                btnLogin.Enabled = true;
        }
    }

    private static async Task<LoginCheckResult> CheckAdminLoginAsync(string username, string password)
    {
        await using SqlConnection connection = DbConnectionFactory.Create();
        await connection.OpenAsync();

        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        const string selectSql = @"SELECT TOP 1 UserID, PasswordHash, IsLocked, Role, FailedLoginCount
                                   FROM dbo.Users WITH (UPDLOCK, ROWLOCK)
                                   WHERE UserName = @username";

        int userId, failedCount;
        string passwordHash, role;
        bool isLocked;

        await using (var command = new SqlCommand(selectSql, connection, transaction))
        {
            command.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = username;

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                await reader.CloseAsync();
                await transaction.CommitAsync();

                return new(false, "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            userId = reader.GetInt32(0);
            passwordHash = reader.GetString(1);
            isLocked = reader.GetBoolean(2);
            role = reader.GetString(3);
            failedCount = reader.GetInt32(4);
        }

        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            await transaction.CommitAsync();

            return new(false,
                "Tài khoản này có quyền Agent. Chỉ Admin được phép sử dụng ứng dụng WinForms.");
        }

        if (!AccountLockout.CanAttemptLogin(isLocked))
        {
            await transaction.CommitAsync();

            return new(false,
                "Tài khoản đã bị khóa do nhập sai mật khẩu 3 lần. Vui lòng liên hệ Admin để mở khóa.");
        }

        if (!PasswordHasher.VerifyPassword(password, passwordHash))
        {
            int nextCount = AccountLockout.RecordFailedAttempt(failedCount);
            bool lockNow = AccountLockout.ShouldLock(nextCount);

            const string updateSql = @"UPDATE dbo.Users
                                       SET FailedLoginCount = @count, IsLocked = @locked
                                       WHERE UserID = @id";

            await using var update = new SqlCommand(updateSql, connection, transaction);

            update.Parameters.Add("@count", SqlDbType.Int).Value = nextCount;
            update.Parameters.Add("@locked", SqlDbType.Bit).Value = lockNow;
            update.Parameters.Add("@id", SqlDbType.Int).Value = userId;

            await update.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            return new(false, lockNow
                ? "Bạn đã nhập sai 3 lần. Tài khoản đã bị khóa, vui lòng liên hệ Admin để mở khóa."
                : $"Mật khẩu không đúng. Bạn còn {AccountLockout.MaxFailedAttempts - nextCount} lần thử.");
        }

        await using (var reset = new SqlCommand(
            "UPDATE dbo.Users SET FailedLoginCount = @count WHERE UserID = @id",
            connection, transaction))
        {
            reset.Parameters.Add("@count", SqlDbType.Int).Value =
                AccountLockout.ResetAfterSuccessfulLogin();

            reset.Parameters.Add("@id", SqlDbType.Int).Value = userId;

            await reset.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();

        return new(true, "Đăng nhập thành công.");
    }

    private sealed record LoginCheckResult(bool Success, string Message);
}
