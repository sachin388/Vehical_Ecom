using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Application.DTO
{
    public class CalculatePriceBreakupRequest
    {
        public int ItemId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string StateCode { get; set; } = string.Empty;

        public string VehicleCategory { get; set; } = string.Empty;

        public int PriceVersion { get; set; }

        public decimal ExShowroomPrice { get; set; }

        public decimal TotalDiscount { get; set; }

        public decimal GstRate { get; set; }

        public decimal RtoAmount { get; set; }

        public decimal RegistrationAmount { get; set; }

        public decimal InsuranceAmount { get; set; }

        public decimal HandlingAmount { get; set; }

        public decimal OtherCharges { get; set; }

        public string Currency { get; set; } = "INR";
    }

}
