using SalesOrder.Application.DTOs;
using SalesOrder.Application.Interfaces;
using SalesOrder.Domain.Entities;
using SalesOrder.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SalesOrder.Application.Services
{
    using SalesOrder = SalesOrder.Domain.Entities.SalesOrder;

    public sealed class SalesOrderApplicationService : ISalesOrderApplicationService
    {
        private readonly ISalesOrderRepository _orders;
        private readonly IPricingClient _pricing;
        private readonly IOutboxWriter _outbox;

        public SalesOrderApplicationService(
            ISalesOrderRepository orders, IPricingClient pricing, IOutboxWriter outbox)
        {
            _orders = orders;
            _pricing = pricing;
            _outbox = outbox;
        }

        public async Task<SalesOrderResponse> CreateAsync(
            string customerId, CreateSalesOrderRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("Authenticated customer is required.");
            if (request.Lines.Count == 0)
                throw new ArgumentException("At least one order line is required.");

            var prices = await _pricing.GetPriceSnapshotsAsync(request.Lines, request.Currency, ct);
            if (prices.Count != request.Lines.Count)
                throw new InvalidOperationException("Pricing response is incomplete.");

            var order = new SalesOrder
            {
                CustomerId = customerId,
                Currency = request.Currency.ToUpperInvariant(),
                OrderNumber = $"SO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24],
            };

            decimal subtotal = 0, discountTotal = 0, taxTotal = 0;
            foreach (var requested in request.Lines)
            {
                var price = prices.SingleOrDefault(p => p.ItemId == requested.ItemId)
                    ?? throw new InvalidOperationException($"No valid price for item {requested.ItemId}.");

                if (price.Currency != order.Currency || price.UnitPrice < 0 || price.Discount < 0)
                    throw new InvalidOperationException("Invalid price snapshot returned by pricing service.");

                var gross = price.UnitPrice * requested.Quantity;
                var discount = price.Discount * requested.Quantity;
                var taxable = Math.Max(0, gross - discount);
                var tax = decimal.Round(taxable * price.TaxRate, 2, MidpointRounding.AwayFromZero);
                var lineTotal = taxable + tax;

                order.Lines.Add(new SalesOrderLine
                {
                    ItemId = requested.ItemId,
                    ItemCode = price.ItemCode,
                    ItemNameSnapshot = price.ItemName,
                    Quantity = requested.Quantity,
                    UnitPriceSnapshot = price.UnitPrice,
                    DiscountSnapshot = discount,
                    TaxSnapshot = tax,
                    LineTotalSnapshot = lineTotal,
                    Currency = order.Currency
                });
                subtotal += gross;
                discountTotal += discount;
                taxTotal += tax;
            }

            order.SubtotalSnapshot = subtotal;
            order.DiscountSnapshot = discountTotal;
            order.TaxSnapshot = taxTotal;
            order.GrandTotalSnapshot = subtotal - discountTotal + taxTotal;

            // Persist aggregate, initial history row, and Outbox message in one DB transaction.
            await _orders.AddAsync(order, ct);
            // This history/outbox API is intentionally explicit in the guide; implement them
            // in the same transaction/unit-of-work in Infrastructure.
            await _outbox.AddAsync("SalesOrderCreated", order.PublicId.ToString(),
                new { order.PublicId, order.OrderNumber, order.CustomerId, order.GrandTotalSnapshot, order.Currency }, ct);
            await _orders.SaveChangesAsync(ct);

            return Map(order);
        }

        public async Task<SalesOrderResponse?> GetAsync(Guid publicId, CancellationToken ct)
        {
            var order = await _orders.GetByPublicIdAsync(publicId, ct);
            return order is null ? null : Map(order);
        }

        public Task<SalesOrderResponse?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
            => GetAsync(publicId, ct);

        public async Task CancelAsync(
            Guid publicId, string customerId, string reason, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Cancellation reason is required.");

            var order = await _orders.GetByPublicIdAsync(publicId, ct)
                ?? throw new KeyNotFoundException("Sales order was not found.");

            if (!string.Equals(order.CustomerId, customerId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("You cannot cancel this order.");

            if (order.Status is SalesOrderStatus.Completed or SalesOrderStatus.Cancelled)
                throw new InvalidOperationException($"An order in {order.Status} cannot be cancelled.");

            if (order.Status is SalesOrderStatus.Confirmed or SalesOrderStatus.Allocated)
                throw new InvalidOperationException(
                    "This order requires payment/dealer compensation before cancellation. Start the cancellation workflow.");

            var previous = order.Status;
            order.ChangeStatus(SalesOrderStatus.Cancelled);
            // Add SalesOrderHistory(previous, Cancelled, customerId, reason) and Outbox event
            // in the same transaction before commit.
            await _outbox.AddAsync("SalesOrderCancelled", order.PublicId.ToString(),
                new { order.PublicId, PreviousStatus = previous.ToString(), Reason = reason, CustomerId = customerId }, ct);
            await _orders.SaveChangesAsync(ct);
        }

        private static SalesOrderResponse Map(SalesOrder order) => new()
        {
            Id = order.PublicId,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            DealerId = order.DealerId,
            Status = order.Status.ToString(),
            GrandTotal = order.GrandTotalSnapshot,
            Currency = order.Currency,
            CreatedAtUtc = order.CreatedAtUtc,
            Lines = order.Lines.Select(l => new SalesOrderLineResponse
            {
                ItemId = l.ItemId,
                ItemName = l.ItemNameSnapshot,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPriceSnapshot,
                LineTotal = l.LineTotalSnapshot
            }).ToList()
        };
    }

}
