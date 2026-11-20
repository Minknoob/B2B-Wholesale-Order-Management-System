using System.Data;
using Microsoft.Data.SqlClient;
using B2B.WinForms.Database;

namespace B2B.WinForms.Forms;

internal static class M2Ui
{
    public static DataTable Query(string sql, params SqlParameter[] parameters)
    {
        using var connection = DbConnectionFactory.Create();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        using var adapter = new SqlDataAdapter(command);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }
    public static int Execute(string sql, params SqlParameter[] parameters)
    {
        using var connection = DbConnectionFactory.Create();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        connection.Open();
        return command.ExecuteNonQuery();
    }
    public static SqlParameter P(string name, object value) => new(name, value);
    public static Button Button(string title, int x, int y, int width = 115) => new() { Text = title, Left = x, Top = y, Width = width, Height = 32 };
    public static Label Label(string title, int x, int y) => new() { Text = title, Left = x, Top = y + 5, Width = 105 };
    public static TextBox Text(int x, int y, int width = 170) => new() { Left = x, Top = y, Width = width };
    public static DataGridView Grid(int x, int y, int width, int height) => new() { Left = x, Top = y, Width = width, Height = height, ReadOnly = true, AllowUserToAddRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    public static int SelectedId(DataGridView grid, string column) => grid.CurrentRow == null ? 0 : Convert.ToInt32(grid.CurrentRow.Cells[column].Value);
    public static void Error(Exception ex) => MessageBox.Show(ex is SqlException ? "Database operation failed: " + ex.Message : ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    public static bool Confirm(string text) => MessageBox.Show(text, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}
