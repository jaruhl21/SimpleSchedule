using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleSchedule.Migrations
{
    public partial class AddAndChange_DaysUsed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VacationDaysPlanned",
                table: "AspNetUsers",
                newName: "VacationDaysUsed");

            migrationBuilder.AddColumn<int>(
                name: "SickDaysUsed",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SickDaysUsed",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "VacationDaysUsed",
                table: "AspNetUsers",
                newName: "VacationDaysPlanned");
        }
    }
}
