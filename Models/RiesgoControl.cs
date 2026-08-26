namespace RiesgosElor.Models;

public class RiesgoControl
{
    public int Id { get; set; }
    public int RiesgoId { get; set; }

    public string CodigoControl { get; set; } = "";
    public string DescripcionControl { get; set; } = ""; // JSON array de descripciones
    public string AreaResponsable { get; set; } = "";
    public string ResponsablesControl { get; set; } = ""; // JSON array
    public string FrecuenciaControl { get; set; } = "";
    public string OportunidadControl { get; set; } = "";
    public string AutomatizacionControl { get; set; } = "";
    public string EvidenciaControl { get; set; } = "";

    public Riesgo? Riesgo { get; set; }
}