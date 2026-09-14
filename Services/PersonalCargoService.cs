using System.Net.Http.Json;
using System.Text.Json;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

// Cache estatico compartido entre todas las instancias Scoped
// (equivale al comportamiento anterior del Singleton pero sin sus problemas)
file static class PersonalCargoCache
{
    public static List<PersonalCargo>? Datos { get; set; }
    public static DateTime Expira { get; set; } = DateTime.MinValue;
    public static readonly object Lock = new();
}

public class PersonalCargoService
{
    private readonly HttpClient _http;
    private readonly ILogger<PersonalCargoService> _logger;

    private const string ApiUrl =
        "https://www1.elor.com.pe/ApiPersonalCargo/api/Persona/ListarPersonaCargo";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int CacheMinutos = 10;

    public PersonalCargoService(HttpClient http, ILogger<PersonalCargoService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<PersonalCargo>> GetTodosAsync()
    {
        lock (PersonalCargoCache.Lock)
        {
            if (PersonalCargoCache.Datos != null &&
                DateTime.Now < PersonalCargoCache.Expira)
            {
                _logger.LogInformation("PersonalCargo: {Count} registros desde cache.",
                    PersonalCargoCache.Datos.Count);
                return PersonalCargoCache.Datos;
            }
        }

        _logger.LogInformation("PersonalCargo: llamando API → {Url}", ApiUrl);

        try
        {
            var resp = await _http.GetFromJsonAsync<PersonalCargoApiResponse>(ApiUrl, JsonOpts);

            _logger.LogInformation("PersonalCargo: status={Status}, registros={Count}",
                resp?.Status, resp?.Value?.Count ?? 0);

            if (resp?.Value?.Count > 0)
            {
                var p = resp.Value[0];
                _logger.LogInformation(
                    "PersonalCargo[0]: Nombre={N}, Cargo={C}, Sede={S}, Area={A}",
                    p.Nombre, p.Cargo, p.Sede, p.Area);
            }

            if (resp?.Status == true && resp.Value.Any())
            {
                var lista = resp.Value
                    .OrderBy(p => p.ApellidoPaterno)
                    .ThenBy(p => p.ApellidoMaterno)
                    .ThenBy(p => p.Nombre)
                    .ToList();

                lock (PersonalCargoCache.Lock)
                {
                    PersonalCargoCache.Datos = lista;
                    PersonalCargoCache.Expira = DateTime.Now.AddMinutes(CacheMinutos);
                }

                _logger.LogInformation("PersonalCargo: cache actualizado con {Count} registros.", lista.Count);
                return lista;
            }

            _logger.LogWarning("PersonalCargo: API respondio sin datos. Status={S}", resp?.Status);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "PersonalCargo: ERROR HTTP {Code} — {Msg}", ex.StatusCode, ex.Message);
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("PersonalCargo: TIMEOUT — la API tardo mas de 20s.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "PersonalCargo: ERROR JSON — {Msg}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PersonalCargo: ERROR {Type} — {Msg}",
                ex.GetType().Name, ex.Message);
        }

        return PersonalCargoCache.Datos ?? new List<PersonalCargo>();
    }

    public async Task<List<PersonalCargo>> BuscarAsync(string texto, int maxResultados = 10)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Length < 2)
            return new List<PersonalCargo>();

        var todos = await GetTodosAsync();
        var q = texto.Trim();

        return todos
            .Where(p =>
                p.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.ApellidoPaterno.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.ApellidoMaterno.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Cargo.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Sede.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Area.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.NombreCompleto.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(maxResultados)
            .ToList();
    }

    public void LimpiarCache()
    {
        lock (PersonalCargoCache.Lock)
        {
            PersonalCargoCache.Datos = null;
            PersonalCargoCache.Expira = DateTime.MinValue;
        }
        _logger.LogInformation("PersonalCargo: cache limpiado manualmente.");
    }
}
