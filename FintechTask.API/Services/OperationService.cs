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
        private readonly IProviderClient _providerClient;
        private readonly ILogger<OperationService> _logger;

        public OperationService(AppDbContext dbContext, IProviderClient providerClient, ILogger<OperationService> logger)
        {
            _dbContext = dbContext;
            _providerClient = providerClient;
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
                Message = "Операция создана",
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
            var operation = await _dbContext.Operations.FirstOrDefaultAsync(o => o.Id == operationId);

            if (operation == null)
            {
                _logger.LogWarning($"Операция {operationId} не найдена при попытке отправки");
                throw new KeyNotFoundException($"Операция '{operationId} не найдена");
            }

            if (operation.Status == "PROCESSING")
            {
                _logger.LogInformation($"Операция {operationId} уже в процессе отправки");
                return MapToResponse(operation);
            }

            if (operation.Status == "COMPLETED" || operation.Status == "REJECTED")
            {
                _logger.LogInformation($"Операция {operationId} уже завершена и её статус: {operation.Status}");
                return MapToResponse(operation);
            }

            if (operation.Status != "CREATED")
            {
                _logger.LogWarning($"Оперция {operationId} имеет недопустимый статус {operation.Status} для отправки");
                throw new InvalidOperationException($"Операция в статусе '{operation.Status}' не может быть отправлена");
            }

            operation.Status = "PROCESSING";
            operation.UpdatedAt = DateTime.UtcNow;

            operation.Events.Add(new OperationEvent
            {
                Type = "SUBMITTED",
                FromStatus = "CREATED",
                ToStatus = "PROCESSING",
                Message = "Операция отправлена провайдеру",
                OccurredAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Операция {operationId} переведена в статус PROCESSING");

            try
            {
                var providerRequest = new ProviderRequest
                {
                    OperationId = operation.Id,
                    Amount = operation.Amount,
                    Currency = operation.Currency
                };

                var providerResponse = await _providerClient.SendPaymentAsync(
                    providerRequest,
                    operation.Id,
                    CancellationToken.None);

                operation.ProviderPaymentId = providerResponse.ProviderPaymentId;
                operation.UpdatedAt = DateTime.UtcNow;

                operation.Events.Add(new OperationEvent
                {
                    Type = "PROVIDER_ACCEPTED",
                    FromStatus = "PROCESSING",
                    ToStatus = "PROCESSING",
                    Message = $"Платеж принят провайдером. ProviderPaymentId: {providerResponse.ProviderPaymentId}",
                    OccurredAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"операция {operationId} успешно отправлена провайдеру. ProviderPaymentId: {providerResponse.ProviderPaymentId}");
            }
            catch (Exception ex)
            {
                operation.Events.Add(new OperationEvent
                {
                    Type = "PROVIDER_ERROR",
                    FromStatus = "PROCESSING",
                    ToStatus = "PROCESSING",
                    Message = $"Ошибка при отправке провайдеру: {ex.Message}",
                    OccurredAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync();

                _logger.LogError(ex, $"Ошибка при отправке операции {operationId} провайдеру");
     
                throw;
            }

            return MapToResponse(operation);
        }

        public async Task ProcessReceiptAsync(ReceiptRequest request)
        {
            var operation = await _dbContext.Operations
                .Include(o => o.Events)
                .FirstOrDefaultAsync(o => o.Id == request.OperationId);

            if (operation == null)
            {
                _logger.LogWarning($"Операция {request.OperationId} не найдена при обработке квитанции");
                throw new KeyNotFoundException($"Операция '{request.OperationId}' не найдена");
            }

            if (!string.IsNullOrEmpty(operation.ProviderPaymentId) &&
                operation.ProviderPaymentId != request.ProviderPaymentId)
            {
                _logger.LogWarning($"Конфликт providerPaymentId для операции {request.OperationId}. " +
                    $"Ожидался {operation.ProviderPaymentId}, получен {request.ProviderPaymentId}");

                throw new InvalidOperationException($"Конфликт providerPaymentId для операции '{request.OperationId}'");
            }

            if (string.IsNullOrEmpty(operation.ProviderPaymentId))
            {
                operation.ProviderPaymentId = request.ProviderPaymentId;
                _logger.LogInformation($"Сохранён ProviderPaymentId {request.ProviderPaymentId} для операции {request.OperationId}");
            }

            if (operation.Status == "COMPLETED" || operation.Status == "REJECTED")
            {
                _logger.LogInformation($"Операция {request.OperationId} уже завершена и её статус: {operation.Status}. Квитанция проигнорирована");
                return;
            }

            var newStatus = request.Result == "COMPLETED" ? "COMPLETED" : "REJECTED";
            var oldStatus = operation.Status;
            operation.Status = newStatus;
            operation.UpdatedAt = DateTime.UtcNow;

            operation.Events.Add(new OperationEvent
            {
                Type = "RECEIPT_RECEIVED",
                FromStatus = oldStatus,
                ToStatus = newStatus,
                Message = newStatus == "COMPLETED" ? "Платеж успешно завершён"  : "Платеж отклонен провайдером",
                OccurredAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Операция {request.OperationId} переведена в статус {newStatus} на основе квитанции");
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
