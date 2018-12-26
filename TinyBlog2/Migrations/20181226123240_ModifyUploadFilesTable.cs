using Microsoft.EntityFrameworkCore.Migrations;

namespace TinyBlog2.Migrations
{
    public partial class ModifyUploadFilesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnialUri",
                table: "uploadFiles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnialUri",
                table: "uploadFiles");
        }
    }
}
