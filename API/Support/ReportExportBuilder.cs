using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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

    public static byte[] BuildPdfFromHtml(string html)
    {
        try
        {
            return BuildBrowserPdf(WrapHtmlDocument(html));
        }
        catch
        {
            return BuildSimplePdfFromHtml(html);
        }
    }

    private static byte[] BuildBrowserPdf(string html)
    {
        var browserPath = FindBrowserPath()
            ?? throw new InvalidOperationException("Chrome or Edge executable was not found.");

        var tempRoot = Path.Combine(Path.GetTempPath(), "FoodOrderAppPdf", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var htmlPath = Path.Combine(tempRoot, "document.html");
            var pdfPath = Path.Combine(tempRoot, "document.pdf");
            var userDataPath = Path.Combine(tempRoot, "profile");
            File.WriteAllText(htmlPath, html, new UTF8Encoding(false));

            var startInfo = new ProcessStartInfo
            {
                FileName = browserPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("--headless=new");
            startInfo.ArgumentList.Add("--disable-gpu");
            startInfo.ArgumentList.Add("--disable-extensions");
            startInfo.ArgumentList.Add("--disable-background-networking");
            startInfo.ArgumentList.Add("--no-first-run");
            startInfo.ArgumentList.Add("--no-default-browser-check");
            startInfo.ArgumentList.Add($"--user-data-dir={userDataPath}");
            startInfo.ArgumentList.Add("--print-to-pdf-no-header");
            startInfo.ArgumentList.Add($"--print-to-pdf={pdfPath}");
            startInfo.ArgumentList.Add(new Uri(htmlPath).AbsoluteUri);

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start browser PDF renderer.");

            if (!process.WaitForExit(30000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException("Browser PDF renderer timed out.");
            }

            if (process.ExitCode != 0 || !File.Exists(pdfPath))
            {
                throw new InvalidOperationException("Browser PDF renderer failed.");
            }

            return File.ReadAllBytes(pdfPath);
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }

    private static byte[] BuildSimplePdfFromHtml(string html)
    {
        var lines = HtmlToLines(html)
            .SelectMany(line => WrapLine(line, MaxPdfLineLength))
            .ToList();

        if (lines.Count == 0)
        {
            lines.Add("Document");
        }

        return BuildSimplePdf(Paginate(lines, PdfLinesPerPage));
    }

    private static string WrapHtmlDocument(string html)
    {
        html ??= string.Empty;

        if (Regex.IsMatch(html, @"<\s*html[\s>]", RegexOptions.IgnoreCase))
        {
            return html;
        }

        return $$"""
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8">
          <style>
            @page { size: A4; margin: 14mm; }
            * { box-sizing: border-box; }
            body {
              margin: 0;
              color: #172033;
              font-family: Arial, Helvetica, sans-serif;
              font-size: 12px;
              line-height: 1.45;
            }
            h1, h2, h3 { margin: 0 0 10px; line-height: 1.2; color: #111827; }
            h1 { font-size: 24px; }
            h2 { font-size: 18px; margin-top: 18px; }
            h3 { font-size: 15px; margin-top: 14px; }
            p { margin: 0 0 10px; }
            ul, ol { margin: 0 0 12px 22px; padding: 0; }
            table {
              width: 100%;
              border-collapse: collapse;
              margin: 12px 0;
              page-break-inside: auto;
            }
            thead { display: table-header-group; }
            tr { page-break-inside: avoid; page-break-after: auto; }
            th, td {
              border: 1px solid #d0d5dd;
              padding: 6px 8px;
              text-align: left;
              vertical-align: top;
            }
            th { background: #f2f4f7; font-weight: 700; }
            hr { border: 0; border-top: 1px solid #d0d5dd; margin: 14px 0; }
            .text-center { text-align: center; }
            .text-right { text-align: right; }
          </style>
        </head>
        <body>
        {{html}}
        </body>
        </html>
        """;
    }

    private static string? FindBrowserPath()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("FOODORDER_PDF_BROWSER_PATH"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe")
        };

        return candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
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

    private static IReadOnlyList<string> HtmlToLines(string html)
    {
        var normalized = string.IsNullOrWhiteSpace(html) ? string.Empty : html;
        normalized = Regex.Replace(normalized, @"<\s*br\s*/?\s*>", "\n", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"<\s*/\s*(p|div|h[1-6]|li|tr|table|ul|ol)\s*>", "\n", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"<\s*(td|th)\b[^>]*>", " | ", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"<[^>]+>", " ");
        normalized = WebUtility.HtmlDecode(normalized);

        return normalized
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .Select(line => Regex.Replace(line, @"\s+", " ").Trim(' ', '|'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
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
