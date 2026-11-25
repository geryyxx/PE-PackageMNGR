using PackageDelivery.Data;
using PackageDelivery.Enums;
using PackageDelivery.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDelivery.Services
{
    public class PackageService
    {
        private readonly AppDbContext _db;

        public PackageService(AppDbContext db)
        {
            _db = db;
        }

        public (Customer Sender, Customer Recipient) GetSenderAndRecipient(
            int senderId, int recipientId)
        {
            if (senderId == recipientId)
                throw new ArgumentException(
                    "A feladó és a címzett nem lehet ugyanaz az ügyfél.");

            Customer? sender = _db.Customers
                .FirstOrDefault(c => c.Id == senderId);

            if (sender == null)
                throw new ArgumentException("A feladó nem található.");

            Customer? recipient = _db.Customers
                .FirstOrDefault(c => c.Id == recipientId);

            if (recipient == null)
                throw new ArgumentException("A címzett nem található.");

            return (sender, recipient);
        }
        public void ValidateDeliveryAddress(
            string postalCode,
            string city,
            string streetName,
            string streetType,
            string houseNumber)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
                throw new ArgumentException("Az irányítószám kötelező.");

            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("A település megadása kötelező.");

            if (string.IsNullOrWhiteSpace(streetName))
                throw new ArgumentException("A közterület neve kötelező.");

            if (string.IsNullOrWhiteSpace(streetType))
                throw new ArgumentException("A közterület jellege kötelező.");

            if (string.IsNullOrWhiteSpace(houseNumber))
                throw new ArgumentException("A házszám megadása kötelező.");
        }

        public void ValidateWeight(Package package)
        {
            if (!double.IsFinite(package.WeightKg) || package.WeightKg <= 0)
                throw new ArgumentException("A tömegnek pozitív számnak kell lennie.");

            double maxWeight = package switch
            {
                NormalPackage => 30,
                FragilePackage => 10,
                RefrigeratedPackage => 20,
                _ => throw new ArgumentException("Ismeretlen csomagtípus.")
            };

            if (package.WeightKg > maxWeight)
                throw new ArgumentException(
                    $"Ennél a csomagtípusnál a maximális tömeg {maxWeight} kg.");
        }

        public void ValidateSpecialData(Package package)
        {
            if (package is FragilePackage fragile)
            {
                if (!Enum.IsDefined(typeof(FragilityLevel), fragile.Fragility))
                    throw new ArgumentException("Érvénytelen törékenységi szint.");

                if (string.IsNullOrWhiteSpace(fragile.HandlingInstructions))
                    throw new ArgumentException("A kezelési utasítás kötelező.");
            }
            else if (package is RefrigeratedPackage refrigerated)
            {
                double min = refrigerated.MinTemperatureCelsius;
                double max = refrigerated.MaxTemperatureCelsius;

                if (!double.IsFinite(min) || !double.IsFinite(max))
                    throw new ArgumentException("Érvénytelen hőmérséklet.");

                if (min < -30 || max > 20)
                    throw new ArgumentException(
                        "A hőmérsékletnek -30 és +20 °C között kell lennie.");

                if (min >= max)
                    throw new ArgumentException(
                        "A minimum hőmérsékletnek kisebbnek kell lennie a maximumnál.");
            }
            else if (package is not NormalPackage)
            {
                throw new ArgumentException("Ismeretlen csomagtípus.");
            }
        }

        public Package CreatePackage(
    Package package,
    int senderId,
    int recipientId,
    string postalCode,
    string city,
    string streetName,
    string streetType,
    string houseNumber,
    string? additionalInfo)
        {
            var (sender, recipient) =
                GetSenderAndRecipient(senderId, recipientId);

            ValidateDeliveryAddress(
                postalCode, city, streetName, streetType, houseNumber);

            ValidateWeight(package);
            ValidateSpecialData(package);

            DateTime now = DateTime.Now;

            // Egyedi nyomkövetési szám előállítása
            string prefix = $"PKG-{now.Year}-";

            string? lastTrackingNumber = _db.Packages
                .Where(p => p.TrackingNumber.StartsWith(prefix))
                .OrderByDescending(p => p.TrackingNumber)
                .Select(p => p.TrackingNumber)
                .FirstOrDefault();

            int nextNumber = 1;

            if (lastTrackingNumber != null)
            {
                int lastNumber = int.Parse(
                    lastTrackingNumber.Substring(prefix.Length));

                nextNumber = lastNumber + 1;
            }

            if (nextNumber > 99999)
                throw new InvalidOperationException(
                    "Ebben az évben elfogytak a nyomkövetési azonosítók.");

            // Csomag adatainak beállítása
            package.TrackingNumber = $"{prefix}{nextNumber:D5}";

            package.SenderId = sender.Id;
            package.Sender = sender;

            package.RecipientId = recipient.Id;
            package.Recipient = recipient;

            package.PostalCode = postalCode.Trim();
            package.City = city.Trim();
            package.StreetName = streetName.Trim();
            package.StreetType = streetType.Trim();
            package.HouseNumber = houseNumber.Trim();

            package.AdditionalAddressInfo =
                string.IsNullOrWhiteSpace(additionalInfo)
                    ? null
                    : additionalInfo.Trim();

            package.ShippedAt = now;
            package.Status = PackageStatus.Registered;

            // Csomag hozzáadása
            _db.Packages.Add(package);

            // Első nyomkövetési esemény
            TrackingEvent trackingEvent = new TrackingEvent
            {
                Package = package,
                Status = PackageStatus.Registered,
                OccurredAt = now,
                Comment = "Csomag feladva"
            };

            _db.TrackingEvents.Add(trackingEvent);

            // Adatok mentése
            _db.SaveChanges();

            return package;
        }

        public Package? GetPackageByTrackingNumber(string trackingNumber)
        {
            trackingNumber = trackingNumber.Trim().ToUpperInvariant();

            return _db.Packages
                .Include(p => p.Sender)
                .Include(p => p.Recipient)
                .FirstOrDefault(p => p.TrackingNumber == trackingNumber);
        }

        public List<TrackingEvent> GetTrackingHistory(int packageId)
        {
            return _db.TrackingEvents
                .Where(t => t.PackageId == packageId)
                .OrderBy(t => t.OccurredAt)
                .ThenBy(t => t.Id)
                .ToList();
        }

        public Package AdvancePackage(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                throw new ArgumentException(
                    "A nyomkövetési azonosító kötelező.");

            Package? package = GetPackageByTrackingNumber(trackingNumber);

            if (package == null)
                throw new ArgumentException("Nem található ilyen csomag.");

            PackageStatus nextStatus = package.Status switch
            {
                PackageStatus.Registered => PackageStatus.Accepted,
                PackageStatus.Accepted => PackageStatus.AtDepot,
                PackageStatus.AtDepot => PackageStatus.OutForDelivery,
                _ => throw new InvalidOperationException(
                    "Ebből az állapotból ez a művelet nem hajtható végre.")
            };

            package.Status = nextStatus;
            TrackingEvent trackingEvent = new TrackingEvent
            {
                PackageId = package.Id,
                Status = nextStatus,
                OccurredAt = DateTime.Now,
                Comment = nextStatus switch
                {
                    PackageStatus.Accepted => "Csomag átvéve",
                    PackageStatus.AtDepot => "Csomag elosztóközpontba érkezett",
                    PackageStatus.OutForDelivery => "Kiszállítás megkezdve",
                    _ => null
                }
            };

            _db.TrackingEvents.Add(trackingEvent);

            _db.SaveChanges();

            return package;
        }


        public Package CompleteDelivery(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                throw new ArgumentException(
                    "A nyomkövetési azonosító kötelező.");

            Package? package = GetPackageByTrackingNumber(trackingNumber);

            if (package == null)
                throw new ArgumentException("Nem található ilyen csomag.");

            if (package.Status != PackageStatus.OutForDelivery)
                throw new InvalidOperationException(
                    "Sikeres kézbesítés csak kiszállítás alatt lévő csomagnál rögzíthető.");

            package.Status = PackageStatus.Delivered;

            TrackingEvent trackingEvent = new TrackingEvent
            {
                PackageId = package.Id,
                Status = PackageStatus.Delivered,
                OccurredAt = DateTime.Now,
                Comment = "Csomag sikeresen kézbesítve"
            };

            _db.TrackingEvents.Add(trackingEvent);

            _db.SaveChanges();

            return package;
        }

        public Package FailDelivery(
            string trackingNumber,
            DeliveryFailureReason reason)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                throw new ArgumentException(
                    "A nyomkövetési azonosító kötelező.");

            Package? package = GetPackageByTrackingNumber(trackingNumber);

            if (package == null)
                throw new ArgumentException("Nem található ilyen csomag.");

            if (package.Status != PackageStatus.OutForDelivery)
                throw new InvalidOperationException(
                    "Sikertelen kézbesítés csak kiszállítás alatt rögzíthető.");

            if (!Enum.IsDefined(typeof(DeliveryFailureReason), reason))
                throw new ArgumentException("Érvénytelen sikertelenségi ok.");

            int failureCount = GetDeliveryFailureCount(package.Id);

            if (failureCount >= 3)
                throw new InvalidOperationException(
                    "A csomag már elérte a három sikertelen kézbesítést.");

            package.Status = PackageStatus.DeliveryFailed;

            TrackingEvent trackingEvent = new TrackingEvent
            {
                PackageId = package.Id,
                Status = PackageStatus.DeliveryFailed,
                OccurredAt = DateTime.Now,
                FailureReason = reason,
                Comment = "Sikertelen kézbesítés"
            };

            _db.TrackingEvents.Add(trackingEvent);
            _db.SaveChanges();

            return package;
        }

        public int GetDeliveryFailureCount(int packageId)
        {
            return _db.TrackingEvents
                .Count(t => t.PackageId == packageId &&
                            t.Status == PackageStatus.DeliveryFailed);
        }


        public Package RetryDelivery(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                throw new ArgumentException(
                    "A nyomkövetési azonosító kötelező.");

            Package? package = GetPackageByTrackingNumber(trackingNumber);

            if (package == null)
                throw new ArgumentException("Nem található ilyen csomag.");

            if (package.Status != PackageStatus.DeliveryFailed)
                throw new InvalidOperationException(
                    "Újrapróbálkozás csak sikertelen kézbesítés után indítható.");

            int failureCount = GetDeliveryFailureCount(package.Id);

            if (failureCount >= 3)
                throw new InvalidOperationException(
                    "Három sikertelen kézbesítés után nem indítható újabb próbálkozás.");

            package.Status = PackageStatus.OutForDelivery;

            TrackingEvent trackingEvent = new TrackingEvent
            {
                PackageId = package.Id,
                Status = PackageStatus.OutForDelivery,
                OccurredAt = DateTime.Now,
                Comment = "Új kézbesítési kísérlet megkezdve"
            };

            _db.TrackingEvents.Add(trackingEvent);
            _db.SaveChanges();

            return package;
        }

        public Package StartReturn(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                throw new ArgumentException(
                    "A nyomkövetési azonosító kötelező.");

            Package? package = GetPackageByTrackingNumber(trackingNumber);

            if (package == null)
                throw new ArgumentException("Nem található ilyen csomag.");

            if (package.Status != PackageStatus.DeliveryFailed)
                throw new InvalidOperationException(
                    "Visszaküldés csak sikertelen kézbesítés után indítható.");

            package.Status = PackageStatus.Returning;

            TrackingEvent trackingEvent = new TrackingEvent
            {
                PackageId = package.Id,
                Status = PackageStatus.Returning,
                OccurredAt = DateTime.Now,
                Comment = "Visszaküldés megkezdve"
            };

            _db.TrackingEvents.Add(trackingEvent);
            _db.SaveChanges();

            return package;
        }

        public Package CompleteReturn(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                throw new ArgumentException(
                    "A nyomkövetési azonosító kötelező.");

            Package? package = GetPackageByTrackingNumber(trackingNumber);

            if (package == null)
                throw new ArgumentException("Nem található ilyen csomag.");

            if (package.Status != PackageStatus.Returning)
                throw new InvalidOperationException(
                    "Visszaküldés csak Returning állapotból fejezhető be.");

            package.Status = PackageStatus.Returned;

            TrackingEvent trackingEvent = new TrackingEvent
            {
                PackageId = package.Id,
                Status = PackageStatus.Returned,
                OccurredAt = DateTime.Now,
                Comment = "Csomag visszaküldve"
            };

            _db.TrackingEvents.Add(trackingEvent);
            _db.SaveChanges();

            return package;
        }

        public List<Package> GetPackagesBySender(int senderId)
        {
            return _db.Packages
                .Include(p => p.Sender)
                .Include(p => p.Recipient)
                .Where(p => p.SenderId == senderId)
                .OrderByDescending(p => p.ShippedAt)
                .ThenByDescending(p => p.Id)
                .ToList();
        }

        public List<Package> GetPackagesByRecipient(int recipientId)
        {
            return _db.Packages
                .Include(p => p.Sender)
                .Include(p => p.Recipient)
                .Where(p => p.RecipientId == recipientId)
                .OrderByDescending(p => p.ShippedAt)
                .ThenByDescending(p => p.Id)
                .ToList();
        }

        public List<Package> GetPackagesByStatus(PackageStatus status)
        {
            if (!Enum.IsDefined(typeof(PackageStatus), status))
                throw new ArgumentException("Érvénytelen csomagállapot.");

            return _db.Packages
                .Include(p => p.Sender)
                .Include(p => p.Recipient)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.ShippedAt)
                .ThenByDescending(p => p.Id)
                .ToList();
        }

        public List<Package> GetPackagesByType(int typeNumber)
        {
            IQueryable<Package> query;

            switch (typeNumber)
            {
                case 1:
                    query = _db.Packages.OfType<NormalPackage>();
                    break;

                case 2:
                    query = _db.Packages.OfType<FragilePackage>();
                    break;

                case 3:
                    query = _db.Packages.OfType<RefrigeratedPackage>();
                    break;

                default:
                    throw new ArgumentException("Érvénytelen csomagtípus.");
            }

            return query
                .Include(p => p.Sender)
                .Include(p => p.Recipient)
                .OrderByDescending(p => p.ShippedAt)
                .ThenByDescending(p => p.Id)
                .ToList();
        }

        public List<Package> GetPackagesByPeriod(DateTime startDate, DateTime endDate)
        {
            DateTime start = startDate.Date;
            DateTime end = endDate.Date;

            if (start > end)
                throw new ArgumentException(
                    "A kezdő dátum nem lehet későbbi a záró dátumnál.");

            IQueryable<Package> query = _db.Packages
                .Include(p => p.Sender)
                .Include(p => p.Recipient)
                .Where(p => p.ShippedAt >= start);

            if (end < DateTime.MaxValue.Date)
            {
                DateTime nextDay = end.AddDays(1);

                query = query.Where(p => p.ShippedAt < nextDay);
            }

            return query
                .OrderBy(p => p.ShippedAt)
                .ThenBy(p => p.Id)
                .ToList();
        }
    }
}
