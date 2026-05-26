using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLXAlerts.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxPrice",
                table: "search_jobs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinPrice",
                table: "search_jobs",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxPrice",
                table: "search_jobs");

            migrationBuilder.DropColumn(
                name: "MinPrice",
                table: "search_jobs");
        }
    }
}
