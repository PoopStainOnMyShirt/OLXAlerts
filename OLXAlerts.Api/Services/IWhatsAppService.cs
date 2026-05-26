using OLXAlerts.Api.Entities;

namespace OLXAlerts.Api.Services;

public interface IWhatsAppService
{
    Task<string?> SendAlertAsync(string toNumber, Listing listing);
}
