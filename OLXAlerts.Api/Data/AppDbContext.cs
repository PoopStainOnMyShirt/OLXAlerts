using Microsoft.EntityFrameworkCore;
using OLXAlerts.Api.Entities;

namespace OLXAlerts.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SearchJob> SearchJobs => Set<SearchJob>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<AlertLog> AlertLogs => Set<AlertLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SearchJob>(e =>
        {
            e.ToTable("search_jobs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            e.Property(x => x.SearchTerm).HasColumnName("search_term").IsRequired();
            e.Property(x => x.LocationCode).HasColumnName("location_code").HasDefaultValue("1000001");
            e.Property(x => x.LocationName).HasColumnName("location_name");
            e.Property(x => x.CategoryId).HasColumnName("category_id");
            e.Property(x => x.WhatsAppNumber).HasColumnName("whatsapp_number");
            e.Property(x => x.NotificationChannel).HasColumnName("notification_channel").HasDefaultValue(NotificationChannel.WhatsApp);
            e.Property(x => x.TelegramChatId).HasColumnName("telegram_chat_id");
            e.Property(x => x.IntervalMinutes).HasColumnName("interval_minutes").HasDefaultValue(60);
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.Property(x => x.LastRunAt).HasColumnName("last_run_at");
            e.Property(x => x.NextRunAt).HasColumnName("next_run_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Listing>(e =>
        {
            e.ToTable("listings");
            e.HasKey(x => new { x.Id, x.JobId });
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.JobId).HasColumnName("job_id");
            e.Property(x => x.Title).HasColumnName("title");
            e.Property(x => x.UserName).HasColumnName("user_name");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.OlxCreatedAt).HasColumnName("olx_created_at");
            e.Property(x => x.CarBodyType).HasColumnName("car_body_type");
            e.Property(x => x.AdId).HasColumnName("ad_id");
            e.Property(x => x.IsBusiness).HasColumnName("is_business");
            e.Property(x => x.PriceDisplay).HasColumnName("price_display");
            e.Property(x => x.PriceValue).HasColumnName("price_value").HasColumnType("numeric(14,2)");
            e.Property(x => x.Location).HasColumnName("location");
            e.Property(x => x.RawData).HasColumnName("raw_data").HasColumnType("jsonb");
            e.Property(x => x.ScrapedAt).HasColumnName("scraped_at").HasDefaultValueSql("NOW()");
            e.HasOne(x => x.Job).WithMany(j => j.Listings).HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlertLog>(e =>
        {
            e.ToTable("alert_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityColumn();
            e.Property(x => x.JobId).HasColumnName("job_id");
            e.Property(x => x.ListingId).HasColumnName("listing_id").IsRequired();
            e.Property(x => x.WhatsAppNumber).HasColumnName("whatsapp_number");
            e.Property(x => x.TelegramChatId).HasColumnName("telegram_chat_id");
            e.Property(x => x.MessageSid).HasColumnName("message_sid");
            e.Property(x => x.SentAt).HasColumnName("sent_at").HasDefaultValueSql("NOW()");
            e.Property(x => x.Status).HasColumnName("status").HasDefaultValue("sent");
            e.HasOne(x => x.Job).WithMany(j => j.AlertLogs).HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
