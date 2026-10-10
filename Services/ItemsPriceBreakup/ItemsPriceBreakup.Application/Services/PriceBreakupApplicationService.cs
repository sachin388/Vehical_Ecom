using ItemsPriceBreakup.Application.DTO;
using ItemsPriceBreakup.Application.Interfaces;
using ItemsPriceBreakup.Application.Pricing;
using ItemsPriceBreakup.Domain.Entities;
using ItemsPriceBreakup.Domain.Enums;
using ItemsPriceBreakup.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPriceBreakup.Application.Services
{
    public class PriceBreakupApplicationService
    {
        private readonly IPriceBreakupRepository _repository;
        private readonly IPriceBreakupCalculator _calculator;
        private readonly IEventPublisher _eventPublisher;

        public PriceBreakupApplicationService(
            IPriceBreakupRepository repository,
            IPriceBreakupCalculator calculator,
            IEventPublisher eventPublisher)
        {
            _repository = repository;
            _calculator = calculator;
            _eventPublisher = eventPublisher;
        }

        public async Task<PriceBreakupResponse> CalculateAsync(
            CalculatePriceBreakupRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.ItemId <= 0)
                throw new ArgumentException("Invalid ItemId.");

            if (string.IsNullOrWhiteSpace(request.SKU))
                throw new ArgumentException("SKU is required.");

            if (string.IsNullOrWhiteSpace(request.StateCode))
                throw new ArgumentException("StateCode is required.");

            var context = new PriceCalculationContext
            {
                ItemId = request.ItemId,
                SKU = request.SKU,
                StateCode = request.StateCode,
                VehicleCategory = request.VehicleCategory,
                ExShowroomPrice = request.ExShowroomPrice,
                Discount = request.TotalDiscount,
                GstRate = request.GstRate,
                RtoAmount = request.RtoAmount,
                RegistrationAmount = request.RegistrationAmount,
                InsuranceAmount = request.InsuranceAmount,
                HandlingAmount = request.HandlingAmount,
                OtherCharges = request.OtherCharges
            };

            var lines = _calculator.Calculate(context);

            var subtotal = Math.Max(
                request.ExShowroomPrice -
                request.TotalDiscount, 0);

            var totalTax = lines.Sum(x => x.TaxAmount);

            var totalCharges = lines
                .Where(x =>
                    x.ChargeType != ChargeType.ExShowroom &&
                    x.ChargeType != ChargeType.GST)
                .Sum(x => x.Amount);

            var entity = new PriceBreakup
            {
                ItemId = request.ItemId,
                SKU = request.SKU,
                StateCode = request.StateCode,
                VehicleCategory = request.VehicleCategory,
                PriceVersion = request.PriceVersion,
                Currency = request.Currency,
                SubTotal = subtotal,
                TotalTax = totalTax,
                TotalCharges = totalCharges,
                TotalDiscount = request.TotalDiscount,
                FinalOnRoadPrice =
                    subtotal + totalTax + totalCharges,
                Lines = lines
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(
                "vehicle.price.breakup.calculated",
                new PriceBreakupCalculatedEvent
                {
                    PriceBreakupId = entity.Id,
                    ItemId = entity.ItemId,
                    SKU = entity.SKU,
                    StateCode = entity.StateCode,
                    PriceVersion = entity.PriceVersion,
                    FinalOnRoadPrice = entity.FinalOnRoadPrice
                },
                cancellationToken);

            return Map(entity);
        }

        private static PriceBreakupResponse Map(
            PriceBreakup entity)
        {
            return new PriceBreakupResponse
            {
                Id = entity.Id,
                ItemId = entity.ItemId,
                SKU = entity.SKU,
                StateCode = entity.StateCode,
                VehicleCategory = entity.VehicleCategory,
                PriceVersion = entity.PriceVersion,
                Currency = entity.Currency,
                SubTotal = entity.SubTotal,
                TotalTax = entity.TotalTax,
                TotalCharges = entity.TotalCharges,
                TotalDiscount = entity.TotalDiscount,
                FinalOnRoadPrice = entity.FinalOnRoadPrice,
                Status = entity.Status,
                CreatedAtUtc = entity.CreatedAtUtc,
                Lines = entity.Lines.Select(x =>
                    new PriceBreakupLineResponse
                    {
                        Id = x.Id,
                        ChargeType = x.ChargeType,
                        Code = x.Code,
                        Name = x.Name,
                        Amount = x.Amount,
                        TaxAmount = x.TaxAmount,
                        IsTaxIncluded = x.IsTaxIncluded,
                        CalculationBasis =
                            x.CalculationBasis,
                        RuleVersion = x.RuleVersion
                    }).ToList()
            };
        }
    }

}
