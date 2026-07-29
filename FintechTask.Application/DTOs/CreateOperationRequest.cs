using System.ComponentModel.DataAnnotations;

namespace FintechTask.Application.DTOs
{
    public class CreateOperationRequest
    {
        [Required(ErrorMessage = "OperationId обязателен")]
        public string OperationId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Сумма обязательна")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$",
            ErrorMessage = "Сумма должна быть положительным числом с не более чем двумя знаками после запятой")]
        public string Amount { get; set; } = string.Empty;

        [Required(ErrorMessage = "Валюта обязательна")]
        [RegularExpression("^RUB",
            ErrorMessage = "Поддерживается только валюта RUB")]
        public string Currency { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
