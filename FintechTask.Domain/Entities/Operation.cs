namespace FintechTask.Domain.Entities
{
    public class Operation
    {
        public string Id { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string? ProviderPaymentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        //навигационные свойства
        public ICollection<OperationEvent> Events { get; set; } = new List<OperationEvent>();
    }
}
