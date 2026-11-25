using PackageDelivery.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDelivery.Models
{
    public abstract class Package
    {
        public int Id { get; set; }

        public string TrackingNumber { get; set; } = string.Empty;

        public int SenderId { get; set; }
        public Customer Sender { get; set; } = null!;

        public int RecipientId { get; set; }
        public Customer Recipient { get; set; } = null!;

        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public string StreetType { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string? AdditionalAddressInfo { get; set; }

        public double WeightKg { get; set; }

        public DateTime ShippedAt { get; set; }

        public PackageStatus Status { get; set; } = PackageStatus.Registered;
    }
}
