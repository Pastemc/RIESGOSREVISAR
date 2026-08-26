using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiesgosElor.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioColumnsExtendidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaestroProcesos_MaestroAreas_AreaResponsableId",
                table: "MaestroProcesos");

            migrationBuilder.DropForeignKey(
                name: "FK_MaestroProcesos_MaestroProcesos_PadreId",
                table: "MaestroProcesos");

            migrationBuilder.DropForeignKey(
                name: "FK_MaestroProcesos_MaestroResponsables_ResponsableId",
                table: "MaestroProcesos");

            migrationBuilder.DropForeignKey(
                name: "FK_MaestroResponsables_MaestroAreas_AreaId",
                table: "MaestroResponsables");

            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gerencia",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaSincronizacionApi",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BitacoraDepartamentoGerencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartamentoId = table.Column<int>(type: "int", nullable: false),
                    NombreDepartamento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GerenciaAnteriorId = table.Column<int>(type: "int", nullable: true),
                    GerenciaAnteriorNombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GerenciaNuevaId = table.Column<int>(type: "int", nullable: true),
                    GerenciaNuevaNombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioNombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitacoraDepartamentoGerencias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BitacorasUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    ModificadoPor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Campo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValorAnterior = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValorNuevo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitacorasUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BitacorasUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BitacorasUsuario_UsuarioId",
                table: "BitacorasUsuario",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaestroProcesos_MaestroAreas_AreaResponsableId",
                table: "MaestroProcesos",
                column: "AreaResponsableId",
                principalTable: "MaestroAreas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaestroProcesos_MaestroProcesos_PadreId",
                table: "MaestroProcesos",
                column: "PadreId",
                principalTable: "MaestroProcesos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaestroProcesos_MaestroResponsables_ResponsableId",
                table: "MaestroProcesos",
                column: "ResponsableId",
                principalTable: "MaestroResponsables",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaestroResponsables_MaestroAreas_AreaId",
                table: "MaestroResponsables",
                column: "AreaId",
                principalTable: "MaestroAreas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaestroProcesos_MaestroAreas_AreaResponsableId",
                table: "MaestroProcesos");

            migrationBuilder.DropForeignKey(
                name: "FK_MaestroProcesos_MaestroProcesos_PadreId",
                table: "MaestroProcesos");

            migrationBuilder.DropForeignKey(
                name: "FK_MaestroProcesos_MaestroResponsables_ResponsableId",
                table: "MaestroProcesos");

            migrationBuilder.DropForeignKey(
                name: "FK_MaestroResponsables_MaestroAreas_AreaId",
                table: "MaestroResponsables");

            migrationBuilder.DropTable(
                name: "BitacoraDepartamentoGerencias");

            migrationBuilder.DropTable(
                name: "BitacorasUsuario");

            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Gerencia",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "UltimaSincronizacionApi",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Usuarios");

            migrationBuilder.AddForeignKey(
                name: "FK_MaestroProcesos_MaestroAreas_AreaResponsableId",
                table: "MaestroProcesos",
                column: "AreaResponsableId",
                principalTable: "MaestroAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MaestroProcesos_MaestroProcesos_PadreId",
                table: "MaestroProcesos",
                column: "PadreId",
                principalTable: "MaestroProcesos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaestroProcesos_MaestroResponsables_ResponsableId",
                table: "MaestroProcesos",
                column: "ResponsableId",
                principalTable: "MaestroResponsables",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MaestroResponsables_MaestroAreas_AreaId",
                table: "MaestroResponsables",
                column: "AreaId",
                principalTable: "MaestroAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
