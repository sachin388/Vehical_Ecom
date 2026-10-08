using Microsoft.EntityFrameworkCore;
using Items.Domain.Entities;
namespace Items.Infrastructure.Persistence
{
    public class ItemDbContext : DbContext
    {
        public ItemDbContext(DbContextOptions<ItemDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items => Set<Item>();

        public DbSet<VehicleMake> VehicleMakes => Set<VehicleMake>();

        public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();

        public DbSet<VehicleVariant> VehicleVariants => Set<VehicleVariant>();

        public DbSet<VehicleCategory> VehicleCategories => Set<VehicleCategory>();

        public DbSet<VehicleSpecification> VehicleSpecifications =>
            Set<VehicleSpecification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.SKU)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(x => x.SKU)
                    .IsUnique();

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Description)
                    .HasMaxLength(2000);

                entity.HasOne(x => x.VehicleMake)
                    .WithMany()
                    .HasForeignKey(x => x.VehicleMakeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.VehicleModel)
                    .WithMany()
                    .HasForeignKey(x => x.VehicleModelId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.VehicleVariant)
                    .WithMany()
                    .HasForeignKey(x => x.VehicleVariantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.VehicleCategory)
                    .WithMany(x => x.Items)
                    .HasForeignKey(x => x.VehicleCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VehicleMake>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });

            modelBuilder.Entity<VehicleModel>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasOne(x => x.VehicleMake)
                    .WithMany(x => x.Models)
                    .HasForeignKey(x => x.VehicleMakeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VehicleVariant>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.VariantCode)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(x => x.VariantCode)
                    .IsUnique();

                entity.HasOne(x => x.VehicleModel)
                    .WithMany(x => x.Variants)
                    .HasForeignKey(x => x.VehicleModelId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VehicleCategory>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });

            modelBuilder.Entity<VehicleSpecification>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Value)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasOne(x => x.VehicleVariant)
                    .WithMany(x => x.Specifications)
                    .HasForeignKey(x => x.VehicleVariantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

}
