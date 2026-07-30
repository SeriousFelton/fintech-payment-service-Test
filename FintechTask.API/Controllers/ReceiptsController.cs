using FintechTask.Application.DTOs;
using FintechTask.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FintechTask.API.Controllers
{
    [ApiController]
    [Route("receipts")]
    public class ReceiptsController : ControllerBase
    {
        private readonly IOperationService _operationService;
        private readonly ILogger<ReceiptsController> _logger;

        public ReceiptsController(IOperationService operationService, ILogger<ReceiptsController> logger)
        {
            _operationService = operationService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ReceiveReceipt([FromBody] ReceiptRequest request)
        {
            _logger.LogInformation($"Получена квитанция для операции {request.OperationId}. Результат: {request.Result}");

            try
            {
                await _operationService.ProcessReceiptAsync(request);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Операция '{request.OperationId}' не найдена");
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("конфликт"))
            {
                _logger.LogWarning(ex, $"Конфликт providerPaymentId для операции {request.OperationId}");
                return Conflict( new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обработке квитанции для операции {request.OperationId}");
                return BadRequest( new { error = "Ошибка при обработки квитанции"});
            }
        }
    }
}
