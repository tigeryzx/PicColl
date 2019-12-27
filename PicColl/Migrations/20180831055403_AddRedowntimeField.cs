using Microsoft.EntityFrameworkCore.Migrations;

namespace PicColl.Migrations
{
    public partial class AddRedowntimeField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReDownTime",
                table: "PicInfo",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReDownTime",
                table: "PicInfo");
        }
    }
}
