using System.Net.Http.Json;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

public class PersonalCargoService
{
    private readonly HttpClient _http;
    private readonly ILogger<PersonalCargoService> _logger;

    private const string ApiUrl = "https://www1.elor.com.pe/ApiPersonalCargo/api/Persona/ListarPersonaCargo";

    // Caché en memoria: evita llamar la API en cada tecla
    private List<PersonalCargo>? _cache;
    private DateTime _cacheExpira = DateTime.MinValue;
    private const int CacheMinutos = 5;

    public PersonalCargoService(HttpClient http, ILogger<PersonalCargoService> logger)
    {
        _http = http;
        _logger = logger;
    }

    // Obtiene todos los registros (con caché)
    public async Task<List<PersonalCargo>> GetTodosAsync()
    {
        if (_cache != null && DateTime.Now < _cacheExpira)
            return _cache;

        try
        {
            var resp = await _http.GetFromJsonAsync<PersonalCargoApiResponse>(ApiUrl);
            if (resp?.Status == true && resp.Value.Any())
            {
                _cache = resp.Value
                    .OrderBy(p => p.ApellidoPaterno)
                    .ThenBy(p => p.ApellidoMaterno)
                    .ThenBy(p => p.Nombre)
                    .ToList();
                _cacheExpira = DateTime.Now.AddMinutes(CacheMinutos);
                return _cache;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar API de personal ELORSA");
        }

        return _cache ?? new List<PersonalCargo>();
    }

    // Busca por texto libre (nombre, apellido o cargo)
    public async Task<List<PersonalCargo>> BuscarAsync(string texto, int maxResultados = 10)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Length < 2)
            return new List<PersonalCargo>();

        var todos = await GetTodosAsync();
        var q = texto.Trim().ToUpperInvariant();

        return todos
            .Where(p =>
                p.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.ApellidoPaterno.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.ApellidoMaterno.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Cargo.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.NombreCompleto.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(maxResultados)
            .ToList();
    }

    // Fuerza recarga del caché (útil para botón "Actualizar")
    public void LimpiarCache()
    {
        _cache = null;
        _cacheExpira = DateTime.MinValue;
    }
}