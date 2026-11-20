namespace B2B.WinForms.Forms;

/// <summary>Main menu shown after a successful Admin login.</summary>
public class MainForm : Form
{
    public MainForm(string adminName)
    {
        Text = "B2B Wholesale - Admin Dashboard";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(650, 380);
        var welcome = new Label
        {
            Text = $"Xin chào Admin: {adminName}\n\nĐăng nhập thành công.",
            AutoSize = true,
            Font = new Font("Segoe UI", 12),
            Location = new Point(45, 55)
        };
        var exit = new Button { Text = "Đăng xuất / Thoát", AutoSize = true, Location = new Point(45, 220) };
        exit.Click += (_, _) => Close();
        Controls.Add(welcome);
        Controls.Add(exit);
    }
}
