using Microsoft.EntityFrameworkCore.Migrations;

namespace DataBaseSetup.Migrations
{
    public partial class FinalMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Count",
                table: "Recognitions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Count",
                table: "Recognitions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
