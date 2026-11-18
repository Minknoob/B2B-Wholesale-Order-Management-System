namespace B2B.WinForms.Forms;
/// <summary>Standalone M2 launcher; M1 MainForm stays unchanged. Open after successful Admin login.</summary>
public class M2MenuForm : Form
{
    public M2MenuForm()
    {
        Text = "M2 - Warehouse Administration"; StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(440, 315);
        var buttons = new (string Title, Func<Form> Create)[] { ("Items / Inventory", () => new ItemForm()), ("Agents", () => new AgentForm()), ("Orders / Shipping Manifest", () => new OrderForm()), ("Sales Dashboard", () => new DashboardForm()) };
        for (int i = 0; i < buttons.Length; i++) { var item = buttons[i]; var b = M2Ui.Button(item.Title, 60, 30 + i * 62, 320); b.Height = 44; b.Click += (_, _) => { using var form = item.Create(); form.ShowDialog(this); }; Controls.Add(b); }
    }
}
