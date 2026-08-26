using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class MaestroProceso
{
    public int Id { get; set; }

    [Required]
    public string Codigo { get; set; } = ""; // Ej: "E1.1", "O2.1", "S3.2"

    [Required]
    public string Nombre { get; set; } = ""; // Ej: "Gestión de la Infraestructura Tecnológica"

    public string Nivel { get; set; } = "Proceso"; // "Macroproceso", "Proceso", "Subproceso"
    public string Macroproceso { get; set; } = ""; // Ej: "EVALUACIÓN Y MEJORA CONTINUA"
    public string AreaResponsableDefault { get; set; } = "";
    public int? PadreId { get; set; }
    public MaestroProceso? Padre { get; set; }
    public ICollection<MaestroProceso> Subprocesos { get; set; } = new List<MaestroProceso>();

    public int? AreaResponsableId { get; set; }
    public MaestroArea? AreaResponsable { get; set; }

    public int? ResponsableId { get; set; }
    public MaestroResponsable? Responsable { get; set; }

    public string Objetivo { get; set; } = "";
    public string Descripcion { get; set; } = "";

    public bool Activo { get; set; } = true;
    public int Orden { get; set; } = 0;
}
