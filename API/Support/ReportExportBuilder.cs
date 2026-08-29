using System.Data;
using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace API.Support;

internal static class ReportExportBuilder
{
    public static byte[] BuildCsv(DataTable table)
    {
        var sb = new StringBuilder();

        for (var columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
        {
            if (columnIndex > 0)
            {
                sb.Append(',');
            }

            sb.Append(EscapeCsv(table.Columns[columnIndex].ColumnName));
        }

        sb.AppendLine();

        foreach (DataRow row in table.Rows)
        {
            for (var columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
            {
                if (columnIndex > 0)
                {
                    sb.Append(',');
                }

                sb.Append(EscapeCsv(FormatCell(row[columnIndex])));
            }

            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] BuildExcel(DataTable table, string sheetName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Report" : sheetName[..Math.Min(sheetName.Length, 31)]);
        worksheet.Cell(1, 1).InsertTable(table, true);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"'))
        {
            value = value.Replace("\"", "\"\"");
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value}\"";
        }

        return value;
    }

    private static string FormatCell(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DBNull => string.Empty,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal dec => dec.ToString("0.00", CultureInfo.InvariantCulture),
            double dbl => dbl.ToString("0.##", CultureInfo.InvariantCulture),
            float flt => flt.ToString("0.##", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }
}
