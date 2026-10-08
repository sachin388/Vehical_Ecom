using Items.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Application.Queries
{
    public sealed record SearchItemsQuery(ItemSearchRequest Request);
}
