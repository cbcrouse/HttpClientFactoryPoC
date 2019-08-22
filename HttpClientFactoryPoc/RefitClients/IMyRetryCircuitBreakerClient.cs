using Refit;
using System.Threading.Tasks;

namespace PollyPoC.RefitClients
{
    public interface IMyRetryCircuitBreakerClient
    {
        // Refit vs RestEase :: https://dotnet.libhunt.com/compare-restease-vs-refit

        // Not specifying ApiResponse will cause ApiExceptions to be thrown. - https://github.com/reactiveui/refit/issues/456

        [Get("/500")]
        Task<ApiResponse<string>> ServerError();

        [Get("/408")]
        Task<ApiResponse<string>> Timeout();

        [Get("/200")]
        Task<ApiResponse<string>> Ok();
    }
}
