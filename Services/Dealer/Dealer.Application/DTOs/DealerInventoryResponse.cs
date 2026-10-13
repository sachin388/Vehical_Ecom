using System;

namespace Dealer.Application.DTOs
{
    public class DealerInventoryResponse
    {
        public long Id { get; set; }
        public long DealerId { get; set; }
        public int ItemId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
