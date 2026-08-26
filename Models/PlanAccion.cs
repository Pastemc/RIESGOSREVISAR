using System;

namespace RiesgosElor.Models;

public class PlanAccion
{
    public int Id { get; set; }
    public int RiesgoId { get; set; }

    public string EstrategiaRespuesta { get; set; } = "";
    public string CodigoPlan { get; set; } = "";
    public string DescripcionPlan { get; set; } = "";
    public string AreaResponsable { get; set; } = "";
    public string ResponsablePlan { get; set; } = "";
    public DateTime? InicioPlan { get; set; }
    public DateTime? FinPlan { get; set; }
    public string EstadoPlan { get; set; } = "";

    public Riesgo? Riesgo { get; set; }
}