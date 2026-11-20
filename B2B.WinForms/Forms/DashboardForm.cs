namespace B2B.WinForms.Forms;
public class DashboardForm : Form
{
    readonly DataGridView top = M2Ui.Grid(15, 65, 430, 240), low = M2Ui.Grid(465, 65, 430, 240), revenue = M2Ui.Grid(15, 390, 880, 220);
    readonly ComboBox agents = new() { Left = 115, Top = 340, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };
    readonly DateTimePicker from = new() { Left = 430, Top = 340, Width = 190 }, to = new() { Left = 655, Top = 340, Width = 190 };
    public DashboardForm()
    {
        Text = "M2 - Sales Dashboard"; ClientSize = new Size(920, 630); StartPosition = FormStartPosition.CenterParent;
        var reload = M2Ui.Button("Refresh", 770, 15);
        Controls.AddRange(new Control[] { M2Ui.Label("Best-selling items", 15, 20), M2Ui.Label("Low stock (< 10)", 465, 20), top, low, M2Ui.Label("Agent", 15, 340), agents, M2Ui.Label("From", 370, 340), from, M2Ui.Label("To", 615, 340), to, M2Ui.Label("Revenue", 15, 365), revenue, reload });
        from.Value = DateTime.Today.AddMonths(-3); to.Value = DateTime.Today;
        agents.SelectedIndexChanged += (_, _) => LoadRevenue(); from.ValueChanged += (_, _) => LoadRevenue(); to.ValueChanged += (_, _) => LoadRevenue(); reload.Click += (_, _) => LoadAll();
        LoadAll();
    }
    void LoadAll()
    {
        try {
            top.DataSource = M2Ui.Query("SELECT TOP (10) i.ItemID,i.ItemName,COALESCE(SUM(CASE WHEN o.OrderStatus <> 'Cancelled' THEN d.Quantity ELSE 0 END),0) AS UnitsSold FROM dbo.Item i LEFT JOIN dbo.OrderDetail d ON d.ItemID=i.ItemID LEFT JOIN dbo.[Order] o ON o.OrderID=d.OrderID GROUP BY i.ItemID,i.ItemName ORDER BY UnitsSold DESC");
            low.DataSource = M2Ui.Query("SELECT ItemID,ItemName,StockQuantity FROM dbo.Item WHERE StockQuantity < 10 ORDER BY StockQuantity,ItemName");
            var t = M2Ui.Query("SELECT AgentID,AgentName FROM dbo.Agent ORDER BY AgentName");
            agents.Items.Clear(); agents.Items.Add(new AgentOption(0,"All agents"));
            foreach (System.Data.DataRow row in t.Rows) agents.Items.Add(new AgentOption(Convert.ToInt32(row["AgentID"]),row["AgentName"].ToString()!));
            agents.SelectedIndex = 0; LoadRevenue();
        } catch (Exception ex) { M2Ui.Error(ex); }
    }
    void LoadRevenue()
    {
        if (agents.SelectedItem is not AgentOption a || from.Value.Date > to.Value.Date) return;
        try { revenue.DataSource = M2Ui.Query("SELECT a.AgentID,a.AgentName,COALESCE(SUM(CASE WHEN o.OrderStatus <> 'Cancelled' THEN d.Quantity*d.UnitAmount ELSE 0 END),0) AS Revenue FROM dbo.Agent a LEFT JOIN dbo.[Order] o ON o.AgentID=a.AgentID AND o.OrderDate>=@from AND o.OrderDate<@to LEFT JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID WHERE (@agent=0 OR a.AgentID=@agent) GROUP BY a.AgentID,a.AgentName ORDER BY Revenue DESC", M2Ui.P("@agent", a.Id), M2Ui.P("@from", from.Value.Date), M2Ui.P("@to", to.Value.Date.AddDays(1))); }
        catch (Exception ex) { M2Ui.Error(ex); }
    }
    sealed record AgentOption(int Id, string Name) { public override string ToString() => Name; }
}
