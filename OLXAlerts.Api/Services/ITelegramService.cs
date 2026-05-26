using OLXAlerts.Api.Entities;

namespace OLXAlerts.Api.Services;

public interface ITelegramService
{
    Task<string?> SendAlertAsync(long chatId, Listing listing);
}
