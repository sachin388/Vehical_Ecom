using Dealer.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dealer.Domain.Entities
{
    public class Dealer
    {
        public long Id { get; set; }
        public string DealerCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DealerStatus Status { get; set; } = DealerStatus.Active;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public ICollection<DealerBranch> Branches { get; set; } = new List<DealerBranch>();
    }

}
