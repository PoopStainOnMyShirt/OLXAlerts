using OLXAlerts.Api.Entities;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace OLXAlerts.Api.Services;

public class TwilioWhatsAppService(IConfiguration config, ILogger<TwilioWhatsAppService> logger) : IWhatsAppService
{
    private readonly string _accountSid = config["Twilio:AccountSid"] ?? throw new InvalidOperationException("Twilio:AccountSid is required");
    private readonly string _authToken = config["Twilio:AuthToken"] ?? throw new InvalidOperationException("Twilio:AuthToken is required");
    private readonly string _from = config["Twilio:WhatsAppFrom"] ?? "whatsapp:+14155238886";

    public async Task<string?> SendAlertAsync(string toNumber, Listing listing)
    {
        TwilioClient.Init(_accountSid, _authToken);

        var url = BuildOlxUrl(listing);
        var sellerType = listing.IsBusiness == true ? "Business" : "Individual";
        var price = string.IsNullOrEmpty(listing.PriceDisplay) ? "Price not listed" : listing.PriceDisplay;
        var location = string.IsNullOrEmpty(listing.Location) ? "Location unknown" : listing.Location;

        var body = $"""
            *New OLX Listing Alert*

            *{listing.Title}*
            Price: {price}
            Location: {location}
            Seller: {sellerType}

            {url}
            """;

        try
        {
            var message = await MessageResource.CreateAsync(
                from: new Twilio.Types.PhoneNumber(_from),
                to: new Twilio.Types.PhoneNumber($"whatsapp:{toNumber}"),
                body: body
            );
            logger.LogInformation("WhatsApp sent to {To}, SID: {Sid}", toNumber, message.Sid);
            return message.Sid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send WhatsApp to {To} for listing {ListingId}", toNumber, listing.Id);
            return null;
        }
    }

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
