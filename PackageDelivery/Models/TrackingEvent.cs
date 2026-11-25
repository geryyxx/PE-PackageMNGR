using PackageDelivery.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDelivery.Models
{
    public class TrackingEvent
    {
        public int Id { get; set; }

        public int PackageId { get; set; }
        public Package Package { get; set; } = null!;

        public PackageStatus Status { get; set; }

        public DateTime OccurredAt { get; set; }

        public string? Comment { get; set; }

        public DeliveryFailureReason? FailureReason { get; set; }
    }
}
