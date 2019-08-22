using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using PollyPoC.Constants;
using PollyPoC.Handlers;
using PollyPoC.Policies;
using PollyPoC.RefitClients;
using Refit;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PollyPoC
{
    public class Program
    {
        private static readonly string _httpStatBaseUrl = "https://httpstat.us";
        private static ILogger _logger;

        static async Task<int> Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient();
                    services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Trace)).AddTransient<Program>();

                    // Add all standard policies to the registry
                    AddPoliciesToPolicyRegistry(services);

                    // Register any custom message handlers.
                    services.AddTransient<ValidateHeaderHandler>();

                    // Register the HttpClients with Refit typed clients, adding the policies and handlers.
                    RegistryRetryClient(services);
                    RegisterRetryWithCircuitBreakerClient(services);
                    RegisterBulkheadClient(services);
                    RegisterCustomHandlerClient(services);

                }).UseConsoleLifetime();

            var host = builder.Build();

            using (IServiceScope serviceScope = host.Services.CreateScope())
            {
                IServiceProvider services = serviceScope.ServiceProvider;
                var retryClient = services.GetRequiredService<IMyRetryClient>();
                var retryCircuitClient = services.GetRequiredService<IMyRetryCircuitBreakerClient>();
                var bulkheadClient = services.GetRequiredService<IMyBulkHeadClient>();
                var customHandlerClient = services.GetRequiredService<IMyCustomHandlerClient>();
                _logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    _logger.LogWarning("TESTING RETRY CLIENT: SERVER ERROR");
                    await retryClient.ServerError();
                    _logger.LogWarning("TESTING RETRY CLIENT: TIMEOUT ERROR");
                    
                    await retryClient.Timeout();

                    try
                    {
                        _logger.LogWarning("TESTING RETRY CIRCUIT BREAKER CLIENT: SERVER ERROR - 3 retries - 5 globally allowed before breaking");
                        await retryCircuitClient.ServerError();
                        _logger.LogWarning("TESTING RETRY CIRCUIT BREAKER CLIENT: TIMEOUT ERROR - 3 retries - 5 globally allowed before breaking");
                        
                        
                        await retryCircuitClient.Timeout();
                    }
                    catch(Exception exception)
                    {
                        _logger.LogWarning(exception.Message);
                    }

                    _logger.LogWarning("TESTING BULKHEAD CLIENT: STARTING 30 TASKS");
                    
                    var taskList = new List<Task>();
                    for (int i = 0; i < 30; i++)
                    {
                        taskList.Add(Task.Run(() =>
                        {
                            Thread.Sleep(3);
                            bulkheadClient.Ok();
                        }));
                    }

                    _logger.LogWarning("TESTING BULKHEAD CLIENT: AWAITING TASKS");
                    await Task.WhenAll(taskList);

                    _logger.LogWarning("TESTING CUSTOM HANDLER CLIENT: BAD REQUEST");
                    var badRequest = await customHandlerClient.Ok();
                    if (badRequest.StatusCode == HttpStatusCode.BadRequest)
                    {
                        _logger.LogWarning("TESTING CUSTOM HANDLER CLIENT: SUCCESS");
                    }
                }
                catch
                {
                    // Do nothing
                }
            }

            Console.ReadKey();
            return 0;
        }

        private static void AddPoliciesToPolicyRegistry(IServiceCollection services)
        {
            // An approach to managing regularly used policies is to define them once and register them with a PolicyRegistry.
            // Add policies to the registry so they can be accessed consistently.
            var registry = services.AddPolicyRegistry();
            registry.Add(PolicyRegistryKeys.Retry, HttpPolicyHandlers.GetRetryPolicy());
            registry.Add(PolicyRegistryKeys.CircuitBreaker, HttpPolicyHandlers.GetCircuitBreakerPolicy());
            registry.Add(PolicyRegistryKeys.BulkHead, Policy.BulkheadAsync(maxParallelization: 10, maxQueuingActions: 1, OnBulkheadRejected).AsAsyncPolicy<HttpResponseMessage>());

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Task<Context> OnBulkheadRejected(Context context)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                // Do some custom stuff here like logging when the number of execution slots limit has been hit.
                _logger.LogWarning("Bulk head rejected executed.");
                return context;
            }
        }

        private static void RegistryRetryClient(IServiceCollection services)
        {
            services.AddHttpClient<IMyRetryClient>((serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(_httpStatBaseUrl);
            })
            .AddPolicyHandlerFromRegistry(PolicyRegistryKeys.Retry)
            .AddTypedClient(RestService.For<IMyRetryClient>);
        }

        private static void RegisterRetryWithCircuitBreakerClient(IServiceCollection services)
        {
            services.AddHttpClient<IMyRetryCircuitBreakerClient>((serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(_httpStatBaseUrl);
            })
            .AddPolicyHandlerFromRegistry(PolicyRegistryKeys.Retry) // This handler is on the outside and called first during the request, last during the response.
            .AddPolicyHandlerFromRegistry(PolicyRegistryKeys.CircuitBreaker) // This handler is on the inside, closest to the request being sent. Circuit breaker policies are stateful. All calls through this client share the same circuit state.
            .AddTypedClient(RestService.For<IMyRetryCircuitBreakerClient>);
        }

        private static void RegisterBulkheadClient(IServiceCollection services)
        {
            services.AddHttpClient<IMyBulkHeadClient>((serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(_httpStatBaseUrl);
            })
            .AddPolicyHandlerFromRegistry(PolicyRegistryKeys.BulkHead)
            .AddTypedClient(RestService.For<IMyBulkHeadClient>);
        }

        private static void RegisterCustomHandlerClient(IServiceCollection services)
        {
            services.AddHttpClient<IMyCustomHandlerClient>((serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(_httpStatBaseUrl);
            })
            .AddHttpMessageHandler<ValidateHeaderHandler>()
            .AddTypedClient(RestService.For<IMyCustomHandlerClient>);
        }
    }
}
