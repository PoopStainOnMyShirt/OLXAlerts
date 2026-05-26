using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLXAlerts.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationNameCategoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "search_jobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location_name",
                table: "search_jobs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category_id",
                table: "search_jobs");

            migrationBuilder.DropColumn(
                name: "location_name",
                table: "search_jobs");
        }
    }
}
