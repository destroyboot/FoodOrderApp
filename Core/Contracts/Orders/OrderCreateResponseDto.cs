using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Orders
{
    public class OrderCreateResponseDto
    {
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public decimal Total { get; set; }
        public int? EstimatedPreparationMinutes { get; set; }
        public DateTime? EstimatedReadyAt { get; set; }
    }
}
