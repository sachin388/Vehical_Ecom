using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SalesOrder.Application.Interfaces;
using SalesOrder.Domain.Entities;
using SalesOrder.Domain.Enums;

namespace SalesOrder.Application.Services
{
    using SalesOrder = SalesOrder.Domain.Entities.SalesOrder;

    public sealed class OrderCancellationService
    {
        private readonly ISalesOrderRepository _orders;
        private readonly IOutboxWriter _outbox;
        private readonly IUnitOfWork _unitOfWork;

        public OrderCancellationService(
            ISalesOrderRepository orders,
            IOutboxWriter outbox,
            IUnitOfWork unitOfWork)
        {
            _orders = orders;
            _outbox = outbox;
            _unitOfWork = unitOfWork;
        }

        public async Task RequestCancellationAsync(
            Guid orderId,
            string customerId,
            string reason,
            CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            var order = await _orders.GetByPublicIdAsync(orderId, ct)
                ?? throw new KeyNotFoundException("Sales order was not found.");

            if (!string.Equals(order.CustomerId, customerId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Customer does not own this order.");

            if (order.Status is SalesOrderStatus.Completed or SalesOrderStatus.Cancelled)
                throw new InvalidOperationException($"Order in {order.Status} cannot be cancelled.");

            var previous = order.Status;
            order.ChangeStatus(SalesOrderStatus.Cancelled);

            await _outbox.AddAsync("OrderCancellationRequested", order.PublicId.ToString(), new
            {
                SalesOrderId = order.PublicId,
                CustomerId = customerId,
                Reason = reason,
                PreviousStatus = previous.ToString()
            }, ct);

            await _orders.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
        }
    }
}
