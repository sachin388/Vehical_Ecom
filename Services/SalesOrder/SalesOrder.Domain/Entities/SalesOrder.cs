using SalesOrder.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesOrder.Domain.Entities
{
    public sealed class SalesOrder
    {
        public long Id { get; set; }
        public Guid PublicId { get; set; } = Guid.NewGuid();
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public long? DealerId { get; set; }
        public SalesOrderStatus Status { get; private set; } = SalesOrderStatus.Draft;
        public decimal SubtotalSnapshot { get; set; }
        public decimal DiscountSnapshot { get; set; }
        public decimal TaxSnapshot { get; set; }
        public decimal GrandTotalSnapshot { get; set; }
        public string Currency { get; set; } = "INR";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public byte[] RowVersion { get; set; } = [];
        public List<SalesOrderLine> Lines { get; set; } = [];

        private static readonly IReadOnlyDictionary<SalesOrderStatus, SalesOrderStatus[]> Allowed =
            new Dictionary<SalesOrderStatus, SalesOrderStatus[]>
            {
                [SalesOrderStatus.Draft] = [SalesOrderStatus.PendingPayment, SalesOrderStatus.Cancelled],
                [SalesOrderStatus.PendingPayment] = [SalesOrderStatus.Confirmed, SalesOrderStatus.Cancelled],
                [SalesOrderStatus.Confirmed] = [SalesOrderStatus.Allocated, SalesOrderStatus.Cancelled],
                [SalesOrderStatus.Allocated] = [SalesOrderStatus.Processing, SalesOrderStatus.Cancelled],
                [SalesOrderStatus.Processing] = [SalesOrderStatus.Completed],
                [SalesOrderStatus.Completed] = [],
                [SalesOrderStatus.Cancelled] = []
            };

        public void ChangeStatus(SalesOrderStatus target)
        {
            if (!Allowed.TryGetValue(Status, out var allowed) || !allowed.Contains(target))
                throw new InvalidOperationException(
                    $"Order status transition {Status} -> {target} is not allowed.");
            Status = target;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

}
