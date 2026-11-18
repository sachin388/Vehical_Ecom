using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Payment.Infrastructure.Gateway.PayU
{
    public static class PayUResiliencePolicy
    {
        public static ResiliencePipeline<HttpResponseMessage> CreateHttpRetryPipeline(ILogger? logger = null)
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(500),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutException>()
                        .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout),
                    OnRetry = args =>
                    {
                        if (logger is not null)
                        {
                            logger.LogWarning("PayU gateway request retry attempt #{Attempt}. Delay: {Delay}ms. Reason: {Reason}",
                                args.AttemptNumber,
                                args.RetryDelay.TotalMilliseconds,
                                args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                        }
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }
    }
}
