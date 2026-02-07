using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        // Owner key: either Identity userId OR guest token (same pattern as Order.CustomerId)
        public string OwnerKey { get; set; } = null!;

        public NotificationType Type { get; set; }

        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;

        // Optional: store extra data if you want
        public string? PayloadJson { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}
