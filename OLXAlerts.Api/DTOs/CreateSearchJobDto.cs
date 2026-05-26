using System.ComponentModel.DataAnnotations;
using OLXAlerts.Api.Entities;

namespace OLXAlerts.Api.DTOs;

public class CreateSearchJobDto : IValidatableObject
{
    [Required]
    public string SearchTerm { get; set; } = string.Empty;

    public string LocationCode { get; set; } = "1000001";

    public string? LocationName { get; set; }

    public int? CategoryId { get; set; }

    public NotificationChannel NotificationChannel { get; set; } = NotificationChannel.WhatsApp;

    [RegularExpression(@"^\+\d{10,15}$", ErrorMessage = "WhatsAppNumber must be E.164 format, e.g. +91XXXXXXXXXX")]
    public string? WhatsAppNumber { get; set; }

    public long? TelegramChatId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "MinPrice must be non-negative.")]
    public decimal? MinPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "MaxPrice must be non-negative.")]
    public decimal? MaxPrice { get; set; }

    [Range(1, 10080)]
    public int IntervalMinutes { get; set; } = 60;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NotificationChannel == NotificationChannel.WhatsApp && string.IsNullOrEmpty(WhatsAppNumber))
            yield return new ValidationResult("WhatsAppNumber is required for WhatsApp channel.", [nameof(WhatsAppNumber)]);

        if (NotificationChannel == NotificationChannel.Telegram && !TelegramChatId.HasValue)
            yield return new ValidationResult("TelegramChatId is required for Telegram channel.", [nameof(TelegramChatId)]);
    }
}
