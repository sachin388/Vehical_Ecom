using ItemsPriceBreakup.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Application.DTO
{
    public class PriceBreakupResponse
    {
        public long Id { get; set; }

        public int ItemId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string StateCode { get; set; } = string.Empty;

        public string VehicleCategory { get; set; } = string.Empty;

        public int PriceVersion { get; set; }

        public string Currency { get; set; } = "INR";

        public decimal SubTotal { get; set; }

        public decimal TotalTax { get; set; }

        public decimal TotalCharges { get; set; }

        public decimal TotalDiscount { get; set; }

        public decimal FinalOnRoadPrice { get; set; }

        public PriceBreakupStatus Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public List<PriceBreakupLineResponse> Lines { get; set; } =
            new List<PriceBreakupLineResponse>();
    }

}
