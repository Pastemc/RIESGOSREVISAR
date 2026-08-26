using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class DocumentoGrc
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Codigo { get; set; } = "";

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = "";

    [MaxLength(80)]
    public string Tipo { get; set; } = "Politica";

    [MaxLength(20)]
    public string CodigoProceso { get; set; } = "";

    [MaxLength(40)]
    public string Version { get; set; } = "1.0";

    [MaxLength(160)]
    public string Responsable { get; set; } = "";

    public DateTime? FechaAprobacion { get; set; }
    public DateTime? FechaRevision { get; set; }

    [MaxLength(40)]
    public string Estado { get; set; } = "Vigente";

    public string Ubicacion { get; set; } = "";
    public string Observaciones { get; set; } = "";
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
