using System.Globalization;
using System.Text;

namespace API.Support
{
    internal static class InvoicePdfBuilder
    {
        public static byte[] Build(dynamic invoice)
        {
            var lines = new List<string>
            {
                $"Invoice {invoice.InvoiceNumber}",
                $"Order #{invoice.OrderId}",
                $"Date: {invoice.CreatedAt:yyyy-MM-dd HH:mm}",
                $"Customer: {invoice.CustomerName}",
                $"Address: {invoice.Address}",
                $"Tax ID: {invoice.TaxId}",
                " ",
                "Items:"
            };

            foreach (var item in invoice.Items)
            {
                lines.Add($"- MenuItem #{item.MenuItemId} x{item.Quantity} @ {((decimal)item.UnitPrice).ToString("0.00", CultureInfo.InvariantCulture)} = {((decimal)item.LineTotal).ToString("0.00", CultureInfo.InvariantCulture)}");
                if (!string.IsNullOrWhiteSpace((string?)item.Note))
                    lines.Add($"  Note: {item.Note}");
            }

            lines.Add(" ");
            lines.Add($"Subtotal: {((decimal)invoice.Subtotal).ToString("0.00", CultureInfo.InvariantCulture)}");
            lines.Add($"Delivery fee: {((decimal)invoice.DeliveryFee).ToString("0.00", CultureInfo.InvariantCulture)}");
            lines.Add($"Total: {((decimal)invoice.Total).ToString("0.00", CultureInfo.InvariantCulture)}");
            lines.Add(" ");
            lines.Add("Mock invoice PDF generated for development/testing.");

            var content = BuildContentStream(lines);
            return BuildPdf(content);
        }

        private static string BuildContentStream(IEnumerable<string> lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BT");
            sb.AppendLine("/F1 12 Tf");
            sb.AppendLine("50 780 Td");

            var isFirst = true;
            foreach (var line in lines)
            {
                if (!isFirst)
                    sb.AppendLine("0 -16 Td");
                sb.AppendLine($"({Escape(line)}) Tj");
                isFirst = false;
            }

            sb.AppendLine("ET");
            return sb.ToString();
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
                sb.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");

            sb.Append("trailer << /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
            sb.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private static string Escape(string value) =>
            value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
