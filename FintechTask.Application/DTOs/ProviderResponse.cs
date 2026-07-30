using System.Text.Json.Serialization;

namespace FintechTask.Application.DTOs
{
    public class ProviderResponse
    {
        [JsonPropertyName("providerPaymentId")]
        public string ProviderPaymentId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}
