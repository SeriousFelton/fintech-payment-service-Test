using FintechTask.Application.DTOs;
using FintechTask.Application.Interfaces;
using FintechTask.Domain.Entities;
using FintechTask.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FintechTask.API.Services
{
    public class OperationService : IOperationService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<OperationService> _logger;

        public OperationService(AppDbContext dbContext, ILogger<OperationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<OperationResponse> CreateOperationAsync(CreateOperationRequest request)
        {
            var existing = await _dbContext.Operations.FindAsync(request.OperationId);
            if (existing != null)
            {
                _logger.LogWarning($"Операция {request.OperationId} уже существует");
                throw new InvalidOperationException($"Операция с ID '{request.OperationId}' уже существует");
            }

            var operation = new Operation
            {
                Id = request.OperationId,
                Amount = request.Amount,
                Currency = request.Currency,
                Description = request.Description ?? string.Empty,
                Status = "CREATED",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            operation.Events.Add(new OperationEvent
            {
                Type = "CREATED",
                FromStatus = null,
                ToStatus = "CREATED",
                Message = "Operation created",
                OccurredAt = DateTime.UtcNow
            });

            _dbContext.Operations.Add(operation);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Операция {operation.Id} успешно создана");

            return MapToResponse(operation);
        }

        public async Task<OperationResponse> GetOperationAsync(string operationId)
        {
            var operation = await _dbContext.Operations.FirstOrDefaultAsync(o => o.Id == operationId);

            if (operation == null)
            {
                throw new KeyNotFoundException($"Операция '{operationId}' не найдена");
            }

            return MapToResponse(operation);
        }

        public async Task<List<OperationEventDto>> GetOperationEventsAsync(string operationId)
        {
            var operation = await _dbContext.Operations
                .Include(o => o.Events)
                .FirstOrDefaultAsync(o => o.Id == operationId);

            if (operation == null)
            {
                throw new KeyNotFoundException($"Операция '{operationId}' не найдена");
            }

            return operation.Events
                .OrderBy(e => e.Id)
                .Select(e => new OperationEventDto
                {
                    EventId = e.Id,
                    Type = e.Type,
                    FromStatus = e.FromStatus,
                    ToStatus = e.ToStatus,
                    Message = e.Message,
                    OccurredAt = e.OccurredAt
                })
                .ToList();
        }

        public async Task<OperationResponse> SubmitOperationAsync(string operationId)
        {
            throw new NotImplementedException();
        }

        private OperationResponse MapToResponse(Operation operation)
        {
            return new OperationResponse
            {
                OperationId = operation.Id,
                Amount = operation.Amount,
                Currency = operation.Currency,
                Description = operation.Description,
                Status = operation.Status,
                ProviderPaymentId = operation.ProviderPaymentId,
                CreatedAt = operation.CreatedAt,
                UpdatedAt = operation.UpdatedAt
            };
        }
    }
}
