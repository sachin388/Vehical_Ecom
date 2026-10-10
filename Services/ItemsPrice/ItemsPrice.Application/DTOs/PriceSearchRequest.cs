using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Application.DTOs
{
    public class PriceSearchRequest
    {
        public int? ItemId { get; set; }

        public string? SKU { get; set; }

        public PriceStatus? Status { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string SortBy { get; set; } = "effectiveFrom";

        public bool Descending { get; set; }
    }

}
