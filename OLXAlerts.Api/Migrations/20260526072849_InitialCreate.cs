using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLXAlerts.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "search_jobs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    search_term = table.Column<string>(type: "text", nullable: false),
                    location_code = table.Column<string>(type: "text", nullable: false, defaultValue: "1000001"),
                    whatsapp_number = table.Column<string>(type: "text", nullable: false),
                    interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alert_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_id = table.Column<int>(type: "integer", nullable: false),
                    listing_id = table.Column<string>(type: "text", nullable: false),
                    whatsapp_number = table.Column<string>(type: "text", nullable: false),
                    message_sid = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "sent")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_alert_logs_search_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "search_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listings",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    job_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    user_name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    olx_created_at = table.Column<string>(type: "text", nullable: true),
                    car_body_type = table.Column<string>(type: "text", nullable: true),
                    ad_id = table.Column<string>(type: "text", nullable: true),
                    is_business = table.Column<bool>(type: "boolean", nullable: true),
                    price_display = table.Column<string>(type: "text", nullable: true),
                    price_value = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    location = table.Column<string>(type: "text", nullable: true),
                    raw_data = table.Column<string>(type: "jsonb", nullable: true),
                    scraped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listings", x => new { x.id, x.job_id });
                    table.ForeignKey(
                        name: "FK_listings_search_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "search_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_logs_job_id",
                table: "alert_logs",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_listings_job_id",
                table: "listings",
                column: "job_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_logs");

            migrationBuilder.DropTable(
                name: "listings");

            migrationBuilder.DropTable(
                name: "search_jobs");
        }
    }
}
