using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class ActivoInformacion
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Codigo { get; set; } = "";

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = "";

    [MaxLength(80)]
    public string TipoActivo { get; set; } = "Aplicacion";

    [MaxLength(20)]
    public string CodigoProceso { get; set; } = "";

    [MaxLength(160)]
    public string Responsable { get; set; } = "";

    [MaxLength(40)]
    public string Criticidad { get; set; } = "Media";

    [MaxLength(40)]
    public string Confidencialidad { get; set; } = "Media";

    [MaxLength(40)]
    public string Integridad { get; set; } = "Media";

    [MaxLength(40)]
    public string Disponibilidad { get; set; } = "Media";

    [MaxLength(40)]
    public string Estado { get; set; } = "Activo";

    public int? RiesgoId { get; set; }
    public Riesgo? Riesgo { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
