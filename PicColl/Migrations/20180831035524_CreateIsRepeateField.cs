using Microsoft.EntityFrameworkCore.Migrations;

namespace PicColl.Migrations
{
    public partial class CreateIsRepeateField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRepeat",
                table: "PicInfo",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRepeat",
                table: "PicInfo");
        }
    }
}
