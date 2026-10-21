using System;
using System.Threading;
using System.Threading.Tasks;
using SalesOrder.Application.DTOs;

namespace SalesOrder.Application.Services
{
    public interface ISalesOrderApplicationService
    {
        Task<SalesOrderResponse> CreateAsync(
            string customerId, CreateSalesOrderRequest request, CancellationToken ct = default);

        Task<SalesOrderResponse?> GetAsync(
            Guid publicId, CancellationToken ct = default);

        Task<SalesOrderResponse?> GetByPublicIdAsync(
            Guid publicId, CancellationToken ct = default);

        Task CancelAsync(
            Guid publicId, string customerId, string reason, CancellationToken ct = default);
    }
}
