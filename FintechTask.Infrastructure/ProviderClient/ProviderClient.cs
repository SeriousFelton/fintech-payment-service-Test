using System.Text;
using System.Text.Json;
using FintechTask.Application.DTOs;
using FintechTask.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FintechTask.Infrastructure.ProviderClient
{
    public class ProviderClient : IProviderClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProviderClient> _logger;
        
        public ProviderClient(HttpClient httpClient,  ILogger<ProviderClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ProviderResponse> SendPaymentAsync(ProviderRequest request, string operationId, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Idempotency-Key", operationId);
            _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", operationId);

            _logger.LogInformation($"Отправка платежа {operationId}");

            var response = await _httpClient.PostAsync("/payments", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var providerResponse = JsonSerializer.Deserialize<ProviderResponse>(responseBody);

            _logger.LogInformation($"Платёж {operationId} принят. ProviderPaymentId: {providerResponse?.ProviderPaymentId}");

            return providerResponse;
        }
    }
}
