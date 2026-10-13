namespace Dealer.Application.DTOs
{
    public class SetDealerInventoryRequest
    {
        public int ItemId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
