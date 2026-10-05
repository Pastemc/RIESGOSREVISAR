using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

// ════════════════════════════════════════════════════════════════════════
// MODELO: Riesgo  (tabla dbo.Riesgos)
// ════════════════════════════════════════════════════════════════════════
public class Riesgo
{
    public int Id { get; set; }

    public int? MatrizGrupoId { get; set; }
    public MatrizGrupo? MatrizGrupo { get; set; }

    [Required]
    public string CodigoProceso { get; set; } = "";
    public string NombreProceso { get; set; } = "";
    public string Nivel { get; set; } = "Proceso";
    public string GerenciaResponsable { get; set; } = "";
    public string Subproceso { get; set; } = "";
    public string CodigoRiesgo { get; set; } = "";
    public string DescripcionRiesgo { get; set; } = "";
    public string OrigenRiesgo { get; set; } = "";
    public string FrecuenciaRiesgo { get; set; } = "";
    public string TipoRiesgo { get; set; } = "";
    public string EstrategiaResidual { get; set; } = "";

    public int ProbabilidadInherente { get; set; } = 1;
    public int ImpactoInherente { get; set; } = 1;
    public int ProbabilidadResidual { get; set; } = 1;
    public int ImpactoResidual { get; set; } = 1;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public int UsuarioId { get; set; }

    public ICollection<RiesgoControl> Controles { get; set; } = new List<RiesgoControl>();
    public ICollection<PlanAccion> PlanesAccion { get; set; } = new List<PlanAccion>();
    public ICollection<Indicador> Indicadores { get; set; } = new List<Indicador>();

    // ── Propiedades calculadas ───────────────────────────────────────────
    public int SeveridadInherente => ProbabilidadInherente * ImpactoInherente;
    public string NivelInherente => GetNivel(ProbabilidadInherente, ImpactoInherente);
    public int SeveridadResidual => ProbabilidadResidual * ImpactoResidual;
    public string NivelResidual => GetNivel(ProbabilidadResidual, ImpactoResidual);
    public bool RequierePlanAccion => NivelResidual == "Alto" || NivelResidual == "Extremo";

    // ── GetNivel 2 parámetros — tabla FONAFE ELORSA (fuente de verdad) ──
    //
    //  Prob\Imp  | 1        2        3        4
    //  ----------+--------------------------------
    //     1      | Bajo     Bajo     Moderado Moderado
    //     2      | Bajo     Moderado Alto     Alto
    //     3      | Moderado Alto     Alto     Extremo
    //     4      | Moderado Alto     Extremo  Extremo
    //
    public static string GetNivel(int prob, int imp) => (prob, imp) switch
    {
        (1, 1) or (2, 1) or (1, 2) => "Bajo",
        (3, 1) or (1, 3) or (4, 1) or (1, 4) or (2, 2) => "Moderado",
        (3, 2) or (2, 3) or (4, 2) or (2, 4) or (3, 3) => "Alto",
        (4, 3) or (3, 4) or (4, 4) => "Extremo",
        _ => "Bajo"
    };

    // ── GetNivel 1 parámetro — compatibilidad con código legacy ─────────
    //   sev=6 → Alto  (antes era Moderado, BUG corregido)
    public static string GetNivel(int severidad) => severidad switch
    {
        1 => "Bajo",
        2 => "Bajo",
        3 => "Moderado",
        4 => "Moderado",
        6 => "Alto",
        8 => "Alto",
        9 => "Alto",
        12 => "Extremo",
        16 => "Extremo",
        _ => "Bajo"
    };
}