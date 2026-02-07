using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Orders
{
    public class CartPreviewResponseDto
    {
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Total { get; set; }

        public int EstimatedPreparationMinutes { get; set; }
        public DateTime? EstimatedReadyAt { get; set; }
    }
}
