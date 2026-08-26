using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiesgosElor.Migrations
{
    /// <inheritdoc />
    public partial class AddEstrategiaResidual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstrategiaResidual",
                table: "Riesgos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstrategiaResidual",
                table: "Riesgos");
        }
    }
}
