namespace OLXAlerts.Api.Entities;

public class Listing
{
    public string Id { get; set; } = string.Empty;
    public int JobId { get; set; }
    public string? Title { get; set; }
    public string? UserName { get; set; }
    public string? Description { get; set; }
    public string? OlxCreatedAt { get; set; }
    public string? CarBodyType { get; set; }
    public string? AdId { get; set; }
    public bool? IsBusiness { get; set; }
    public string? PriceDisplay { get; set; }
    public decimal? PriceValue { get; set; }
    public string? Location { get; set; }
    public string? RawData { get; set; }
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

    public SearchJob Job { get; set; } = null!;
}
