using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class EvaluacionControl
{
    public int Id { get; set; }

    public int RiesgoControlId { get; set; }
    public RiesgoControl? RiesgoControl { get; set; }

    public DateTime FechaEvaluacion { get; set; } = DateTime.Today;

    [MaxLength(160)]
    public string Evaluador { get; set; } = "";

    [MaxLength(40)]
    public string Diseno { get; set; } = "Pendiente";

    [MaxLength(40)]
    public string Ejecucion { get; set; } = "Pendiente";

    [MaxLength(40)]
    public string Resultado { get; set; } = "Pendiente";

    public string Evidencia { get; set; } = "";
    public string Observaciones { get; set; } = "";
}
