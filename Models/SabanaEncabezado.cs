namespace RiesgosElor.Models;

public class SabanaEncabezado
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "PGEP-024-F001";
    public string Version { get; set; } = "02";
    public string Fecha { get; set; } = "";
    public string ElaboradoPor { get; set; } = "JEFE DEL DEPARTAMENTO DE PLANEAMIENTO Y REGULACIÓN";
    public string RevisadoPor { get; set; } = "GERENTE DE PLANEAMIENTO, GESTIÓN Y REGULACIÓN";
    public string AprobadoPor { get; set; } = "GERENTE GENERAL";
    public string ElaboradoPorFirma { get; set; } = "Gerencia de Planeamiento, Gestión y Regulación";
    public string RevisadoPorFirma { get; set; } = "Responsable de la Gestión Integral de Riesgos - GIR";
    public string AprobadoPorFirma { get; set; } = "Comité Técnico de Riesgos - CTR";
    public DateTime FechaModificacion { get; set; } = DateTime.Now;
    public string ModificadoPor { get; set; } = "Sistema";
}