using Microsoft.EntityFrameworkCore;
using PackageDelivery.Data;
using PackageDelivery.Enums;
using PackageDelivery.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDelivery.Services
{
    public class ReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db)
        {
            _db = db;
        }

        public List<(PackageStatus Status, int Count)>
            GetPackageCountsByStatus()
        {
            var results = _db.Packages
                .AsNoTracking()
                .GroupBy(p => p.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Status)
                .ToList();

            return results
                .Select(x => (Status: x.Status, Count: x.Count))
                .ToList();
        }

        public List<(string Type, int Count, double TotalWeight, double AverageWeight)>
            GetPackageTypeStatistics()
        {
            var grouped = _db.Packages
                .AsNoTracking()
                .GroupBy(p => EF.Property<string>(p, "PackageType"))
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    TotalWeight = g.Sum(p => p.WeightKg),
                    AverageWeight = g.Average(p => p.WeightKg)
                })
                .ToList();

            var types = new[]
            {
                new { Key = "Normal", Name = "Normál" },
                new { Key = "Fragile", Name = "Törékeny" },
                new { Key = "Refrigerated", Name = "Hűtött" }
            };

            var results = new List<
                (string Type, int Count, double TotalWeight, double AverageWeight)>();

            foreach (var type in types)
            {
                var group = grouped.FirstOrDefault(g => g.Type == type.Key);

                if (group == null)
                {
                    results.Add((type.Name, 0, 0, 0));
                }
                else
                {
                    results.Add((
                        type.Name,
                        group.Count,
                        group.TotalWeight,
                        group.AverageWeight
                    ));
                }
            }

            return results
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Type)
                .ToList();
        }

        public List<(string SenderName, int PackageCount, double TotalWeight)>
            GetTopSenders()
        {
            var results = _db.Packages
                .AsNoTracking()
                .GroupBy(p => new
                {
                    p.SenderId,
                    p.Sender.Name
                })
                .Select(g => new
                {
                    SenderName = g.Key.Name,
                    PackageCount = g.Count(),
                    TotalWeight = g.Sum(p => p.WeightKg)
                })
                .OrderByDescending(x => x.PackageCount)
                .ThenBy(x => x.SenderName)
                .Take(5)
                .ToList();

            return results
                .Select(x => (
                    SenderName: x.SenderName,
                    PackageCount: x.PackageCount,
                    TotalWeight: x.TotalWeight
                ))
                .ToList();
        }

        public List<(DeliveryFailureReason Reason, int Count)>
            GetDeliveryFailureStatistics()
        {
            var grouped = _db.TrackingEvents
                .AsNoTracking()
                .Where(t => t.Status == PackageStatus.DeliveryFailed &&
                            t.FailureReason.HasValue)
                .GroupBy(t => t.FailureReason)
                .Select(g => new
                {
                    Reason = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var results = new List<
                (DeliveryFailureReason Reason, int Count)>();

            foreach (DeliveryFailureReason reason
                in Enum.GetValues<DeliveryFailureReason>())
            {
                var group = grouped.FirstOrDefault(
                    g => g.Reason == reason);

                int count = group == null ? 0 : group.Count;

                results.Add((reason, count));
            }

            return results
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Reason)
                .ToList();
        }

        public (int Count, double TotalWeight, double AverageWeight,List<Package> Packages)
            GetShipmentPeriodReport(DateTime startDate, DateTime endDate)
        {
            DateTime start = startDate.Date;
            DateTime end = endDate.Date;

            if (start > end)
                throw new ArgumentException(
                    "A kezdő dátum nem lehet későbbi a záró dátumnál.");

            IQueryable<Package> query = _db.Packages
                .AsNoTracking()
                .Where(p => p.ShippedAt >= start);

            if (end < DateTime.MaxValue.Date)
            {
                DateTime nextDay = end.AddDays(1);

                query = query.Where(p => p.ShippedAt < nextDay);
            }

            int count = query.Count();

            double totalWeight = 0;
            double averageWeight = 0;

            if (count > 0)
            {
                totalWeight = query.Sum(p => p.WeightKg);
                averageWeight = query.Average(p => p.WeightKg);
            }

            List<Package> packages = query
                .Include(p => p.Sender)
                .Include(p => p.Recipient)
                .OrderBy(p => p.ShippedAt)
                .ThenBy(p => p.Id)
                .ToList();

            return (count, totalWeight, averageWeight, packages);
        }

        public List<Package> GetActiveSpecialPackages()
        {
            return _db.Packages
                .AsNoTracking()
                .Include(p => p.Sender)
                .Include(p => p.Recipient)
                .Where(p =>
                    (EF.Property<string>(p, "PackageType") == "Fragile" ||
                     EF.Property<string>(p, "PackageType") == "Refrigerated") &&
                    p.Status != PackageStatus.Delivered &&
                    p.Status != PackageStatus.Returned)
                .OrderBy(p => p.ShippedAt)
                .ThenBy(p => p.Id)
                .ToList();
        }
    }
}
