using System.Data;
using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace API.Support;

internal static class ReportExportBuilder
{
    private const int MaxPdfLineLength = 96;
    private const int PdfLinesPerPage = 43;

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

    public static byte[] BuildPdf(
        DataTable table,
        string title,
        IReadOnlyList<(string Label, string Value)> summary,
        IReadOnlyDictionary<string, string> columnLabels)
    {
        var lines = new List<string>
        {
            string.IsNullOrWhiteSpace(title) ? "Business report" : title,
            $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC",
            $"Rows: {table.Rows.Count}",
            string.Empty
        };

        if (summary.Count > 0)
        {
            lines.Add("Summary:");
            foreach (var metric in summary)
            {
                lines.Add($"- {metric.Label}: {metric.Value}");
            }

            lines.Add(string.Empty);
        }

        lines.Add("Data:");
        var rowNumber = 1;
        foreach (DataRow row in table.Rows)
        {
            var cells = table.Columns
                .Cast<DataColumn>()
                .Select(column =>
                {
                    var label = columnLabels.TryGetValue(column.ColumnName, out var resolvedLabel)
                        ? resolvedLabel
                        : column.ColumnName;
                    return $"{label}: {FormatCell(row[column])}";
                });

            foreach (var line in WrapLine($"{rowNumber}. {string.Join(" | ", cells)}", MaxPdfLineLength))
            {
                lines.Add(line);
            }

            rowNumber++;
        }

        if (table.Rows.Count == 0)
        {
            lines.Add("- No data");
        }

        return BuildSimplePdf(Paginate(lines, PdfLinesPerPage));
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

    private static List<List<string>> Paginate(IReadOnlyList<string> lines, int linesPerPage)
    {
        var pages = new List<List<string>>();
        for (var i = 0; i < lines.Count; i += linesPerPage)
        {
            pages.Add(lines.Skip(i).Take(linesPerPage).ToList());
        }

        if (pages.Count == 0)
        {
            pages.Add(new List<string>());
        }

        return pages;
    }

    private static IEnumerable<string> WrapLine(string value, int maxLength)
    {
        value = ToPdfText(value);
        if (value.Length <= maxLength)
        {
            yield return value;
            yield break;
        }

        var remaining = value;
        var isContinuation = false;
        while (remaining.Length > maxLength)
        {
            var prefix = isContinuation ? "   " : string.Empty;
            var available = maxLength - prefix.Length;
            var splitAt = remaining.LastIndexOf(' ', available);
            if (splitAt < available / 2)
            {
                splitAt = available;
            }

            yield return prefix + remaining[..splitAt].TrimEnd();
            remaining = remaining[splitAt..].TrimStart();
            isContinuation = true;
        }

        if (remaining.Length > 0)
        {
            yield return "   " + remaining;
        }
    }

    private static byte[] BuildSimplePdf(IReadOnlyList<List<string>> pages)
    {
        var objects = new List<string>
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj"
        };

        var pageObjectNumbers = Enumerable.Range(0, pages.Count)
            .Select(index => 3 + index * 2)
            .ToList();
        objects.Add($"2 0 obj << /Type /Pages /Count {pages.Count} /Kids [{string.Join(" ", pageObjectNumbers.Select(number => $"{number} 0 R"))}] >> endobj");

        for (var i = 0; i < pages.Count; i++)
        {
            var pageObjectNumber = pageObjectNumbers[i];
            var contentObjectNumber = pageObjectNumber + 1;
            var content = BuildContentStream(pages[i], i + 1, pages.Count);
            objects.Add($"{pageObjectNumber} 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 {3 + pages.Count * 2} 0 R >> >> /Contents {contentObjectNumber} 0 R >> endobj");
            objects.Add($"{contentObjectNumber} 0 obj << /Length {Encoding.ASCII.GetByteCount(content)} >> stream\n{content}endstream endobj");
        }

        objects.Add($"{3 + pages.Count * 2} 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj");

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append(obj).Append('\n');
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append($"xref\n0 {objects.Count + 1}\n");
        sb.Append("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            sb.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        sb.Append("trailer << /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        sb.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string BuildContentStream(IReadOnlyList<string> lines, int pageNumber, int pageCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BT");
        sb.AppendLine("/F1 10 Tf");
        sb.AppendLine("40 555 Td");

        var printableLines = lines
            .Concat(new[] { string.Empty, $"Page {pageNumber} of {pageCount}" })
            .ToList();

        var isFirst = true;
        foreach (var line in printableLines)
        {
            if (!isFirst)
            {
                sb.AppendLine("0 -12 Td");
            }

            sb.AppendLine($"({EscapePdfText(line)}) Tj");
            isFirst = false;
        }

        sb.AppendLine("ET");
        return sb.ToString();
    }

    private static string EscapePdfText(string value)
        => ToPdfText(value)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    private static string ToPdfText(string value)
    {
        var normalized = value
            .Replace("ą", "a", StringComparison.Ordinal).Replace("Ą", "A", StringComparison.Ordinal)
            .Replace("ć", "c", StringComparison.Ordinal).Replace("Ć", "C", StringComparison.Ordinal)
            .Replace("ę", "e", StringComparison.Ordinal).Replace("Ę", "E", StringComparison.Ordinal)
            .Replace("ł", "l", StringComparison.Ordinal).Replace("Ł", "L", StringComparison.Ordinal)
            .Replace("ń", "n", StringComparison.Ordinal).Replace("Ń", "N", StringComparison.Ordinal)
            .Replace("ó", "o", StringComparison.Ordinal).Replace("Ó", "O", StringComparison.Ordinal)
            .Replace("ś", "s", StringComparison.Ordinal).Replace("Ś", "S", StringComparison.Ordinal)
            .Replace("ż", "z", StringComparison.Ordinal).Replace("Ż", "Z", StringComparison.Ordinal)
            .Replace("ź", "z", StringComparison.Ordinal).Replace("Ź", "Z", StringComparison.Ordinal);

        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            sb.Append(ch is >= ' ' and <= '~' ? ch : '?');
        }

        return sb.ToString();
    }
}
