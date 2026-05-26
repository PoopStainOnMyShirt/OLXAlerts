namespace OLXAlerts.Api.Entities;

public class AlertLog
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string ListingId { get; set; } = string.Empty;
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string? MessageSid { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "sent";

    public SearchJob Job { get; set; } = null!;
}
