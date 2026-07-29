using FintechTask.Application.DTOs;
using FintechTask.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FintechTask.API.Controllers
{
    [ApiController]
    [Route("operations")]
    public class OperationsController : ControllerBase
    {
        private readonly IOperationService _operationService;
        private readonly ILogger<OperationsController> _logger;

        public OperationsController(IOperationService operationService, ILogger<OperationsController> logger)
        {
            _operationService = operationService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOperation([FromBody] CreateOperationRequest request)
        {
            _logger.LogInformation($"Запрос на создание операции {request.OperationId}");

            try
            {
                var result = await _operationService.CreateOperationAsync(request);
                return CreatedAtAction(nameof(GetOperation), new { id = result.OperationId }, result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("уже существует"))
            {
                _logger.LogWarning(ex, $"Попытка создать дублирующую операцию {request.OperationId}");
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при создании операции {request.OperationId}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOperation(string id)
        {
            _logger.LogInformation($"Запрос на получение операции {id}");

            try
            {
                var result = await _operationService.GetOperationAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = $"Операция '{id}' не найдена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении операции {id}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}/events")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvents(string id)
        {
            _logger.LogInformation($"Запрос на получение истории операции {id}");

            try
            {
                var result = await _operationService.GetOperationEventsAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = $"Операция '{id}' не найдена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка пи получении истории операции {id}");
                return BadRequest(new { error = ex.Message});
            }
        }

        [HttpPost("{id}/submit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitOperation(string id)
        {
            _logger.LogInformation($"Запрос на отправку операции {id}");

            try
            {
                var result = await _operationService.SubmitOperationAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = $"Операция '{id}' не найдена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке операции {id}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
