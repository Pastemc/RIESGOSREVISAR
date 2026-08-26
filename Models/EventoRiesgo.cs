using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class EventoRiesgo
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Codigo { get; set; } = "";

    public int? RiesgoId { get; set; }
    public Riesgo? Riesgo { get; set; }

    [MaxLength(20)]
    public string CodigoProceso { get; set; } = "";

    [Required, MaxLength(80)]
    public string TipoEvento { get; set; } = "Incidente";

    [Required]
    public string Descripcion { get; set; } = "";

    [MaxLength(160)]
    public string AreaReporta { get; set; } = "";

    [MaxLength(160)]
    public string ResponsableRegistro { get; set; } = "";

    public DateTime FechaEvento { get; set; } = DateTime.Today;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public decimal? MontoPerdidaEstimado { get; set; }

    [MaxLength(40)]
    public string Estado { get; set; } = "Reportado";

    public string CausaRaiz { get; set; } = "";
    public string AccionInmediata { get; set; } = "";
    public string LeccionAprendida { get; set; } = "";
}
