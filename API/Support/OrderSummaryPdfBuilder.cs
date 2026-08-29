using System.Globalization;
using System.Text;

namespace API.Support;

internal static class OrderSummaryPdfBuilder
{
    public static byte[] Build(dynamic summary)
    {
        var lines = new List<string>
        {
            $"Order summary #{summary.DisplayOrderNumber}",
            $"Restaurant: {summary.RestaurantName}",
            $"Created: {summary.CreatedAt:yyyy-MM-dd HH:mm}",
            $"Status: {summary.Status}",
            $"Order type: {summary.OrderType}",
            $"Payment method: {summary.PaymentMethod}",
            $"Payment status: {summary.PaymentStatus}",
            string.Empty
        };

        foreach (var item in summary.Items)
        {
            lines.Add($"{item.Name} | Qty: {item.Quantity} | Price: {((decimal)item.UnitPrice).ToString("0.00", CultureInfo.InvariantCulture)} | Total: {((decimal)item.LineTotal).ToString("0.00", CultureInfo.InvariantCulture)}");
            if (!string.IsNullOrWhiteSpace((string?)item.Note))
            {
                lines.Add($"Note: {item.Note}");
            }
        }

        lines.Add(string.Empty);
        lines.Add($"Subtotal: {((decimal)summary.Subtotal).ToString("0.00", CultureInfo.InvariantCulture)}");
        lines.Add($"Delivery fee: {((decimal)summary.DeliveryFee).ToString("0.00", CultureInfo.InvariantCulture)}");
        lines.Add($"Total: {((decimal)summary.Total).ToString("0.00", CultureInfo.InvariantCulture)}");
        lines.Add(string.Empty);
        return BuildPdf(BuildContentStream(lines));
    }

    private static string BuildContentStream(IEnumerable<string> lines)
    {
        var content = new StringBuilder("BT\n/F1 12 Tf\n50 780 Td\n");
        var first = true;
        foreach (var line in lines)
        {
            if (!first)
                content.AppendLine("0 -16 Td");
            content.Append('(').Append(Escape(line)).AppendLine(") Tj");
            first = false;
        }

        content.AppendLine("ET");
        return content.ToString();
    }

    private static byte[] BuildPdf(string content)
    {
        var objects = new List<string>
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
            "2 0 obj << /Type /Pages /Count 1 /Kids [3 0 R] >> endobj",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj",
            $"5 0 obj << /Length {Encoding.ASCII.GetByteCount(content)} >> stream\n{content}endstream endobj"
        };

        var document = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(document.ToString()));
            document.Append(obj).Append('\n');
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(document.ToString());
        document.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        for (var index = 1; index < offsets.Count; index++)
            document.Append(offsets[index].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");

        document.Append($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return Encoding.ASCII.GetBytes(document.ToString());
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("(", "\\(", StringComparison.Ordinal)
        .Replace(")", "\\)", StringComparison.Ordinal);
}
