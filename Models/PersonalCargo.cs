namespace RiesgosElor.Models;

public class PersonalCargo
{
    public string Nombre { get; set; } = "";
    public string ApellidoPaterno { get; set; } = "";
    public string ApellidoMaterno { get; set; } = "";
    public string CodigoCargo { get; set; } = "";
    public string Cargo { get; set; } = "";

    // Nombre completo formateado: "APELLIDO PATERNO APELLIDO MATERNO, NOMBRE"
    public string NombreCompleto =>
        $"{ApellidoPaterno} {ApellidoMaterno}, {Nombre}".Trim();

    // Para mostrar en buscador: "APELLIDO PATERNO APELLIDO MATERNO NOMBRE — CARGO"
    public string Etiqueta =>
        $"{ApellidoPaterno} {ApellidoMaterno} {Nombre} — {Cargo}".Trim();
}

public class PersonalCargoApiResponse
{
    public bool Status { get; set; }
    public List<PersonalCargo> Value { get; set; } = new();
    public string Msg { get; set; } = "";
}