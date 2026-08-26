using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiesgosElor.Migrations
{
    /// <inheritdoc />
    public partial class AddSabanaEncabezado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SabanaEncabezados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fecha = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ElaboradoPor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RevisadoPor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AprobadoPor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ElaboradoPorFirma = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RevisadoPorFirma = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AprobadoPorFirma = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModificadoPor = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SabanaEncabezados", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SabanaEncabezados");
        }
    }
}
