using FintechTask.Application.DTOs;

namespace FintechTask.Application.Interfaces
{
    public interface IProviderClient
    {
        Task<ProviderResponse> SendPaymentAsync(ProviderRequest request, string operationId, CancellationToken cancellationToken);
    }
}
