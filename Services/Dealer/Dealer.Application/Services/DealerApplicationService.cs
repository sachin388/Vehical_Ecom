using Dealer.Application.DTOs;
using Dealer.Application.Interfaces;
using Dealer.Domain.Entities;
using Dealer.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Dealer.Application.Services
{
    public class DealerApplicationService
    {
        private readonly IDealerRepository _dealers;
        private readonly IDealerInventoryRepository _inventory;
        private readonly IDealerSelectionStrategy _strategy;
        private readonly IEventPublisher _events;

        public DealerApplicationService(
            IDealerRepository dealers,
            IDealerInventoryRepository inventory,
            IDealerSelectionStrategy strategy,
            IEventPublisher events)
        {
            _dealers = dealers;
            _inventory = inventory;
            _strategy = strategy;
            _events = events;
        }

        public async Task<DealerResponse> CreateDealerAsync(
            CreateDealerRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.DealerCode))
                throw new ArgumentException("DealerCode is required.");
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Dealer name is required.");

            var code = request.DealerCode.Trim().ToUpperInvariant();
            if (await _dealers.DealerCodeExistsAsync(code, ct))
                throw new InvalidOperationException("DealerCode already exists.");

            var dealer = new Dealer
            {
                DealerCode = code,
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone.Trim(),
                Status = DealerStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _dealers.AddAsync(dealer, ct);
            await _dealers.SaveChangesAsync(ct);

            return new DealerResponse
            {
                Id = dealer.Id,
                DealerCode = dealer.DealerCode,
                Name = dealer.Name,
                Email = dealer.Email,
                Phone = dealer.Phone,
                Status = dealer.Status
            };
        }

        public async Task<List<DealerAvailabilityResponse>> GetAvailableDealersAsync(
            DealerAvailabilityRequest request, CancellationToken ct = default)
        {
            ValidateAvailability(request);
            var candidates = await _inventory.GetCandidatesAsync(
                request.ItemId, request.SKU.Trim(), request.Quantity,
                request.City, request.StateCode, ct);

            return candidates.Select(c => new DealerAvailabilityResponse
            {
                DealerId = c.DealerId,
                DealerName = c.DealerName,
                BranchName = c.BranchName,
                City = c.City,
                AvailableQuantity = c.AvailableQuantity,
                SelectionScore = _strategy.Score(c, request)
            })
                .OrderByDescending(x => request.PreferredDealerId == x.DealerId)
                .ThenByDescending(x => x.SelectionScore)
                .ThenBy(x => x.DealerId)
                .ToList();
        }

        public async Task<DealerAssignmentResponse?> AssignOrderAsync(
            AssignDealerRequest request, CancellationToken ct = default)
        {
            if (request.SalesOrderId == Guid.Empty)
                throw new ArgumentException("SalesOrderId is required.");
            ValidateAvailability(request);

            // Idempotency: repeated request returns the existing assignment.
            var existing = await _inventory.GetAssignmentByOrderIdAsync(
                request.SalesOrderId, ct);
            if (existing is not null)
                return new DealerAssignmentResponse
                {
                    AssignmentId = existing.Id,
                    SalesOrderId = existing.SalesOrderId,
                    DealerId = existing.DealerId,
                    SKU = existing.SKU,
                    Quantity = existing.Quantity,
                    Status = existing.Status,
                    AssignedAtUtc = existing.AssignedAtUtc
                };

            var candidates = await _inventory.GetCandidatesAsync(
                request.ItemId, request.SKU.Trim(), request.Quantity,
                request.City, request.StateCode, ct);

            var ranked = candidates.Select(c => new
            {
                Candidate = c,
                Score = _strategy.Score(c, request)
            })
                .OrderByDescending(x => request.PreferredDealerId == x.Candidate.DealerId)
                .ThenByDescending(x => x.Score)
                .ThenBy(x => x.Candidate.DealerId);

            foreach (var item in ranked)
            {
                // TryReserveAsync must perform an atomic conditional update.
                if (!await _inventory.TryReserveAsync(
                    item.Candidate.DealerId, request.ItemId, request.Quantity, ct))
                    continue;

                var assignment = new DealerOrderAssignment
                {
                    SalesOrderId = request.SalesOrderId,
                    DealerId = item.Candidate.DealerId,
                    DealerBranchId = item.Candidate.DealerBranchId,
                    ItemId = request.ItemId,
                    SKU = request.SKU.Trim(),
                    Quantity = request.Quantity,
                    Status = "Assigned",
                    AssignedAtUtc = DateTime.UtcNow
                };

                await _inventory.AddAssignmentAsync(assignment, ct);
                await _inventory.SaveChangesAsync(ct);

                await _events.PublishAsync("dealer.order.allocated", new
                {
                    EventId = Guid.NewGuid(),
                    assignment.SalesOrderId,
                    assignment.DealerId,
                    assignment.DealerBranchId,
                    assignment.ItemId,
                    assignment.SKU,
                    assignment.Quantity,
                    assignment.Status,
                    assignment.AssignedAtUtc
                }, ct);

                return new DealerAssignmentResponse
                {
                    AssignmentId = assignment.Id,
                    SalesOrderId = assignment.SalesOrderId,
                    DealerId = assignment.DealerId,
                    DealerName = item.Candidate.DealerName,
                    SKU = assignment.SKU,
                    Quantity = assignment.Quantity,
                    Status = assignment.Status,
                    AssignedAtUtc = assignment.AssignedAtUtc
                };
            }

            return null; // API should map this to HTTP 409 Conflict.
        }

        private static void ValidateAvailability(DealerAvailabilityRequest request)
        {
            if (request.ItemId <= 0) throw new ArgumentException("ItemId must be positive.");
            if (request.Quantity <= 0) throw new ArgumentException("Quantity must be positive.");
            if (string.IsNullOrWhiteSpace(request.SKU)) throw new ArgumentException("SKU is required.");
        }
    }

}
