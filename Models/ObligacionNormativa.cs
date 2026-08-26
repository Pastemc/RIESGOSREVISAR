using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class ObligacionNormativa
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Codigo { get; set; } = "";

    [Required, MaxLength(200)]
    public string Norma { get; set; } = "";

    [MaxLength(80)]
    public string Articulo { get; set; } = "";

    [Required]
    public string Descripcion { get; set; } = "";

    [MaxLength(20)]
    public string CodigoProceso { get; set; } = "";

    [MaxLength(160)]
    public string AreaResponsable { get; set; } = "";

    [MaxLength(160)]
    public string Responsable { get; set; } = "";

    [MaxLength(60)]
    public string Periodicidad { get; set; } = "Anual";

    public DateTime? FechaVencimiento { get; set; }

    [MaxLength(40)]
    public string Estado { get; set; } = "Pendiente";

    public string Evidencia { get; set; } = "";

    public int? RiesgoId { get; set; }
    public Riesgo? Riesgo { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
