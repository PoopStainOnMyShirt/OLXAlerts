namespace OLXAlerts.Api.Entities;

public class SearchJob
{
    public int Id { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public string LocationCode { get; set; } = "1000001";
    public string? LocationName { get; set; }
    public int? CategoryId { get; set; }
    public string WhatsAppNumber { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRunAt { get; set; }
    public DateTime NextRunAt { get; set; } = DateTime.UtcNow;

    public ICollection<Listing> Listings { get; set; } = [];
    public ICollection<AlertLog> AlertLogs { get; set; } = [];
}
