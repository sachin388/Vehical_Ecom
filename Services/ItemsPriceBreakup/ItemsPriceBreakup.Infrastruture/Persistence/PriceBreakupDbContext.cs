using ItemsPriceBreakup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Infrastruture.Persistence
{
    public class PriceBreakupDbContext : DbContext
    {
        public PriceBreakupDbContext(
            DbContextOptions<PriceBreakupDbContext> options)
            : base(options) { }

        public DbSet<PriceBreakup> PriceBreakups { get; set; }

        public DbSet<PriceBreakupLine> PriceBreakupLines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PriceBreakup>()
                .Property(x => x.FinalOnRoadPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PriceBreakup>()
                .HasMany(x => x.Lines)
                .WithOne(x => x.PriceBreakup)
                .HasForeignKey(x => x.PriceBreakupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PriceBreakupLine>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PriceBreakupLine>()
                .Property(x => x.TaxAmount)
                .HasPrecision(18, 2);
        }
    }

}
