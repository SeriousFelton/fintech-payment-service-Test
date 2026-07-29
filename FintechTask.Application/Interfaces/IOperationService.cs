using FintechTask.Application.DTOs;

namespace FintechTask.Application.Interfaces
{
    public interface IOperationService
    {
        Task<OperationResponse> CreateOperationAsync(CreateOperationRequest request);
        Task<OperationResponse> GetOperationAsync(string operationId);
        Task<List<OperationEventDto>> GetOperationEventsAsync(string operationId);
        Task<OperationResponse> SubmitOperationAsync(string operationId);
    }
}
