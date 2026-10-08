using Items.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Domain.Entities
{
    public class Item
    {
        public int Id { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int VehicleMakeId { get; set; }

        public int VehicleModelId { get; set; }

        public int VehicleVariantId { get; set; }

        public int VehicleCategoryId { get; set; }

        public ItemStatus Status { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public VehicleMake VehicleMake { get; set; } = null!;

        public VehicleModel VehicleModel { get; set; } = null!;

        public VehicleVariant VehicleVariant { get; set; } = null!;

        public VehicleCategory VehicleCategory { get; set; } = null!;
    }

}
