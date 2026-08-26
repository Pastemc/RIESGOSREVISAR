using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class AuditoriaGrc
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Codigo { get; set; } = "";

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = "";

    [MaxLength(80)]
    public string Tipo { get; set; } = "Interna";

    public string Alcance { get; set; } = "";

    [MaxLength(20)]
    public string CodigoProceso { get; set; } = "";

    [MaxLength(160)]
    public string Responsable { get; set; } = "";

    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    [MaxLength(40)]
    public string Estado { get; set; } = "Planificada";

    public string Observaciones { get; set; } = "";

    public ICollection<HallazgoAuditoria> Hallazgos { get; set; } = new List<HallazgoAuditoria>();
}
