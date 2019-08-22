using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

namespace PollyPoC.Policies
{
    public static class HttpPolicyHandlers
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(600));
        }

        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }
    }
}
