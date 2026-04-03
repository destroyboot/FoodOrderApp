using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data.Enums
{
    public enum OrderStatus
    {
        Draft = 0,
        Pending = 1,
        Accepted = 2,
        Preparing = 3,
        Ready = 4,
        Completed = 5,
        Cancelled = 6
    }
}
