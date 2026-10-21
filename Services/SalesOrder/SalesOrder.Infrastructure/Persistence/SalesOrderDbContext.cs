using Microsoft.EntityFrameworkCore;
using SalesOrder.Domain.Entities;

namespace SalesOrder.Infrastructure.Persistence
{
    using SalesOrder = SalesOrder.Domain.Entities.SalesOrder;

    public sealed class SalesOrderDbContext : DbContext
    {
        public SalesOrderDbContext(DbContextOptions<SalesOrderDbContext> options)
            : base(options)
        {
        }

        public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
        public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
        public DbSet<SalesOrderHistory> SalesOrderHistory => Set<SalesOrderHistory>();
        public DbSet<OrderCancellationSaga> OrderCancellationSagas => Set<OrderCancellationSaga>();
        public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SalesOrder>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.PublicId).IsUnique();
                entity.HasIndex(x => x.OrderNumber).IsUnique();
                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.Property(x => x.GrandTotalSnapshot).HasPrecision(18, 2);
                entity.Property(x => x.SubtotalSnapshot).HasPrecision(18, 2);
                entity.Property(x => x.DiscountSnapshot).HasPrecision(18, 2);
                entity.Property(x => x.TaxSnapshot).HasPrecision(18, 2);
                entity.HasMany(x => x.Lines)
                    .WithOne()
                    .HasForeignKey(x => x.SalesOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SalesOrderLine>(entity =>
            {
                entity.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2);
                entity.Property(x => x.DiscountSnapshot).HasPrecision(18, 2);
                entity.Property(x => x.TaxSnapshot).HasPrecision(18, 2);
                entity.Property(x => x.LineTotalSnapshot).HasPrecision(18, 2);
            });

            modelBuilder.Entity<SalesOrderHistory>(entity =>
            {
                entity.HasIndex(x => new { x.SalesOrderPublicId, x.ChangedAtUtc });
                entity.Property(x => x.Reason).HasMaxLength(500);
            });

            modelBuilder.Entity<OrderCancellationSaga>(entity =>
            {
                entity.HasKey(x => x.Id);
            });

            modelBuilder.Entity<ProcessedIntegrationEvent>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ConsumerName, x.EventId });
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(x => x.Id);
            });
        }
    }
}
