using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class MaestroParametro
{
    public int Id { get; set; }

    [Required]
    public string Grupo { get; set; } = "TipoControl"; // "TipoControl", "FrecuenciaControl", "NaturalezaControl", "EfectividadControl", "EstrategiaTratamiento"

    [Required]
    public string Valor { get; set; } = ""; // Ej: "Preventivo", "Detectivo", "Mitigar"

    public string Descripcion { get; set; } = "";
    public bool Activo { get; set; } = true;
    public int Orden { get; set; } = 0;
}
