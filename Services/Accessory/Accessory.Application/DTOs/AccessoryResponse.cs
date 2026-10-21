using System;
using System.Collections.Generic;
using Accessory.Domain.Enums;

namespace Accessory.Application.DTOs
{
    public class AccessoryResponse
    {
        public long Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Currency { get; set; } = "INR";

        public int AvailableQuantity { get; set; }

        public AccessoryStatus Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public List<AccessoryCompatibilityResponse> Compatibilities { get; set; } =
            new List<AccessoryCompatibilityResponse>();
    }
}
