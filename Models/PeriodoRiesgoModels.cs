namespace RiesgosElor.Models;

// ════════════════════════════════════════════════════════════════════════════
// MODELO: PeriodoRiesgo
// ════════════════════════════════════════════════════════════════════════════
public class PeriodoRiesgo
{
    public int Id { get; set; }
    public string NombrePeriodo { get; set; } = "";
    public string Tipo { get; set; } = "Ordinario";
    public string Frecuencia { get; set; } = "Anual";
    public DateTime FechaInicio { get; set; } = DateTime.Now;
    public DateTime FechaCierre { get; set; } = DateTime.Now.AddMonths(12);
    public DateTime? FechaCierreReal { get; set; }
    public string Estado { get; set; } = "Activo";
    public string CreadoPor { get; set; } = "SuperAdmin";
    public string CerradoPor { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // ── Propiedades nuevas — versioning y tipo de periodo ────────────────
    public string TipoPeriodo { get; set; } = "Apertura";   // Apertura | Validación | Evidencia
    public int Version { get; set; } = 1;            // Sube cuando hay cambios en carga base
    public int NumeroPeriodo { get; set; } = 1;            // Correlativo por año: P-1, P-2, P-3...
    public int AnioRef { get; set; } = DateTime.Now.Year; // Año de referencia del periodo
    public bool TuvoModificaciones { get; set; } = false;   // Flag — hubo cambios de contenido

    // Para Validacion/Evidencia: Id del periodo Apertura cerrado que se valida.
    // Null para periodos de Apertura. El periodo de Validacion lee el snapshot de este Apertura.
    public int? PeriodoAperturaRefId { get; set; } = null;

    // Navegación
    public ICollection<BitacoraPeriodoRiesgo> Bitacora { get; set; }
        = new List<BitacoraPeriodoRiesgo>();
}

// ════════════════════════════════════════════════════════════════════════════
// MODELO: BitacoraPeriodoRiesgo
// ════════════════════════════════════════════════════════════════════════════
public class BitacoraPeriodoRiesgo
{
    public int Id { get; set; }
    public int PeriodoId { get; set; }
    public string Accion { get; set; } = ""; // Creado|Editado|Cerrado|Eliminado
    public string CampoModificado { get; set; } = "";
    public string ValorAnterior { get; set; } = "";
    public string ValorNuevo { get; set; } = "";
    public string UsuarioNombre { get; set; } = "SuperAdmin";
    public DateTime FechaCambio { get; set; } = DateTime.Now;
    public string Motivo { get; set; } = "";

    // Navegación
    public PeriodoRiesgo? Periodo { get; set; }
}