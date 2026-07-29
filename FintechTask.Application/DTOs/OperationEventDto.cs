namespace FintechTask.Application.DTOs
{
    public class OperationEventDto
    {
        public int EventId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
