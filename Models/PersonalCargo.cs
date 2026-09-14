using System.Text.Json.Serialization;

namespace RiesgosElor.Models;

public class PersonalCargo
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = "";

    [JsonPropertyName("apellidoPaterno")]
    public string ApellidoPaterno { get; set; } = "";

    [JsonPropertyName("apellidoMaterno")]
    public string ApellidoMaterno { get; set; } = "";

    [JsonPropertyName("codigoCargo")]
    public string CodigoCargo { get; set; } = "";

    [JsonPropertyName("cargo")]
    public string Cargo { get; set; } = "";

    [JsonPropertyName("sede")]        // ← API devuelve "sede" en minúsculas
    public string Sede { get; set; } = "";

    [JsonPropertyName("area")]        // ← API devuelve "area" en minúsculas
    public string Area { get; set; } = "";

    // Nombre completo: "APELLIDO PATERNO APELLIDO MATERNO, NOMBRE"
    public string NombreCompleto =>
        $"{ApellidoPaterno} {ApellidoMaterno}, {Nombre}".Trim();

    // Para buscador: "APELLIDO PATERNO APELLIDO MATERNO NOMBRE — CARGO"
    public string Etiqueta =>
        $"{ApellidoPaterno} {ApellidoMaterno} {Nombre} — {Cargo}".Trim();
}

public class PersonalCargoApiResponse
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("value")]
    public List<PersonalCargo> Value { get; set; } = new();

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = "";
}