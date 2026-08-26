using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class MaestroArea
{
    public int Id { get; set; }

    [Required]
    public string Categoria { get; set; } = "DEPARTAMENTOS"; // "GERENCIAS", "OFICINAS", "DIRECTORIO", "DEPARTAMENTOS", "OTROS"

    [Required]
    public string Nombre { get; set; } = ""; // Ej: "Gerencia de Administración y Finanzas", "Dpto. de TIC"

    public string Codigo { get; set; } = ""; // Ej: "GAF", "TIC"

    public int? PadreId { get; set; }
    public MaestroArea? Padre { get; set; }
    public ICollection<MaestroArea> UnidadesHijas { get; set; } = new List<MaestroArea>();

    public bool Activo { get; set; } = true;
    public int Orden { get; set; } = 0;
}
