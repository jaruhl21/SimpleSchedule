using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleSchedule.Migrations
{
    public partial class AddERType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdjustmentType",
                table: "EarlyReleases",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustmentType",
                table: "EarlyReleases");
        }
    }
}
