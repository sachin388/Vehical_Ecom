using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Application.DTOs
{
    public sealed class UpdateItemRequest
    {
        public string Name { get; set; } = string.Empty;

        public int VehicleMakeId { get; set; }

        public int VehicleModelId { get; set; }

        public int VehicleVariantId { get; set; }

        public int VehicleCategoryId { get; set; }

        public string? Description { get; set; }
    }


}
