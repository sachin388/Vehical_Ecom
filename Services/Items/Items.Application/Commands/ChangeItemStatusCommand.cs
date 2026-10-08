using Items.Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemService.Application.Commands;

public sealed record ChangeItemStatusCommand(

    int ItemId,
    ItemStatus NewStatus);


