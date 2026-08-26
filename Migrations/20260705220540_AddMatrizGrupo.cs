using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiesgosElor.Migrations
{
    /// <inheritdoc />
    public partial class AddMatrizGrupo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatrizGrupoId",
                table: "Riesgos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MatrizGrupos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Cerrada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatrizGrupos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Riesgos_MatrizGrupoId",
                table: "Riesgos",
                column: "MatrizGrupoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Riesgos_MatrizGrupos_MatrizGrupoId",
                table: "Riesgos",
                column: "MatrizGrupoId",
                principalTable: "MatrizGrupos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Riesgos_MatrizGrupos_MatrizGrupoId",
                table: "Riesgos");

            migrationBuilder.DropTable(
                name: "MatrizGrupos");

            migrationBuilder.DropIndex(
                name: "IX_Riesgos_MatrizGrupoId",
                table: "Riesgos");

            migrationBuilder.DropColumn(
                name: "MatrizGrupoId",
                table: "Riesgos");
        }
    }
}
