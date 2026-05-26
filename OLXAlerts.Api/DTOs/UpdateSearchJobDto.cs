using System.ComponentModel.DataAnnotations;

namespace OLXAlerts.Api.DTOs;

public class UpdateSearchJobDto
{
    public string? SearchTerm { get; set; }
    public string? LocationCode { get; set; }
    public string? LocationName { get; set; }
    public int? CategoryId { get; set; }

    [Range(1, 10080)]
    public int? IntervalMinutes { get; set; }

    public bool? IsActive { get; set; }
}
