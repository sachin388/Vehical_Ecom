using Items.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items.Application.DTOs
{
    public sealed class ItemResponse
    {
        public int Id { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int MakeId { get; set; }

        public string MakeName { get; set; } = string.Empty;

        public int ModelId { get; set; }

        public string ModelName { get; set; } = string.Empty;

        public int VariantId { get; set; }

        public string VariantName { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public ItemStatus Status { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }



}
