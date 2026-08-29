using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data.Enums
{
    public enum NotificationType
    {
        OrderCreated = 1,
        OrderStatusChanged = 2,
        ReservationNoShow = 3,
        DeliveryAssigned = 4
    }
}
