using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data.Enums
{
    public enum PaymentStatus
    {
        Unpaid = 0,
        Pending = 1,
        Paid = 2,
        Failed = 3,
        Refunded = 4
    }
}
