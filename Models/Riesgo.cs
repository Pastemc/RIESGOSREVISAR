using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

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

    // ─── Propiedades calculadas ───────────────────────────────────────────────
    public int SeveridadInherente => ProbabilidadInherente * ImpactoInherente;
    public string NivelInherente => GetNivel(ProbabilidadInherente, ImpactoInherente);
    public int SeveridadResidual => ProbabilidadResidual * ImpactoResidual;
    public string NivelResidual => GetNivel(ProbabilidadResidual, ImpactoResidual);
    public bool RequierePlanAccion => NivelResidual == "Alto" || NivelResidual == "Extremo";

    // ─── GetNivel 2 parámetros — tabla FONAFE ELORSA (fuente de verdad) ───────
    // Prob\Imp   1          2          3          4
    //    1      Bajo       Bajo       Moderado   Moderado
    //    2      Bajo       Moderado   Alto       Alto
    //    3      Moderado   Alto       Alto       Extremo
    //    4      Moderado   Alto       Extremo    Extremo
    public static string GetNivel(int prob, int imp) => (prob, imp) switch
    {
        (1, 1) or (2, 1) or (1, 2) => "Bajo",
        (3, 1) or (1, 3) or (4, 1) or (1, 4) or (2, 2) => "Moderado",
        (3, 2) or (2, 3) or (4, 2) or (2, 4) or (3, 3) => "Alto",
        (4, 3) or (3, 4) or (4, 4) => "Extremo",
        _ => "Bajo"
    };

    // ─── GetNivel 1 parámetro — delega en la tabla FONAFE via prob=imp=√sev ──
    // IMPORTANTE: la tabla FONAFE no es simétrica con la severidad simple.
    // Para compatibilidad con código legacy que pase solo severidad,
    // usamos la misma lógica que la tabla FONAFE con los casos conocidos:
    //   sev=1(1×1)→Bajo  sev=2(1×2 o 2×1)→Bajo  sev=3(1×3 o 3×1)→Moderado
    //   sev=4(1×4,4×1,2×2)→Moderado  sev=6(2×3,3×2)→Alto  sev=8(2×4,4×2)→Alto
    //   sev=9(3×3)→Alto  sev=12(3×4,4×3)→Extremo  sev=16(4×4)→Extremo
    public static string GetNivel(int severidad) => severidad switch
    {
        1 => "Bajo",       // 1×1
        2 => "Bajo",       // 1×2, 2×1
        3 => "Moderado",   // 1×3, 3×1
        4 => "Moderado",   // 1×4, 4×1, 2×2
        6 => "Alto",       // 2×3, 3×2  ← antes era Moderado (BUG)
        8 => "Alto",       // 2×4, 4×2
        9 => "Alto",       // 3×3
        12 => "Extremo",    // 3×4, 4×3
        16 => "Extremo",    // 4×4
        _ => "Bajo"        // cualquier otro valor inesperado
    };
}