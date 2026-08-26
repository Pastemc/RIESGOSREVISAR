using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiesgosElor.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizacionGirMasters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaestroAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaestroAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaestroParametros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Grupo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaestroParametros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaestroTiposRiesgo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaestroTiposRiesgo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaestroResponsables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaestroResponsables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaestroResponsables_MaestroAreas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "MaestroAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MaestroProcesos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nivel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Macroproceso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaResponsableDefault = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PadreId = table.Column<int>(type: "int", nullable: true),
                    AreaResponsableId = table.Column<int>(type: "int", nullable: true),
                    ResponsableId = table.Column<int>(type: "int", nullable: true),
                    Objetivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaestroProcesos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaestroProcesos_MaestroAreas_AreaResponsableId",
                        column: x => x.AreaResponsableId,
                        principalTable: "MaestroAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MaestroProcesos_MaestroProcesos_PadreId",
                        column: x => x.PadreId,
                        principalTable: "MaestroProcesos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaestroProcesos_MaestroResponsables_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "MaestroResponsables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaestroProcesos_AreaResponsableId",
                table: "MaestroProcesos",
                column: "AreaResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_MaestroProcesos_PadreId",
                table: "MaestroProcesos",
                column: "PadreId");

            migrationBuilder.CreateIndex(
                name: "IX_MaestroProcesos_ResponsableId",
                table: "MaestroProcesos",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_MaestroResponsables_AreaId",
                table: "MaestroResponsables",
                column: "AreaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaestroParametros");

            migrationBuilder.DropTable(
                name: "MaestroProcesos");

            migrationBuilder.DropTable(
                name: "MaestroTiposRiesgo");

            migrationBuilder.DropTable(
                name: "MaestroResponsables");

            migrationBuilder.DropTable(
                name: "MaestroAreas");
        }
    }
}
