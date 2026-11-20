using System.Globalization;
using System.Text;
using B2B.WinForms.Forms;

namespace B2B.WinForms.Reports;

// Small dependency-free PDF writer. Uses ASCII text (diacritics transliterated) with PDF built-in Helvetica.
public static class ManifestPdf
{
    public static void Export(int orderId, string path)
    {
        var header = M2Ui.Query("SELECT o.OrderID,o.OrderDate,o.OrderStatus,a.AgentName,a.Address,a.Phone FROM dbo.[Order] o JOIN dbo.Agent a ON a.AgentID=o.AgentID WHERE o.OrderID=@id", M2Ui.P("@id", orderId));
        if (header.Rows.Count == 0) throw new InvalidOperationException("Order not found.");
        var lines = M2Ui.Query("SELECT i.ItemName,i.Size,d.Quantity,d.UnitAmount,d.Quantity*d.UnitAmount AS LineTotal FROM dbo.OrderDetail d JOIN dbo.Item i ON i.ItemID=d.ItemID WHERE d.OrderID=@id ORDER BY d.ID", M2Ui.P("@id", orderId));
        var h = header.Rows[0];
        var content = new List<string> { "SHIPPING MANIFEST", $"Order: #{orderId}    Date: {Convert.ToDateTime(h["OrderDate"]):yyyy-MM-dd HH:mm}", $"Status: {h["OrderStatus"]}", $"Agent: {h["AgentName"]}", $"Address: {h["Address"]}", $"Phone: {h["Phone"]}", "", "ITEM / SIZE                           QTY       UNIT PRICE          LINE TOTAL", new string('-', 79) };
        decimal total = 0;
        foreach (System.Data.DataRow row in lines.Rows)
        {
            decimal amount = Convert.ToDecimal(row["LineTotal"]); total += amount;
            string item = $"{row["ItemName"]} ({row["Size"]})";
            if (item.Length > 34) item = item[..34];
            content.Add($"{item,-35} {row["Quantity"],5} {Convert.ToDecimal(row["UnitAmount"]),16:N2} {amount,18:N2}");
        }
        content.Add(new string('-', 79)); content.Add($"TOTAL: {total:N2}");
        // Each page holds up to 55 lines; PDF pages are written with valid cross-reference offsets.
        var pages = content.Chunk(55).ToList();
        var objects = new List<byte[]>();
        void Add(string value) => objects.Add(Encoding.ASCII.GetBytes(value));
        Add("<< /Type /Catalog /Pages 2 0 R >>");
        var pageRefs = string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => $"{4 + i * 2} 0 R"));
        Add($"<< /Type /Pages /Kids [{pageRefs}] /Count {pages.Count} >>");
        Add("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
        foreach (var page in pages)
        {
            int pageObj = objects.Count + 1, streamObj = pageObj + 1;
            Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {streamObj} 0 R >>");
            var stream = new StringBuilder("BT /F1 9 Tf 36 755 Td 12 TL ");
            foreach (var line in page)
            {
                var clean = Ascii(line).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
                stream.Append('(').Append(clean).Append(") Tj T* ");
            }
            stream.Append("ET");
            var bytes = Encoding.ASCII.GetBytes(stream.ToString());
            var prefix = Encoding.ASCII.GetBytes($"<< /Length {bytes.Length} >>\nstream\n");
            var suffix = Encoding.ASCII.GetBytes("\nendstream");
            objects.Add(prefix.Concat(bytes).Concat(suffix).ToArray());
        }
        using var file = File.Create(path);
        void Write(string value) { var b = Encoding.ASCII.GetBytes(value); file.Write(b); }
        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (int i = 0; i < objects.Count; i++) { offsets.Add(file.Position); Write($"{i + 1} 0 obj\n"); file.Write(objects[i]); Write("\nendobj\n"); }
        long xref = file.Position; Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
    }
    static string Ascii(string value)
    {
        value = value.Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        return new string(value.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).Select(c => c <= 127 ? c : '?').ToArray());
    }
}
