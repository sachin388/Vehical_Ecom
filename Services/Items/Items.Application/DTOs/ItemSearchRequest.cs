using Items.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Application.DTOs
{
    public sealed class ItemSearchRequest
    {
        public string? Search { get; set; }

        public int? MakeId { get; set; }

        public int? ModelId { get; set; }

        public int? VariantId { get; set; }

        public int? CategoryId { get; set; }

        public ItemStatus? Status { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string SortBy { get; set; } = "name";

        public bool Descending { get; set; }
    }

}
