using System;
using System.Collections.Generic;

namespace RiesgosElor.Models;

public class MatrizGrupo
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public string CodigoProceso { get; set; } = "";
    public string NombreProceso { get; set; } = "";
    public string Nombre { get; set; } = "";
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public bool Cerrada { get; set; } = false;

    public string Codigo { get; set; } = "PGEP-024-F001";
    public string Version { get; set; } = "02";
    public string Fecha { get; set; } = "";
    public string ElaboradoPor { get; set; } = "JEFE DEL DEPARTAMENTO DE PLANEAMIENTO Y REGULACIÓN";
    public string RevisadoPor { get; set; } = "GERENTE DE PLANEAMIENTO, GESTIÓN Y REGULACIÓN";
    public string AprobadoPor { get; set; } = "GERENTE GENERAL";
    public string CodigoMatriz { get; set; } = "";
    public string VersionMatriz { get; set; } = "04";
    public string FechaAprobacion { get; set; } = "";
    public string MatrizNivel { get; set; } = "Proceso";
    public string ElaboradoPorFirma { get; set; } = "Gerencia de Planeamiento, Gestión y Regulación";
    public string RevisadoPorFirma { get; set; } = "Responsable de la Gestión Integral de Riesgos - GIR";
    public string AprobadoPorFirma { get; set; } = "Comité Técnico de Riesgos - CTR";

    public ICollection<Riesgo> Riesgos { get; set; } = new List<Riesgo>();
}