using Items.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemService.Domain.Events;

public sealed record VehicleAvailabilityChangedEvent(

    Guid EventId,
    int ItemId,
    ItemStatus OldStatus,
    ItemStatus NewStatus,
    DateTime OccurredAtUtc);




