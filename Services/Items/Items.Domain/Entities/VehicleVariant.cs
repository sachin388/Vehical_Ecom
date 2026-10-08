using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Domain.Entities
{
    public class VehicleVariant
    {
        public int Id { get; set; }

        public int VehicleModelId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string VariantCode { get; set; } = string.Empty;

        public string? Engine { get; set; }

        public string? Transmission { get; set; }

        public decimal? EngineCapacity { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public VehicleModel VehicleModel { get; set; } = null!;

        public ICollection<VehicleSpecification> Specifications { get; set; }
            = new List<VehicleSpecification>();
    }


}
