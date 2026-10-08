using Items.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Application.DTOs
{
    public sealed class ChangeItemStatusRequest
    {
        public ItemStatus Status { get; set; }
    }

}
