using FintechTask.Application.DTOs;
using FintechTask.Application.Interfaces;
using FintechTask.Domain.Entities;
using FintechTask.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FintechTask.API.Services
{
    public class SubmitBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubmitBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(2);

        public SubmitBackgroundService(IServiceProvider serviceProvider, ILogger<SubmitBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновый сервис отправки операций запущен");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingOperationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке операций в фоновом сервисе");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Фоновый сервис отправки операций остановлен");
        }

        private async Task ProcessPendingOperationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var providerClient = scope.ServiceProvider.GetRequiredService<IProviderClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SubmitBackgroundService>>();

            var pendingOperations = await dbContext.Operations
                .Where(o => o.Status == "PROCESSING")
                .ToListAsync(cancellationToken);

            if (!pendingOperations.Any())
            {
                return;
            }

            logger.LogInformation($"Найдено {pendingOperations.Count} операций в статусе PROCESSING для повторной отправки");

            var stuckOperation = DateTime.UtcNow.AddMinutes(-5);

            foreach (var operation in pendingOperations)
            {
                try
                {
                    logger.LogInformation($"Обработка операции {operation.Id}, ProviderPaymentId: {operation.ProviderPaymentId ?? "NULL"}");

                    if (operation.UpdatedAt.HasValue && operation.UpdatedAt.Value > stuckOperation)
                    {
                        logger.LogInformation($"Операция {operation.Id} обновлена недавно, пропуск");
                        continue;
                    }

                    var providerRequest = new ProviderRequest
                    {
                        OperationId = operation.Id,
                        Amount = operation.Amount,
                        Currency = operation.Currency
                    };

                    var providerResponse = await providerClient.SendPaymentAsync(
                        providerRequest,
                        operation.Id,
                        cancellationToken);


                    if (string.IsNullOrEmpty(operation.ProviderPaymentId))
                    {
                        operation.ProviderPaymentId = providerResponse.ProviderPaymentId;
                        operation.UpdatedAt = DateTime.UtcNow;
                        logger.LogInformation($"Сохранение ProviderPaymentId {providerResponse.ProviderPaymentId} для операции {operation.Id}");
                    }
                    else
                    {
                        if (operation.ProviderPaymentId != providerResponse.ProviderPaymentId)
                        {
                            logger.LogError($"КРИТИЧЕСКАЯ ОШИБКА: Провайдер вернул новый ProviderPaymentId для операции {operation.Id}. " +
                                $"Старый: {operation.ProviderPaymentId}, Новый: {providerResponse.ProviderPaymentId}");
                        }
                    }

                    operation.Events.Add(new OperationEvent
                    {
                        Type = "PROVIDER_ACCEPTED",
                        FromStatus = "PROCESSING",
                        ToStatus = "PROCESSING",
                        Message = $"Повторная отправка. ProviderPaymentId: {providerResponse.ProviderPaymentId}",
                        OccurredAt = DateTime.UtcNow
                    });

                    await dbContext.SaveChangesAsync(cancellationToken);

                    logger.LogInformation($"Операция {operation.Id} повторно отправлена. ProviderPaymentId: {providerResponse.ProviderPaymentId}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Ошибка при повторной отправке операции {operation.Id}");
                }
            }
        }
    }
}
