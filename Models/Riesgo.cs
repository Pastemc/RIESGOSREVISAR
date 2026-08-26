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

    public int SeveridadInherente => ProbabilidadInherente * ImpactoInherente;
    public string NivelInherente => GetNivel(SeveridadInherente);
    public int SeveridadResidual => ProbabilidadResidual * ImpactoResidual;
    public string NivelResidual => GetNivel(SeveridadResidual);
    public bool RequierePlanAccion => NivelResidual == "Alto" || NivelResidual == "Extremo";

    public static string GetNivel(int severidad) => severidad switch
    {
        <= 2 => "Bajo",
        <= 6 => "Moderado",
        <= 9 => "Alto",
        _ => "Extremo"
    };
}