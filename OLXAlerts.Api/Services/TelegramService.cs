using System.Net.Http.Json;
using System.Text.Json;
using OLXAlerts.Api.Entities;

namespace OLXAlerts.Api.Services;

public class TelegramService(
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<TelegramService> logger) : ITelegramService
{
    private readonly string _botToken = config["Telegram:BotToken"]
        ?? throw new InvalidOperationException("Telegram:BotToken is required");

    public async Task<string?> SendAlertAsync(long chatId, Listing listing)
    {
        var client = httpClientFactory.CreateClient("telegram");
        var url = BuildOlxUrl(listing);
        var sellerType = listing.IsBusiness == true ? "Business" : "Individual";
        var price = string.IsNullOrEmpty(listing.PriceDisplay) ? "Price not listed" : listing.PriceDisplay;
        var location = string.IsNullOrEmpty(listing.Location) ? "Location unknown" : listing.Location;

        var text = $"""
            <b>New OLX Listing Alert</b>

            <b>{HtmlEscape(listing.Title ?? "")}</b>
            Price: {HtmlEscape(price)}
            Location: {HtmlEscape(location)}
            Seller: {sellerType}

            {url}
            """;

        var payload = new { chat_id = chatId, text, parse_mode = "HTML" };

        try
        {
            var response = await client.PostAsJsonAsync($"https://api.telegram.org/bot{_botToken}/sendMessage", payload);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogError("Telegram API error {Status} for chatId {ChatId}: {Body}", response.StatusCode, chatId, body);
                return null;
            }
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var messageId = json.GetProperty("result").GetProperty("message_id").GetInt32().ToString();
            logger.LogInformation("Telegram sent to {ChatId}, message_id: {MessageId}", chatId, messageId);
            return messageId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Telegram to {ChatId} for listing {ListingId}", chatId, listing.Id);
            return null;
        }
    }

    private static string HtmlEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string BuildOlxUrl(Listing listing)
    {
        if (string.IsNullOrEmpty(listing.AdId))
            return "https://www.olx.in/";

        var slug = listing.Title is null
            ? "listing"
            : string.Join("-", listing.Title
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(6)
                .Select(w => new string(w.Where(char.IsLetterOrDigit).ToArray()))
                .Where(w => w.Length > 0));

        return $"https://www.olx.in/item/{slug}-iid-{listing.AdId}";
    }
}
