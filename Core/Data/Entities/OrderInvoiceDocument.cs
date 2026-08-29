namespace Core.Data.Entities
{
    public class OrderInvoiceDocument
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
        public byte[] PdfBytes { get; set; } = [];
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public Order? Order { get; set; }
    }
}
