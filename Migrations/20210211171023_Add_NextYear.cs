using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleSchedule.Migrations
{
    public partial class Add_NextYear : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NextYearVacationDaysLeft",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NextYearVacationDaysUsed",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextYearVacationDaysLeft",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NextYearVacationDaysUsed",
                table: "AspNetUsers");
        }
    }
}
