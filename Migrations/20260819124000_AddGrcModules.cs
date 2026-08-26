using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RiesgosElor.Data;

#nullable disable

namespace RiesgosElor.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260819124000_AddGrcModules")]
    public partial class AddGrcModules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivosInformacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoActivo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CodigoProceso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Criticidad = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Confidencialidad = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Integridad = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Disponibilidad = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RiesgoId = table.Column<int>(type: "int", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivosInformacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivosInformacion_Riesgos_RiesgoId",
                        column: x => x.RiesgoId,
                        principalTable: "Riesgos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AuditoriasGrc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Alcance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoProceso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriasGrc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosGrc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CodigoProceso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosGrc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluacionesControl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiesgoControlId = table.Column<int>(type: "int", nullable: false),
                    FechaEvaluacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Evaluador = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Diseno = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ejecucion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Evidencia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionesControl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluacionesControl_RiesgosControl_RiesgoControlId",
                        column: x => x.RiesgoControlId,
                        principalTable: "RiesgosControl",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventosRiesgo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RiesgoId = table.Column<int>(type: "int", nullable: true),
                    CodigoProceso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoEvento = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaReporta = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ResponsableRegistro = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    FechaEvento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoPerdidaEstimado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CausaRaiz = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccionInmediata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeccionAprendida = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosRiesgo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosRiesgo_Riesgos_RiesgoId",
                        column: x => x.RiesgoId,
                        principalTable: "Riesgos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ObligacionesNormativas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Norma = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Articulo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoProceso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AreaResponsable = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Periodicidad = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Evidencia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiesgoId = table.Column<int>(type: "int", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObligacionesNormativas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObligacionesNormativas_Riesgos_RiesgoId",
                        column: x => x.RiesgoId,
                        principalTable: "Riesgos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HallazgosAuditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditoriaGrcId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severidad = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    FechaCompromiso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlanAccionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HallazgosAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HallazgosAuditoria_AuditoriasGrc_AuditoriaGrcId",
                        column: x => x.AuditoriaGrcId,
                        principalTable: "AuditoriasGrc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HallazgosAuditoria_PlanesAccion_PlanAccionId",
                        column: x => x.PlanAccionId,
                        principalTable: "PlanesAccion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivosInformacion_RiesgoId",
                table: "ActivosInformacion",
                column: "RiesgoId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesControl_RiesgoControlId",
                table: "EvaluacionesControl",
                column: "RiesgoControlId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosRiesgo_RiesgoId",
                table: "EventosRiesgo",
                column: "RiesgoId");

            migrationBuilder.CreateIndex(
                name: "IX_HallazgosAuditoria_AuditoriaGrcId",
                table: "HallazgosAuditoria",
                column: "AuditoriaGrcId");

            migrationBuilder.CreateIndex(
                name: "IX_HallazgosAuditoria_PlanAccionId",
                table: "HallazgosAuditoria",
                column: "PlanAccionId");

            migrationBuilder.CreateIndex(
                name: "IX_ObligacionesNormativas_RiesgoId",
                table: "ObligacionesNormativas",
                column: "RiesgoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ActivosInformacion");
            migrationBuilder.DropTable(name: "DocumentosGrc");
            migrationBuilder.DropTable(name: "EvaluacionesControl");
            migrationBuilder.DropTable(name: "EventosRiesgo");
            migrationBuilder.DropTable(name: "HallazgosAuditoria");
            migrationBuilder.DropTable(name: "ObligacionesNormativas");
            migrationBuilder.DropTable(name: "AuditoriasGrc");
        }
    }
}
