using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class MaestroResponsable
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Nombre { get; set; } = "";

    [MaxLength(160)]
    public string Cargo { get; set; } = "";

    [MaxLength(160)]
    public string Correo { get; set; } = "";

    public int? AreaId { get; set; }
    public MaestroArea? Area { get; set; }

    public bool Activo { get; set; } = true;
    public int Orden { get; set; } = 0;
}
