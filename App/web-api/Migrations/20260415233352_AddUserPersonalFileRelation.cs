using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPersonalFileRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonalFileId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonalFileId",
                table: "Users",
                column: "PersonalFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_PersonalFiles_PersonalFileId",
                table: "Users",
                column: "PersonalFileId",
                principalTable: "PersonalFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_PersonalFiles_PersonalFileId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PersonalFileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersonalFileId",
                table: "Users");
        }
    }
}
