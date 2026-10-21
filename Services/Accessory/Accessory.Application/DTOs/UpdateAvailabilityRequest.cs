using System;
using Accessory.Domain.Enums;

namespace Accessory.Application.DTOs
{
    public class UpdateAvailabilityRequest
    {
        public int AvailableQuantity { get; set; }

        public AccessoryStatus Status { get; set; }
    }
}
