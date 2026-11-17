using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence
{
    using PaymentEntity = Payment.Domain.Entities.Payment;

    public sealed class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
            : base(options)
        {
        }

        public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
        public DbSet<PaymentRefund> PaymentRefunds => Set<PaymentRefund>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PaymentEntity>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();
                entity.Property(x => x.PaymentReference).HasMaxLength(100).IsRequired();
                entity.Property(x => x.CustomerId).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("INR").IsRequired();
                entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
                entity.Property(x => x.PaymentMethod).HasMaxLength(50).IsRequired();
                entity.Property(x => x.IdempotencyKey).HasMaxLength(150).IsRequired();
                entity.Property(x => x.GatewayTransactionId).HasMaxLength(150);

                entity.HasIndex(x => x.PaymentReference).IsUnique();
                entity.HasIndex(x => x.IdempotencyKey).IsUnique();
                entity.HasIndex(x => x.SalesOrderId);

                entity.HasMany(x => x.Transactions)
                    .WithOne()
                    .HasForeignKey(x => x.PaymentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Refunds)
                    .WithOne()
                    .HasForeignKey(x => x.PaymentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.ToTable("PaymentTransactions");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();
                entity.Property(x => x.TransactionType).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("INR").IsRequired();
                entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
                entity.Property(x => x.GatewayResponseCode).HasMaxLength(50);
                entity.Property(x => x.GatewayMessage).HasMaxLength(500);

                entity.HasIndex(x => x.PaymentId);
            });

            modelBuilder.Entity<PaymentRefund>(entity =>
            {
                entity.ToTable("PaymentRefunds");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();
                entity.Property(x => x.RefundReference).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(50).IsRequired();

                entity.HasIndex(x => x.RefundReference).IsUnique();
                entity.HasIndex(x => x.SalesOrderPublicId);
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Topic).HasMaxLength(100).IsRequired();
                entity.Property(x => x.AggregateId).HasMaxLength(100).IsRequired();

                entity.HasIndex(x => x.CreatedAtUtc);
            });
        }
    }
}
