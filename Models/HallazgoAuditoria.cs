using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class HallazgoAuditoria
{
    public int Id { get; set; }

    public int AuditoriaGrcId { get; set; }
    public AuditoriaGrc? AuditoriaGrc { get; set; }

    [Required, MaxLength(40)]
    public string Codigo { get; set; } = "";

    [Required]
    public string Descripcion { get; set; } = "";

    [MaxLength(40)]
    public string Severidad { get; set; } = "Media";

    [MaxLength(160)]
    public string Responsable { get; set; } = "";

    public DateTime? FechaCompromiso { get; set; }

    [MaxLength(40)]
    public string Estado { get; set; } = "Abierto";

    public int? PlanAccionId { get; set; }
    public PlanAccion? PlanAccion { get; set; }
}
