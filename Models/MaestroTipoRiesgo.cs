using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class MaestroTipoRiesgo
{
    public int Id { get; set; }

    [Required]
    public string Categoria { get; set; } = "Operacional"; // "Operacional", "Estratégico", "Financiero", "Tecnológico", "Cumplimiento", "Reputacional"

    [Required]
    public string Nombre { get; set; } = ""; // Ej: "Falla de Infraestructura TI", "Fraude Interno"

    public string Descripcion { get; set; } = "";
    public bool Activo { get; set; } = true;
    public int Orden { get; set; } = 0;
}
