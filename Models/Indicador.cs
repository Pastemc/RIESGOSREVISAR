namespace RiesgosElor.Models;

public class Indicador
{
    public int Id { get; set; }
    public int RiesgoId { get; set; }

    public string CodigoKRI { get; set; } = "";
    public string DefinicionKRI { get; set; } = "";
    public string Frecuencia { get; set; } = "";
    public string MetaKRI { get; set; } = "";
    public string KRIActual { get; set; } = "";
    public string ResponsableKRI { get; set; } = "";

    public Riesgo? Riesgo { get; set; }
}