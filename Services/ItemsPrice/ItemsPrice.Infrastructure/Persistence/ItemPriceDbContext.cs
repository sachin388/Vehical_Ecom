using ItemsPrice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Infrastructure.Persistence
{
    public class ItemPriceDbContext : DbContext
    {
        public ItemPriceDbContext(
            DbContextOptions<ItemPriceDbContext> options)
            : base(options)
        {
        }

        public DbSet<ItemPrice> ItemPrices { get; set; }

        public DbSet<PriceDiscount> PriceDiscounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ItemPrice>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.SKU)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Currency)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(x => x.BasePrice)
                    .HasPrecision(18, 2);

                entity.HasIndex(x => new { x.ItemId, x.Version })
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.ItemId,
                    x.EffectiveFromUtc
                });

                entity.HasMany(x => x.Discounts)
                    .WithOne(x => x.ItemPrice)
                    .HasForeignKey(x => x.ItemPriceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PriceDiscount>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.DiscountValue)
                    .HasPrecision(18, 2);

                entity.Property(x => x.MaximumDiscountAmount)
                    .HasPrecision(18, 2);
            });
        }
    }

}
