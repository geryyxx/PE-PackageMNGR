using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PackageDelivery.Models;

namespace PackageDelivery.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<TrackingEvent> TrackingEvents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=packagedelivery.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Package>()
                .HasDiscriminator<string>("PackageType")
                .HasValue<NormalPackage>("Normal")
                .HasValue<FragilePackage>("Fragile")
                .HasValue<RefrigeratedPackage>("Refrigerated");

            modelBuilder.Entity<Package>()
                .HasOne(p => p.Sender)
                .WithMany()
                .HasForeignKey(p => p.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Package>()
                .HasOne(p => p.Recipient)
                .WithMany()
                .HasForeignKey(p => p.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Package>()
                .HasIndex(p => p.TrackingNumber)
                .IsUnique();

            modelBuilder.Entity<TrackingEvent>()
                .HasOne(t => t.Package)
                .WithMany()
                .HasForeignKey(t => t.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
