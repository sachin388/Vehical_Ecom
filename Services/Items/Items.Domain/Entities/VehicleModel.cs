using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Domain.Entities
{
    public class VehicleModel
    {
        public int Id { get; set; }

        public int VehicleMakeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public VehicleMake VehicleMake { get; set; } = null!;

        public ICollection<VehicleVariant> Variants { get; set; }
            = new List<VehicleVariant>();
    }

}
