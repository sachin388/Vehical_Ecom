using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SalesOrder.Application.DTOs;
using SalesOrder.Application.Services;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Security.Claims;

namespace SalesOrder.API.Controllers
{
    public sealed class SalesOrderDbContext(DbContextOptions<SalesOrderDbContext> options)
      : DbContext(options)
    {
        public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
        public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
        public DbSet<SalesOrderHistory> SalesOrderHistory => Set<SalesOrderHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SalesOrder>().HasKey(x => x.Id);
            modelBuilder.Entity<SalesOrder>().HasIndex(x => x.PublicId).IsUnique();
            modelBuilder.Entity<SalesOrder>().HasIndex(x => x.OrderNumber).IsUnique();
            modelBuilder.Entity<SalesOrder>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<SalesOrder>().Property(x => x.GrandTotalSnapshot).HasPrecision(18, 2);
            modelBuilder.Entity<SalesOrder>().HasMany(x => x.Lines).WithOne()
                .HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesOrderLine>().Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2);
            modelBuilder.Entity<SalesOrderLine>().Property(x => x.DiscountSnapshot).HasPrecision(18, 2);
            modelBuilder.Entity<SalesOrderLine>().Property(x => x.TaxSnapshot).HasPrecision(18, 2);
            modelBuilder.Entity<SalesOrderLine>().Property(x => x.LineTotalSnapshot).HasPrecision(18, 2);

            modelBuilder.Entity<SalesOrderHistory>().HasIndex(x => new { x.SalesOrderPublicId, x.ChangedAtUtc });
            modelBuilder.Entity<SalesOrderHistory>().Property(x => x.Reason).HasMaxLength(500);
        }
    }




}
