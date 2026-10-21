using System;

namespace SalesOrder.Domain.Enums
{
    public enum CancellationSagaStatus
    {
        Initiated = 1,
        WaitingForPaymentCompensation = 2,
        WaitingForDealerRelease = 3,
        Completed = 4,
        Failed = 5
    }
}
