using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ItemService.Domain.Events;

public sealed record VehicleCreatedEvent(
    Guid EventId,
    int ItemId,
    string SKU,
    string Name,
    int VehicleMakeId,
    int VehicleModelId,
    int VehicleVariantId,
    int VehicleCategoryId,
    DateTime OccurredAtUtc);


