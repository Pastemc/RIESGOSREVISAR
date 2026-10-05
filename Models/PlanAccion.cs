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

    // ── Columnas nuevas AI-AN (Excel FONAFE/ELORSA) ──────────────────────
    public DateTime? FechaPrevista { get; set; }        // AI(35)
    public string PlanEficaz { get; set; } = "";  // AJ(36) Sí/No/Parcialmente
    public DateTime? FechaVerificacion { get; set; }        // AK(37)
    public string VerificadoPor { get; set; } = "";  // AL(38)
    public string EvidenciaPlan { get; set; } = "";  // AM(39)
    public string ObservacionesPlan { get; set; } = "";  // AN(40)

    public Riesgo? Riesgo { get; set; }
}