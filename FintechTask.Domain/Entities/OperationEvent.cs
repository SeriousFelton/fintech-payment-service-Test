namespace FintechTask.Domain.Entities
{
    public class OperationEvent
    {
        public int Id { get; set; }
        public string OperationId { get; set; }
        public string Type { get; set; }
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public string Message { get; set; }
        public DateTime OccurredAt { get; set; }
        public Operation Operation { get; set; }
    }
}
