using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLXAlerts.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "whatsapp_number",
                table: "search_jobs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "notification_channel",
                table: "search_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "telegram_chat_id",
                table: "search_jobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "whatsapp_number",
                table: "alert_logs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<long>(
                name: "telegram_chat_id",
                table: "alert_logs",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notification_channel",
                table: "search_jobs");

            migrationBuilder.DropColumn(
                name: "telegram_chat_id",
                table: "search_jobs");

            migrationBuilder.DropColumn(
                name: "telegram_chat_id",
                table: "alert_logs");

            migrationBuilder.AlterColumn<string>(
                name: "whatsapp_number",
                table: "search_jobs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "whatsapp_number",
                table: "alert_logs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
