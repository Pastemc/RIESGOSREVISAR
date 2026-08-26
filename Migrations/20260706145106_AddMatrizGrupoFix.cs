using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiesgosElor.Migrations
{
    /// <inheritdoc />
    public partial class AddMatrizGrupoFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AprobadoPor",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AprobadoPorFirma",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CodigoMatriz",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CodigoProceso",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ElaboradoPor",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ElaboradoPorFirma",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Fecha",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FechaAprobacion",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MatrizNivel",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NombreProceso",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RevisadoPor",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RevisadoPorFirma",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VersionMatriz",
                table: "MatrizGrupos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AprobadoPor",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "AprobadoPorFirma",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "CodigoMatriz",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "CodigoProceso",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "ElaboradoPor",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "ElaboradoPorFirma",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "Fecha",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "FechaAprobacion",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "MatrizNivel",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "NombreProceso",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "RevisadoPor",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "RevisadoPorFirma",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MatrizGrupos");

            migrationBuilder.DropColumn(
                name: "VersionMatriz",
                table: "MatrizGrupos");
        }
    }
}
