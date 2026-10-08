using Items.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Application.Commands
{
    public sealed record CreateItemCommand(CreateItemRequest Request);
}
