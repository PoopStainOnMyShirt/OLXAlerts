using System.ComponentModel.DataAnnotations;

namespace OLXAlerts.Api.DTOs;

public class CreateSearchJobDto
{
    [Required]
    public string SearchTerm { get; set; } = string.Empty;

    public string LocationCode { get; set; } = "1000001";

    public string? LocationName { get; set; }

    public int? CategoryId { get; set; }

    [Required]
    [RegularExpression(@"^\+\d{10,15}$", ErrorMessage = "WhatsAppNumber must be E.164 format, e.g. +91XXXXXXXXXX")]
    public string WhatsAppNumber { get; set; } = string.Empty;

    [Range(1, 10080)]
    public int IntervalMinutes { get; set; } = 60;
}
