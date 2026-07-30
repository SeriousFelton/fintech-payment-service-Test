namespace FintechTask.Application.DTOs
{
    public class ReceiptRequest
    {
        public string ProviderPaymentId { get; set; }
        public string OperationId { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Message {  get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
