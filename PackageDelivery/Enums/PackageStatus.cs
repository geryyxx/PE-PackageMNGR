using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDelivery.Enums
{
    public enum PackageStatus
    {
        Registered,
        Accepted,
        AtDepot,
        OutForDelivery,
        DeliveryFailed,
        Delivered,
        Returning,
        Returned
    }
}
