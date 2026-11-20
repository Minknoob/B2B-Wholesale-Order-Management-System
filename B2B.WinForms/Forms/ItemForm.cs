using Microsoft.Data.SqlClient;
namespace B2B.WinForms.Forms;
public class ItemForm : Form
{
    readonly DataGridView grid = M2Ui.Grid(15, 15, 850, 320);
    readonly TextBox name = M2Ui.Text(130, 365), size = M2Ui.Text(425, 365), price = M2Ui.Text(130, 410), stock = M2Ui.Text(425, 410);
    int id;
    public ItemForm()
    {
        Text = "M2 - Item Management"; ClientSize = new Size(890, 520); StartPosition = FormStartPosition.CenterParent;
        Controls.Add(grid);
        Controls.AddRange(new Control[] { M2Ui.Label("Item name", 15, 365), name, M2Ui.Label("Size", 315, 365), size, M2Ui.Label("Price", 15, 410), price, M2Ui.Label("Stock", 315, 410), stock });
        var add = M2Ui.Button("Add", 15, 465); var edit = M2Ui.Button("Update", 145, 465); var delete = M2Ui.Button("Delete", 275, 465); var refresh = M2Ui.Button("Refresh", 405, 465);
        Controls.AddRange(new Control[] { add, edit, delete, refresh });
        grid.SelectionChanged += (_, _) => { if (grid.CurrentRow == null) return; id = M2Ui.SelectedId(grid, "ItemID"); name.Text = grid.CurrentRow.Cells["ItemName"].Value?.ToString(); size.Text = grid.CurrentRow.Cells["Size"].Value?.ToString(); price.Text = grid.CurrentRow.Cells["Price"].Value?.ToString(); stock.Text = grid.CurrentRow.Cells["StockQuantity"].Value?.ToString(); };
        add.Click += (_, _) => Save(false); edit.Click += (_, _) => Save(true); delete.Click += (_, _) => Delete(); refresh.Click += (_, _) => LoadRows();
        LoadRows();
    }
    void LoadRows() { try { grid.DataSource = M2Ui.Query("SELECT ItemID,ItemName,Size,Price,StockQuantity FROM dbo.Item ORDER BY ItemID"); } catch (Exception ex) { M2Ui.Error(ex); } }
    void Save(bool update)
    {
        if (string.IsNullOrWhiteSpace(name.Text) || string.IsNullOrWhiteSpace(size.Text) || !decimal.TryParse(price.Text, out var p) || p < 0 || !int.TryParse(stock.Text, out var s) || s < 0) { MessageBox.Show("Enter name, size, non-negative price and stock."); return; }
        if (update && id == 0) return;
        try {
            string sql = update ? "UPDATE dbo.Item SET ItemName=@n,Size=@s,Price=@p,StockQuantity=@q WHERE ItemID=@id" : "INSERT INTO dbo.Item(ItemName,Size,Price,StockQuantity) VALUES(@n,@s,@p,@q)";
            M2Ui.Execute(sql, M2Ui.P("@n", name.Text.Trim()), M2Ui.P("@s", size.Text.Trim()), M2Ui.P("@p", p), M2Ui.P("@q", s), M2Ui.P("@id", id)); LoadRows();
        } catch (Exception ex) { M2Ui.Error(ex); }
    }
    void Delete()
    {
        if (id == 0 || !M2Ui.Confirm("Delete selected item?")) return;
        try { M2Ui.Execute("DELETE FROM dbo.Item WHERE ItemID=@id", M2Ui.P("@id", id)); LoadRows(); }
        catch (SqlException ex) when (ex.Number == 547) { MessageBox.Show("Cannot delete an item used in an order."); }
        catch (Exception ex) { M2Ui.Error(ex); }
    }
}
