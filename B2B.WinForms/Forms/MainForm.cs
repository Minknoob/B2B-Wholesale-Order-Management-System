
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

        // Button to open M2
        var m2Button = new Button
        {
            Text = "M2 - Warehouse Administration",
            Size = new Size(260, 45),
            Location = new Point(45, 165)
        };

        m2Button.Click += (_, _) =>
        {
            using var m2Form = new M2MenuForm();
            m2Form.ShowDialog(this);
        };

        var exit = new Button
        {
            Text = "Đăng xuất / Thoát",
            AutoSize = true,
            Location = new Point(45, 240)
        };

        exit.Click += (_, _) => Close();

        Controls.Add(welcome);
        Controls.Add(m2Button);
        Controls.Add(exit);
    }
}
