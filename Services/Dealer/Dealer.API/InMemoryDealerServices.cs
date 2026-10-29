using System.Collections.Concurrent;
using Dealer.Application.DTOs;
using Dealer.Application.Interfaces;
using Dealer.Domain.Entities;

namespace Dealer.API;

public class InMemoryDealerRepository : IDealerRepository
{
    private readonly ConcurrentDictionary<long, Dealer.Domain.Entities.Dealer> _dealers = new();
    private long _idCounter = 1;

    public InMemoryDealerRepository()
    {
        var defaultDealer = new Dealer.Domain.Entities.Dealer
        {
            Id = 1,
            DealerCode = "DLR001",
            Name = "Apex Auto City",
            Email = "contact@apexauto.com",
            Phone = "+1-555-0199",
            Status = Dealer.Domain.Enums.DealerStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dealers[defaultDealer.Id] = defaultDealer;
    }

    public Task<Dealer.Domain.Entities.Dealer?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _dealers.TryGetValue(id, out var dealer);
        return Task.FromResult(dealer);
    }

    public Task<bool> DealerCodeExistsAsync(string code, CancellationToken ct = default)
    {
        var exists = _dealers.Values.Any(d => string.Equals(d.DealerCode, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task AddAsync(Dealer.Domain.Entities.Dealer dealer, CancellationToken ct = default)
    {
        if (dealer.Id <= 0)
        {
            dealer.Id = Interlocked.Increment(ref _idCounter);
        }
        _dealers[dealer.Id] = dealer;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public class InMemoryDealerInventoryRepository : IDealerInventoryRepository
{
    private readonly ConcurrentDictionary<Guid, DealerOrderAssignment> _assignments = new();

    public Task<List<DealerAvailabilityCandidate>> GetCandidatesAsync(
        int itemId, string sku, int quantity, string? city, string? stateCode, CancellationToken ct = default)
    {
        var candidates = new List<DealerAvailabilityCandidate>
        {
            new DealerAvailabilityCandidate
            {
                DealerId = 1,
                DealerName = "Apex Auto City",
                BranchName = "Main Branch",
                City = city ?? "Metropolis",
                AvailableQuantity = 10,
                DistanceKm = 5
            }
        };
        return Task.FromResult(candidates);
    }

    public Task<DealerOrderAssignment?> GetAssignmentByOrderIdAsync(Guid salesOrderId, CancellationToken ct = default)
    {
        _assignments.TryGetValue(salesOrderId, out var assignment);
        return Task.FromResult(assignment);
    }

    public Task<bool> TryReserveAsync(long dealerId, int itemId, int quantity, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task AddAssignmentAsync(DealerOrderAssignment assignment, CancellationToken ct = default)
    {
        _assignments[assignment.SalesOrderId] = assignment;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public class DefaultDealerSelectionStrategy : IDealerSelectionStrategy
{
    public decimal Score(DealerAvailabilityCandidate c, DealerAvailabilityRequest r)
    {
        decimal score = 100m;
        if (!string.IsNullOrWhiteSpace(r.City) &&
            string.Equals(c.City, r.City, StringComparison.OrdinalIgnoreCase))
            score += 100m;
        score += Math.Max(0m, 50m - c.DistanceKm);
        score += Math.Min(c.AvailableQuantity, 10) * 2m;
        return score;
    }
}

public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(string topic, T message, CancellationToken ct = default) => Task.CompletedTask;
}
