using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiesgosElor.Migrations
{
    /// <inheritdoc />
    public partial class AddJerarquiaAreasGir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PadreId",
                table: "MaestroAreas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaestroAreas_PadreId",
                table: "MaestroAreas",
                column: "PadreId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaestroAreas_MaestroAreas_PadreId",
                table: "MaestroAreas",
                column: "PadreId",
                principalTable: "MaestroAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaestroAreas_MaestroAreas_PadreId",
                table: "MaestroAreas");

            migrationBuilder.DropIndex(
                name: "IX_MaestroAreas_PadreId",
                table: "MaestroAreas");

            migrationBuilder.DropColumn(
                name: "PadreId",
                table: "MaestroAreas");
        }
    }
}
