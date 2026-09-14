namespace RiesgosElor.Models;

// ════════════════════════════════════════════════════════════════════════════
// MODELO: PeriodoRiesgo
// ════════════════════════════════════════════════════════════════════════════
public class PeriodoRiesgo
{
    public int      Id              { get; set; }
    public string   NombrePeriodo   { get; set; } = "";
    public string   Tipo            { get; set; } = "Ordinario";
    public string   Frecuencia      { get; set; } = "Anual";
    public DateTime FechaInicio     { get; set; } = DateTime.Now;
    public DateTime FechaCierre     { get; set; } = DateTime.Now.AddMonths(12);
    public DateTime? FechaCierreReal{ get; set; }
    public string   Estado          { get; set; } = "Activo";
    public string   CreadoPor       { get; set; } = "SuperAdmin";
    public string   CerradoPor      { get; set; } = "";
    public string   Descripcion     { get; set; } = "";
    public DateTime FechaCreacion   { get; set; } = DateTime.Now;

    // Navegación
    public ICollection<BitacoraPeriodoRiesgo> Bitacora { get; set; }
        = new List<BitacoraPeriodoRiesgo>();
}

// ════════════════════════════════════════════════════════════════════════════
// MODELO: BitacoraPeriodoRiesgo
// ════════════════════════════════════════════════════════════════════════════
public class BitacoraPeriodoRiesgo
{
    public int      Id              { get; set; }
    public int      PeriodoId       { get; set; }
    public string   Accion          { get; set; } = ""; // Creado|Editado|Cerrado|Eliminado
    public string   CampoModificado { get; set; } = "";
    public string   ValorAnterior   { get; set; } = "";
    public string   ValorNuevo      { get; set; } = "";
    public string   UsuarioNombre   { get; set; } = "SuperAdmin";
    public DateTime FechaCambio     { get; set; } = DateTime.Now;
    public string   Motivo          { get; set; } = "";

    // Navegación
    public PeriodoRiesgo? Periodo { get; set; }
}
