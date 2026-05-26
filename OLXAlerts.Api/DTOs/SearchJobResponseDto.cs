namespace OLXAlerts.Api.DTOs;

public class SearchJobResponseDto
{
    public int Id { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string? LocationName { get; set; }
    public int? CategoryId { get; set; }
    public string WhatsAppNumber { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime NextRunAt { get; set; }
}
