using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Accessory.Infrastructure.Persistence
{
    public class AccessoryDbContext : DbContext
    {
        public AccessoryDbContext(
            DbContextOptions<AccessoryDbContext> options)
            : base(options) { }

        public DbSet<Accessory> Accessories { get; set; }

        public DbSet<AccessoryCompatibility>
            AccessoryCompatibilities
        { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Accessory>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Code)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(x => x.Code)
                    .IsUnique();

                entity.Property(x => x.Price)
                    .HasPrecision(18, 2);

                entity.HasMany(x => x.Compatibilities)
                    .WithOne(x => x.Accessory)
                    .HasForeignKey(x => x.AccessoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AccessoryCompatibility>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new
                {
                    x.AccessoryId,
                    x.ItemId
                }).IsUnique();
            });
        }
    }

}
