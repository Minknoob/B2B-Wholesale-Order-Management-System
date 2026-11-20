using Microsoft.Data.SqlClient;
namespace B2B.WinForms.Forms;
public class AgentForm : Form
{
    readonly DataGridView grid = M2Ui.Grid(15, 15, 850, 320);
    readonly TextBox name = M2Ui.Text(125, 360), address = M2Ui.Text(425, 360, 350), phone = M2Ui.Text(125, 405);
    int id;
    public AgentForm()
    {
        Text = "M2 - Agent Management"; ClientSize = new Size(890, 505); StartPosition = FormStartPosition.CenterParent;
        Controls.Add(grid);
        Controls.AddRange(new Control[] { M2Ui.Label("Agent name", 15, 360), name, M2Ui.Label("Address", 315, 360), address, M2Ui.Label("Phone", 15, 405), phone });
        var add = M2Ui.Button("Add", 15, 455); var edit = M2Ui.Button("Update", 145, 455); var delete = M2Ui.Button("Delete", 275, 455); var refresh = M2Ui.Button("Refresh", 405, 455);
        Controls.AddRange(new Control[] { add, edit, delete, refresh });
        grid.SelectionChanged += (_, _) => { if (grid.CurrentRow == null) return; id = M2Ui.SelectedId(grid, "AgentID"); name.Text = grid.CurrentRow.Cells["AgentName"].Value?.ToString(); address.Text = grid.CurrentRow.Cells["Address"].Value?.ToString(); phone.Text = grid.CurrentRow.Cells["Phone"].Value?.ToString(); };
        add.Click += (_, _) => Save(false); edit.Click += (_, _) => Save(true); delete.Click += (_, _) => Delete(); refresh.Click += (_, _) => LoadRows(); LoadRows();
    }
    void LoadRows() { try { grid.DataSource = M2Ui.Query("SELECT AgentID,AgentName,Address,Phone FROM dbo.Agent ORDER BY AgentID"); } catch (Exception ex) { M2Ui.Error(ex); } }
    void Save(bool update)
    {
        if (string.IsNullOrWhiteSpace(name.Text) || string.IsNullOrWhiteSpace(address.Text) || string.IsNullOrWhiteSpace(phone.Text)) { MessageBox.Show("All fields are required."); return; }
        if (update && id == 0) return;
        try {
            var sql = update ? "UPDATE dbo.Agent SET AgentName=@n,Address=@a,Phone=@p WHERE AgentID=@id" : "INSERT INTO dbo.Agent(AgentName,Address,Phone) VALUES(@n,@a,@p)";
            M2Ui.Execute(sql, M2Ui.P("@n", name.Text.Trim()), M2Ui.P("@a", address.Text.Trim()), M2Ui.P("@p", phone.Text.Trim()), M2Ui.P("@id", id)); LoadRows();
        } catch (Exception ex) { M2Ui.Error(ex); }
    }
    void Delete()
    {
        if (id == 0 || !M2Ui.Confirm("Delete selected agent?")) return;
        try { M2Ui.Execute("DELETE FROM dbo.Agent WHERE AgentID=@id", M2Ui.P("@id", id)); LoadRows(); }
        catch (SqlException ex) when (ex.Number == 547) { MessageBox.Show("Cannot delete an agent linked to orders or user accounts."); }
        catch (Exception ex) { M2Ui.Error(ex); }
    }
}
