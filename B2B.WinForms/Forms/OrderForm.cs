namespace B2B.WinForms.Forms;
public class OrderForm : Form
{
    readonly DataGridView orders = M2Ui.Grid(15, 55, 900, 300), details = M2Ui.Grid(15, 395, 900, 200);
    readonly ComboBox filter = new() { Left = 120, Top = 15, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly ComboBox status = new() { Left = 435, Top = 15, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly string[] statuses = { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };
    public OrderForm()
    {
        Text = "M2 - Order Management"; ClientSize = new Size(940, 660); StartPosition = FormStartPosition.CenterParent;
        filter.Items.Add("All"); filter.Items.AddRange(statuses); filter.SelectedIndex = 0;
        status.Items.AddRange(statuses); status.SelectedIndex = 0;
        var update = M2Ui.Button("Update status", 635, 15, 135); var manifest = M2Ui.Button("Export manifest", 780, 15, 135);
        Controls.AddRange(new Control[] { M2Ui.Label("Filter status", 15, 15), filter, M2Ui.Label("Set status", 325, 15), status, update, manifest, orders, M2Ui.Label("Order details", 15, 365), details });
        filter.SelectedIndexChanged += (_, _) => LoadOrders();
        orders.SelectionChanged += (_, _) => LoadDetails();
        update.Click += (_, _) => UpdateStatus(); manifest.Click += (_, _) => Export(); LoadOrders();
    }
    void LoadOrders()
    {
        try { orders.DataSource = M2Ui.Query("SELECT o.OrderID,o.OrderDate,o.AgentID,a.AgentName,o.OrderStatus,COALESCE(SUM(d.Quantity*d.UnitAmount),0) AS TotalAmount FROM dbo.[Order] o JOIN dbo.Agent a ON a.AgentID=o.AgentID LEFT JOIN dbo.OrderDetail d ON d.OrderID=o.OrderID WHERE (@status='All' OR o.OrderStatus=@status) GROUP BY o.OrderID,o.OrderDate,o.AgentID,a.AgentName,o.OrderStatus ORDER BY o.OrderDate DESC", M2Ui.P("@status", filter.SelectedItem?.ToString() ?? "All")); }
        catch (Exception ex) { M2Ui.Error(ex); }
    }
    void LoadDetails()
    {
        int id = M2Ui.SelectedId(orders, "OrderID");
        if (id == 0) { details.DataSource = null; return; }
        try { details.DataSource = M2Ui.Query("SELECT d.ID,d.ItemID,i.ItemName,d.Quantity,d.UnitAmount,(d.Quantity*d.UnitAmount) AS LineTotal FROM dbo.OrderDetail d JOIN dbo.Item i ON i.ItemID=d.ItemID WHERE d.OrderID=@id ORDER BY d.ID", M2Ui.P("@id", id));
            var current = orders.CurrentRow?.Cells["OrderStatus"].Value?.ToString(); if (current != null) status.SelectedItem = current;
        } catch (Exception ex) { M2Ui.Error(ex); }
    }
    void UpdateStatus()
    {
        int id = M2Ui.SelectedId(orders, "OrderID"); if (id == 0) return;
        if (!M2Ui.Confirm($"Change order #{id} status to {status.SelectedItem}?")) return;
        try { M2Ui.Execute("UPDATE dbo.[Order] SET OrderStatus=@s WHERE OrderID=@id", M2Ui.P("@s", status.SelectedItem!.ToString()!), M2Ui.P("@id", id)); LoadOrders(); }
        catch (Exception ex) { M2Ui.Error(ex); }
    }
    void Export()
    {
        int id = M2Ui.SelectedId(orders, "OrderID"); if (id == 0) return;
        using var dialog = new SaveFileDialog { Filter = "PDF files|*.pdf", FileName = $"ShippingManifest_{id}.pdf" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try { Reports.ManifestPdf.Export(id, dialog.FileName); MessageBox.Show("PDF exported successfully."); }
        catch (Exception ex) { M2Ui.Error(ex); }
    }
}
