using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OLXAlerts.Api.Data;
using OLXAlerts.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// EF Core + PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// HttpClient for OLX reference API (locations, categories)
builder.Services.AddHttpClient("olx", c =>
{
    c.BaseAddress = new Uri("https://www.olx.in/");
    c.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
    c.DefaultRequestHeaders.Add("Accept", "application/json");
    c.DefaultRequestHeaders.Add("Referer", "https://www.olx.in/");
    c.Timeout = TimeSpan.FromSeconds(15);
});

// Application services
builder.Services.AddScoped<ScraperService>();
builder.Services.AddScoped<IWhatsAppService, TwilioWhatsAppService>();
builder.Services.AddSingleton<AlertSchedulerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AlertSchedulerService>());

// Rate limiting (keyed by client IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // General CRUD: 60 requests / minute per IP
    options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
        }));

    // Manual scraper trigger: 5 requests / minute per IP (spawns a subprocess)
    options.AddPolicy("scraper-run", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
        }));

    // OLX reference data: 20 requests / minute per IP (responses are cached, but still)
    options.AddPolicy("reference", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
        }));
});

var app = builder.Build();

// Auto-apply EF migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger UI in all environments (portfolio / demo)
app.MapOpenApi();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "OLXAlerts API v1"));

app.UseHttpsRedirection();
app.UseRateLimiter();
app.MapControllers();
app.Run();
