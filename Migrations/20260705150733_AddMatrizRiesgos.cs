using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiesgosElor.Migrations
{
    /// <inheritdoc />
    public partial class AddMatrizRiesgos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Riesgos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoProceso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreProceso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nivel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GerenciaResponsable = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subproceso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoRiesgo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescripcionRiesgo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrigenRiesgo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FrecuenciaRiesgo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoRiesgo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProbabilidadInherente = table.Column<int>(type: "int", nullable: false),
                    ImpactoInherente = table.Column<int>(type: "int", nullable: false),
                    ProbabilidadResidual = table.Column<int>(type: "int", nullable: false),
                    ImpactoResidual = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Riesgos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Indicadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiesgoId = table.Column<int>(type: "int", nullable: false),
                    CodigoKRI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefinicionKRI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Frecuencia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaKRI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KRIActual = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsableKRI = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indicadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Indicadores_Riesgos_RiesgoId",
                        column: x => x.RiesgoId,
                        principalTable: "Riesgos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanesAccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiesgoId = table.Column<int>(type: "int", nullable: false),
                    EstrategiaRespuesta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescripcionPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaResponsable = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsablePlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InicioPlan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinPlan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstadoPlan = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesAccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanesAccion_Riesgos_RiesgoId",
                        column: x => x.RiesgoId,
                        principalTable: "Riesgos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiesgosControl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiesgoId = table.Column<int>(type: "int", nullable: false),
                    CodigoControl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescripcionControl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaResponsable = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsablesControl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FrecuenciaControl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OportunidadControl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutomatizacionControl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenciaControl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiesgosControl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiesgosControl_Riesgos_RiesgoId",
                        column: x => x.RiesgoId,
                        principalTable: "Riesgos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Indicadores_RiesgoId",
                table: "Indicadores",
                column: "RiesgoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesAccion_RiesgoId",
                table: "PlanesAccion",
                column: "RiesgoId");

            migrationBuilder.CreateIndex(
                name: "IX_RiesgosControl_RiesgoId",
                table: "RiesgosControl",
                column: "RiesgoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Indicadores");

            migrationBuilder.DropTable(
                name: "PlanesAccion");

            migrationBuilder.DropTable(
                name: "RiesgosControl");

            migrationBuilder.DropTable(
                name: "Riesgos");
        }
    }
}
