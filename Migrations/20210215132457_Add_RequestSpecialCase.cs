using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleSchedule.Migrations
{
    public partial class Add_RequestSpecialCase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecialCase",
                table: "Requests",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecialCase",
                table: "Requests");
        }
    }
}
